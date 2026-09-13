import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { PrivateSessionListItem } from '../../../models/private-session.model';
import { PrivateSessionService } from '../../../services/private-session.service';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { FilterStateService } from '../../../services/filter-state.service';
import { CoachNamePipe } from '../../../shared/coach-name/coach-name.pipe';

type ViewState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-private-session-list',
  imports: [RouterLink, PageHeader, SearchFilterToolbar, LoadingIndicator, EmptyState, ErrorState, Pagination, StatusBadge, DisplayDatePipe, CoachNamePipe],
  templateUrl: './private-session-list.html',
  styleUrl: './private-session-list.css',
})
export class PrivateSessionList implements OnInit {
  state = signal<ViewState>('loading');
  sessions = signal<PrivateSessionListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');

  constructor(private readonly privateSessionService: PrivateSessionService, private readonly filterState: FilterStateService, private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    const filters = this.filterState.restore<{ search: string; page: number }>('private-sessions-list', { search: '', page: 1 }, this.route.snapshot.queryParamMap, ['page']);
    this.search.set(filters.search);
    this.page.set(filters.page ?? 1);
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.privateSessionService.list(this.page(), this.pageSize(), this.search());
      this.sessions.set(result.items);
      this.totalCount.set(result.totalCount);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  onSearch(term: string): void {
    this.search.set(term);
    this.page.set(1);
    this.rememberFilters();
    void this.load();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.rememberFilters();
    void this.load();
  }

  private rememberFilters(): void {
    this.filterState.save('private-sessions-list', { search: this.search(), page: this.page() }, this.route);
  }
}
