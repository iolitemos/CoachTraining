export interface ParentRoutinePlanLinkStatus { exists: boolean; isEnabled: boolean; token: string | null; tokenHint: string | null; createdDate: string | null; }
export interface ParentRoutinePlanLinkCreated { token: string; tokenHint: string; createdDate: string; }
export interface ParentRoutinePlanDate { trainingDate: string; isSelected: boolean; }
export interface RoutineTrainingDate { routineTrainingDateId: number; trainingDate: string; }
export interface ParentRoutinePlanCalendar { athleteId: number; athleteNickname: string | null; athleteFullName: string; dates: ParentRoutinePlanDate[]; }
export interface RoutineParticipationPlanSummary { trainingDate: string; athleteCount: number; }
export interface RoutineParticipationPlanAthlete { athleteId: number; athleteCode: string; athleteName: string; athleteNickname: string | null; }
