import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { CompetitionMatch } from '../../../models/competition-match.model';
import { CompetitionMatchService } from '../../../services/competition-match.service';
import { FilterStateService } from '../../../services/filter-state.service';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { PageHeader } from '../../../shared/page-header/page-header';
import { Pagination } from '../../../shared/pagination/pagination';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';

type ViewState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-competition-match-list',
  imports: [RouterLink, PageHeader, SearchFilterToolbar, LoadingIndicator, EmptyState, ErrorState, Pagination, ConfirmationDialog, DisplayDatePipe],
  templateUrl: './competition-match-list.html',
})
export class CompetitionMatchList implements OnInit {
  state = signal<ViewState>('loading');
  matches = signal<CompetitionMatch[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');
  pendingDelete = signal<CompetitionMatch | null>(null);
  deleting = signal(false);

  constructor(private readonly service: CompetitionMatchService, private readonly filters: FilterStateService, private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    const restored = this.filters.restore<{ search: string; page: number }>('competition-matches', { search: '', page: 1 }, this.route.snapshot.queryParamMap, ['page']);
    this.search.set(restored.search);
    this.page.set(restored.page ?? 1);
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.service.list(this.page(), this.pageSize(), this.search());
      this.matches.set(result.items);
      this.totalCount.set(result.totalCount);
      this.state.set('ready');
    } catch { this.state.set('error'); }
  }

  onSearch(search: string): void { this.search.set(search); this.page.set(1); this.remember(); void this.load(); }
  onPageChange(page: number): void { this.page.set(page); this.remember(); void this.load(); }
  requestDelete(match: CompetitionMatch): void { this.pendingDelete.set(match); }
  cancelDelete(): void { this.pendingDelete.set(null); }

  async confirmDelete(): Promise<void> {
    const match = this.pendingDelete();
    if (!match) return;
    this.deleting.set(true);
    try { await this.service.delete(match.competitionMatchId); this.pendingDelete.set(null); await this.load(); }
    finally { this.deleting.set(false); }
  }

  private remember(): void { this.filters.save('competition-matches', { search: this.search(), page: this.page() }, this.route); }
}
