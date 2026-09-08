import { TrainingType } from './training-session.model';

/** Union of Routine (Present, Late) and Private (Present, Absent, Late, Excused) statuses. */
export type ReportAttendanceStatus = 'Present' | 'Absent' | 'Late' | 'Excused';

/** One recorded attendance entry, for the per-athlete history (FR-RPT-ATH-001). */
export interface AthleteAttendanceRecord {
  trainingSessionId: number;
  trainingType: TrainingType;
  sessionDate: string;
  status: ReportAttendanceStatus;
  arrivalTime: string | null;
  remark: string | null;
}

/** One athlete's attendance summary and history for the filtered range. */
export interface AthleteAttendanceReportItem {
  athleteId: number;
  athleteCode: string;
  fullName: string;
  /** Routine supports Present/Late only (FR-RATT-005) — recorded attendance only,
   * never inferred (FR-RPT-ATH-006). */
  routinePresentCount: number;
  routineLateCount: number;
  /** Private supports the full status set (FR-RPT-ATH-005). */
  privatePresentCount: number;
  privateAbsentCount: number;
  privateLateCount: number;
  privateExcusedCount: number;
  records: AthleteAttendanceRecord[];
}

/** Athlete Attendance Report response (requirement.md 6.19, todo.md 4.19/5.16). */
export interface AthleteAttendanceReportResponse {
  items: AthleteAttendanceReportItem[];
}

export interface AthleteAttendanceReportFilter {
  athleteId: number | null;
  startDate: string | null;
  endDate: string | null;
}
