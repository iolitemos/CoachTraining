import { Pipe, PipeTransform } from '@angular/core';

/** Adds the Thai coach title for display without changing stored coach names. */
export function formatCoachName(value: string | null | undefined, fallback = '-'): string {
  const name = value?.trim();
  if (!name || name === '-') {
    return fallback;
  }

  return `โค้ช${name.replace(/^โค้ช\s*/, '')}`;
}

@Pipe({ name: 'coachName', standalone: true })
export class CoachNamePipe implements PipeTransform {
  transform(value: string | null | undefined, fallback = '-'): string {
    return formatCoachName(value, fallback);
  }
}
