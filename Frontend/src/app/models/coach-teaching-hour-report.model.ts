import { TrainingType } from './training-session.model';

/** One coach's teaching-hour totals for the filtered range (FR-RPT-COACH-001–009). */
export interface CoachTeachingHourReportItem {
  coachId: number;
  coachCode: string;
  coachFullName: string;
  sessionCount: number;
  routineHours: number;
  privateHours: number;
  totalHours: number;
}

/** Coach Teaching-Hour Report response (requirement.md 6.18, todo.md 4.18/5.15). */
export interface CoachTeachingHourReportResponse {
  items: CoachTeachingHourReportItem[];
  totalRoutineHours: number;
  totalPrivateHours: number;
  grandTotalHours: number;
}

export interface CoachTeachingHourReportFilter {
  coachId: number | null;
  startDate: string | null;
  endDate: string | null;
  trainingType: TrainingType | null;
}
