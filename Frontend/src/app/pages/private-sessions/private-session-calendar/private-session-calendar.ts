import { Component, OnInit, computed, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { LucideTrash2 } from '@lucide/angular';
import { PrivateSessionListItem } from '../../../models/private-session.model';
import { PrivateSessionService } from '../../../services/private-session.service';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { PageHeader } from '../../../shared/page-header/page-header';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { CoachNamePipe, formatCoachName } from '../../../shared/coach-name/coach-name.pipe';
import { CalendarNote } from '../../../models/calendar-note.model';
import { CalendarNoteService } from '../../../services/calendar-note.service';
import { CalendarNoteDialog } from '../../../shared/calendar-note-dialog/calendar-note-dialog';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { TrainingSessionService } from '../../../services/training-session.service';
import { CompetitionMatch } from '../../../models/competition-match.model';
import { CompetitionMatchService } from '../../../services/competition-match.service';

type ViewState = 'loading' | 'error' | 'ready';

interface CalendarDay {
  date: Date;
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  sessions: PrivateSessionListItem[];
  competitionMatches: CompetitionMatch[];
  note: CalendarNote | null;
}

interface CoachFilterOption {
  coachCode: string;
  coachName: string;
}

@Component({
  selector: 'app-private-session-calendar',
  imports: [RouterLink, PageHeader, LoadingIndicator, EmptyState, ErrorState, StatusBadge, DisplayDatePipe, CoachNamePipe, CalendarNoteDialog, ConfirmationDialog, LucideTrash2],
  templateUrl: './private-session-calendar.html',
  styleUrl: './private-session-calendar.css',
})
export class PrivateSessionCalendar implements OnInit {
  readonly dayHeaders = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];
  state = signal<ViewState>('loading');
  sessions = signal<PrivateSessionListItem[]>([]);
  competitionMatches = signal<CompetitionMatch[]>([]);
  notes = signal<CalendarNote[]>([]);
  noteDialogOpen = signal(false);
  noteDialogDateSelectable = signal(false);
  deleteTarget = signal<PrivateSessionListItem | null>(null);
  deleting = signal(false);
  actionError = signal<string | null>(null);
  visibleMonth = signal(startOfMonth(new Date()));
  selectedDate = signal(toIsoDate(new Date()));
  selectedCoachCode = signal('');
  selectedParticipantName = signal('');
  exportingImage = signal(false);
  exportMessage = signal<string | null>(null);

  coachOptions = computed<CoachFilterOption[]>(() => {
    const coaches = new Map<string, string>();
    for (const session of this.sessions()) {
      if (!coaches.has(session.coachCode)) {
        coaches.set(session.coachCode, session.coachNickname?.trim() || session.coachFullName);
      }
    }

    return [...coaches.entries()]
      .map(([coachCode, coachName]) => ({ coachCode, coachName }))
      .sort((a, b) => a.coachCode.localeCompare(b.coachCode));
  });

  participantOptions = computed(() =>
    [...new Set(this.sessions().flatMap((session) => session.participantNames.map((name) => name.trim())).filter(Boolean))]
      .sort((a, b) => a.localeCompare(b, 'th')),
  );

  filteredSessions = computed(() => {
    const coachCode = this.selectedCoachCode();
    const participantName = this.selectedParticipantName();

    return this.sessions().filter(
      (session) =>
        (!coachCode || session.coachCode === coachCode) &&
        (!participantName || session.participantNames.some((name) => name.trim() === participantName)),
    );
  });

  monthLabel = computed(() =>
    new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(this.visibleMonth()),
  );

  calendarDays = computed<CalendarDay[]>(() => {
    const month = this.visibleMonth();
    const gridStart = new Date(month.getFullYear(), month.getMonth(), 1 - month.getDay());

    return Array.from({ length: 42 }, (_, index) => {
      const date = addDays(gridStart, index);
      const isoDate = toIsoDate(date);
      return {
        date,
        isoDate,
        dayNumber: date.getDate(),
        isCurrentMonth: date.getMonth() === month.getMonth(),
        isToday: isoDate === toIsoDate(new Date()),
        sessions: this.filteredSessions().filter((session) => session.sessionDate === isoDate),
        competitionMatches: this.competitionMatches().filter((match) => match.startDate <= isoDate && isoDate <= match.endDate),
        note: this.notes().find((note) => note.noteDate === isoDate) ?? null,
      };
    });
  });

  selectedDay = computed(() => this.calendarDays().find((day) => day.isoDate === this.selectedDate()) ?? null);
  currentMonthSessionCount = computed(() =>
    this.calendarDays()
      .filter((day) => day.isCurrentMonth)
      .reduce((total, day) => total + day.sessions.length, 0),
  );

  constructor(
    private readonly privateSessionService: PrivateSessionService,
    private readonly calendarNoteService: CalendarNoteService,
    private readonly trainingSessionService: TrainingSessionService,
    private readonly competitionMatchService: CompetitionMatchService,
  ) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    const days = this.calendarDays();
    try {
      const [sessions, competitionMatches, notes] = await Promise.all([
        this.privateSessionService.listCalendar(days[0].isoDate, days[days.length - 1].isoDate),
        this.competitionMatchService.listAll(),
        this.calendarNoteService.list(days[0].isoDate, days[days.length - 1].isoDate),
      ]);
      this.sessions.set(sessions);
      this.competitionMatches.set(competitionMatches);
      this.notes.set(notes);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  openNote(noteDate: string, allowDateSelection = false): void { this.selectedDate.set(noteDate); this.noteDialogDateSelectable.set(allowDateSelection); this.noteDialogOpen.set(true); }
  selectedNote(): CalendarNote | null { return this.notes().find((note) => note.noteDate === this.selectedDate()) ?? null; }
  async noteChanged(): Promise<void> { this.noteDialogOpen.set(false); await this.load(); }

  requestDelete(session: PrivateSessionListItem): void {
    this.actionError.set(null);
    this.deleteTarget.set(session);
  }

  cancelDelete(): void { this.deleteTarget.set(null); }

  async confirmDelete(): Promise<void> {
    const target = this.deleteTarget();
    if (!target) return;

    this.deleting.set(true);
    this.actionError.set(null);
    try {
      await this.trainingSessionService.delete(target.trainingSessionId);
      this.sessions.update((sessions) => sessions.filter((session) => session.trainingSessionId !== target.trainingSessionId));
      this.deleteTarget.set(null);
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? error.error as ApiErrorBody | undefined : undefined;
      this.actionError.set(body?.message ?? 'ไม่สามารถลบเซสชันฝึกซ้อมได้');
      this.deleteTarget.set(null);
    } finally {
      this.deleting.set(false);
    }
  }

  moveMonth(offset: number): void {
    const current = this.visibleMonth();
    const next = new Date(current.getFullYear(), current.getMonth() + offset, 1);
    this.visibleMonth.set(next);
    this.selectedDate.set(toIsoDate(next));
    void this.load();
  }

  goToCurrentMonth(): void {
    const today = new Date();
    this.visibleMonth.set(startOfMonth(today));
    this.selectedDate.set(toIsoDate(today));
    void this.load();
  }

  onCoachFilterChange(coachCode: string): void {
    this.selectedCoachCode.set(coachCode);
    if (coachCode) {
      this.selectedParticipantName.set('');
    }
  }

  onParticipantFilterChange(participantName: string): void {
    this.selectedParticipantName.set(participantName);
    if (participantName) {
      this.selectedCoachCode.set('');
    }
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
      link.download = `private-training-calendar-${toIsoMonth(this.visibleMonth())}.png`;
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
    context.fillText(`ปฏิทินฝึกซ้อมส่วนตัว · ${this.monthLabel()}`, margin, 70);
    context.fillStyle = '#475569';
    context.font = '34px Prompt, "Noto Sans Thai", sans-serif';
    context.fillText(this.exportFilterLabel(), margin, 120);
    context.font = '30px Prompt, "Noto Sans Thai", sans-serif';
    context.fillText(`${this.currentMonthSessionCount()} เซสชัน`, margin, 158);

    this.dayHeaders.forEach((header, column) => {
      const x = margin + column * columnWidth;
      context.fillStyle = '#eff6ff';
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
        context.fillText('แข่งขัน', x + columnWidth - 72, y + 32);
      }
      if (day.note) {
        context.fillStyle = '#d97706';
        context.font = '700 30px Prompt, "Noto Sans Thai", sans-serif';
        context.fillText('!', x + columnWidth - 22, y + 32);
      }

      day.sessions.slice(0, 3).forEach((session, sessionIndex) => {
        const eventY = y + 48 + sessionIndex * 72;
        context.fillStyle = session.coachColorHex;
        context.fillRect(x + 12, eventY, 8, 68);
        context.fillStyle = '#f8fafc';
        context.fillRect(x + 20, eventY, columnWidth - 32, 68);
        context.fillStyle = '#1e293b';
        context.font = '600 34px Prompt, "Noto Sans Thai", sans-serif';
        const timeLabel = `${session.startTime.slice(0, 5)}–${session.endTime.slice(0, 5)}`;
        context.fillText(timeLabel, x + 32, eventY + 32);
        context.font = '500 30px Prompt, "Noto Sans Thai", sans-serif';
        const coachName = session.coachNickname?.trim() || session.coachFullName;
        const participantLabel = `${formatCoachName(coachName)} · ${session.participantNames.join(', ')}`;
        context.fillText(truncateCanvasText(context, participantLabel, columnWidth - 56), x + 32, eventY + 62);
      });

      if (day.sessions.length > 3) {
        context.fillStyle = '#2563eb';
        context.font = '600 25px Prompt, "Noto Sans Thai", sans-serif';
        context.fillText(`+ อีก ${day.sessions.length - 3} รายการ`, x + 14, y + 292);
      }
    });

    context.textAlign = 'right';
    context.fillStyle = '#64748b';
    context.font = '28px Prompt, "Noto Sans Thai", sans-serif';
    context.fillText(`สร้างเมื่อ ${new Intl.DateTimeFormat('th-TH', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date())}`, canvas.width - margin, canvas.height - 28);
    return canvas;
  }

  private exportFilterLabel(): string {
    if (this.selectedCoachCode()) {
      const coach = this.coachOptions().find((option) => option.coachCode === this.selectedCoachCode());
      return `ตัวกรอง: ${formatCoachName(coach?.coachName ?? this.selectedCoachCode())}`;
    }
    if (this.selectedParticipantName()) {
      return `ตัวกรองผู้เข้าร่วม: ${this.selectedParticipantName()}`;
    }
    return 'แสดงโค้ชและผู้เข้าร่วมทั้งหมด';
  }

  selectDay(day: CalendarDay): void {
    this.selectedDate.set(day.isoDate);
    if (!day.isCurrentMonth) {
      this.visibleMonth.set(startOfMonth(day.date));
      void this.load();
    }
  }

  calendarDayBackground(day: CalendarDay, isSelected = false): string | null {
    if (day.sessions.length > 0) {
      return '#dbeafe';
    }
    if (isSelected) {
      return '#e5e7eb';
    }
    return null;
  }
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

function toIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
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
