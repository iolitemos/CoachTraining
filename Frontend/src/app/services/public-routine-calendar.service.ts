import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import {
  PublicRoutineCalendarData,
  RoutineCalendarShareCreated,
  RoutineCalendarShareStatus,
} from '../models/public-routine-calendar.model';

@Injectable({ providedIn: 'root' })
export class PublicRoutineCalendarService {
  private readonly baseUrl = `${environment.apiBaseUrl}/public/routine-calendar`;

  constructor(private readonly http: HttpClient) {}

  async getShareStatus(): Promise<RoutineCalendarShareStatus> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<RoutineCalendarShareStatus>>(`${this.baseUrl}/share-link`),
    );
    return response.data;
  }

  async rotateShareLink(): Promise<RoutineCalendarShareCreated> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RoutineCalendarShareCreated>>(`${this.baseUrl}/share-link`, {}),
    );
    return response.data;
  }

  async revokeShareLink(): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.baseUrl}/share-link`));
  }

  async getCalendar(token: string, startDate: string, endDate: string): Promise<PublicRoutineCalendarData> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PublicRoutineCalendarData>>(
        `${this.baseUrl}/${encodeURIComponent(token)}`,
        { params },
      ),
    );
    return response.data;
  }
}
