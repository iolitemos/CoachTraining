import { SlicePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  LucideCalendarClock,
  LucideCircleCheckBig,
  LucideLock,
  LucidePlay,
  LucideRepeat,
  LucideSend,
  LucideX,
} from '@lucide/angular';
import { PageHeader } from '../../shared/page-header/page-header';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { ErrorState } from '../../shared/error-state/error-state';
import { StatusBadge } from '../../shared/status-badge/status-badge';
import { RoutineAttendance } from '../../shared/routine-attendance/routine-attendance';
import { PrivateAttendance } from '../../shared/private-attendance/private-attendance';
import { TrainingLogForm } from '../../shared/training-log-form/training-log-form';
import { SubstituteCoachDialog } from '../../shared/substitute-coach-dialog/substitute-coach-dialog';
import { CancellationDialog } from '../../shared/cancellation-dialog/cancellation-dialog';
import { RescheduleDialog } from '../../shared/reschedule-dialog/reschedule-dialog';
import { ApprovalActionDialog } from '../../shared/approval-action-dialog/approval-action-dialog';
import { SessionHistoryTimeline } from '../../shared/session-history-timeline/session-history-timeline';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';
import { ApiErrorBody } from '../../models/paged-result.model';
import { TrainingLog, TrainingSessionDetail } from '../../models/training-session.model';
import { SubstituteCoachResponse } from '../../models/substitute-coach.model';
import { RescheduleResponse } from '../../models/reschedule.model';
import { ApprovalActionType, TrainingApprovalActionResponse } from '../../models/approval.model';
import { AppRole } from '../../models/auth.model';
import { AuthService } from '../../services/auth.service';
import { TrainingSessionService } from '../../services/training-session.service';

type ViewState = 'loading' | 'error' | 'ready';

const NON_EDITABLE_STATUSES = ['Submitted', 'Approved', 'Locked', 'Cancelled', 'Rescheduled'];

/**
 * Coach Session / Administrative Review detail (requirement.md 9.2/9.7,
 * todo.md 5.6/5.9/5.10/5.11/5.12/5.13). Presents the operational flow in
 * sequence: Session Summary, Start/Actual Teaching Information, Athlete
 * Attendance, Training Log, Complete, Submit. An Administrator viewing the
 * same screen may also assign a substitute coach (FR-SUB-001–005), cancel
 * (FR-CR-001–003), reschedule (FR-CR-004–007), and review the submitted
 * record — Approve/Reject/Request Revision/Unlock (FR-APPROVAL-001–007) — so
 * this single page serves as both Coach Session and Administrative Review.
 */
