import { Routes } from '@angular/router';
import { ReviewList } from './review-list/review-list';

export const REVIEW_ROUTES: Routes = [{ path: '', component: ReviewList, pathMatch: 'full' }];
