import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { CoachCalendarColleague, CoachDashboardResponse } from '../models/coach-dashboard.model';

/** Coach Home dashboard (requirement.md 9.1, todo.md 4.16/5.5) — always scoped to the signed-in Coach. */
@Injectable({ providedIn: 'root' })
export class CoachDashboardService {
  private readonly baseUrl = `${environment.apiBaseUrl}/dashboard/coach`;

  constructor(private readonly http: HttpClient) {}

  async get(): Promise<CoachDashboardResponse> {
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<CoachDashboardResponse>>(this.baseUrl));
    return response.data;
  }

  async getCalendarColleagues(startDate: string, endDate: string): Promise<CoachCalendarColleague[]> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<CoachCalendarColleague[]>>(
        `${this.baseUrl}/calendar-colleagues`,
        { params: { startDate, endDate } },
      ),
    );
    return response.data;
  }
}
