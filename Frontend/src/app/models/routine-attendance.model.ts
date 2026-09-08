/** FR-RATT-005/006 — Routine attendance only supports Present and Late. */
export type RoutineAttendanceStatus = 'Present' | 'Late';

export interface RoutineAttendanceItem {
  attendanceId: number;
  athleteId: number;
  athleteCode: string;
  fullName: string;
  nickname: string | null;
  status: RoutineAttendanceStatus;
  arrivalTime: string | null;
  remark: string | null;
  recordedDate: string;
}

export interface RoutineAttendanceCreateRequest {
  athleteId: number;
  status: RoutineAttendanceStatus;
  arrivalTime: string | null;
  remark: string | null;
}

export type RoutineAttendanceUpdateRequest = Omit<RoutineAttendanceCreateRequest, 'athleteId'>;
