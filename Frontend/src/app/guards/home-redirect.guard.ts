import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AppRole } from '../models/auth.model';
import { AuthService } from '../services/auth.service';

/** Redirects signed-in users to the dashboard with the highest authorized role. */
export const homeRedirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.hasRole(AppRole.Administrator, AppRole.ManagementViewer)) {
    return router.createUrlTree(['/admin-dashboard']);
  }

  if (authService.hasRole(AppRole.Coach)) {
    return router.createUrlTree(['/coach/home']);
  }

  // Accounts without a supported role remain on the fallback Home page.
  return true;
};
