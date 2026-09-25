import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RoutineTrainingDate } from '../../../models/parent-routine-plan.model';
import { ParentRoutinePlanService } from '../../../services/parent-routine-plan.service';
import { PageHeader } from '../../../shared/page-header/page-header';
import { CompetitionMatch } from '../../../models/competition-match.model';
import { CalendarNote } from '../../../models/calendar-note.model';
import { CompetitionMatchService } from '../../../services/competition-match.service';
import { CalendarNoteService } from '../../../services/calendar-note.service';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';

interface CalendarDay { isoDate: string; dayNumber: number; isCurrentMonth: boolean; isToday: boolean; isTrainingDate: boolean; competitionMatches: CompetitionMatch[]; note: CalendarNote | null; }
const DAY_HEADERS = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];

@Component({ selector: 'app-routine-training-dates', imports: [RouterLink, PageHeader, ConfirmationDialog], templateUrl: './routine-training-dates.html' })
export class RoutineTrainingDates implements OnInit {
  dates = signal<RoutineTrainingDate[]>([]);
  competitionMatches = signal<CompetitionMatch[]>([]);
  notes = signal<CalendarNote[]>([]);
  visibleMonth = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  loading = signal(true); processingDate = signal<string | null>(null); message = signal<string | null>(null); error = signal<string | null>(null);
  pendingRemoval = signal<CalendarDay | null>(null);
  readonly dayHeaders = DAY_HEADERS;
  readonly monthLabel = computed(() => new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(this.visibleMonth()));
  readonly calendarDays = computed<CalendarDay[]>(() => {
    const month = this.visibleMonth();
    const start = new Date(month.getFullYear(), month.getMonth(), 1 - month.getDay());
    const configured = new Set(this.dates().map(item => item.trainingDate));
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index);
      const isoDate = toIso(date);
      return {
        isoDate,
        dayNumber: date.getDate(),
        isCurrentMonth: date.getMonth() === month.getMonth(),
        isToday: isoDate === toIso(new Date()),
        isTrainingDate: configured.has(isoDate),
        competitionMatches: this.competitionMatches().filter(match => match.startDate <= isoDate && isoDate <= match.endDate),
        note: this.notes().find(note => note.noteDate === isoDate) ?? null,
      };
    });
  });
  constructor(
    private readonly service: ParentRoutinePlanService,
    private readonly competitionMatchService: CompetitionMatchService,
    private readonly calendarNoteService: CalendarNoteService,
  ) {}
  ngOnInit(): void { void this.load(); }
  async load(): Promise<void> {
    this.loading.set(true); this.error.set(null);
    const [start, end] = monthRange(this.visibleMonth());
    try {
      const [dates, competitionMatches, notes] = await Promise.all([
        this.service.listTrainingDates(start, end),
        this.competitionMatchService.listAll(),
        this.calendarNoteService.list(start, end),
      ]);
      this.dates.set(dates); this.competitionMatches.set(competitionMatches); this.notes.set(notes);
    } catch { this.error.set('ไม่สามารถโหลดปฏิทินวันฝึกซ้อมประจำได้'); }
    finally { this.loading.set(false); }
  }
  moveMonth(offset: number): void { const month = this.visibleMonth(); this.visibleMonth.set(new Date(month.getFullYear(), month.getMonth() + offset, 1)); void this.load(); }
  async toggle(day: CalendarDay): Promise<void> {
    if (!day.isCurrentMonth || this.processingDate()) return;
    if (day.isTrainingDate) { this.pendingRemoval.set(day); return; }
    this.processingDate.set(day.isoDate); this.message.set(null); this.error.set(null);
    try {
      const created = await this.service.addTrainingDate(day.isoDate); this.dates.update(items => [...items, created]); this.message.set('เพิ่มวันฝึกซ้อมประจำแล้ว');
    } catch { this.error.set('บันทึกวันฝึกซ้อมประจำไม่สำเร็จ'); }
    finally { this.processingDate.set(null); }
  }
  cancelRemoval(): void { this.pendingRemoval.set(null); }
  async confirmRemoval(): Promise<void> {
    const day = this.pendingRemoval();
    if (!day || this.processingDate()) return;
    this.processingDate.set(day.isoDate); this.message.set(null); this.error.set(null);
    try {
      await this.service.removeTrainingDate(day.isoDate);
      this.dates.update(items => items.filter(item => item.trainingDate !== day.isoDate));
      this.message.set('ยกเลิกวันฝึกซ้อมประจำแล้ว'); this.pendingRemoval.set(null);
    } catch { this.error.set('ยกเลิกวันฝึกซ้อมประจำไม่สำเร็จ'); }
    finally { this.processingDate.set(null); }
  }
}
function monthRange(month: Date): [string, string] { return [toIso(month), toIso(new Date(month.getFullYear(), month.getMonth() + 1, 0))]; }
function toIso(date: Date): string { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`; }
