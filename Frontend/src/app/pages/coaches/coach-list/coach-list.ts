import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { CoachListItem } from '../../../models/coach.model';
import { CoachService } from '../../../services/coach.service';

type ViewState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-coach-list',
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
  templateUrl: './coach-list.html',
  styleUrl: './coach-list.css',
})
export class CoachList implements OnInit {
  state = signal<ViewState>('loading');
  coaches = signal<CoachListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');

  pendingStatusChange = signal<CoachListItem | null>(null);
  statusChangeProcessing = signal(false);

  constructor(private readonly coachService: CoachService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.coachService.list(this.page(), this.pageSize(), this.search());
      this.coaches.set(result.items);
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

  requestStatusChange(coach: CoachListItem): void {
    this.pendingStatusChange.set(coach);
  }

  cancelStatusChange(): void {
    this.pendingStatusChange.set(null);
  }

  async confirmStatusChange(): Promise<void> {
    const coach = this.pendingStatusChange();
    if (!coach) {
      return;
    }

    this.statusChangeProcessing.set(true);
    try {
      await this.coachService.setStatus(coach.coachId, !coach.isActive);
      this.pendingStatusChange.set(null);
      await this.load();
    } finally {
      this.statusChangeProcessing.set(false);
    }
  }
}
