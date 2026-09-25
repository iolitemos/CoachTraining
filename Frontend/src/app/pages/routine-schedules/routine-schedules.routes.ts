import { Routes } from '@angular/router';
import { RoutineScheduleCalendar } from './routine-schedule-calendar/routine-schedule-calendar';
import { RoutineScheduleList } from './routine-schedule-list/routine-schedule-list';
import { RoutineScheduleForm } from './routine-schedule-form/routine-schedule-form';

export const ROUTINE_SCHEDULES_ROUTES: Routes = [
  { path: 'participation-plans', loadComponent: () => import('./routine-plan-summary/routine-plan-summary').then((m) => m.RoutinePlanSummary) },
  { path: 'training-dates', loadComponent: () => import('./routine-training-dates/routine-training-dates').then((m) => m.RoutineTrainingDates) },
  { path: '', component: RoutineScheduleCalendar, pathMatch: 'full' },
  { path: 'list', component: RoutineScheduleList },
  { path: 'new', component: RoutineScheduleForm },
  { path: ':id/edit', component: RoutineScheduleForm },
];
