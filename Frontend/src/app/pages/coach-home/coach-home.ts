import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCalendarCheck2,
  LucideCalendarDays,
  LucideChevronRight,
  LucideCircleCheckBig,
  LucideClipboardCheck,
  LucideClock,
  LucideDumbbell,
  LucideHourglass,
  LucideRepeat,
} from '@lucide/angular';
import { PageHeader } from '../../shared/page-header/page-header';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { ErrorState } from '../../shared/error-state/error-state';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { StatusBadge } from '../../shared/status-badge/status-badge';
import {
  CoachCalendarColleague,
  CoachDashboardResponse,
  CoachDashboardSession,
} from '../../models/coach-dashboard.model';
import { CoachDashboardService } from '../../services/coach-dashboard.service';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';
import { CoachNamePipe } from '../../shared/coach-name/coach-name.pipe';
import { TrainingSessionListItem } from '../../models/training-session.model';
import { TrainingSessionService } from '../../services/training-session.service';
import { RoutineScheduleService } from '../../services/routine-schedule.service';
import { ConfirmationDialog } from '../../shared/confirmation-dialog/confirmation-dialog';
import { CompetitionMatch } from '../../models/competition-match.model';
import { CompetitionMatchService } from '../../services/competition-match.service';
import { CalendarNote } from '../../models/calendar-note.model';
import { CalendarNoteService } from '../../services/calendar-note.service';
import { CalendarNoteDialog } from '../../shared/calendar-note-dialog/calendar-note-dialog';
import { RoutineScheduleRequest } from '../../models/routine-schedule-request.model';
import { RoutineScheduleRequestService } from '../../services/routine-schedule-request.service';
import { FilterStateService } from '../../services/filter-state.service';

type ViewState = 'loading' | 'error' | 'ready';
type CoachHomeTab = 'overview' | 'calendar';

interface CalendarDay {
  date: Date;
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  sessions: TrainingSessionListItem[];
  requests: RoutineScheduleRequest[];
  competitionMatches: CompetitionMatch[];
  note: CalendarNote | null;
}

/**
 * Coach Home (requirement.md 9.1, FR-CDASH-001–003, todo.md 5.5). Prioritizes
 * today's/upcoming sessions, training type, status, and the required next
 * action; every session card opens the session directly (FR-CDASH-001).
 */
