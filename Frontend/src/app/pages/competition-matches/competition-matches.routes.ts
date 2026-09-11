import { Routes } from '@angular/router';
import { CompetitionMatchForm } from './competition-match-form/competition-match-form';
import { CompetitionMatchList } from './competition-match-list/competition-match-list';

export const COMPETITION_MATCHES_ROUTES: Routes = [
  { path: '', component: CompetitionMatchList, pathMatch: 'full' },
  { path: 'new', component: CompetitionMatchForm },
  { path: ':id/edit', component: CompetitionMatchForm },
];
