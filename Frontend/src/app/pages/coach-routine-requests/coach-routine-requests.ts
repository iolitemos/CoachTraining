import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { PageHeader } from '../../shared/page-header/page-header';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { ErrorState } from '../../shared/error-state/error-state';
import { DateInput } from '../../shared/date-input/date-input';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';
import { ConfirmationDialog } from '../../shared/confirmation-dialog/confirmation-dialog';
import {
  RoutineScheduleRequest,
  RoutineScheduleRequestStatus,
} from '../../models/routine-schedule-request.model';
import { RoutineScheduleRequestService } from '../../services/routine-schedule-request.service';

type ViewState = 'loading' | 'ready' | 'error';
type RequestFilter = 'All' | 'Pending' | 'RevisionRequested' | 'Approved' | 'Closed';

const THAI_WEEKDAYS = ['อาทิตย์', 'จันทร์', 'อังคาร', 'พุธ', 'พฤหัสบดี', 'ศุกร์', 'เสาร์'];
const THAI_SHORT_MONTHS = [
  'ม.ค.',
  'ก.พ.',
  'มี.ค.',
  'เม.ย.',
  'พ.ค.',
  'มิ.ย.',
  'ก.ค.',
  'ส.ค.',
  'ก.ย.',
  'ต.ค.',
  'พ.ย.',
  'ธ.ค.',
];

@Component({
  selector: 'app-coach-routine-requests',
  imports: [
    FormsModule,
    RouterLink,
    PageHeader,
    LoadingIndicator,
    EmptyState,
    ErrorState,
    DateInput,
    DisplayDatePipe,
    ConfirmationDialog,
  ],
  templateUrl: './coach-routine-requests.html',
})
export class CoachRoutineRequests implements OnInit {
  state = signal<ViewState>('loading');
  requests = signal<RoutineScheduleRequest[]>([]);
  filter = signal<RequestFilter>('All');
  editing = signal<RoutineScheduleRequest | null>(null);
  cancelling = signal<RoutineScheduleRequest | null>(null);
  processing = signal(false);
  formError = signal<string | null>(null);
  feedback = signal<{ kind: 'success' | 'error'; message: string } | null>(null);

  date = '';
  startTime = '';
  endTime = '';
  remarks = '';

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
  readonly filteredRequests = computed(() => {
    const selected = this.filter();
    if (selected === 'All') return this.requests();
    if (selected === 'Closed') {
      return this.requests().filter((request) =>
        ['Rejected', 'Cancelled'].includes(request.status),
      );
    }
    return this.requests().filter((request) => request.status === selected);
  });

