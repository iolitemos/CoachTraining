import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { AppRole } from '../models/auth.model';
import { AuthService } from '../services/auth.service';
import { homeRedirectGuard } from './home-redirect.guard';

describe('homeRedirectGuard', () => {
  const authService = { hasRole: vi.fn() };
  const router = { createUrlTree: vi.fn((commands: string[]) => ({ commands }) as unknown as UrlTree) };

  beforeEach(() => {
    authService.hasRole.mockReset();
    router.createUrlTree.mockClear();
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('redirects Administrator or Management Viewer to the administrator dashboard', () => {
    authService.hasRole.mockImplementation((...roles: string[]) => roles.includes(AppRole.Administrator));

    const result = TestBed.runInInjectionContext(() => homeRedirectGuard({} as never, {} as never));

    expect(result).toEqual({ commands: ['/admin-dashboard'] });
  });

  it('redirects Coach to Coach Home', () => {
    authService.hasRole.mockImplementation((...roles: string[]) => roles.includes(AppRole.Coach));

    const result = TestBed.runInInjectionContext(() => homeRedirectGuard({} as never, {} as never));

    expect(result).toEqual({ commands: ['/coach/home'] });
  });

  it('allows an account without a supported role to view the fallback', () => {
    authService.hasRole.mockReturnValue(false);

    const result = TestBed.runInInjectionContext(() => homeRedirectGuard({} as never, {} as never));

    expect(result).toBe(true);
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });
});
