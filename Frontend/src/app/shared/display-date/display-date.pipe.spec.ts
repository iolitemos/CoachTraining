import { DisplayDatePipe } from './display-date.pipe';

describe('DisplayDatePipe', () => {
  const pipe = new DisplayDatePipe();

  it('formats date-only values as dd MMM yyyy using Gregorian year', () => {
    expect(pipe.transform('2026-09-08')).toBe('08 ก.ย. 2026');
  });

  it('formats the date portion of date-time values without timezone conversion', () => {
    expect(pipe.transform('2026-01-02T23:30:00Z')).toBe('02 ม.ค. 2026');
  });

  it('shows a dash for missing values', () => {
    expect(pipe.transform(null)).toBe('-');
  });
});
