import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody, PagedResult } from '../models/paged-result.model';
import {
  RoutineScheduleDetail,
  RoutineScheduleListItem,
  RoutineScheduleSaveRequest,
  RoutineScheduleSaveResult,
} from '../models/routine-schedule.model';

@Injectable({ providedIn: 'root' })
export class RoutineScheduleService {
  private readonly baseUrl = `${environment.apiBaseUrl}/routine-schedules`;

  constructor(private readonly http: HttpClient) {}

  async list(
    page: number,
    pageSize: number,
    search: string,
  ): Promise<PagedResult<RoutineScheduleListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('search', search ?? '');

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PagedResult<RoutineScheduleListItem>>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async listAll(): Promise<RoutineScheduleListItem[]> {
    const firstPage = await this.list(1, 100, '');
    if (firstPage.totalPages <= 1) {
      return firstPage.items;
    }

    const remainingPages = await Promise.all(
      Array.from({ length: firstPage.totalPages - 1 }, (_, index) => this.list(index + 2, 100, '')),
    );

    return [firstPage, ...remainingPages].flatMap((page) => page.items);
  }

  async getById(routineScheduleId: number): Promise<RoutineScheduleDetail> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<RoutineScheduleDetail>>(`${this.baseUrl}/${routineScheduleId}`),
    );
    return response.data;
  }

  /** Create returns 201 on success or 409 with RoutineScheduleSaveResult.conflicts on schedule conflict. */
  async create(request: RoutineScheduleSaveRequest): Promise<RoutineScheduleSaveResult> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RoutineScheduleSaveResult>>(this.baseUrl, request),
    );
    return response.data;
  }

  async update(
    routineScheduleId: number,
    request: RoutineScheduleSaveRequest,
  ): Promise<RoutineScheduleDetail> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<RoutineScheduleDetail>>(
        `${this.baseUrl}/${routineScheduleId}`,
        request,
      ),
    );
    return response.data;
  }

  async setStatus(routineScheduleId: number, isActive: boolean): Promise<void> {
    await firstValueFrom(
      this.http.patch(`${this.baseUrl}/${routineScheduleId}/status`, { isActive }),
    );
  }

  async delete(routineScheduleId: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${this.baseUrl}/${routineScheduleId}`));
  }
}
