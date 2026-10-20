import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiErrorBody } from '../../models/paged-result.model';
import { CompetitionMatch } from '../../models/competition-match.model';
import { RoutineParticipationPlanSummary } from '../../models/parent-routine-plan.model';
import {
  RoutineScheduleRequest,
  RoutineScheduleRequestStatus,
} from '../../models/routine-schedule-request.model';
import { RoutineScheduleRequestService } from '../../services/routine-schedule-request.service';
import { CompetitionMatchService } from '../../services/competition-match.service';
import { ParentRoutinePlanService } from '../../services/parent-routine-plan.service';
import { DateInput } from '../../shared/date-input/date-input';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { ErrorState } from '../../shared/error-state/error-state';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { PageHeader } from '../../shared/page-header/page-header';
import { ConfirmationDialog } from '../../shared/confirmation-dialog/confirmation-dialog';

type ViewState = 'loading' | 'ready' | 'error';
type ViewMode = 'calendar' | 'list';

interface CalendarDay {
  date: Date;
  isoDate: string;
  dayNumber: number;
  isCurrentMonth: boolean;
  isToday: boolean;
  requests: RoutineScheduleRequest[];
  competitionMatches: CompetitionMatch[];
  isRoutineTrainingDate: boolean;
  plannedAthleteCount: number;
}

interface CoachOption {
  coachId: number;
  label: string;
}

