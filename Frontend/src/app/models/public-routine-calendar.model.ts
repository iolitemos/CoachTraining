export interface PublicRoutineCalendarItem {
  trainingDate: string;
  startTime: string;
  endTime: string;
  coachNickname: string;
  coachColorHex: string;
}

export interface PublicCompetitionMatch {
  name: string;
  province: string;
  startDate: string;
  endDate: string;
}

export interface PublicRoutineCalendarData {
  schedules: PublicRoutineCalendarItem[];
  competitionMatches: PublicCompetitionMatch[];
}

export interface RoutineCalendarShareStatus {
  isActive: boolean;
  tokenHint: string | null;
  createdDate: string | null;
}

export interface RoutineCalendarShareCreated {
  token: string;
  tokenHint: string;
  createdDate: string;
}
