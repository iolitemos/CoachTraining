import { TrainingSessionStatus } from './training-session-status.model';

/** Private Training Management (requirement.md 4.5, FR-PRIVATE-001–009). */
export interface PrivateSessionListItem {
  trainingSessionId: number;
  sessionDate: string;
  startTime: string;
  endTime: string;
  coachCode: string;
  coachFullName: string;
  coachNickname: string | null;
  coachColorHex: string;
  location: string | null;
  status: TrainingSessionStatus;
  athleteCount: number;
}

export interface PrivateSessionAthlete {
  athleteId: number;
  athleteCode: string;
  fullName: string;
}

export interface PrivateSessionDetail {
  trainingSessionId: number;
  sessionDate: string;
  startTime: string;
  endTime: string;
  coachId: number;
  coachCode: string;
  coachFullName: string;
  location: string | null;
  remarks: string | null;
  status: TrainingSessionStatus;
  athletes: PrivateSessionAthlete[];
}

export interface PrivateSessionSaveRequest {
  coachId: number;
  sessionDate: string;
  startTime: string;
  endTime: string;
  location: string | null;
  remarks: string | null;
  athleteIds: number[];
}
