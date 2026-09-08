/** General-purpose audit trail entry (FR-AUDIT-001–003) — cancellation and
 * rescheduling events land here today; other event types are covered by
 * their own dedicated history sources (Approval, Substitution, below). */
export interface AuditLogEntry {
  auditLogId: number;
  entityName: string;
  entityId: number;
  action: string;
  previousValue: string | null;
  newValue: string | null;
  actionByUserId: number;
  actionDate: string;
}

/** requirement.md FR-CONFLICT-001–003. */
export type ConflictType = 'CoachOverlap' | 'AthleteOverlap' | 'PrivateVsRoutineOverlap';

/** One recorded schedule-conflict override (FR-CONFLICT-005). */
export interface ConflictOverrideHistoryEntry {
  conflictOverrideHistoryId: number;
  conflictType: ConflictType;
  trainingSessionId: number | null;
  routineScheduleId: number | null;
  reason: string;
  actionByUserId: number;
  actionDate: string;
}

/** One entry in the unified session history timeline (todo.md 5.17), merged
 * from Audit, Approval, Substitution, and Conflict-Override history. */
export type HistoryEventType = 'Audit' | 'Approval' | 'Substitution' | 'ConflictOverride';

export interface HistoryTimelineEvent {
  type: HistoryEventType;
  title: string;
  detail: string | null;
  actionByUserId: number;
  actionDate: string;
}
