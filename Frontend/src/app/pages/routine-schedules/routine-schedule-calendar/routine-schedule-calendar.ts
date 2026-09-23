import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RoutineScheduleListItem } from '../../../models/routine-schedule.model';
import { RoutineScheduleService } from '../../../services/routine-schedule.service';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { PageHeader } from '../../../shared/page-header/page-header';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { CompetitionMatch } from '../../../models/competition-match.model';
import { CompetitionMatchService } from '../../../services/competition-match.service';
import { RoutineCalendarShareStatus } from '../../../models/public-routine-calendar.model';
import { PublicRoutineCalendarService } from '../../../services/public-routine-calendar.service';
import QRCode from 'qrcode';
import { CoachNamePipe, formatCoachName } from '../../../shared/coach-name/coach-name.pipe';
import { CalendarNote } from '../../../models/calendar-note.model';
import { CalendarNoteService } from '../../../services/calendar-note.service';
import { CalendarNoteDialog } from '../../../shared/calendar-note-dialog/calendar-note-dialog';

type ViewState = 'loading' | 'error' | 'ready';

interface CalendarDay {
  date: Date;
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  schedules: RoutineScheduleListItem[];
  competitionMatches: CompetitionMatch[];
  note: CalendarNote | null;
}

interface CoachFilterOption {
  coachId: number;
  coachCode: string;
  coachNickname: string | null;
  coachFullName: string;
}

const DAY_HEADERS = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];

@Component({
  selector: 'app-routine-schedule-calendar',
  imports: [RouterLink, PageHeader, LoadingIndicator, EmptyState, ErrorState, ConfirmationDialog, DisplayDatePipe, CoachNamePipe, CalendarNoteDialog],
  templateUrl: './routine-schedule-calendar.html',
  styleUrl: './routine-schedule-calendar.css',
})
export class RoutineScheduleCalendar implements OnInit {
  state = signal<ViewState>('loading');
  schedules = signal<RoutineScheduleListItem[]>([]);
  competitionMatches = signal<CompetitionMatch[]>([]);
  notes = signal<CalendarNote[]>([]);
  noteDialogOpen = signal(false);
  noteDialogDateSelectable = signal(false);
  visibleMonth = signal(startOfMonth(new Date()));
  selectedDate = signal(toIsoDate(new Date()));
  selectedCoachId = signal<number | null>(null);
  exportingImage = signal(false);
  exportMessage = signal<string | null>(null);
  pendingDelete = signal<RoutineScheduleListItem | null>(null);
  deleteProcessing = signal(false);
  actionError = signal<string | null>(null);
  shareStatus = signal<RoutineCalendarShareStatus | null>(null);
  shareUrl = signal<string | null>(null);
  shareQrCode = signal<string | null>(null);
  shareProcessing = signal(false);
  shareMessage = signal<string | null>(null);

  readonly dayHeaders = DAY_HEADERS;

  coachOptions = computed<CoachFilterOption[]>(() => {
    const coaches = new Map<number, CoachFilterOption>();

    for (const schedule of this.schedules()) {
      if (!coaches.has(schedule.coachId)) {
        coaches.set(schedule.coachId, {
          coachId: schedule.coachId,
          coachCode: schedule.coachCode,
          coachNickname: schedule.coachNickname,
          coachFullName: schedule.coachFullName,
        });
      }
    }

    return [...coaches.values()].sort((a, b) => a.coachCode.localeCompare(b.coachCode));
  });

  filteredSchedules = computed(() => {
    const coachId = this.selectedCoachId();
    return coachId === null
      ? this.schedules()
      : this.schedules().filter((schedule) => schedule.coachId === coachId);
  });

  monthLabel = computed(() =>
    new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(
      this.visibleMonth(),
    ),
  );

  calendarDays = computed<CalendarDay[]>(() => {
    const month = this.visibleMonth();
    const gridStart = new Date(month.getFullYear(), month.getMonth(), 1 - month.getDay());

    return Array.from({ length: 42 }, (_, index) => {
      const date = addDays(gridStart, index);
      return {
        date,
        isoDate: toIsoDate(date),
        dayNumber: date.getDate(),
        isCurrentMonth: date.getMonth() === month.getMonth(),
        isToday: toIsoDate(date) === toIsoDate(new Date()),
        schedules: this.filteredSchedules()
          .filter((schedule) => occursOn(schedule, date))
          .sort(
            (a, b) =>
              a.coachCode.localeCompare(b.coachCode) ||
              a.startTime.localeCompare(b.startTime),
          ),
        competitionMatches: this.competitionMatches().filter((match) =>
          occursDuringCompetition(match, toIsoDate(date)),
        ),
        note: this.notes().find((note) => note.noteDate === toIsoDate(date)) ?? null,
      };
    });
  });

