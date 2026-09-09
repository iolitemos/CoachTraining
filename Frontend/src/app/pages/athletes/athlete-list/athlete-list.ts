import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { AthleteListItem, AthleteType, athleteTypeLabel } from '../../../models/athlete.model';
import { AthleteService } from '../../../services/athlete.service';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { FilterStateService } from '../../../services/filter-state.service';

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
    DisplayDatePipe,
  ],
  templateUrl: './athlete-list.html',
  styleUrl: './athlete-list.css',
})
export class AthleteList implements OnInit {
  readonly athleteTypeLabel = athleteTypeLabel;
  state = signal<ViewState>('loading');
  athletes = signal<AthleteListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');
  athleteType = signal<AthleteType>('Affiliated');

  pendingStatusChange = signal<AthleteListItem | null>(null);
  statusChangeProcessing = signal(false);

  constructor(private readonly athleteService: AthleteService, private readonly filterState: FilterStateService, private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    const filters = this.filterState.restore<{ search: string; page: number; athleteType: string }>('athletes', { search: '', page: 1, athleteType: 'Affiliated' }, this.route.snapshot.queryParamMap, ['page']);
    this.search.set(filters.search);
    this.page.set(filters.page ?? 1);
    this.athleteType.set(filters.athleteType === 'General' ? 'General' : 'Affiliated');
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.athleteService.list(this.page(), this.pageSize(), this.search(), this.athleteType());
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
    this.rememberFilters();
    void this.load();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.rememberFilters();
    void this.load();
  }

  onTypeChange(athleteType: AthleteType): void {
    if (athleteType === this.athleteType()) return;
    this.athleteType.set(athleteType);
    this.page.set(1);
    this.rememberFilters();
    void this.load();
  }

  private rememberFilters(): void {
    this.filterState.save('athletes', { search: this.search(), page: this.page(), athleteType: this.athleteType() }, this.route);
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
