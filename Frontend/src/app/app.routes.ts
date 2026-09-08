import { Routes } from '@angular/router';
import { AppShell } from './components/layout/app-shell/app-shell';
import { AppRole } from './models/auth.model';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { Home } from './pages/home/home';
import { Login } from './pages/login/login';
import { AccessDenied } from './pages/access-denied/access-denied';
import { CoachHome } from './pages/coach-home/coach-home';
import { CoachSession } from './pages/coach-session/coach-session';

export const routes: Routes = [
  { path: 'login', component: Login },
  { path: 'access-denied', component: AccessDenied },
  {
    path: '',
    component: AppShell,
    canActivate: [authGuard],
    children: [
      { path: '', component: Home, pathMatch: 'full' },
      {
        // No guard on the parent itself — each child is scoped individually,
        // since the session detail view (unlike Coach Home) also serves
        // Administrator review/substitute/cancel/reschedule/approval actions.
        path: 'coach',
        children: [
          {
            path: 'home',
            component: CoachHome,
            pathMatch: 'full',
            canActivate: [roleGuard],
            data: { roles: [AppRole.Coach] },
          },
          {
            path: 'sessions/:id',
            component: CoachSession,
            canActivate: [roleGuard],
            data: { roles: [AppRole.Coach, AppRole.Administrator] },
          },
        ],
      },
      {
        path: 'review',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator] },
        loadChildren: () => import('./pages/review/review.routes').then((m) => m.REVIEW_ROUTES),
      },
      {
        path: 'admin-dashboard',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator, AppRole.ManagementViewer] },
        loadComponent: () =>
          import('./pages/administrator-dashboard/administrator-dashboard').then((m) => m.AdministratorDashboard),
      },
      {
        path: 'reports/coach-teaching-hours',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator, AppRole.ManagementViewer] },
        loadComponent: () =>
          import('./pages/reports/coach-teaching-hour-report/coach-teaching-hour-report').then(
            (m) => m.CoachTeachingHourReport,
          ),
      },
      {
        path: 'reports/athlete-attendance',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator, AppRole.ManagementViewer] },
        loadComponent: () =>
          import('./pages/reports/athlete-attendance-report/athlete-attendance-report').then(
            (m) => m.AthleteAttendanceReport,
          ),
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator] },
        loadChildren: () => import('./pages/users/users.routes').then((m) => m.USERS_ROUTES),
      },
      {
        path: 'coaches',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator] },
        loadChildren: () => import('./pages/coaches/coaches.routes').then((m) => m.COACHES_ROUTES),
      },
      {
        path: 'athletes',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator] },
        loadChildren: () => import('./pages/athletes/athletes.routes').then((m) => m.ATHLETES_ROUTES),
      },
      {
        path: 'routine-schedules',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator] },
        loadChildren: () =>
          import('./pages/routine-schedules/routine-schedules.routes').then((m) => m.ROUTINE_SCHEDULES_ROUTES),
      },
      {
        path: 'private-sessions',
        canActivate: [roleGuard],
        data: { roles: [AppRole.Administrator] },
        loadChildren: () =>
          import('./pages/private-sessions/private-sessions.routes').then((m) => m.PRIVATE_SESSIONS_ROUTES),
      },
      // Additional feature module routes are added incrementally per
      // todo.md as each backend module is completed.
    ],
  },
];
