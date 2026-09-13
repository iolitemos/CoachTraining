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
  privateSessionAthleteId: number;
  athleteId: number | null;
  isGuest: boolean;
  athleteCode: string;
  fullName: string;
  guestPhone: string | null;
  guestRemark: string | null;
  clientKey?: string;
}

export interface GuestParticipantRequest {
  fullName: string;
  phone: string | null;
  remark: string | null;
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
  guestParticipants: GuestParticipantRequest[];
}

export interface PrivateSessionBatchCreateRequest extends Omit<PrivateSessionSaveRequest, 'sessionDate'> {
  startDate: string;
  endDate: string;
  daysOfWeek: number[];
}

export interface PrivateSessionBatchCreateResult {
  createdCount: number;
  createdDates: string[];
}
