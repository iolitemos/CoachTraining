import { Routes } from '@angular/router';
import { RoutineScheduleList } from './routine-schedule-list/routine-schedule-list';
import { RoutineScheduleForm } from './routine-schedule-form/routine-schedule-form';

export const ROUTINE_SCHEDULES_ROUTES: Routes = [
  { path: '', component: RoutineScheduleList, pathMatch: 'full' },
  { path: 'new', component: RoutineScheduleForm },
  { path: ':id/edit', component: RoutineScheduleForm },
];
