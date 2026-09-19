import { SlicePipe } from '@angular/common';
import { Component, computed, ElementRef, OnInit, signal, ViewChild } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
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
import { FilterStateService } from '../../../services/filter-state.service';
import { CoachNamePipe } from '../../../shared/coach-name/coach-name.pipe';
import { ApprovalService } from '../../../services/approval.service';
import { RoutineBatchApprovalDay } from '../../../models/approval.model';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { ApiErrorBody } from '../../../models/paged-result.model';

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
  imports: [FormsModule, SlicePipe, RouterLink, PageHeader, LoadingIndicator, EmptyState, ErrorState, Pagination, StatusBadge, DisplayDatePipe, DateInput, CoachNamePipe, ConfirmationDialog],
  templateUrl: './review-list.html',
  styleUrl: './review-list.css',
})
export class ReviewList implements OnInit {
  @ViewChild('batchDayScroller') private batchDayScroller?: ElementRef<HTMLElement>;

  state = signal<ViewState>('loading');
  sessions = signal<TrainingSessionListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  batchDays = signal<RoutineBatchApprovalDay[]>([]);
  selectedBatchDates = signal<Set<string>>(new Set());
  batchLoading = signal(false);
  batchProcessing = signal(false);
  batchDialogOpen = signal(false);
  batchError = signal<string | null>(null);
  batchSuccess = signal<string | null>(null);
  selectedBatchDays = computed(() => this.batchDays().filter((day) => this.selectedBatchDates().has(day.sessionDate)));

  coachOptions = signal<CoachOption[]>([]);

  status: TrainingSessionStatus | '' = '';
  trainingType: TrainingType | '' = '';
  coachId: number | null = null;
  dateFrom = todayIsoDate();
  dateTo = todayIsoDate();

  constructor(
    private readonly trainingSessionService: TrainingSessionService,
    private readonly coachService: CoachService,
    private readonly filterState: FilterStateService,
    private readonly route: ActivatedRoute,
    private readonly approvalService: ApprovalService,
  ) {}

  ngOnInit(): void {
    const filters = this.filterState.restore<{ status: string; trainingType: string; coachId: number | null; dateFrom: string; dateTo: string; page: number }>('review', {
      status: '', trainingType: '', coachId: null, dateFrom: todayIsoDate(), dateTo: todayIsoDate(), page: 1,
    }, this.route.snapshot.queryParamMap, ['coachId', 'page']);
    this.status = filters.status as TrainingSessionStatus | '';
    this.trainingType = filters.trainingType as TrainingType | '';
    this.coachId = filters.coachId;
    this.dateFrom = filters.dateFrom;
    this.dateTo = filters.dateTo;
    this.page.set(filters.page ?? 1);
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
      await this.loadBatchDays();
    } catch {
      this.state.set('error');
    }
  }

  applyFilters(): void {
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
    this.filterState.save('review', {
      status: this.status, trainingType: this.trainingType, coachId: this.coachId,
      dateFrom: this.dateFrom, dateTo: this.dateTo, page: this.page(),
    }, this.route);
  }

  trainingTypeLabel(type: TrainingType): string {
    return type === 'Routine' ? 'ฝึกซ้อมประจำ' : 'ฝึกซ้อมส่วนตัว';
  }

  showBatchPanel(): boolean {
    return (!this.trainingType || this.trainingType === 'Routine') && (!this.status || this.status === 'Scheduled');
  }

  isBatchDateSelected(date: string): boolean {
    return this.selectedBatchDates().has(date);
  }

  toggleBatchDate(day: RoutineBatchApprovalDay): void {
    if (!day.isEligible || this.batchProcessing()) return;
    const selected = new Set(this.selectedBatchDates());
    selected.has(day.sessionDate) ? selected.delete(day.sessionDate) : selected.add(day.sessionDate);
    this.selectedBatchDates.set(selected);
    this.batchError.set(null);
    this.batchSuccess.set(null);
  }

  openBatchConfirmation(): void {
    if (this.selectedBatchDays().length > 0) this.batchDialogOpen.set(true);
  }

  scrollBatchDays(direction: 'previous' | 'next'): void {
    this.batchDayScroller?.nativeElement.scrollBy({
      left: direction === 'next' ? 328 : -328,
      behavior: 'smooth',
    });
  }

  batchConfirmationMessage(): string {
    const days = this.selectedBatchDays();
    const sessions = days.reduce((sum, day) => sum + day.scheduledSessionCount, 0);
    const withAttendance = days.reduce((sum, day) => sum + day.sessionsWithAttendanceCount, 0);
    const withoutAttendance = days.reduce((sum, day) => sum + day.sessionsWithoutAttendanceCount, 0);
    return `เลือก ${days.length} วัน รวม ${sessions} รายการ • มีข้อมูลนักกีฬา ${withAttendance} รายการ • ไม่มีข้อมูลนักกีฬา ${withoutAttendance} รายการ ระบบจะใช้โค้ชและเวลาตามกำหนด แล้วอนุมัติและล็อกทั้งหมด`;
  }

  async confirmBatchApproval(): Promise<void> {
    this.batchProcessing.set(true);
    this.batchError.set(null);
    this.batchSuccess.set(null);
    try {
      const result = await this.approvalService.batchApproveRoutine([...this.selectedBatchDates()]);
      this.batchDialogOpen.set(false);
      this.selectedBatchDates.set(new Set());
      this.batchSuccess.set(`อนุมัติสำเร็จ ${result.approvedSessionCount} รายการ จาก ${result.approvedDateCount} วัน`);
      await this.load();
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? error.error as ApiErrorBody | undefined : undefined;
      this.batchError.set(body?.message ?? 'ไม่สามารถอนุมัติรายการแบบกลุ่มได้');
      this.batchDialogOpen.set(false);
    } finally {
      this.batchProcessing.set(false);
    }
  }

  private async loadBatchDays(): Promise<void> {
    this.selectedBatchDates.set(new Set());
    this.batchError.set(null);
    if (!this.showBatchPanel() || !this.dateFrom || !this.dateTo) {
      this.batchDays.set([]);
      return;
    }

    this.batchLoading.set(true);
    try {
      this.batchDays.set(await this.approvalService.getRoutineBatchDays(this.dateFrom, this.dateTo));
    } catch {
      this.batchDays.set([]);
      this.batchError.set('ไม่สามารถโหลดข้อมูลสำหรับอนุมัติแบบกลุ่มได้');
    } finally {
      this.batchLoading.set(false);
    }
  }
}

function todayIsoDate(): string {
  const today = new Date();
  const month = String(today.getMonth() + 1).padStart(2, '0');
  const day = String(today.getDate()).padStart(2, '0');
  return `${today.getFullYear()}-${month}-${day}`;
}
