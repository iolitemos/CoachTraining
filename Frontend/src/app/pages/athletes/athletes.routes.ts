import { Routes } from '@angular/router';
import { AthleteList } from './athlete-list/athlete-list';
import { AthleteForm } from './athlete-form/athlete-form';

export const ATHLETES_ROUTES: Routes = [
  { path: '', component: AthleteList, pathMatch: 'full' },
  { path: 'new', component: AthleteForm },
  { path: ':id/edit', component: AthleteForm },
];
