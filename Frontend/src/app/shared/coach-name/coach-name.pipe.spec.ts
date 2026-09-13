import { CoachNamePipe, formatCoachName } from './coach-name.pipe';

describe('CoachNamePipe', () => {
  const pipe = new CoachNamePipe();

  it('adds the coach title to a name', () => {
    expect(pipe.transform('มอส')).toBe('โค้ชมอส');
  });

  it('does not add the coach title twice', () => {
    expect(pipe.transform('โค้ช มอส')).toBe('โค้ชมอส');
  });

  it('uses the requested fallback for a missing name', () => {
    expect(formatCoachName(null, 'ไม่ระบุชื่อโค้ช')).toBe('ไม่ระบุชื่อโค้ช');
  });
});