  selectedDay = computed(
    () => this.calendarDays().find((day) => day.isoDate === this.selectedDate()) ?? null,
  );

  currentMonthOccurrenceCount = computed(() =>
    this.calendarDays()
      .filter((day) => day.isCurrentMonth)
      .reduce((total, day) => total + day.schedules.length, 0),
  );

  constructor(
    private readonly routineScheduleService: RoutineScheduleService,
    private readonly competitionMatchService: CompetitionMatchService,
    private readonly publicCalendarService: PublicRoutineCalendarService,
    private readonly calendarNoteService: CalendarNoteService,
  ) {}

  ngOnInit(): void {
    void this.load();
    void this.loadShareStatus();
  }

  async loadShareStatus(): Promise<void> {
    try { await this.applyShareStatus(await this.publicCalendarService.getShareStatus()); }
    catch { this.shareMessage.set('ไม่สามารถโหลดสถานะลิงก์แชร์ได้'); }
  }

  async createShareLink(): Promise<void> {
    if (this.shareStatus()?.exists && !window.confirm('เปลี่ยนลิงก์ฉุกเฉินหรือไม่? ลิงก์และ QR Code เดิมจะใช้งานไม่ได้ทันที')) return;
    this.shareProcessing.set(true); this.shareMessage.set(null);
    try {
      const result = await this.publicCalendarService.rotateShareLink();
      const url = `${window.location.origin}/public/routine-calendar/${result.token}`;
      this.shareUrl.set(url);
      this.shareQrCode.set(await QRCode.toDataURL(url, {
        errorCorrectionLevel: 'M',
        margin: 2,
        width: 512,
        color: { dark: '#064e3b', light: '#ffffff' },
      }));
      this.shareStatus.set({ exists: true, isEnabled: true, token: result.token, tokenHint: result.tokenHint, createdDate: result.createdDate });
      this.shareMessage.set('สร้างลิงก์ถาวรแล้ว ลิงก์นี้จะคงเดิมจนกว่าจะสั่งเปลี่ยนลิงก์ฉุกเฉิน');
    } catch { this.shareMessage.set('ไม่สามารถสร้างลิงก์แชร์ได้'); }
    finally { this.shareProcessing.set(false); }
  }

  async copyShareLink(): Promise<void> {
    const url = this.shareUrl();
    if (!url) return;
    try { await navigator.clipboard.writeText(url); this.shareMessage.set('คัดลอกลิงก์แล้ว'); }
    catch { this.shareMessage.set('คัดลอกอัตโนมัติไม่ได้ กรุณาเลือกลิงก์แล้วคัดลอก'); }
  }

  downloadQrCode(): void {
    const dataUrl = this.shareQrCode();
    if (!dataUrl) return;
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = 'routine-training-calendar-qr.png';
    link.click();
  }

  async revokeShareLink(): Promise<void> {
    this.shareProcessing.set(true); this.shareMessage.set(null);
    try {
      await this.publicCalendarService.revokeShareLink();
      this.shareStatus.set({ exists: false, isEnabled: false, token: null, tokenHint: null, createdDate: null });
      this.shareUrl.set(null); this.shareQrCode.set(null); this.shareMessage.set('ยกเลิกลิงก์แชร์แล้ว');
    } catch { this.shareMessage.set('ไม่สามารถยกเลิกลิงก์แชร์ได้'); }
    finally { this.shareProcessing.set(false); }
  }

  async setShareAccess(isEnabled: boolean): Promise<void> {
    this.shareProcessing.set(true); this.shareMessage.set(null);
    try {
      await this.applyShareStatus(await this.publicCalendarService.setShareAccess(isEnabled));
      this.shareMessage.set(isEnabled ? 'เปิดการเข้าถึงปฏิทินแล้ว' : 'ปิดการเข้าถึงปฏิทินชั่วคราวแล้ว ลิงก์เดิมยังคงอยู่');
    } catch { this.shareMessage.set('ไม่สามารถเปลี่ยนสถานะการเข้าถึงได้'); }
    finally { this.shareProcessing.set(false); }
  }

