import { TrainingSessionStatus } from './training-session-status.model';
import { TrainingType } from './training-session.model';

/** One session row shown on the Coach Home dashboard (FR-CDASH-001–003). */
export interface CoachDashboardSession {
  trainingSessionId: number;
  trainingType: TrainingType;
  sessionDate: string;
  scheduledStartDateTime: string;
  scheduledEndDateTime: string;
  status: TrainingSessionStatus;
  /** Thai label for the action the Coach still needs to take, or null when the
   * session is finalized / needs no Coach action (FR-CDASH-003). */
  requiredNextAction: string | null;
}

/** Coach Home dashboard response (requirement.md 9.1, todo.md 4.16/5.5). */
export interface CoachDashboardResponse {
  todaySessions: CoachDashboardSession[];
  upcomingSessions: CoachDashboardSession[];
  /** Counts below are scoped to the current calendar month. */
  completedSessionCount: number;
  remainingSessionCount: number;
  routineSessionCount: number;
  privateSessionCount: number;
  /** Sessions still needing a Coach action (FR-TEACH-006). */
  pendingActionCount: number;
  monthlyTeachingHours: number;
}
