import { describe, expect, it } from 'vitest';
import { AthleteAttendanceReportItem } from '../../../models/athlete-attendance-report.model';
import { AthleteAttendanceReport } from './athlete-attendance-report';

function reportItem(overrides: Partial<AthleteAttendanceReportItem>): AthleteAttendanceReportItem {
  return {
    athleteId: 1,
    isGuest: false,
    participantKey: 'name-athlete',
    guestPhone: null,
    athleteCode: 'A001',
    fullName: 'Athlete',
    nickname: 'นักกีฬา',
    routinePlannedDateCount: 0,
    routineAttendanceCount: 0,
    privateAttendanceCount: 0,
    records: [],
    ...overrides,
  };
}

describe('AthleteAttendanceReport', () => {
  it('separates Routine and Private report items and keeps plan-only athletes in Routine', () => {
    const component = new AthleteAttendanceReport(null!, null!, null!, null!);
    const routinePlanOnly = reportItem({ participantKey: 'routine-plan', routinePlannedDateCount: 2 });
    const privateOnly = reportItem({ participantKey: 'private', athleteId: 2, privateAttendanceCount: 1 });
    component.report.set({ items: [routinePlanOnly, privateOnly] });

    expect(component.visibleItems().map((item) => item.participantKey)).toEqual(['routine-plan']);

    component.selectTrainingType('Private');

    expect(component.visibleItems().map((item) => item.participantKey)).toEqual(['private']);
  });

  it('shows only the selected training type in attendance calendar days', () => {
    const component = new AthleteAttendanceReport(null!, null!, null!, null!);
    component.calendarMonth.set(new Date(2026, 0, 1));
    component.calendarAthlete.set(reportItem({
      records: [
        { trainingSessionId: 1, trainingType: 'Routine', sessionDate: '2026-01-05', status: 'Present', arrivalTime: null, remark: null },
        { trainingSessionId: 2, trainingType: 'Private', sessionDate: '2026-01-05', status: 'Present', arrivalTime: null, remark: null },
      ],
    }));

    expect(component.calendarDays().find((day) => day.isoDate === '2026-01-05')?.records.map((record) => record.trainingType)).toEqual(['Routine']);

    component.activeTrainingType.set('Private');

    expect(component.calendarDays().find((day) => day.isoDate === '2026-01-05')?.records.map((record) => record.trainingType)).toEqual(['Private']);
  });
});
