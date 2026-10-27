import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import {
  RoutineScheduleRequest,
  RoutineScheduleRequestReview,
  RoutineScheduleRequestStatus,
} from '../models/routine-schedule-request.model';

@Injectable({ providedIn: 'root' })
export class RoutineScheduleRequestService {
  private readonly baseUrl = `${environment.apiBaseUrl}/routine-schedule-requests`;
  constructor(private readonly http: HttpClient) {}

  async listOwn(dateFrom?: string, dateTo?: string): Promise<RoutineScheduleRequest[]> {
    let params = new HttpParams();
    if (dateFrom) params = params.set('dateFrom', dateFrom);
    if (dateTo) params = params.set('dateTo', dateTo);
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<RoutineScheduleRequest[]>>(
        `${environment.apiBaseUrl}/coach/routine-schedules/requests`,
        { params },
      ),
    );
    return response.data;
  }

  async updateOwn(
    id: number,
    request: {
      effectiveStartDate: string;
      startTime: string;
      endTime: string;
      remarks: string | null;
    },
  ): Promise<RoutineScheduleRequest> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<RoutineScheduleRequest>>(
        `${environment.apiBaseUrl}/coach/routine-schedules/requests/${id}`,
        request,
      ),
    );
    return response.data;
  }

  async cancelOwn(id: number): Promise<RoutineScheduleRequest> {
    const response = await firstValueFrom(
      this.http.delete<ApiSuccessBody<RoutineScheduleRequest>>(
        `${environment.apiBaseUrl}/coach/routine-schedules/requests/${id}`,
      ),
    );
    return response.data;
  }

  async list(
    status: RoutineScheduleRequestStatus | '' = 'Pending',
  ): Promise<RoutineScheduleRequest[]> {
    const params = status ? new HttpParams().set('status', status) : new HttpParams();
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<RoutineScheduleRequest[]>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async approve(
    id: number,
    request: RoutineScheduleRequestReview,
  ): Promise<RoutineScheduleRequest> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RoutineScheduleRequest>>(
        `${this.baseUrl}/${id}/approve`,
        request,
      ),
    );
    return response.data;
  }

  async approveBatch(requestIds: number[]): Promise<RoutineScheduleRequest[]> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RoutineScheduleRequest[]>>(`${this.baseUrl}/batch-approve`, {
        requestIds,
      }),
    );
    return response.data;
  }

  async requestRevision(id: number, reason: string): Promise<RoutineScheduleRequest> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RoutineScheduleRequest>>(
        `${this.baseUrl}/${id}/request-revision`,
        { reason },
      ),
    );
    return response.data;
  }

  async reject(id: number, reason: string): Promise<RoutineScheduleRequest> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RoutineScheduleRequest>>(`${this.baseUrl}/${id}/reject`, {
        reason,
      }),
    );
    return response.data;
  }

  async deleteApproved(id: number): Promise<RoutineScheduleRequest> {
    const response = await firstValueFrom(
      this.http.delete<ApiSuccessBody<RoutineScheduleRequest>>(
        `${this.baseUrl}/${id}/approved-schedule`,
      ),
    );
    return response.data;
  }
}
