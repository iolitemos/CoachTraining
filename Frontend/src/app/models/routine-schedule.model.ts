/** Routine Training Management (requirement.md 4.4, FR-ROUTINE-001–009).
 * Deliberately has no Group, Team, or Location field — see CLAUDE.md 4.2. */

export const DAY_OF_WEEK_OPTIONS = [
  { value: 'Monday', label: 'จันทร์' },
  { value: 'Tuesday', label: 'อังคาร' },
  { value: 'Wednesday', label: 'พุธ' },
  { value: 'Thursday', label: 'พฤหัสบดี' },
  { value: 'Friday', label: 'ศุกร์' },
  { value: 'Saturday', label: 'เสาร์' },
  { value: 'Sunday', label: 'อาทิตย์' },
] as const;

const DAY_OF_WEEK_LABELS: Record<string, string> = Object.fromEntries(
  DAY_OF_WEEK_OPTIONS.map((option) => [option.value, option.label]),
);

export function getDayOfWeekLabel(dayOfWeek: string): string {
  return DAY_OF_WEEK_LABELS[dayOfWeek] ?? dayOfWeek;
}

export interface RoutineScheduleListItem {
  routineScheduleId: number;
  name: string;
  coachId: number;
  coachCode: string;
  coachFullName: string;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
  isActive: boolean;
}

export interface RoutineScheduleDetail {
  routineScheduleId: number;
  name: string;
  coachId: number;
  coachCode: string;
  coachFullName: string;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
  recurrencePattern: string;
  isActive: boolean;
  remarks: string | null;
}

export interface RoutineScheduleSaveRequest {
  name: string | null;
  coachId: number;
  dayOfWeek: string;
  startTime: string;
  endTime: string;
  effectiveStartDate: string;
  effectiveEndDate: string | null;
  recurrencePattern: string | null;
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