@Component({
  selector: 'app-coach-home',
  imports: [
    RouterLink,
    PageHeader,
    LoadingIndicator,
    ErrorState,
    EmptyState,
    StatusBadge,
    LucideArrowRight,
    LucideCalendarCheck2,
    LucideCalendarDays,
    LucideChevronRight,
    LucideCircleCheckBig,
    LucideClipboardCheck,
    LucideClock,
    LucideDumbbell,
    LucideHourglass,
    LucideRepeat,
    DisplayDatePipe,
    CoachNamePipe,
    ConfirmationDialog,
    CalendarNoteDialog,
  ],
  templateUrl: './coach-home.html',
  styleUrl: './coach-home.css',
})
export class CoachHome implements OnInit {
  private longPressTimer: ReturnType<typeof setTimeout> | null = null;
  private longPressTriggered = false;
  readonly dayHeaders = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];
  state = signal<ViewState>('loading');
  activeTab = signal<CoachHomeTab>('calendar');
  dashboard = signal<CoachDashboardResponse | null>(null);
  calendarState = signal<ViewState>('loading');
  calendarSessions = signal<TrainingSessionListItem[]>([]);
  calendarRequests = signal<RoutineScheduleRequest[]>([]);
  calendarColleagues = signal<CoachCalendarColleague[]>([]);
  calendarCompetitionMatches = signal<CompetitionMatch[]>([]);
  calendarNotes = signal<CalendarNote[]>([]);
  noteDialogOpen = signal(false);
  visibleMonth = signal(startOfMonth(new Date()));
  selectedDate = signal(toIsoDate(new Date()));
  overdueSessionsExpanded = signal(false);
  pendingDelete = signal<TrainingSessionListItem | null>(null);
  deleteProcessing = signal(false);
  calendarActionError = signal<string | null>(null);

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
        sessions: this.calendarSessions()
          .filter((session) => session.sessionDate === isoDate)
          .sort((a, b) => a.scheduledStartDateTime.localeCompare(b.scheduledStartDateTime)),
        requests: this.calendarRequests()
          .filter((request) => request.proposedDate === isoDate && (request.status === 'Pending' || request.status === 'RevisionRequested'))
          .sort((a, b) => a.proposedStartTime.localeCompare(b.proposedStartTime)),
        competitionMatches: this.calendarCompetitionMatches().filter(
          (match) => match.startDate <= isoDate && isoDate <= match.endDate,
        ),
        note: this.calendarNotes().find((note) => note.noteDate === isoDate) ?? null,
      };
    });
  });

  selectedDay = computed(
    () => this.calendarDays().find((day) => day.isoDate === this.selectedDate()) ?? null,
  );

  selectedDayColleagues = computed(() =>
    this.calendarColleagues()
      .filter((item) => item.sessionDate === this.selectedDate())
      .map((item) => item.coachNickname),
  );

  currentMonthSessionCount = computed(() =>
    this.calendarDays()
      .filter((day) => day.isCurrentMonth)
      .reduce((total, day) => total + day.sessions.length, 0),
  );

  currentMonthPendingRequestCount = computed(() =>
    this.calendarDays()
      .filter((day) => day.isCurrentMonth)
      .reduce((total, day) => total + day.requests.length, 0),
  );

  upcomingSessionsThisMonth = computed(() => this.dashboard()?.upcomingSessions ?? []);

  constructor(
    private readonly coachDashboardService: CoachDashboardService,
    private readonly trainingSessionService: TrainingSessionService,
    private readonly routineScheduleService: RoutineScheduleService,
    private readonly routineScheduleRequestService: RoutineScheduleRequestService,
    private readonly competitionMatchService: CompetitionMatchService,
    private readonly calendarNoteService: CalendarNoteService,
    private readonly filterState: FilterStateService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    this.restoreCalendarState();
    if (this.route.snapshot.queryParamMap.get('view') === 'calendar') {
      this.activeTab.set('calendar');
    }
    void this.load();
    void this.loadCalendar();
  }

  selectTab(tab: CoachHomeTab): void {
    this.activeTab.set(tab);
    if (tab === 'calendar') this.rememberCalendarState();
  }

  toggleOverdueSessions(): void {
    this.overdueSessionsExpanded.update((expanded) => !expanded);
  }

  async loadCalendar(): Promise<void> {
    this.calendarState.set('loading');
    const days = this.calendarDays();
    try {
      const [result, requests, colleagues, competitionMatches, notes] = await Promise.all([
        this.trainingSessionService.list({
          page: 1,
          pageSize: 100,
          coachId: null,
          trainingType: null,
          status: null,
          dateFrom: days[0].isoDate,
          dateTo: days[days.length - 1].isoDate,
        }),
        this.routineScheduleRequestService.listOwn(days[0].isoDate, days[days.length - 1].isoDate),
        this.coachDashboardService.getCalendarColleagues(
          days[0].isoDate,
          days[days.length - 1].isoDate,
        ),
        this.competitionMatchService.listAll(),
        this.calendarNoteService.list(days[0].isoDate, days[days.length - 1].isoDate),
      ]);
      this.calendarSessions.set(result.items);
      this.calendarRequests.set(requests);
      this.calendarColleagues.set(colleagues);
      this.calendarCompetitionMatches.set(competitionMatches);
      this.calendarNotes.set(notes);
      this.calendarState.set('ready');
    } catch {
      this.calendarState.set('error');
    }
  }

  openNote(noteDate: string): void { this.selectedDate.set(noteDate); this.noteDialogOpen.set(true); }
  selectedNote(): CalendarNote | null { return this.calendarNotes().find((note) => note.noteDate === this.selectedDate()) ?? null; }

  moveMonth(offset: number): void {
    const current = this.visibleMonth();
    const next = new Date(current.getFullYear(), current.getMonth() + offset, 1);
    this.visibleMonth.set(next);
    this.selectedDate.set(toIsoDate(next));
    this.rememberCalendarState();
    void this.loadCalendar();
  }

  goToCurrentMonth(): void {
    const today = new Date();
    this.visibleMonth.set(startOfMonth(today));
    this.selectedDate.set(toIsoDate(today));
    this.rememberCalendarState();
    void this.loadCalendar();
  }

  selectDay(day: CalendarDay): void {
    if (this.longPressTriggered) {
      this.longPressTriggered = false;
      return;
    }

    this.selectedDate.set(day.isoDate);
    if (!day.isCurrentMonth) {
      this.visibleMonth.set(startOfMonth(day.date));
      this.rememberCalendarState();
      void this.loadCalendar();
    } else {
      this.rememberCalendarState();
    }
  }

  startDateLongPress(day: CalendarDay): void {
    this.cancelDateLongPress();
    this.longPressTriggered = false;
    this.longPressTimer = setTimeout(() => {
      this.longPressTimer = null;
      this.longPressTriggered = true;
      this.selectedDate.set(day.isoDate);
      this.visibleMonth.set(startOfMonth(day.date));
      this.rememberCalendarState();
      void this.router.navigate(['/coach/routine-schedules/new'], {
        queryParams: { date: day.isoDate },
      });
    }, 600);
  }

  cancelDateLongPress(): void {
    if (this.longPressTimer !== null) {
      clearTimeout(this.longPressTimer);
      this.longPressTimer = null;
    }
  }

  private restoreCalendarState(): void {
    const today = new Date();
    const restored = this.filterState.restore(
      'coach-home-calendar',
      { month: toIsoDate(startOfMonth(today)), date: toIsoDate(today) },
      this.route.snapshot.queryParamMap,
    );
    const month = parseIsoDate(restored.month);
    const selected = parseIsoDate(restored.date);
    if (month) this.visibleMonth.set(startOfMonth(month));
    if (selected) this.selectedDate.set(toIsoDate(selected));
  }

  private rememberCalendarState(): void {
    this.filterState.save(
      'coach-home-calendar',
      { month: toIsoDate(this.visibleMonth()), date: this.selectedDate() },
      this.route,
      { view: 'calendar', month: toIsoDate(this.visibleMonth()), date: this.selectedDate() },
    );
  }

  canDelete(session: TrainingSessionListItem): boolean {
    return session.trainingType === 'Routine' && session.status === 'Scheduled' && session.routineScheduleId !== null;
  }

  hasTrainingType(day: CalendarDay, trainingType: 'Routine' | 'Private'): boolean {
    return day.sessions.some((session) => session.trainingType === trainingType);
  }

  hasPendingRequest(day: CalendarDay): boolean {
    return day.requests.some((request) => request.status === 'Pending');
  }

  requestStatusLabel(request: RoutineScheduleRequest): string {
    return request.status === 'RevisionRequested' ? 'ต้องแก้ไข' : 'รออนุมัติ';
  }

  visibleSessionLimit(day: CalendarDay): number {
    return Math.max(0, 3 - Math.min(day.requests.length, 2));
  }

  hiddenCalendarItemCount(day: CalendarDay): number {
    const visibleRequests = Math.min(day.requests.length, 2);
    const visibleSessions = Math.min(day.sessions.length, this.visibleSessionLimit(day));
    return day.requests.length + day.sessions.length - visibleRequests - visibleSessions;
  }

  calendarDayBackground(day: CalendarDay, isSelected = false): string | null {
    const hasRoutine = this.hasTrainingType(day, 'Routine');
    const hasPrivate = this.hasTrainingType(day, 'Private');
    const hasRequest = day.requests.length > 0;
    if (hasRoutine && hasPrivate) {
      return 'linear-gradient(135deg, #d1fae5 0 50%, #dbeafe 50% 100%)';
    }
    if (hasRoutine) {
      return '#d1fae5';
    }
    if (hasPrivate) {
      return '#dbeafe';
    }
    if (hasRequest) {
      return '#fef3c7';
    }
    return isSelected ? '#e5e7eb' : null;
  }

  requestDelete(session: TrainingSessionListItem, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.calendarActionError.set(null);
    this.pendingDelete.set(session);
  }

  cancelDelete(): void {
    this.pendingDelete.set(null);
  }

  async confirmDelete(): Promise<void> {
    const session = this.pendingDelete();
    if (!session?.routineScheduleId || this.deleteProcessing()) {
      return;
    }

    this.deleteProcessing.set(true);
    this.calendarActionError.set(null);
    try {
      await this.routineScheduleService.deleteOwn(session.routineScheduleId);
      this.pendingDelete.set(null);
      await Promise.all([this.load(), this.loadCalendar()]);
    } catch {
      this.calendarActionError.set('ไม่สามารถลบตารางได้ รายการอาจเริ่มดำเนินการหรือมีประวัติแล้ว');
      this.pendingDelete.set(null);
    } finally {
      this.deleteProcessing.set(false);
    }
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.dashboard.set(await this.coachDashboardService.get());
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  trainingTypeLabel(session: CoachDashboardSession): string {
    return session.trainingType === 'Routine' ? 'ฝึกซ้อมประจำ' : 'ฝึกซ้อมส่วนตัว';
  }

  timeRange(session: Pick<CoachDashboardSession, 'scheduledStartDateTime' | 'scheduledEndDateTime'>): string {
    const start = new Date(session.scheduledStartDateTime);
    const end = new Date(session.scheduledEndDateTime);
    const fmt = (d: Date) => d.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' });
    return `${fmt(start)} - ${fmt(end)}`;
  }

  sessionDuration(session: Pick<CoachDashboardSession, 'scheduledStartDateTime' | 'scheduledEndDateTime'>): string {
    const start = new Date(session.scheduledStartDateTime);
    const end = new Date(session.scheduledEndDateTime);
    const minutes = Math.max(0, Math.round((end.getTime() - start.getTime()) / 60_000));
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;

    if (hours === 0) return `${remainingMinutes} นาที`;
    if (remainingMinutes === 0) return `${hours} ชั่วโมง`;
    return `${hours} ชม. ${remainingMinutes} นาที`;
  }

  sessionDay(session: Pick<CoachDashboardSession, 'sessionDate'>): string {
    return String(Number(session.sessionDate.slice(8, 10)));
  }

  sessionMonthYear(session: Pick<CoachDashboardSession, 'sessionDate'>): string {
    const [year, month] = session.sessionDate.split('-').map(Number);
    return `${THAI_SHORT_MONTHS[month - 1]} ${year}`;
  }

  sessionWeekday(session: Pick<CoachDashboardSession, 'sessionDate'>): string {
    const [year, month, day] = session.sessionDate.split('-').map(Number);
    return THAI_WEEKDAYS[new Date(year, month - 1, day).getDay()];
  }
}

const THAI_SHORT_MONTHS = ['ม.ค.', 'ก.พ.', 'มี.ค.', 'เม.ย.', 'พ.ค.', 'มิ.ย.', 'ก.ค.', 'ส.ค.', 'ก.ย.', 'ต.ค.', 'พ.ย.', 'ธ.ค.'];
const THAI_WEEKDAYS = ['อาทิตย์', 'จันทร์', 'อังคาร', 'พุธ', 'พฤหัสบดี', 'ศุกร์', 'เสาร์'];

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

function parseIsoDate(value: string | number | null): Date | null {
  if (typeof value !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
  const [year, month, day] = value.split('-').map(Number);
  const parsed = new Date(year, month - 1, day);
  return parsed.getFullYear() === year && parsed.getMonth() === month - 1 && parsed.getDate() === day
    ? parsed
    : null;
}
