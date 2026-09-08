import { TrainingType } from './training-session.model';

/** One coach's session load for today, with identifiers to open related records (FR-ADASH-004). */
export interface CoachTeachingToday {
  coachId: number;
  coachNickname: string;
  coachColorHex: string;
  trainingType: TrainingType;
  sessionCount: number;
  trainingSessionIds: number[];
}

export interface AthleteAttendanceSummaryItem {
  athleteId: number;
  athleteName: string;
  attendanceCount: number;
}

export interface AttendanceByTrainingType {
  totalAttendance: number;
  athletes: AthleteAttendanceSummaryItem[];
  dailySummaries: DailyAttendanceSummary[];
}

export interface DailyAttendanceSummary {
  date: string;
  totalAttendance: number;
  attendances: DailyAttendanceCell[];
  coaches: DailyAttendanceCoach[];
}

export interface DailyAttendanceCell {
  athleteId: number;
  attendanceCount: number;
}

export interface DailyAttendanceCoach {
  coachId: number;
  coachNickname: string;
  coachColorHex: string;
}

export interface AttendanceSummary {
  routine: AttendanceByTrainingType;
  private: AttendanceByTrainingType;
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
}

/** An unset date range defaults to today on the backend, matching "Training Sessions Today". */
export interface AdministratorDashboardFilter {
  startDate: string | null;
  endDate: string | null;
  coachId: number | null;
  trainingType: TrainingType | null;
}
