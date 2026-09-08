import { SlicePipe } from '@angular/common';
import { Component, OnChanges, input, signal } from '@angular/core';
import {
  LucideCalendarClock,
  LucideCircleAlert,
  LucideClipboardCheck,
  LucideHistory,
  LucideRepeat,
} from '@lucide/angular';
import { LoadingIndicator } from '../loading-indicator/loading-indicator';
import { ErrorState } from '../error-state/error-state';
import { EmptyState } from '../empty-state/empty-state';
import { AuditLogEntry, ConflictOverrideHistoryEntry, ConflictType, HistoryTimelineEvent } from '../../models/history.model';
import { getApprovalActionLabel } from '../../models/approval.model';
import { HistoryService } from '../../services/history.service';
import { ApprovalService } from '../../services/approval.service';
import { SubstituteCoachService } from '../../services/substitute-coach.service';

type ViewState = 'loading' | 'error' | 'ready';

const AUDIT_ACTION_LABELS_TH: Record<string, string> = {
  Cancel: 'ยกเลิกเซสชัน',
  Reschedule: 'เลื่อนเซสชัน',
};

const CONFLICT_TYPE_LABELS_TH: Record<ConflictType, string> = {
  CoachOverlap: 'ตารางโค้ชทับซ้อน',
  AthleteOverlap: 'ตารางนักกีฬาทับซ้อน',
  PrivateVsRoutineOverlap: 'ฝึกซ้อมส่วนตัวทับซ้อนกับฝึกซ้อมประจำของโค้ช',
};

/**
 * Session history / audit timeline (requirement.md 6.20, FR-AUDIT-001–003,
 * todo.md 4.20/5.17). Merges four history sources into one chronological
 * list: general audit trail (cancellation/rescheduling), approval workflow
 * (submit/approve/reject/request revision/unlock), substitute coach, and
 * conflict overrides. Every signed-in role may view this for sessions it can
 * already access — a Coach only ever sees history for its own sessions
 * (enforced by each underlying API, CLAUDE.md section 11).
 */
@Component({
  selector: 'app-session-history-timeline',
  imports: [
    SlicePipe,
    LoadingIndicator,
    ErrorState,
    EmptyState,
    LucideHistory,
    LucideRepeat,
    LucideClipboardCheck,
    LucideCircleAlert,
    LucideCalendarClock,
  ],
  templateUrl: './session-history-timeline.html',
  styleUrl: './session-history-timeline.css',
})
export class SessionHistoryTimeline implements OnChanges {
  trainingSessionId = input.required<number>();

  state = signal<ViewState>('loading');
  events = signal<HistoryTimelineEvent[]>([]);

  constructor(
    private readonly historyService: HistoryService,
    private readonly approvalService: ApprovalService,
    private readonly substituteCoachService: SubstituteCoachService,
  ) {}

  ngOnChanges(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const sessionId = this.trainingSessionId();
      const [audit, approvals, substitutions, overrides] = await Promise.all([
        this.historyService.getAuditLog(sessionId),
        this.approvalService.listHistory(sessionId),
        this.substituteCoachService.listHistory(sessionId),
        this.historyService.getConflictOverrideHistory(sessionId),
      ]);

      const events: HistoryTimelineEvent[] = [
        ...audit.map((e) => this.mapAudit(e)),
        ...approvals.map((a) => ({
          type: 'Approval' as const,
          title: getApprovalActionLabel(a.actionType),
          detail: a.reason,
          actionByUserId: a.actionByUserId,
          actionDate: a.actionDate,
        })),
        ...substitutions.map((s) => ({
          type: 'Substitution' as const,
          title: 'เปลี่ยนโค้ชตัวแทน',
          detail: `${s.originalCoachCode} — ${s.originalCoachName} → ${s.substituteCoachCode} — ${s.substituteCoachName} (เหตุผล: ${s.reason})`,
          actionByUserId: s.actionByUserId,
          actionDate: s.actionDate,
        })),
        ...overrides.map((e) => this.mapConflictOverride(e)),
      ];

      events.sort((a, b) => b.actionDate.localeCompare(a.actionDate));
      this.events.set(events);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  /** Lets the host page refresh the timeline right after it triggers a new event. */
  reload(): Promise<void> {
    return this.load();
  }

  private mapAudit(entry: AuditLogEntry): HistoryTimelineEvent {
    return {
      type: 'Audit',
      title: AUDIT_ACTION_LABELS_TH[entry.action] ?? entry.action,
      detail: entry.newValue,
      actionByUserId: entry.actionByUserId,
      actionDate: entry.actionDate,
    };
  }

  private mapConflictOverride(entry: ConflictOverrideHistoryEntry): HistoryTimelineEvent {
    return {
      type: 'ConflictOverride',
      title: 'ยืนยันดำเนินการทั้งที่มีตารางทับซ้อน',
      detail: `${CONFLICT_TYPE_LABELS_TH[entry.conflictType]} — เหตุผล: ${entry.reason}`,
      actionByUserId: entry.actionByUserId,
      actionDate: entry.actionDate,
    };
  }
}
