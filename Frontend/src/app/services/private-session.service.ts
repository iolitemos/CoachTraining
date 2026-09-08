import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody, PagedResult } from '../models/paged-result.model';
import { PrivateSessionDetail, PrivateSessionListItem, PrivateSessionSaveRequest } from '../models/private-session.model';

@Injectable({ providedIn: 'root' })
export class PrivateSessionService {
  private readonly baseUrl = `${environment.apiBaseUrl}/private-sessions`;

  constructor(private readonly http: HttpClient) {}

  async list(page: number, pageSize: number, search: string): Promise<PagedResult<PrivateSessionListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('search', search ?? '');

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PagedResult<PrivateSessionListItem>>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async getById(trainingSessionId: number): Promise<PrivateSessionDetail> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PrivateSessionDetail>>(`${this.baseUrl}/${trainingSessionId}`),
    );
    return response.data;
  }

  async listCalendar(startDate: string, endDate: string): Promise<PrivateSessionListItem[]> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PrivateSessionListItem[]>>(`${this.baseUrl}/calendar`, { params }),
    );
    return response.data;
  }

  /** Throws HttpErrorResponse(409) with ApiErrorBody.errors on schedule conflict. */
  async create(request: PrivateSessionSaveRequest): Promise<PrivateSessionDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<PrivateSessionDetail>>(this.baseUrl, request),
    );
    return response.data;
  }

  async update(trainingSessionId: number, request: PrivateSessionSaveRequest): Promise<PrivateSessionDetail> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<PrivateSessionDetail>>(`${this.baseUrl}/${trainingSessionId}`, request),
    );
    return response.data;
  }
}
