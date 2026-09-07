/** Coach Management (requirement.md 4.2, FR-COACH-001–004). */
export interface CoachListItem {
  coachId: number;
  coachCode: string;
  fullName: string;
  nickname: string | null;
  phoneNumber: string | null;
  email: string | null;
  coachType: string | null;
  isActive: boolean;
  linkedUsername: string | null;
}

export interface CoachDetail {
  coachId: number;
  coachCode: string;
  fullName: string;
  nickname: string | null;
  phoneNumber: string | null;
  email: string | null;
  coachType: string | null;
  specialization: string | null;
  isActive: boolean;
  remarks: string | null;
  /** Read-only — the link itself is managed from User & Role Management. */
  linkedUserId: number | null;
  linkedUsername: string | null;
}

export interface CoachCreateRequest {
  coachCode: string;
  fullName: string;
  nickname: string | null;
  phoneNumber: string | null;
  email: string | null;
  coachType: string | null;
  specialization: string | null;
  remarks: string | null;
}

export type CoachUpdateRequest = Omit<CoachCreateRequest, 'coachCode'>;

/** Active-coach-only option for selectors (Routine/Private Training coach selection, etc.). */
export interface CoachOption {
  coachId: number;
  coachCode: string;
  fullName: string;
}
