import { convertToParamMap } from '@angular/router';
import { describe, expect, it, vi } from 'vitest';
import { AuthService } from './auth.service';
import { FilterStateService } from './filter-state.service';

describe('FilterStateService', () => {
  it('restores saved state per user and lets URL values take priority', () => {
    sessionStorage.setItem('coach-training:filters:42:athletes', JSON.stringify({ search: 'เดิม', page: 3 }));
    const router = { navigate: vi.fn().mockResolvedValue(true) };
    const auth = { currentUser: () => ({ userId: 42 }) };
    const service = new FilterStateService(auth as AuthService, router as never);

    const restored = service.restore<{ search: string; page: number }>(
      'athletes', { search: '', page: 1 }, convertToParamMap({ search: 'จากลิงก์', page: '2' }), ['page'],
    );

    expect(restored).toEqual({ search: 'จากลิงก์', page: 2 });
  });

  it('stores all state but omits session-only values from the URL', () => {
    const router = { navigate: vi.fn().mockResolvedValue(true) };
    const auth = { currentUser: () => ({ userId: 42 }) };
    const service = new FilterStateService(auth as AuthService, router as never);

    service.save('report', { athleteId: 7, athleteLabel: 'ข้อมูลส่วนบุคคล' }, {} as never, { athleteId: 7 });

    expect(sessionStorage.getItem('coach-training:filters:42:report')).toContain('ข้อมูลส่วนบุคคล');
    expect(router.navigate).toHaveBeenCalledWith([], expect.objectContaining({ queryParams: { athleteId: 7 } }));
  });
});
