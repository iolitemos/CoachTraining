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

type ViewState = 'loading' | 'error' | 'ready';

interface CalendarDay {
  date: Date;
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  schedules: RoutineScheduleListItem[];
}

const DAY_HEADERS = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];

@Component({
  selector: 'app-routine-schedule-calendar',
  imports: [RouterLink, PageHeader, LoadingIndicator, EmptyState, ErrorState, ConfirmationDialog, DisplayDatePipe],
  templateUrl: './routine-schedule-calendar.html',
  styleUrl: './routine-schedule-calendar.css',
})
export class RoutineScheduleCalendar implements OnInit {
  state = signal<ViewState>('loading');
  schedules = signal<RoutineScheduleListItem[]>([]);
  visibleMonth = signal(startOfMonth(new Date()));
  selectedDate = signal(toIsoDate(new Date()));
  pendingDelete = signal<RoutineScheduleListItem | null>(null);
  deleteProcessing = signal(false);
  actionError = signal<string | null>(null);

  readonly dayHeaders = DAY_HEADERS;

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
        schedules: this.schedules()
          .filter((schedule) => occursOn(schedule, date))
          .sort((a, b) => a.startTime.localeCompare(b.startTime)),
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

  constructor(private readonly routineScheduleService: RoutineScheduleService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.schedules.set(await this.routineScheduleService.listAll());
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
  }

  goToCurrentMonth(): void {
    const today = new Date();
    this.visibleMonth.set(startOfMonth(today));
    this.selectedDate.set(toIsoDate(today));
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
    return day.isToday ? '#2563eb' : null;
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
