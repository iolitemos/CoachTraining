import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ParentRoutinePlanDate } from '../../models/parent-routine-plan.model';
import { PublicCompetitionMatch } from '../../models/public-routine-calendar.model';
import { ParentRoutinePlanService } from '../../services/parent-routine-plan.service';
import { ConfirmationDialog } from '../../shared/confirmation-dialog/confirmation-dialog';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';

type ViewState = 'loading' | 'ready' | 'error' | 'invalid';
interface ParentCalendarDay {
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  planDate: ParentRoutinePlanDate | null;
  competitionMatches: PublicCompetitionMatch[];
}
const DAY_HEADERS = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];

@Component({
  selector: 'app-parent-routine-plan',
  imports: [ConfirmationDialog, DisplayDatePipe],
  templateUrl: './parent-routine-plan.html',
  styleUrl: './parent-routine-plan.css',
})
export class ParentRoutinePlan implements OnInit, OnDestroy {
  state = signal<ViewState>('loading');
  athleteNickname = signal<string | null>(null);
  athleteFullName = signal('');
  dates = signal<ParentRoutinePlanDate[]>([]);
  competitionMatches = signal<PublicCompetitionMatch[]>([]);
  visibleMonth = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  saving = signal(false);
  pendingCancellationDate = signal<string | null>(null);
  message = signal<string | null>(null);
  messageType = signal<'success' | 'error'>('success');
  readonly dayHeaders = DAY_HEADERS;
  readonly monthLabel = computed(() => new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(this.visibleMonth()));
  readonly selectedDayCount = computed(() => this.dates().filter(item => item.isSelected).length);
  readonly calendarDays = computed<ParentCalendarDay[]>(() => {
    const month = this.visibleMonth();
    const gridStart = new Date(month.getFullYear(), month.getMonth(), 1 - month.getDay());
    const planDates = new Map(this.dates().map(item => [item.trainingDate, item]));
    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(gridStart.getFullYear(), gridStart.getMonth(), gridStart.getDate() + index);
      const isoDate = toIso(date);
      return {
        isoDate,
        dayNumber: date.getDate(),
        isCurrentMonth: date.getMonth() === month.getMonth(),
        isToday: isoDate === toIso(new Date()),
        planDate: planDates.get(isoDate) ?? null,
        competitionMatches: this.competitionMatches().filter(match => match.startDate <= isoDate && isoDate <= match.endDate),
      };
    });
  });
  private readonly token: string;
  private toastTimeoutId: ReturnType<typeof setTimeout> | null = null;

  constructor(route: ActivatedRoute, private readonly service: ParentRoutinePlanService) { this.token = route.snapshot.paramMap.get('token') ?? ''; }
  ngOnInit(): void { void this.load(); }
  ngOnDestroy(): void { if (this.toastTimeoutId !== null) clearTimeout(this.toastTimeoutId); }
  async load(): Promise<void> {
    if (!this.token) { this.state.set('invalid'); return; }
    this.state.set('loading'); this.dismissToast();
    const [startDate, endDate] = monthRange(this.visibleMonth());
    try {
      const result = await this.service.getCalendar(this.token, startDate, endDate);
      this.athleteNickname.set(result.athleteNickname); this.athleteFullName.set(result.athleteFullName); this.dates.set(result.dates); this.competitionMatches.set(result.competitionMatches); this.state.set('ready');
    } catch (error: unknown) {
      const status = typeof error === 'object' && error !== null && 'status' in error ? (error as { status: number }).status : 0;
      this.state.set(status === 404 ? 'invalid' : 'error');
    }
  }
  moveMonth(offset: number): void { const month = this.visibleMonth(); this.visibleMonth.set(new Date(month.getFullYear(), month.getMonth() + offset, 1)); void this.load(); }
  async selectDate(date: string): Promise<void> {
    if (this.saving()) return;
    const item = this.dates().find(candidate => candidate.trainingDate === date);
    if (!item) return;
    if (item.isSelected) {
      this.pendingCancellationDate.set(date);
      return;
    }
    await this.persistSelection(date, true);
  }
  cancelCancellation(): void { this.pendingCancellationDate.set(null); }
  async confirmCancellation(): Promise<void> {
    const date = this.pendingCancellationDate();
    if (!date) return;
    if (await this.persistSelection(date, false)) this.pendingCancellationDate.set(null);
  }
  private async persistSelection(date: string, selected: boolean): Promise<boolean> {
    this.saving.set(true); this.dismissToast();
    const [startDate, endDate] = monthRange(this.visibleMonth());
    const selectedDates = this.dates()
      .filter(item => item.trainingDate === date ? selected : item.isSelected)
      .map(item => item.trainingDate);
    try {
      const result = await this.service.save(this.token, startDate, endDate, selectedDates);
      this.dates.set(result.dates);
      this.competitionMatches.set(result.competitionMatches);
      this.showToast(selected ? 'บันทึกแผนเข้าซ้อมเรียบร้อยแล้ว' : 'ยกเลิกแผนเข้าซ้อมเรียบร้อยแล้ว', 'success');
      return true;
    } catch (error: unknown) {
      const status = typeof error === 'object' && error !== null && 'status' in error ? (error as { status: number }).status : 0;
      if (status === 404) this.state.set('invalid'); else this.showToast('บันทึกไม่สำเร็จ กรุณาลองใหม่', 'error');
      return false;
    } finally { this.saving.set(false); }
  }
  dismissToast(): void {
    this.message.set(null);
    if (this.toastTimeoutId !== null) {
      clearTimeout(this.toastTimeoutId);
      this.toastTimeoutId = null;
    }
  }
  private showToast(message: string, type: 'success' | 'error'): void {
    if (this.toastTimeoutId !== null) clearTimeout(this.toastTimeoutId);
    this.messageType.set(type);
    this.message.set(message);
    this.toastTimeoutId = setTimeout(() => {
      this.message.set(null);
      this.toastTimeoutId = null;
    }, 3500);
  }
}

function monthRange(month: Date): [string, string] {
  const last = new Date(month.getFullYear(), month.getMonth() + 1, 0);
  return [toIso(month), toIso(last)];
}
function toIso(date: Date): string { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`; }
