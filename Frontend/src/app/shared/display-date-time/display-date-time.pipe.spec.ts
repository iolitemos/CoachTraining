import { DisplayDateTimePipe } from './display-date-time.pipe';

describe('DisplayDateTimePipe', () => {
  const pipe = new DisplayDateTimePipe();

  it('converts a UTC timestamp to Bangkok time', () => {
    expect(pipe.transform('2026-09-08T04:43:00Z')).toBe('08 ก.ย. 2026 11:43');
  });

  it('treats timezone-less API audit timestamps as UTC', () => {
    expect(pipe.transform('2026-09-08T04:43:00')).toBe('08 ก.ย. 2026 11:43');
  });

  it('returns a placeholder for an empty value', () => {
    expect(pipe.transform(null)).toBe('-');
  });
});
