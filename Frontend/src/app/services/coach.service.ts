import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody, PagedResult } from '../models/paged-result.model';
import { CoachCreateRequest, CoachDetail, CoachListItem, CoachOption, CoachUpdateRequest } from '../models/coach.model';

@Injectable({ providedIn: 'root' })
export class CoachService {
  private readonly baseUrl = `${environment.apiBaseUrl}/coaches`;

  constructor(private readonly http: HttpClient) {}

  async list(page: number, pageSize: number, search: string): Promise<PagedResult<CoachListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('search', search ?? '');

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PagedResult<CoachListItem>>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async getById(coachId: number): Promise<CoachDetail> {
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<CoachDetail>>(`${this.baseUrl}/${coachId}`));
    return response.data;
  }

  async create(request: CoachCreateRequest): Promise<CoachDetail> {
    const response = await firstValueFrom(this.http.post<ApiSuccessBody<CoachDetail>>(this.baseUrl, request));
    return response.data;
  }

  async update(coachId: number, request: CoachUpdateRequest): Promise<CoachDetail> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<CoachDetail>>(`${this.baseUrl}/${coachId}`, request),
    );
    return response.data;
  }

  async setStatus(coachId: number, isActive: boolean): Promise<void> {
    await firstValueFrom(this.http.patch(`${this.baseUrl}/${coachId}/status`, { isActive }));
  }

  /** Active coaches only — used by Routine/Private Training coach selectors. */
  async getActiveOptions(): Promise<CoachOption[]> {
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<CoachOption[]>>(`${this.baseUrl}/options`));
    return response.data;
  }
}
