/** Routine Training Management (requirement.md 4.4, FR-ROUTINE-001–009).
 * Deliberately has no Group, Team, or Location field — see CLAUDE.md 4.2. */

const DAY_OF_WEEK_LABELS = ['อาทิตย์', 'จันทร์', 'อังคาร', 'พุธ', 'พฤหัสบดี', 'ศุกร์', 'เสาร์'];

export function getDayOfWeekLabelFromDate(date: string): string {
  return DAY_OF_WEEK_LABELS[getLocalDateDayOfWeek(date)];
}

export function getRoutineScheduleDisplayName(
  schedule: Pick<RoutineScheduleListItem, 'effectiveStartDate' | 'startTime' | 'endTime'>,
): string {
  return `ฝึกซ้อมวันที่ ${schedule.effectiveStartDate} ${schedule.startTime.slice(0, 5)}-${schedule.endTime.slice(0, 5)}`;
}

export function getLocalDateDayOfWeek(date: string): number {
  const [year, month, day] = date.split('-').map(Number);
  return new Date(year, month - 1, day).getDay();
}

export interface RoutineScheduleListItem {
  routineScheduleId: number;
  coachId: number;
  coachCode: string;
  coachFullName: string;
  coachNickname: string | null;
  coachColorHex: string;
  startTime: string;
  endTime: string;
  effectiveStartDate: string;
  isActive: boolean;
}

export interface RoutineScheduleDetail {
  routineScheduleId: number;
  coachId: number;
  coachCode: string;
  coachFullName: string;
  startTime: string;
  endTime: string;
  effectiveStartDate: string;
  isActive: boolean;
  remarks: string | null;
}

export interface RoutineScheduleSaveRequest {
  coachId: number;
  startTime: string;
  endTime: string;
  effectiveStartDate: string;
  remarks: string | null;
}

export interface RoutineTemplateConflictDetail {
  conflictingRoutineScheduleId: number;
  conflictingRoutineScheduleName: string;
  message: string;
}

export interface RoutineScheduleSaveResult {
  schedule: RoutineScheduleDetail | null;
  error: string | null;
  conflicts: RoutineTemplateConflictDetail[];
}
