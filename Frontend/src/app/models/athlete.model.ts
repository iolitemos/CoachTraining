/** Athlete Management (requirement.md 4.3). */
export interface AthleteListItem {
  athleteId: number;
  athleteCode: string;
  fullName: string;
  nickname: string | null;
  dateOfBirth: string | null;
  athleteLevel: string | null;
  isActive: boolean;
}

export interface AthleteDetail {
  athleteId: number;
  athleteCode: string;
  fullName: string;
  nickname: string | null;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  parentName: string | null;
  parentPhoneNumber: string | null;
  athleteLevel: string | null;
  joinDate: string | null;
  isActive: boolean;
  remarks: string | null;
}

export interface AthleteCreateRequest {
  athleteCode: string;
  fullName: string;
  nickname: string | null;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  parentName: string | null;
  parentPhoneNumber: string | null;
  athleteLevel: string | null;
  joinDate: string | null;
  remarks: string | null;
}

export type AthleteUpdateRequest = Omit<AthleteCreateRequest, 'athleteCode'>;

/** Active-athlete-only search result — Routine attendance selection and
 * Private Training athlete assignment (requirement.md 5.7/9.6). */
export interface AthleteOption {
  athleteId: number;
  athleteCode: string;
  fullName: string;
  nickname: string | null;
}
