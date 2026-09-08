import { TrainingSessionStatus } from './training-session-status.model';

/** requirement.md FR-SESSION-002 — every session is exactly one of these. */
export type TrainingType = 'Routine' | 'Private';

export interface TrainingSessionAthlete {
  athleteId: number;
  athleteCode: string;
  fullName: string;
}

/** Null trainingLogId means no log has been saved for the session yet (FR-LOG-003). */
export interface TrainingLog {
  trainingLogId: number | null;
  topic: string | null;
  objective: string | null;
  exerciseDrill: string | null;
  focus: string | null;
  intensity: string | null;
  coachNotes: string | null;
  athleteNotes: string | null;
  generalRemarks: string | null;
  updatedDate: string | null;
}

export type TrainingLogUpsertRequest = Omit<TrainingLog, 'trainingLogId' | 'updatedDate'>;

/** Coach Session detail (requirement.md 9.2, FR-SESSION-001–010). */
export interface TrainingSessionDetail {
  trainingSessionId: number;
  trainingType: TrainingType;
  routineScheduleId: number | null;
  sessionDate: string;
  scheduledStartDateTime: string;
  scheduledEndDateTime: string;
  actualStartDateTime: string | null;
  actualEndDateTime: string | null;
  /** FR-TEACH-003 — computed from Actual Start/End, not stored. */
  actualDurationMinutes: number | null;
  assignedCoachId: number;
  assignedCoachCode: string;
  assignedCoachName: string;
  assignedCoachNickname: string | null;
  assignedCoachColorHex: string;
  actualCoachId: number | null;
  actualCoachCode: string | null;
  actualCoachName: string | null;
  actualCoachNickname: string | null;
  actualCoachColorHex: string | null;
  status: TrainingSessionStatus;
  location: string | null;
  remarks: string | null;
  cancellationReason: string | null;
  originalSessionId: number | null;
  isConflictOverridden: boolean;
  conflictOverrideReason: string | null;
  /** Populated only for Private Training (Routine has no fixed roster). */
  athletes: TrainingSessionAthlete[];
  trainingLog: TrainingLog | null;
}

/** Row shape for the unified Training Session list (todo.md 4.5/5.13). */
export interface TrainingSessionListItem {
  trainingSessionId: number;
  routineScheduleId: number | null;
  trainingType: TrainingType;
  sessionDate: string;
  scheduledStartDateTime: string;
  scheduledEndDateTime: string;
  actualStartDateTime: string | null;
  actualEndDateTime: string | null;
  assignedCoachCode: string;
  assignedCoachName: string;
  assignedCoachNickname: string | null;
  assignedCoachColorHex: string;
  actualCoachCode: string | null;
  actualCoachName: string | null;
  actualCoachNickname: string | null;
  actualCoachColorHex: string | null;
  status: TrainingSessionStatus;
  location: string | null;
}

/** Filters for the unified Training Session list — by coach, type, status, date range. */
export interface TrainingSessionFilter {
  page: number;
  pageSize: number;
  coachId: number | null;
  trainingType: TrainingType | null;
  status: TrainingSessionStatus | null;
  dateFrom: string | null;
  dateTo: string | null;
}
