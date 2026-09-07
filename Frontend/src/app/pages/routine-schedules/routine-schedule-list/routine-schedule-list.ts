import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { getDayOfWeekLabel, RoutineScheduleListItem } from '../../../models/routine-schedule.model';
import { RoutineScheduleService } from '../../../services/routine-schedule.service';

type ViewState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-routine-schedule-list',
  imports: [
    RouterLink,
    PageHeader,
    SearchFilterToolbar,
    LoadingIndicator,
    EmptyState,
    ErrorState,
    Pagination,
    ConfirmationDialog,
  ],
  templateUrl: './routine-schedule-list.html',
  styleUrl: './routine-schedule-list.css',
})
export class RoutineScheduleList implements OnInit {
  state = signal<ViewState>('loading');
  schedules = signal<RoutineScheduleListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');

  pendingStatusChange = signal<RoutineScheduleListItem | null>(null);
  statusChangeProcessing = signal(false);

  getDayOfWeekLabel = getDayOfWeekLabel;

  constructor(private readonly routineScheduleService: RoutineScheduleService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.routineScheduleService.list(this.page(), this.pageSize(), this.search());
      this.schedules.set(result.items);
      this.totalCount.set(result.totalCount);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  onSearch(term: string): void {
    this.search.set(term);
    this.page.set(1);
    void this.load();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    void this.load();
  }

  requestStatusChange(schedule: RoutineScheduleListItem): void {
    this.pendingStatusChange.set(schedule);
  }

  cancelStatusChange(): void {
    this.pendingStatusChange.set(null);
  }

  async confirmStatusChange(): Promise<void> {
    const schedule = this.pendingStatusChange();
    if (!schedule) {
      return;
    }

    this.statusChangeProcessing.set(true);
    try {
      await this.routineScheduleService.setStatus(schedule.routineScheduleId, !schedule.isActive);
      this.pendingStatusChange.set(null);
      await this.load();
    } finally {
      this.statusChangeProcessing.set(false);
    }
  }
}