  constructor(private readonly service: RoutineScheduleRequestService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(showLoading = true): Promise<void> {
    if (showLoading) this.state.set('loading');
    try {
      this.requests.set(await this.service.listOwn());
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  selectFilter(filter: RequestFilter): void {
    this.filter.set(filter);
  }

  filterLabel(): string {
    return (
      {
        All: 'ทั้งหมด',
        Pending: 'รอตรวจสอบ',
        RevisionRequested: 'ต้องแก้ไข',
        Approved: 'อนุมัติแล้ว',
        Closed: 'ปิดรายการ',
      } as const
    )[this.filter()];
  }

  statusLabel(status: RoutineScheduleRequestStatus): string {
    return (
      {
        Pending: 'รอตรวจสอบ',
        Approved: 'อนุมัติแล้ว',
        RevisionRequested: 'ต้องแก้ไข',
        Rejected: 'ไม่อนุมัติ',
        Cancelled: 'ยกเลิกแล้ว',
      } as const
    )[status];
  }

  statusDescription(status: RoutineScheduleRequestStatus): string {
    return (
      {
        Pending: 'ผู้ดูแลระบบกำลังตรวจสอบคำขอนี้',
        Approved: 'สร้างตารางฝึกซ้อมประจำเรียบร้อยแล้ว',
        RevisionRequested: 'ผู้ดูแลระบบส่งกลับมาให้แก้ไขข้อมูล',
        Rejected: 'คำขอนี้ไม่ได้รับการอนุมัติ',
        Cancelled: 'คุณยกเลิกคำขอนี้แล้ว',
      } as const
    )[status];
  }

  day(date: string): string {
    return String(Number(date.slice(8, 10)));
  }

  monthYear(date: string): string {
    const month = Number(date.slice(5, 7)) - 1;
    return `${THAI_SHORT_MONTHS[month]} ${Number(date.slice(0, 4)) + 543}`;
  }

  weekday(date: string): string {
    const [year, month, day] = date.split('-').map(Number);
    return THAI_WEEKDAYS[new Date(Date.UTC(year, month - 1, day)).getUTCDay()];
  }

  duration(request: RoutineScheduleRequest): string {
    const [startHour, startMinute] = this.requestStartTime(request)
      .slice(0, 5)
      .split(':')
      .map(Number);
    const [endHour, endMinute] = this.requestEndTime(request).slice(0, 5).split(':').map(Number);
    const minutes = endHour * 60 + endMinute - (startHour * 60 + startMinute);
    if (minutes <= 0) return '';
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    if (hours && remainingMinutes) return `${hours} ชม. ${remainingMinutes} นาที`;
    if (hours) return `${hours} ชม.`;
    return `${remainingMinutes} นาที`;
  }

  approvedChanged(request: RoutineScheduleRequest): boolean {
    if (request.status !== 'Approved' || !request.approvedDate) return false;
    return (
      request.approvedDate !== request.proposedDate ||
      request.approvedStartTime?.slice(0, 5) !== request.proposedStartTime.slice(0, 5) ||
      request.approvedEndTime?.slice(0, 5) !== request.proposedEndTime.slice(0, 5) ||
      (request.approvedRemarks ?? '').trim() !== (request.proposedRemarks ?? '').trim()
    );
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

  openEdit(request: RoutineScheduleRequest): void {
    this.editing.set(request);
    this.date = request.proposedDate;
    this.startTime = request.proposedStartTime.slice(0, 5);
    this.endTime = request.proposedEndTime.slice(0, 5);
    this.remarks = request.proposedRemarks ?? '';
    this.formError.set(null);
  }

  closeEdit(): void {
    if (this.processing()) return;
    this.editing.set(null);
    this.formError.set(null);
  }

  async save(): Promise<void> {
    const request = this.editing();
    if (!request || this.processing()) return;

    this.processing.set(true);
    this.formError.set(null);
    try {
      await this.service.updateOwn(request.routineScheduleRequestId, {
        effectiveStartDate: this.date,
        startTime: this.startTime,
        endTime: this.endTime,
        remarks: this.remarks.trim() || null,
      });
      this.editing.set(null);
      this.feedback.set({
        kind: 'success',
        message: 'ส่งข้อมูลที่แก้ไขให้ผู้ดูแลตรวจสอบอีกครั้งแล้ว',
      });
      await this.load(false);
    } catch {
      this.formError.set('ส่งคำขอไม่สำเร็จ กรุณาตรวจสอบวัน เวลา และตารางที่อาจทับซ้อน');
    } finally {
      this.processing.set(false);
    }
  }

  requestCancel(request: RoutineScheduleRequest): void {
    this.cancelling.set(request);
  }

  async confirmCancel(): Promise<void> {
    const request = this.cancelling();
    if (!request || this.processing()) return;

    this.processing.set(true);
    try {
      await this.service.cancelOwn(request.routineScheduleRequestId);
      this.cancelling.set(null);
      this.feedback.set({ kind: 'success', message: 'ยกเลิกคำขอเรียบร้อยแล้ว' });
      await this.load(false);
    } catch {
      this.cancelling.set(null);
      this.feedback.set({ kind: 'error', message: 'ยกเลิกคำขอไม่สำเร็จ กรุณาลองอีกครั้ง' });
    } finally {
      this.processing.set(false);
    }
  }
}
