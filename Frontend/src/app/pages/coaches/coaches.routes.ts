import { Routes } from '@angular/router';
import { CoachList } from './coach-list/coach-list';
import { CoachForm } from './coach-form/coach-form';

export const COACHES_ROUTES: Routes = [
  { path: '', component: CoachList, pathMatch: 'full' },
  { path: 'new', component: CoachForm },
  { path: ':id/edit', component: CoachForm },
];
