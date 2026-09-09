/** Athlete Management (requirement.md 4.3). */
export type AthleteType = 'Affiliated' | 'General';

export interface AthleteListItem {
  athleteId: number;
  athleteCode: string;
  athleteType: AthleteType;
  fullName: string;
  nickname: string | null;
  dateOfBirth: string | null;
  athleteLevel: string | null;
  isActive: boolean;
}

export interface AthleteDetail {
  athleteId: number;
  athleteCode: string;
  athleteType: AthleteType;
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
  athleteType: AthleteType;
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

export function athleteTypeLabel(type: AthleteType): string {
  return type === 'Affiliated' ? 'นักกีฬาในสังกัด' : 'นักกีฬาทั่วไป';
}

/** Compact label for athlete pickers: "nickname - full name", with a safe fallback. */
export function athletePickerLabel(athlete: Pick<AthleteOption, 'nickname' | 'fullName'>): string {
  if (athlete.nickname?.trim()) {
    return `${athlete.nickname.trim()} - ${athlete.fullName}`;
  }

  return athlete.fullName;
}
