import { formatCoachName } from '../shared/coach-name/coach-name.pipe';

/** Coach Management (requirement.md 4.2, FR-COACH-001–004). */
export interface CoachListItem {
  coachId: number;
  coachCode: string;
  fullName: string;
  nickname: string | null;
  colorHex: string;
  phoneNumber: string | null;
  email: string | null;
  isActive: boolean;
  linkedUsername: string | null;
}

export interface CoachDetail {
  coachId: number;
  coachCode: string;
  fullName: string;
  nickname: string | null;
  colorHex: string;
  phoneNumber: string | null;
  email: string | null;
  /** Reference data for teaching-compensation payment only — no rate/payment processing. */
  bankName: string | null;
  bankAccountNumber: string | null;
  bankAccountName: string | null;
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
  colorHex: string;
  phoneNumber: string | null;
  email: string | null;
  bankName: string | null;
  bankAccountNumber: string | null;
  bankAccountName: string | null;
  remarks: string | null;
}

export type CoachUpdateRequest = Omit<CoachCreateRequest, 'coachCode'>;

/** Active-coach-only option for selectors (Routine/Private Training coach selection, etc.). */
export interface CoachOption {
  coachId: number;
  coachCode: string;
  fullName: string;
  nickname: string | null;
  colorHex: string;
}

/** Compact selector label: "nickname - full name", falling back to full name. */
export function coachPickerLabel(coach: Pick<CoachOption, 'nickname' | 'fullName'>): string {
  const nickname = coach.nickname?.trim();
  return nickname
    ? `${formatCoachName(nickname)} - ${coach.fullName}`
    : formatCoachName(coach.fullName);
}
