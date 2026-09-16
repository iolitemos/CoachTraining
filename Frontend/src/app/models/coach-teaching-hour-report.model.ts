import { TrainingType } from './training-session.model';

/** One coach's distinct teaching-day totals for the filtered range. */
export interface CoachTeachingHourReportItem {
  coachId: number;
  coachCode: string;
  coachFullName: string;
  coachNickname: string | null;
  coachColorHex: string;
  sessionCount: number;
  plannedSessionCount: number;
  actualSessionCount: number;
  plannedDays: number;
  actualDays: number;
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
  totalPlannedDays: number;
  totalActualDays: number;
  competitionAssignments: CoachCompetitionAssignment[];
  totalCompetitionAssignments: number;
  totalCompetitionDays: number;
}

export interface CoachCompetitionAssignment {
  coachId: number;
  coachCode: string;
  coachFullName: string;
  coachNickname: string | null;
  coachColorHex: string;
  competitionCount: number;
  assignedDays: number;
  competitions: CoachCompetitionDetail[];
}

export interface CoachCompetitionDetail {
  competitionMatchId: number;
  name: string;
  province: string;
  startDate: string;
  endDate: string;
  assignedDays: number;
}

export interface CoachTeachingHourReportFilter {
  coachId: number | null;
  startDate: string | null;
  endDate: string | null;
  trainingType: TrainingType | null;
}