  private async applyShareStatus(status: RoutineCalendarShareStatus): Promise<void> {
    this.shareStatus.set(status);
    if (!status.token) {
      this.shareUrl.set(null);
      this.shareQrCode.set(null);
      return;
    }

    const url = `${window.location.origin}/public/routine-calendar/${status.token}`;
    this.shareUrl.set(url);
    this.shareQrCode.set(await QRCode.toDataURL(url, {
      errorCorrectionLevel: 'M',
      margin: 2,
      width: 512,
      color: { dark: '#064e3b', light: '#ffffff' },
    }));
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const days = this.calendarDays();
      const [schedules, competitionMatches, notes] = await Promise.all([
        this.routineScheduleService.listAll(),
        this.competitionMatchService.listAll(),
        this.calendarNoteService.list(days[0].isoDate, days[days.length - 1].isoDate),
      ]);
      this.schedules.set(schedules);
      this.competitionMatches.set(competitionMatches);
      this.notes.set(notes);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  moveMonth(offset: number): void {
    const current = this.visibleMonth();
    const nextMonth = new Date(current.getFullYear(), current.getMonth() + offset, 1);
    this.visibleMonth.set(nextMonth);
    this.selectedDate.set(toIsoDate(nextMonth));
    void this.loadNotes();
  }

  goToCurrentMonth(): void {
    const today = new Date();
    this.visibleMonth.set(startOfMonth(today));
    this.selectedDate.set(toIsoDate(today));
    void this.loadNotes();
  }

  onCoachFilterChange(value: string): void {
    this.selectedCoachId.set(value === '' ? null : Number(value));
  }

  async exportCalendarImage(): Promise<void> {
    if (this.state() !== 'ready' || this.exportingImage()) {
      return;
    }

    this.exportingImage.set(true);
    this.exportMessage.set(null);
    try {
      await document.fonts?.ready;
      const canvas = this.createCalendarCanvas();
      const blob = await canvasToBlob(canvas);
      const link = document.createElement('a');
      const objectUrl = URL.createObjectURL(blob);
      link.href = objectUrl;
      link.download = `routine-training-calendar-${toIsoMonth(this.visibleMonth())}.png`;
      link.click();
      URL.revokeObjectURL(objectUrl);
      this.exportMessage.set('บันทึกรูปปฏิทินแล้ว');
    } catch {
      this.exportMessage.set('ไม่สามารถสร้างรูปปฏิทินได้ กรุณาลองใหม่อีกครั้ง');
    } finally {
      this.exportingImage.set(false);
    }
  }

  private createCalendarCanvas(): HTMLCanvasElement {
    const canvas = document.createElement('canvas');
    canvas.width = 2400;
    canvas.height = 2140;
    const context = canvas.getContext('2d');
    if (!context) {
      throw new Error('Canvas is not supported');
    }

    const margin = 60;
    const contentWidth = canvas.width - margin * 2;
    const columnWidth = contentWidth / 7;
    const calendarTop = 190;
    const weekdayHeight = 64;
    const rowHeight = 300;

    context.fillStyle = '#ffffff';
    context.fillRect(0, 0, canvas.width, canvas.height);
    context.fillStyle = '#0f172a';
    context.font = '600 64px Prompt, "Noto Sans Thai", sans-serif';
    context.fillText(`ปฏิทินฝึกซ้อมประจำ · ${this.monthLabel()}`, margin, 70);
    context.fillStyle = '#475569';
    context.font = '34px Prompt, "Noto Sans Thai", sans-serif';
    context.fillText(this.exportFilterLabel(), margin, 120);
    context.font = '30px Prompt, "Noto Sans Thai", sans-serif';
    context.fillText(`${this.currentMonthOccurrenceCount()} รอบฝึกซ้อม`, margin, 158);

    this.dayHeaders.forEach((header, column) => {
      const x = margin + column * columnWidth;
      context.fillStyle = '#ecfdf5';
      context.fillRect(x, calendarTop, columnWidth, weekdayHeight);
      context.strokeStyle = '#cbd5e1';
      context.strokeRect(x, calendarTop, columnWidth, weekdayHeight);
      context.fillStyle = '#334155';
      context.font = '600 38px Prompt, "Noto Sans Thai", sans-serif';
      context.textAlign = 'center';
      context.fillText(header, x + columnWidth / 2, calendarTop + 42);
    });

    this.calendarDays().forEach((day, index) => {
      const column = index % 7;
      const row = Math.floor(index / 7);
      const x = margin + column * columnWidth;
      const y = calendarTop + weekdayHeight + row * rowHeight;
      context.fillStyle = day.isCurrentMonth ? '#ffffff' : '#f8fafc';
      context.fillRect(x, y, columnWidth, rowHeight);
      context.strokeStyle = '#cbd5e1';
      context.strokeRect(x, y, columnWidth, rowHeight);
      context.textAlign = 'left';
      context.fillStyle = day.isCurrentMonth ? '#0f172a' : '#94a3b8';
      context.font = '600 38px Prompt, "Noto Sans Thai", sans-serif';
      context.fillText(String(day.dayNumber), x + 14, y + 34);

      if (day.competitionMatches.length > 0) {
        context.fillStyle = '#b45309';
        context.font = '600 25px Prompt, "Noto Sans Thai", sans-serif';
        context.fillText('แข่งขัน', x + columnWidth - 92, y + 32);
      }
      if (day.note) {
        context.fillStyle = '#d97706';
        context.font = '700 30px Prompt, "Noto Sans Thai", sans-serif';
        context.fillText('!', x + columnWidth - 24, y + 32);
      }

      day.schedules.slice(0, 3).forEach((schedule, scheduleIndex) => {
        const eventY = y + 48 + scheduleIndex * 72;
        context.fillStyle = schedule.coachColorHex;
        context.fillRect(x + 12, eventY, 8, 68);
        context.fillStyle = '#f8fafc';
        context.fillRect(x + 20, eventY, columnWidth - 32, 68);
        context.fillStyle = '#1e293b';
        context.font = '600 34px Prompt, "Noto Sans Thai", sans-serif';
        const coachName = schedule.coachNickname?.trim() || schedule.coachFullName;
        context.fillText(truncateCanvasText(context, formatCoachName(coachName), columnWidth - 56), x + 32, eventY + 46);
      });

      if (day.schedules.length > 3) {
        context.fillStyle = '#047857';
        context.font = '600 25px Prompt, "Noto Sans Thai", sans-serif';
        context.fillText(`+ อีก ${day.schedules.length - 3} รายการ`, x + 14, y + 292);
      }
    });

    context.textAlign = 'right';
    context.fillStyle = '#64748b';
    context.font = '28px Prompt, "Noto Sans Thai", sans-serif';
    context.fillText(`สร้างเมื่อ ${new Intl.DateTimeFormat('th-TH', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date())}`, canvas.width - margin, canvas.height - 28);
    return canvas;
  }

  private exportFilterLabel(): string {
    const coachId = this.selectedCoachId();
    if (coachId !== null) {
      const coach = this.coachOptions().find((option) => option.coachId === coachId);
      return `ตัวกรอง: ${formatCoachName(coach?.coachNickname?.trim() || coach?.coachFullName || 'ไม่ระบุชื่อโค้ช')}`;
    }
    return 'แสดงโค้ชทั้งหมด';
  }

  openNote(noteDate: string, allowDateSelection = false): void {
    this.selectedDate.set(noteDate);
    this.noteDialogDateSelectable.set(allowDateSelection);
    this.noteDialogOpen.set(true);
  }

  selectedNote(): CalendarNote | null {
    return this.notes().find((note) => note.noteDate === this.selectedDate()) ?? null;
  }

  async noteChanged(): Promise<void> {
    this.noteDialogOpen.set(false);
    await this.loadNotes();
  }

  private async loadNotes(): Promise<void> {
    const days = this.calendarDays();
    this.notes.set(await this.calendarNoteService.list(days[0].isoDate, days[days.length - 1].isoDate));
  }

  selectDay(day: CalendarDay): void {
    this.selectedDate.set(day.isoDate);
    if (!day.isCurrentMonth) {
      this.visibleMonth.set(startOfMonth(day.date));
    }
  }

  calendarDayBackground(day: CalendarDay, isSelected = false): string | null {
    if (day.schedules.length > 0) {
      return '#d1fae5';
    }
    if (isSelected) {
      return '#e5e7eb';
    }
    return null;
  }

  requestDelete(schedule: RoutineScheduleListItem): void {
    this.actionError.set(null);
    this.pendingDelete.set(schedule);
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  async confirmDelete(): Promise<void> {
    const schedule = this.pendingDelete();
    if (!schedule) {
      return;
    }

    this.deleteProcessing.set(true);
    this.actionError.set(null);
    try {
      await this.routineScheduleService.delete(schedule.routineScheduleId);
      this.pendingDelete.set(null);
      await this.load();
    } catch (error) {
      const body =
        error instanceof HttpErrorResponse ? (error.error as ApiErrorBody | undefined) : undefined;
      this.actionError.set(body?.message ?? 'ไม่สามารถลบตารางฝึกซ้อมได้ กรุณาลองใหม่อีกครั้ง');
      this.pendingDelete.set(null);
    } finally {
      this.deleteProcessing.set(false);
    }
  }
}

function occursOn(schedule: RoutineScheduleListItem, date: Date): boolean {
  return schedule.effectiveStartDate === toIsoDate(date);
}

function occursDuringCompetition(match: CompetitionMatch, isoDate: string): boolean {
  return match.startDate <= isoDate && isoDate <= match.endDate;
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function toIsoMonth(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
}

function truncateCanvasText(context: CanvasRenderingContext2D, value: string, maxWidth: number): string {
  if (context.measureText(value).width <= maxWidth) {
    return value;
  }

  let result = value;
  while (result.length > 0 && context.measureText(`${result}…`).width > maxWidth) {
    result = result.slice(0, -1);
  }
  return `${result}…`;
}

function canvasToBlob(canvas: HTMLCanvasElement): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => blob ? resolve(blob) : reject(new Error('Unable to create image')), 'image/png');
  });
}
