import { describe, expect, it } from 'vitest';
import { PublicRoutineCalendar } from './public-routine-calendar';

describe('PublicRoutineCalendar', () => {
  it('uses distinct displayed teaching dates as the attendance progress maximum', () => {
    const route = { snapshot: { paramMap: { get: () => 'token' } } };
    const component = new PublicRoutineCalendar(route as never, null!);
    component.items.set([
      { trainingDate: '2026-09-01', startTime: '09:00:00', endTime: '10:00:00', coachCode: 'C001', coachNickname: 'A', coachColorHex: '#000000', latestUpdate: '2026-09-01T00:00:00Z' },
      { trainingDate: '2026-09-01', startTime: '13:00:00', endTime: '14:00:00', coachCode: 'C002', coachNickname: 'B', coachColorHex: '#000000', latestUpdate: '2026-09-01T00:00:00Z' },
      { trainingDate: '2026-09-03', startTime: '09:00:00', endTime: '10:00:00', coachCode: 'C001', coachNickname: 'A', coachColorHex: '#000000', latestUpdate: '2026-09-01T00:00:00Z' },
    ]);

    expect(component.teachingDayCount()).toBe(2);
  });

  it('returns only athlete nicknames recorded for the selected date', () => {
    const route = { snapshot: { paramMap: { get: () => 'token' } } };
    const component = new PublicRoutineCalendar(route as never, null!);
    component.selectedDate.set('2026-09-02');
    component.dailyAttendance.set([
      { trainingDate: '2026-09-01', athleteNicknames: ['หนึ่ง'] },
      { trainingDate: '2026-09-02', athleteNicknames: ['สอง', 'สาม'] },
    ]);

    expect(component.selectedAthleteNicknames()).toEqual(['สอง', 'สาม']);
  });

  it('returns planned athlete nicknames and count for each selected calendar date', () => {
    const route = { snapshot: { paramMap: { get: () => 'token' } } };
    const component = new PublicRoutineCalendar(route as never, null!);
    component.visibleMonth.set(new Date(2026, 8, 1));
    component.selectedDate.set('2026-09-02');
    component.dailyPlannedAttendance.set([
      { trainingDate: '2026-09-01', athleteNicknames: ['หนึ่ง'] },
      { trainingDate: '2026-09-02', athleteNicknames: ['สอง', 'สาม'] },
    ]);

    expect(component.selectedPlannedAthleteNicknames()).toEqual(['สอง', 'สาม']);
    expect(component.calendarDays().find(day => day.isoDate === '2026-09-02')?.plannedAthleteCount).toBe(2);
  });
});