@Component({
  selector: 'app-coach-session',
  imports: [
    SlicePipe,
    RouterLink,
    PageHeader,
    LoadingIndicator,
    ErrorState,
    StatusBadge,
    RoutineAttendance,
    PrivateAttendance,
    TrainingLogForm,
    SubstituteCoachDialog,
    CancellationDialog,
    RescheduleDialog,
    ApprovalActionDialog,
    SessionHistoryTimeline,
    DisplayDatePipe,
    LucidePlay,
    LucideCircleCheckBig,
    LucideSend,
    LucideLock,
    LucideRepeat,
    LucideX,
    LucideCalendarClock,
  ],
  templateUrl: './coach-session.html',
  styleUrl: './coach-session.css',
})
export class CoachSession implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);
  private readonly trainingSessionService = inject(TrainingSessionService);

  trainingSessionId = signal<number>(0);
  state = signal<ViewState>('loading');
  session = signal<TrainingSessionDetail | null>(null);

  starting = signal(false);
  completing = signal(false);
  submitting = signal(false);
  actionError = signal<string | null>(null);

  /** FR-PATT-002 — Private Training only; Routine has no completeness concept. */
  privateAttendanceComplete = signal(true);

  isAdministrator = this.authService.hasRole(AppRole.Administrator);

  substituteDialogOpen = signal(false);
  cancellationDialogOpen = signal(false);
  rescheduleDialogOpen = signal(false);
  replacementSessionId = signal<number | null>(null);
  approvalDialogAction = signal<Exclude<ApprovalActionType, 'Submit'> | null>(null);

  async ngOnInit(): Promise<void> {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.trainingSessionId.set(id);
    await this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const session = await this.trainingSessionService.getById(this.trainingSessionId());
      this.session.set(session);
      this.privateAttendanceComplete.set(session.trainingType !== 'Private');
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  isLocked(status: string): boolean {
    return NON_EDITABLE_STATUSES.includes(status);
  }

  canStart(status: string): boolean {
    return status === 'Scheduled' || status === 'CoachAbsent';
  }

  canComplete(status: string): boolean {
    return status === 'InProgress';
  }

  canSubmit(status: string): boolean {
    return status === 'Completed';
  }

  /** FR-SUB-001 — eligible only before the session starts, or when the coach is absent. */
  canSubstitute(status: string): boolean {
    return status === 'Scheduled' || status === 'CoachAbsent';
  }

  /** FR-CR-001 — cancellable while not yet finalized. */
  canCancel(status: string): boolean {
    return status === 'Scheduled' || status === 'InProgress' || status === 'CoachAbsent';
  }

  /** FR-CR-004 — rescheduling, like substitution, only applies before the session starts. */
  canReschedule(status: string): boolean {
    return status === 'Scheduled' || status === 'CoachAbsent';
  }

  /** FR-APPROVAL-002 — Approve/Reject/RequestRevision act on a Submitted record. */
  canReviewSubmission(status: string): boolean {
    return status === 'Submitted';
  }

  /** FR-APPROVAL-005 — Unlock returns a Locked record to Completed for correction. */
  canUnlock(status: string): boolean {
    return status === 'Locked';
  }

  onPrivateAttendanceCompleteChange(isComplete: boolean): void {
    this.privateAttendanceComplete.set(isComplete);
  }

  onLogSaved(trainingLog: TrainingLog): void {
    this.session.update((current) => (current ? { ...current, trainingLog } : current));
  }

  openSubstituteDialog(): void {
    this.substituteDialogOpen.set(true);
  }

  closeSubstituteDialog(): void {
    this.substituteDialogOpen.set(false);
  }

  onSubstitutionAssigned(result: SubstituteCoachResponse): void {
    this.session.set(result.session);
    this.substituteDialogOpen.set(false);
  }

  openCancellationDialog(): void {
    this.cancellationDialogOpen.set(true);
  }

  closeCancellationDialog(): void {
    this.cancellationDialogOpen.set(false);
  }

  onCancellationConfirmed(session: TrainingSessionDetail): void {
    this.session.set(session);
    this.cancellationDialogOpen.set(false);
  }

  openRescheduleDialog(): void {
    this.rescheduleDialogOpen.set(true);
  }

  closeRescheduleDialog(): void {
    this.rescheduleDialogOpen.set(false);
  }

  onRescheduled(result: RescheduleResponse): void {
    this.session.set(result.originalSession);
    this.replacementSessionId.set(result.replacementSession.trainingSessionId);
    this.rescheduleDialogOpen.set(false);
  }

  openApprovalDialog(action: Exclude<ApprovalActionType, 'Submit'>): void {
    this.approvalDialogAction.set(action);
  }

  closeApprovalDialog(): void {
    this.approvalDialogAction.set(null);
  }

  onApprovalActionCompleted(result: TrainingApprovalActionResponse): void {
    this.session.set(result.session);
    this.approvalDialogAction.set(null);
  }

  async start(): Promise<void> {
    this.starting.set(true);
    this.actionError.set(null);
    try {
      this.session.set(await this.trainingSessionService.start(this.trainingSessionId()));
    } catch (error) {
      this.actionError.set(this.extractErrorMessage(error, 'ไม่สามารถเริ่มฝึกซ้อมได้'));
    } finally {
      this.starting.set(false);
    }
  }

  async complete(): Promise<void> {
    this.completing.set(true);
    this.actionError.set(null);
    try {
      this.session.set(await this.trainingSessionService.complete(this.trainingSessionId()));
    } catch (error) {
      this.actionError.set(this.extractErrorMessage(error, 'ไม่สามารถบันทึกการเสร็จสิ้นฝึกซ้อมได้'));
    } finally {
      this.completing.set(false);
    }
  }

  async submit(): Promise<void> {
    this.submitting.set(true);
    this.actionError.set(null);
    try {
      this.session.set(await this.trainingSessionService.submit(this.trainingSessionId()));
    } catch (error) {
      this.actionError.set(this.extractErrorMessage(error, 'ไม่สามารถส่งตรวจได้'));
    } finally {
      this.submitting.set(false);
    }
  }

  private extractErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as ApiErrorBody | undefined;
      return body?.message ?? fallback;
    }
    return fallback;
  }
}
