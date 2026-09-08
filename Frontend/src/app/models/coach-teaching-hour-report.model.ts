import { TrainingType } from './training-session.model';

/** One coach's distinct teaching-day totals for the filtered range. */
export interface CoachTeachingHourReportItem {
  coachId: number;
  coachCode: string;
  coachFullName: string;
  coachNickname: string | null;
  coachColorHex: string;
  sessionCount: number;
  routineDays: number;
  privateDays: number;
  totalDays: number;
}

/** Coach Teaching-Hour Report response (requirement.md 6.18, todo.md 4.18/5.15). */
export interface CoachTeachingHourReportResponse {
  items: CoachTeachingHourReportItem[];
  totalRoutineDays: number;
  totalPrivateDays: number;
  grandTotalDays: number;
}

export interface CoachTeachingHourReportFilter {
  coachId: number | null;
  startDate: string | null;
  endDate: string | null;
  trainingType: TrainingType | null;
}
