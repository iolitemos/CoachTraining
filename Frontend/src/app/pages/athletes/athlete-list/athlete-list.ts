import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { AthleteListItem } from '../../../models/athlete.model';
import { AthleteService } from '../../../services/athlete.service';

type ViewState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-athlete-list',
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
  templateUrl: './athlete-list.html',
  styleUrl: './athlete-list.css',
})
export class AthleteList implements OnInit {
  state = signal<ViewState>('loading');
  athletes = signal<AthleteListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');

  pendingStatusChange = signal<AthleteListItem | null>(null);
  statusChangeProcessing = signal(false);

  constructor(private readonly athleteService: AthleteService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.athleteService.list(this.page(), this.pageSize(), this.search());
      this.athletes.set(result.items);
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

  requestStatusChange(athlete: AthleteListItem): void {
    this.pendingStatusChange.set(athlete);
  }

  cancelStatusChange(): void {
    this.pendingStatusChange.set(null);
  }

  async confirmStatusChange(): Promise<void> {
    const athlete = this.pendingStatusChange();
    if (!athlete) {
      return;
    }

    this.statusChangeProcessing.set(true);
    try {
      await this.athleteService.setStatus(athlete.athleteId, !athlete.isActive);
      this.pendingStatusChange.set(null);
      await this.load();
    } finally {
      this.statusChangeProcessing.set(false);
    }
  }
}
