export interface PublicRoutineCalendarItem {
  trainingDate: string;
  startTime: string;
  endTime: string;
  coachCode: string;
  coachNickname: string;
  coachColorHex: string;
  latestUpdate: string;
}

export interface PublicCompetitionMatch {
  name: string;
  province: string;
  startDate: string;
  endDate: string;
}

export interface PublicCalendarNote {
  noteDate: string;
  content: string;
  latestUpdate: string;
}

export interface PublicRoutineAttendanceItem {
  athleteName: string;
  attendanceCount: number;
}

export interface PublicRoutineCalendarData {
  schedules: PublicRoutineCalendarItem[];
  competitionMatches: PublicCompetitionMatch[];
  notes: PublicCalendarNote[];
  attendanceSummary: PublicRoutineAttendanceItem[];
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
