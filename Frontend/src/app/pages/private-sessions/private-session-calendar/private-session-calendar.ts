import { Component, OnInit, computed, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PrivateSessionListItem } from '../../../models/private-session.model';
import { PrivateSessionService } from '../../../services/private-session.service';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { PageHeader } from '../../../shared/page-header/page-header';
import { StatusBadge } from '../../../shared/status-badge/status-badge';

type ViewState = 'loading' | 'error' | 'ready';

interface CalendarDay {
  date: Date;
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  sessions: PrivateSessionListItem[];
}

@Component({
  selector: 'app-private-session-calendar',
  imports: [RouterLink, PageHeader, LoadingIndicator, EmptyState, ErrorState, StatusBadge, DisplayDatePipe],
  templateUrl: './private-session-calendar.html',
  styleUrl: './private-session-calendar.css',
})
export class PrivateSessionCalendar implements OnInit {
  readonly dayHeaders = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];
  state = signal<ViewState>('loading');
  sessions = signal<PrivateSessionListItem[]>([]);
  visibleMonth = signal(startOfMonth(new Date()));
  selectedDate = signal(toIsoDate(new Date()));

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
        sessions: this.sessions().filter((session) => session.sessionDate === isoDate),
      };
    });
  });

  selectedDay = computed(() => this.calendarDays().find((day) => day.isoDate === this.selectedDate()) ?? null);
  currentMonthSessionCount = computed(() =>
    this.calendarDays()
      .filter((day) => day.isCurrentMonth)
      .reduce((total, day) => total + day.sessions.length, 0),
  );

  constructor(private readonly privateSessionService: PrivateSessionService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    const days = this.calendarDays();
    try {
      this.sessions.set(
        await this.privateSessionService.listCalendar(days[0].isoDate, days[days.length - 1].isoDate),
      );
      this.state.set('ready');
    } catch {
      this.state.set('error');
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

  selectDay(day: CalendarDay): void {
    this.selectedDate.set(day.isoDate);
    if (!day.isCurrentMonth) {
      this.visibleMonth.set(startOfMonth(day.date));
      void this.load();
    }
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
