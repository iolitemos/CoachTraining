import { SlicePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { CoachOption } from '../../../models/coach.model';
import { TrainingSessionListItem, TrainingType } from '../../../models/training-session.model';
import { TrainingSessionStatus } from '../../../models/training-session-status.model';
import { CoachService } from '../../../services/coach.service';
import { TrainingSessionService } from '../../../services/training-session.service';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { DateInput } from '../../../shared/date-input/date-input';

type ViewState = 'loading' | 'error' | 'ready';

/**
 * Administrative Review — submitted training records list (requirement.md
 * 9.7, todo.md 5.13). Defaults to Submitted records awaiting review; the
 * status filter also covers Approved/Locked so an Administrator can look
 * up records already finalized. Opens each row in the shared session detail
 * page (Coach Session), which carries the Approve/Reject/Request
 * Revision/Unlock actions for Administrator.
 */
@Component({
  selector: 'app-review-list',
  imports: [FormsModule, SlicePipe, RouterLink, PageHeader, LoadingIndicator, EmptyState, ErrorState, Pagination, StatusBadge, DisplayDatePipe, DateInput],
  templateUrl: './review-list.html',
  styleUrl: './review-list.css',
})
export class ReviewList implements OnInit {
  state = signal<ViewState>('loading');
  sessions = signal<TrainingSessionListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);

  coachOptions = signal<CoachOption[]>([]);

  status: TrainingSessionStatus | '' = 'Submitted';
  trainingType: TrainingType | '' = '';
  coachId: number | null = null;
  dateFrom = '';
  dateTo = '';

  constructor(
    private readonly trainingSessionService: TrainingSessionService,
    private readonly coachService: CoachService,
  ) {}

  ngOnInit(): void {
    void this.coachService.getActiveOptions().then((options) => this.coachOptions.set(options));
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.trainingSessionService.list({
        page: this.page(),
        pageSize: this.pageSize(),
        coachId: this.coachId,
        trainingType: this.trainingType || null,
        status: this.status || null,
        dateFrom: this.dateFrom || null,
        dateTo: this.dateTo || null,
      });
      this.sessions.set(result.items);
      this.totalCount.set(result.totalCount);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  applyFilters(): void {
    this.page.set(1);
    void this.load();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    void this.load();
  }

  trainingTypeLabel(type: TrainingType): string {
    return type === 'Routine' ? 'ฝึกซ้อมประจำ' : 'ฝึกซ้อมส่วนตัว';
  }
}
