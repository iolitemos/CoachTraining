import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AppRole } from '../models/auth.model';
import { AuthService } from '../services/auth.service';

/**
 * Restricts a route to specific roles, e.g.
 * `{ path: 'users', canActivate: [roleGuard], data: { roles: [AppRole.Administrator] } }`.
 * Backend authorization remains mandatory regardless of this guard (CLAUDE.md section 11).
 */
export const roleGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const requiredRoles = (route.data['roles'] as AppRole[] | undefined) ?? [];

  if (requiredRoles.length === 0 || authService.hasRole(...requiredRoles)) {
    return true;
  }

  return router.createUrlTree(['/access-denied']);
};
