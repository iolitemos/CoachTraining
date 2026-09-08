import { TrainingSessionDetail } from './training-session.model';

/**
 * New date/time for a replacement session (FR-CR-004–007). The coach
 * assignment itself never changes here — reassigning who teaches is
 * Substitute Coach's concern, not Rescheduling's.
 */
export interface RescheduleSessionRequest {
  sessionDate: string;
  startTime: string;
  endTime: string;
  remarks: string | null;
  /** FR-CONFLICT-004 — confirms proceeding despite a detected coach/athlete conflict. */
  overrideConflict: boolean;
  /** Required when overrideConflict is true. */
  overrideReason: string | null;
}

export interface RescheduleResponse {
  /** The original session, now in Rescheduled status (FR-SESSION-010, FR-CR-006). */
  originalSession: TrainingSessionDetail;
  /** The new session created for the replacement date/time, linked back via originalSessionId. */
  replacementSession: TrainingSessionDetail;
}
