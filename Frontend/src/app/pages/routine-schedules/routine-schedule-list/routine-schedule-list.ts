import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucidePencil, LucideTrash2 } from '@lucide/angular';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { RoutineScheduleListItem } from '../../../models/routine-schedule.model';
import { RoutineScheduleService } from '../../../services/routine-schedule.service';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';

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
    LucidePencil,
    LucideTrash2,
    DisplayDatePipe,
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

  pendingDelete = signal<RoutineScheduleListItem | null>(null);
  deleteProcessing = signal(false);
  actionError = signal<string | null>(null);

  constructor(private readonly routineScheduleService: RoutineScheduleService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.routineScheduleService.list(
        this.page(),
        this.pageSize(),
        this.search(),
      );
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
