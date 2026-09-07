import { Routes } from '@angular/router';
import { UserList } from './user-list/user-list';
import { UserForm } from './user-form/user-form';

export const USERS_ROUTES: Routes = [
  { path: '', component: UserList, pathMatch: 'full' },
  { path: 'new', component: UserForm },
  { path: ':id/edit', component: UserForm },
];