@Component({
  selector: 'app-routine-schedule-requests',
  imports: [
    FormsModule,
    PageHeader,
    LoadingIndicator,
    EmptyState,
    ErrorState,
    DateInput,
    DisplayDatePipe,
    ConfirmationDialog,
  ],
  templateUrl: './routine-schedule-requests.html',
})
export class RoutineScheduleRequests implements OnInit {
  readonly dayHeaders = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];

  state = signal<ViewState>('loading');
  requests = signal<RoutineScheduleRequest[]>([]);
  competitionMatches = signal<CompetitionMatch[]>([]);
  participationSummaries = signal<RoutineParticipationPlanSummary[]>([]);
  selected = signal<RoutineScheduleRequest | null>(null);
  processing = signal(false);
  error = signal<string | null>(null);
  success = signal<string | null>(null);
  batchError = signal<string | null>(null);
  batchConfirmationOpen = signal(false);
  selectedIds = signal<Set<number>>(new Set());
  viewMode = signal<ViewMode>('calendar');
  statusFilter = signal<RoutineScheduleRequestStatus | ''>('Pending');
  coachFilter = signal<number | null>(null);
  visibleMonth = signal(startOfMonth(new Date()));
  selectedDate = signal(toIsoDate(new Date()));

  approvedDate = '';
  approvedStartTime = '';
  approvedEndTime = '';
  approvedRemarks = '';
  reason = '';
  overrideConflict = false;

  readonly pendingCount = computed(
    () => this.requests().filter((request) => request.status === 'Pending').length,
  );
  readonly revisionCount = computed(
    () => this.requests().filter((request) => request.status === 'RevisionRequested').length,
  );
  readonly approvedCount = computed(
    () => this.requests().filter((request) => request.status === 'Approved').length,
  );
  readonly closedCount = computed(
    () =>
      this.requests().filter((request) => ['Rejected', 'Cancelled'].includes(request.status))
        .length,
  );

  readonly coachOptions = computed<CoachOption[]>(() => {
    const coaches = new Map<number, string>();
    for (const request of this.requests()) {
      const coachId = this.requestCoachId(request);
      if (!coaches.has(coachId)) {
        coaches.set(
          coachId,
          `โค้ช${this.requestCoachName(request)} · ${this.requestCoachCode(request)}`,
        );
      }
    }
    return [...coaches.entries()]
      .map(([coachId, label]) => ({ coachId, label }))
      .sort((a, b) => a.label.localeCompare(b.label, 'th'));
  });

  readonly filteredRequests = computed(() => {
    const status = this.statusFilter();
    const coachId = this.coachFilter();
    return this.requests()
      .filter(
        (request) =>
          (!status || request.status === status) &&
          (!coachId || this.requestCoachId(request) === coachId),
      )
      .sort(
        (a, b) =>
          this.requestDate(a).localeCompare(this.requestDate(b)) ||
          this.requestStartTime(a).localeCompare(this.requestStartTime(b)),
      );
  });

  readonly calendarRequests = computed(() => {
    const status = this.statusFilter();
    const coachId = this.coachFilter();
    return this.requests().filter((request) => {
      const matchesCoach = !coachId || this.requestCoachId(request) === coachId;
      const matchesStatus =
        !status ||
        request.status === status ||
        (status === 'Pending' && request.status === 'Approved');
      return matchesCoach && matchesStatus;
    });
  });

  readonly monthLabel = computed(() =>
    new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(
      this.visibleMonth(),
    ),
  );

  readonly calendarDays = computed<CalendarDay[]>(() => {
    const month = this.visibleMonth();
    const gridStart = new Date(month.getFullYear(), month.getMonth(), 1 - month.getDay());
    const today = toIsoDate(new Date());
    const requests = this.calendarRequests();

    return Array.from({ length: 42 }, (_, index) => {
      const date = addDays(gridStart, index);
      const isoDate = toIsoDate(date);
      const participation = this.participationSummaries().find(
        (item) => item.trainingDate === isoDate,
      );
      return {
        date,
        isoDate,
        dayNumber: date.getDate(),
        isCurrentMonth: date.getMonth() === month.getMonth(),
        isToday: isoDate === today,
        requests: requests.filter((request) => this.requestDate(request) === isoDate),
        competitionMatches: this.competitionMatches().filter(
          (match) => match.startDate <= isoDate && isoDate <= match.endDate,
        ),
        isRoutineTrainingDate: participation !== undefined,
        plannedAthleteCount: participation?.athleteCount ?? 0,
      };
    });
  });

  readonly selectedDay = computed(
    () => this.calendarDays().find((day) => day.isoDate === this.selectedDate()) ?? null,
  );

  readonly currentMonthRequestCount = computed(() =>
    this.calendarDays()
      .filter((day) => day.isCurrentMonth)
      .reduce((total, day) => total + day.requests.length, 0),
  );

  readonly visiblePendingRequests = computed(() =>
    this.filteredRequests().filter((request) => request.status === 'Pending'),
  );

  readonly selectedPendingRequests = computed(() => {
    const selectedIds = this.selectedIds();
    return this.requests().filter(
      (request) =>
        request.status === 'Pending' && selectedIds.has(request.routineScheduleRequestId),
    );
  });

  readonly allVisiblePendingSelected = computed(() => {
    const visible = this.visiblePendingRequests();
    const selectedIds = this.selectedIds();
    return (
      visible.length > 0 &&
      visible.every((request) => selectedIds.has(request.routineScheduleRequestId))
    );
  });

  constructor(
    private readonly service: RoutineScheduleRequestService,
    private readonly competitionMatchService: CompetitionMatchService,
    private readonly parentRoutinePlanService: ParentRoutinePlanService,
  ) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(showLoading = true): Promise<void> {
    if (showLoading) this.state.set('loading');
    try {
      if (showLoading) {
        const range = this.visibleGridRange();
        const [requests, competitionMatches, participationSummaries] = await Promise.all([
          this.service.list(''),
          this.competitionMatchService.listAll(),
          this.parentRoutinePlanService.getSummary(range.startDate, range.endDate),
        ]);
        this.requests.set(requests);
        this.competitionMatches.set(competitionMatches);
        this.participationSummaries.set(participationSummaries);
      } else {
        this.requests.set(await this.service.list(''));
      }
      const pendingIds = new Set(
        this.requests()
          .filter((request) => request.status === 'Pending')
          .map((request) => request.routineScheduleRequestId),
      );
      this.selectedIds.update(
        (selected) => new Set([...selected].filter((requestId) => pendingIds.has(requestId))),
      );
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  setViewMode(mode: ViewMode): void {
    this.viewMode.set(mode);
  }

  selectStatus(status: RoutineScheduleRequestStatus | ''): void {
    this.statusFilter.set(status);
  }

  selectCoach(value: string): void {
    this.coachFilter.set(value ? Number(value) : null);
  }

  isSelected(requestId: number): boolean {
    return this.selectedIds().has(requestId);
  }

  toggleSelection(requestId: number, checked: boolean): void {
    this.selectedIds.update((current) => {
      const next = new Set(current);
      if (checked) next.add(requestId);
      else next.delete(requestId);
      return next;
    });
    this.batchError.set(null);
  }

  toggleVisiblePending(): void {
    const visibleIds = this.visiblePendingRequests().map(
      (request) => request.routineScheduleRequestId,
    );
    const shouldSelect = !this.allVisiblePendingSelected();
    this.selectedIds.update((current) => {
      const next = new Set(current);
      for (const requestId of visibleIds) {
        if (shouldSelect) next.add(requestId);
        else next.delete(requestId);
      }
      return next;
    });
    this.batchError.set(null);
  }

  clearSelection(): void {
    this.selectedIds.set(new Set());
    this.batchError.set(null);
  }

  requestBatchApprove(): void {
    if (this.selectedPendingRequests().length === 0 || this.processing()) return;
    this.batchConfirmationOpen.set(true);
  }

  async confirmBatchApprove(): Promise<void> {
    const requestIds = this.selectedPendingRequests().map(
      (request) => request.routineScheduleRequestId,
    );
    if (requestIds.length === 0 || this.processing()) return;

    this.processing.set(true);
    this.batchError.set(null);
    try {
      const approved = await this.service.approveBatch(requestIds);
      this.batchConfirmationOpen.set(false);
      this.selectedIds.set(new Set());
      this.success.set(`อนุมัติและสร้างตารางฝึกซ้อมสำเร็จ ${approved.length} รายการ`);
      await this.load(false);
    } catch (error) {
      this.batchConfirmationOpen.set(false);
      this.batchError.set(this.message(error));
    } finally {
      this.processing.set(false);
    }
  }

  moveMonth(offset: number): void {
    const current = this.visibleMonth();
    const next = new Date(current.getFullYear(), current.getMonth() + offset, 1);
    this.visibleMonth.set(next);
    this.selectedDate.set(toIsoDate(next));
    void this.loadParticipationSummaries();
  }

  goToCurrentMonth(): void {
    const today = new Date();
    this.visibleMonth.set(startOfMonth(today));
    this.selectedDate.set(toIsoDate(today));
    void this.loadParticipationSummaries();
  }

  selectDay(day: CalendarDay): void {
    this.selectedDate.set(day.isoDate);
    if (!day.isCurrentMonth) {
      this.visibleMonth.set(startOfMonth(day.date));
      void this.loadParticipationSummaries();
    }
  }

  coachCount(day: CalendarDay): number {
    return new Set(day.requests.map((request) => this.requestCoachId(request))).size;
  }

  hasStatus(day: CalendarDay, status: RoutineScheduleRequestStatus): boolean {
    return day.requests.some((request) => request.status === status);
  }

  requestDate(request: RoutineScheduleRequest): string {
    return request.status === 'Approved'
      ? (request.currentScheduleDate ?? request.approvedDate ?? request.proposedDate)
      : request.proposedDate;
  }

  requestStartTime(request: RoutineScheduleRequest): string {
    return request.status === 'Approved'
      ? (request.currentScheduleStartTime ?? request.approvedStartTime ?? request.proposedStartTime)
      : request.proposedStartTime;
  }

  requestEndTime(request: RoutineScheduleRequest): string {
    return request.status === 'Approved'
      ? (request.currentScheduleEndTime ?? request.approvedEndTime ?? request.proposedEndTime)
      : request.proposedEndTime;
  }

  requestRemarks(request: RoutineScheduleRequest): string | null {
    return request.status === 'Approved'
      ? (request.currentScheduleRemarks ?? request.approvedRemarks ?? request.proposedRemarks)
      : request.proposedRemarks;
  }

  requestCoachId(request: RoutineScheduleRequest): number {
    return request.status === 'Approved'
      ? (request.currentScheduleCoachId ?? request.coachId)
      : request.coachId;
  }

  requestCoachName(request: RoutineScheduleRequest): string {
    if (request.status === 'Approved') {
      return (
        request.currentScheduleCoachNickname?.trim() ||
        request.currentScheduleCoachName ||
        request.coachNickname?.trim() ||
        request.coachName
      );
    }
    return request.coachNickname?.trim() || request.coachName;
  }

  requestCoachCode(request: RoutineScheduleRequest): string {
    return request.status === 'Approved'
      ? (request.currentScheduleCoachCode ?? request.coachCode)
      : request.coachCode;
  }

  requestCoachColor(request: RoutineScheduleRequest): string {
    return request.status === 'Approved'
      ? (request.currentScheduleCoachColorHex ?? request.coachColorHex)
      : request.coachColorHex;
  }

  coachChangedAfterApproval(request: RoutineScheduleRequest): boolean {
    return (
      request.status === 'Approved' &&
      request.currentScheduleCoachId !== null &&
      request.currentScheduleCoachId !== request.coachId
    );
  }

  scheduleChangedAfterApproval(request: RoutineScheduleRequest): boolean {
    if (request.status !== 'Approved' || !request.currentScheduleDate) return false;
    return (
      request.currentScheduleDate !== request.approvedDate ||
      request.currentScheduleStartTime?.slice(0, 5) !== request.approvedStartTime?.slice(0, 5) ||
      request.currentScheduleEndTime?.slice(0, 5) !== request.approvedEndTime?.slice(0, 5) ||
      (request.currentScheduleRemarks ?? '').trim() !== (request.approvedRemarks ?? '').trim() ||
      this.coachChangedAfterApproval(request)
    );
  }

  statusLabel(status: RoutineScheduleRequestStatus): string {
    return (
      {
        Pending: 'รอตรวจสอบ',
        Approved: 'อนุมัติแล้ว',
        RevisionRequested: 'ขอให้แก้ไข',
        Rejected: 'ปฏิเสธ',
        Cancelled: 'ยกเลิก',
      } as const
    )[status];
  }

  statusDescription(status: RoutineScheduleRequestStatus): string {
    return (
      {
        Pending: 'รอผู้ดูแลตรวจสอบและดำเนินการ',
        Approved: 'สร้างตารางฝึกซ้อมประจำแล้ว',
        RevisionRequested: 'ส่งกลับให้โค้ชแก้ไขข้อมูล',
        Rejected: 'ผู้ดูแลไม่อนุมัติคำขอ',
        Cancelled: 'โค้ชยกเลิกคำขอ',
      } as const
    )[status];
  }

  open(request: RoutineScheduleRequest): void {
    this.selected.set(request);
    this.approvedDate = request.approvedDate ?? request.proposedDate;
    this.approvedStartTime = (request.approvedStartTime ?? request.proposedStartTime).slice(0, 5);
    this.approvedEndTime = (request.approvedEndTime ?? request.proposedEndTime).slice(0, 5);
    this.approvedRemarks = request.approvedRemarks ?? request.proposedRemarks ?? '';
    this.reason = request.reviewReason ?? '';
    this.overrideConflict = false;
    this.error.set(null);
  }

  close(): void {
    if (this.processing()) return;
    this.selected.set(null);
    this.error.set(null);
  }

  changed(field: 'date' | 'start' | 'end' | 'remarks'): boolean {
    const request = this.selected();
    if (!request) return false;
    if (field === 'date') return this.approvedDate !== request.proposedDate;
    if (field === 'start') {
      return this.approvedStartTime !== request.proposedStartTime.slice(0, 5);
    }
    if (field === 'end') return this.approvedEndTime !== request.proposedEndTime.slice(0, 5);
    return this.approvedRemarks.trim() !== (request.proposedRemarks ?? '').trim();
  }

  async approve(): Promise<void> {
    const request = this.selected();
    if (!request || this.processing()) return;
    if (this.overrideConflict && !this.reason.trim()) {
      this.error.set('กรุณาระบุเหตุผลเมื่อต้องการอนุมัติตารางทับซ้อน');
      return;
    }

    this.processing.set(true);
    this.error.set(null);
    try {
      await this.service.approve(request.routineScheduleRequestId, {
        approvedDate: this.approvedDate,
        approvedStartTime: this.approvedStartTime,
        approvedEndTime: this.approvedEndTime,
        approvedRemarks: this.approvedRemarks.trim() || null,
        overrideConflict: this.overrideConflict,
        overrideReason: this.overrideConflict ? this.reason.trim() : null,
      });
      this.success.set('อนุมัติและสร้างตารางฝึกซ้อมสำเร็จ');
      this.selected.set(null);
      await this.load(false);
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.processing.set(false);
    }
  }

  async decide(action: 'revision' | 'reject'): Promise<void> {
    const request = this.selected();
    if (!request || this.processing()) return;
    if (!this.reason.trim()) {
      this.error.set('กรุณาระบุเหตุผลสำหรับโค้ช');
      return;
    }

    this.processing.set(true);
    this.error.set(null);
    try {
      if (action === 'revision') {
        await this.service.requestRevision(request.routineScheduleRequestId, this.reason.trim());
      } else {
        await this.service.reject(request.routineScheduleRequestId, this.reason.trim());
      }
      this.success.set(action === 'revision' ? 'ส่งกลับให้โค้ชแก้ไขแล้ว' : 'ปฏิเสธคำขอแล้ว');
      this.selected.set(null);
      await this.load(false);
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.processing.set(false);
    }
  }

  private message(error: unknown): string {
    return error instanceof HttpErrorResponse
      ? ((error.error as ApiErrorBody | undefined)?.message ?? 'ดำเนินการไม่สำเร็จ')
      : 'ดำเนินการไม่สำเร็จ';
  }

  private async loadParticipationSummaries(): Promise<void> {
    const range = this.visibleGridRange();
    try {
      this.participationSummaries.set(
        await this.parentRoutinePlanService.getSummary(range.startDate, range.endDate),
      );
    } catch {
      this.participationSummaries.set([]);
    }
  }

  private visibleGridRange(): { startDate: string; endDate: string } {
    const month = this.visibleMonth();
    const gridStart = new Date(month.getFullYear(), month.getMonth(), 1 - month.getDay());
    return {
      startDate: toIsoDate(gridStart),
      endDate: toIsoDate(addDays(gridStart, 41)),
    };
  }
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

function toIsoDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
