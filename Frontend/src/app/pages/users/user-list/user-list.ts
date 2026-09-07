import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { getRoleLabel } from '../../../models/auth.model';
import { UserListItem } from '../../../models/user.model';
import { UserService } from '../../../services/user.service';

type ViewState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-user-list',
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
  templateUrl: './user-list.html',
  styleUrl: './user-list.css',
})
export class UserList implements OnInit {
  state = signal<ViewState>('loading');
  users = signal<UserListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');

  pendingStatusChange = signal<UserListItem | null>(null);
  statusChangeProcessing = signal(false);

  getRoleLabel = getRoleLabel;

  constructor(private readonly userService: UserService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.userService.list(this.page(), this.pageSize(), this.search());
      this.users.set(result.items);
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

  requestStatusChange(user: UserListItem): void {
    this.pendingStatusChange.set(user);
  }

  cancelStatusChange(): void {
    this.pendingStatusChange.set(null);
  }

  async confirmStatusChange(): Promise<void> {
    const user = this.pendingStatusChange();
    if (!user) {
      return;
    }

    this.statusChangeProcessing.set(true);
    try {
      await this.userService.setStatus(user.userId, !user.isActive);
      this.pendingStatusChange.set(null);
      await this.load();
    } finally {
      this.statusChangeProcessing.set(false);
    }
  }
}
