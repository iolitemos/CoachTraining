import { Routes } from '@angular/router';
import { PrivateSessionList } from './private-session-list/private-session-list';
import { PrivateSessionForm } from './private-session-form/private-session-form';
import { PrivateSessionCalendar } from './private-session-calendar/private-session-calendar';

export const PRIVATE_SESSIONS_ROUTES: Routes = [
  { path: '', component: PrivateSessionCalendar, pathMatch: 'full' },
  { path: 'list', component: PrivateSessionList },
  { path: 'new', component: PrivateSessionForm },
  { path: ':id/edit', component: PrivateSessionForm },
];
