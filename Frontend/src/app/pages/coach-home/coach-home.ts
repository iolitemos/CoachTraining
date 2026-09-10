import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCalendarCheck2,
  LucideCircleCheckBig,
  LucideClipboardCheck,
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
import { TrainingSessionListItem } from '../../models/training-session.model';
import { TrainingSessionService } from '../../services/training-session.service';
import { RoutineScheduleService } from '../../services/routine-schedule.service';
import { ConfirmationDialog } from '../../shared/confirmation-dialog/confirmation-dialog';

type ViewState = 'loading' | 'error' | 'ready';
type CoachHomeTab = 'overview' | 'calendar';

interface CalendarDay {
  date: Date;
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  sessions: TrainingSessionListItem[];
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
    LucideCircleCheckBig,
    LucideClipboardCheck,
    LucideDumbbell,
    LucideHourglass,
    LucideRepeat,
    DisplayDatePipe,
    ConfirmationDialog,
  ],
  templateUrl: './coach-home.html',
  styleUrl: './coach-home.css',
})
export class CoachHome implements OnInit {
  private longPressTimer: ReturnType<typeof setTimeout> | null = null;
  private longPressTriggered = false;
  readonly dayHeaders = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];
  state = signal<ViewState>('loading');
  activeTab = signal<CoachHomeTab>('overview');
  dashboard = signal<CoachDashboardResponse | null>(null);
  calendarState = signal<ViewState>('loading');
  calendarSessions = signal<TrainingSessionListItem[]>([]);
  calendarColleagues = signal<CoachCalendarColleague[]>([]);
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

  constructor(
    private readonly coachDashboardService: CoachDashboardService,
    private readonly trainingSessionService: TrainingSessionService,
    private readonly routineScheduleService: RoutineScheduleService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {}

  ngOnInit(): void {
    if (this.route.snapshot.queryParamMap.get('view') === 'calendar') {
      this.activeTab.set('calendar');
    }
    void this.load();
    void this.loadCalendar();
  }

  selectTab(tab: CoachHomeTab): void {
    this.activeTab.set(tab);
  }

  toggleOverdueSessions(): void {
    this.overdueSessionsExpanded.update((expanded) => !expanded);
  }

  async loadCalendar(): Promise<void> {
    this.calendarState.set('loading');
    const days = this.calendarDays();
    try {
      const [result, colleagues] = await Promise.all([
        this.trainingSessionService.list({
          page: 1,
          pageSize: 100,
          coachId: null,
          trainingType: null,
          status: null,
          dateFrom: days[0].isoDate,
          dateTo: days[days.length - 1].isoDate,
        }),
        this.coachDashboardService.getCalendarColleagues(
          days[0].isoDate,
          days[days.length - 1].isoDate,
        ),
      ]);
      this.calendarSessions.set(result.items);
      this.calendarColleagues.set(colleagues);
      this.calendarState.set('ready');
    } catch {
      this.calendarState.set('error');
    }
  }

  moveMonth(offset: number): void {
    const current = this.visibleMonth();
    const next = new Date(current.getFullYear(), current.getMonth() + offset, 1);
    this.visibleMonth.set(next);
    this.selectedDate.set(toIsoDate(next));
    void this.loadCalendar();
  }

  goToCurrentMonth(): void {
    const today = new Date();
    this.visibleMonth.set(startOfMonth(today));
    this.selectedDate.set(toIsoDate(today));
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
      void this.loadCalendar();
    }
  }

  startDateLongPress(day: CalendarDay): void {
    this.cancelDateLongPress();
    this.longPressTriggered = false;
    this.longPressTimer = setTimeout(() => {
      this.longPressTimer = null;
      this.longPressTriggered = true;
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

  canDelete(session: TrainingSessionListItem): boolean {
    return session.trainingType === 'Routine' && session.status === 'Scheduled' && session.routineScheduleId !== null;
  }

  hasTrainingType(day: CalendarDay, trainingType: 'Routine' | 'Private'): boolean {
    return day.sessions.some((session) => session.trainingType === trainingType);
  }

  calendarDayBackground(day: CalendarDay, isSelected = false): string | null {
    const hasRoutine = this.hasTrainingType(day, 'Routine');
    const hasPrivate = this.hasTrainingType(day, 'Private');
    if (hasRoutine && hasPrivate) {
      return 'linear-gradient(135deg, #d1fae5 0 50%, #dbeafe 50% 100%)';
    }
    if (hasRoutine) {
      return '#d1fae5';
    }
    if (hasPrivate) {
      return '#dbeafe';
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
