import { Routes } from '@angular/router';
import { AppShell } from './components/layout/app-shell/app-shell';
import { AppRole } from './models/auth.model';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';
import { Home } from './pages/home/home';
import { Login } from './pages/login/login';
import { AccessDenied } from './pages/access-denied/access-denied';

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
