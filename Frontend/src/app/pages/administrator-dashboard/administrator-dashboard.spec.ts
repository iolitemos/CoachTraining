import { describe, expect, it } from 'vitest';
import { AttendanceByTrainingType } from '../../models/administrator-dashboard.model';
import { AdministratorDashboard } from './administrator-dashboard';

describe('AdministratorDashboard', () => {
  it('renders one smile emoji for each attendance occurrence', () => {
    const component = new AdministratorDashboard(null!, null!, null!, null!);

    expect(component.attendanceEmoji(0)).toBe('');
    expect(component.attendanceEmoji(1)).toBe('😊');
    expect(component.attendanceEmoji(2)).toBe('😊😊');
    expect(component.attendanceEmoji(3)).toBe('😊😊😊');
  });

  it('uses the number of distinct dates with a teaching coach as the progress maximum', () => {
    const component = new AdministratorDashboard(null!, null!, null!, null!);
    const summary: AttendanceByTrainingType = {
      totalAttendance: 0,
      athletes: [],
      dailySummaries: [
        {
          date: '2026-09-01',
          totalAttendance: 0,
          attendances: [],
          coaches: [{ coachId: 1, coachNickname: 'A', coachColorHex: '#000000' }],
        },
        {
          date: '2026-09-02',
          totalAttendance: 0,
          attendances: [],
          coaches: [{ coachId: 2, coachNickname: 'B', coachColorHex: '#000000' }],
        },
        {
          date: '2026-09-03',
          totalAttendance: 0,
          attendances: [],
          coaches: [],
        },
      ],
    };

    expect(component.teachingDayCount(summary)).toBe(2);
  });
});
