import { TrainingSessionDetail } from './training-session.model';

/** FR-SUB-001–005 — assigns an active substitute coach to an eligible session. */
export interface SubstituteCoachRequest {
  substituteCoachId: number;
  reason: string;
  /** FR-CONFLICT-004 — confirms proceeding despite a detected schedule conflict. */
  overrideConflict: boolean;
  /** Required when overrideConflict is true. */
  overrideReason: string | null;
}

/** Immutable business details recorded for one substitute-coach assignment. */
export interface CoachSubstitutionHistory {
  coachSubstitutionHistoryId: number;
  trainingSessionId: number;
  originalCoachId: number;
  originalCoachCode: string;
  originalCoachName: string;
  substituteCoachId: number;
  substituteCoachCode: string;
  substituteCoachName: string;
  reason: string;
  actionByUserId: number;
  actionDate: string;
}

export interface SubstituteCoachResponse {
  session: TrainingSessionDetail;
  substitution: CoachSubstitutionHistory;
}
