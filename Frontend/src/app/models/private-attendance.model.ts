/** FR-PATT — Private attendance supports all four statuses. */
export type PrivateAttendanceStatus = 'Present' | 'Absent' | 'Late' | 'Excused';

/**
 * One assigned athlete's attendance state. Status is null when the Coach hasn't
 * recorded it yet — the roster itself always lists every assigned athlete
 * (FR-PATT-001), so a null status is how "missing" is identified (requirement.md 9.4).
 */
export interface PrivateAttendanceRosterItem {
  privateSessionAthleteId: number;
  athleteId: number | null;
  isGuest: boolean;
  athleteCode: string;
  fullName: string;
  guestPhone: string | null;
  attendanceId: number | null;
  status: PrivateAttendanceStatus | null;
  arrivalTime: string | null;
  remark: string | null;
  recordedDate: string | null;
}

export interface PrivateAttendanceRoster {
  athletes: PrivateAttendanceRosterItem[];
  /** True once every assigned athlete has a recorded status (FR-PATT-002). */
  isComplete: boolean;
}

export interface PrivateAttendanceSetRequest {
  status: PrivateAttendanceStatus;
  arrivalTime: string | null;
  remark: string | null;
}
