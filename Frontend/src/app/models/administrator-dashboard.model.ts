import { TrainingType } from './training-session.model';

/** One coach's session load for today, with identifiers to open related records (FR-ADASH-004). */
export interface CoachTeachingToday {
  coachId: number;
  coachCode: string;
  coachFullName: string;
  sessionCount: number;
  trainingSessionIds: number[];
}

/** Aggregate athlete attendance counts across the filtered range. */
export interface AttendanceSummary {
  presentCount: number;
  absentCount: number;
  lateCount: number;
  excusedCount: number;
}

/** One coach's teaching-hour totals across the filtered range. */
export interface CoachTeachingHours {
  coachId: number;
  coachCode: string;
  coachFullName: string;
  routineHours: number;
  privateHours: number;
  totalHours: number;
}

/** Administrator Dashboard response (requirement.md 9, FR-ADASH-001–004, todo.md 4.17/5.14). */
export interface AdministratorDashboardResponse {
  sessionsTodayCount: number;
  completedCount: number;
  upcomingCount: number;
  cancelledCount: number;
  routineCount: number;
  privateCount: number;
  coachesTeachingToday: CoachTeachingToday[];
  attendanceSummary: AttendanceSummary;
  coachTeachingHours: CoachTeachingHours[];
}

/** An unset date range defaults to today on the backend, matching "Training Sessions Today". */
export interface AdministratorDashboardFilter {
  startDate: string | null;
  endDate: string | null;
  coachId: number | null;
  trainingType: TrainingType | null;
}
