import { Routes } from '@angular/router';
import { PrivateSessionList } from './private-session-list/private-session-list';
import { PrivateSessionForm } from './private-session-form/private-session-form';

export const PRIVATE_SESSIONS_ROUTES: Routes = [
  { path: '', component: PrivateSessionList, pathMatch: 'full' },
  { path: 'new', component: PrivateSessionForm },
  { path: ':id/edit', component: PrivateSessionForm },
];
