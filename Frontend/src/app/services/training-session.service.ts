import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody, PagedResult } from '../models/paged-result.model';
import { TrainingSessionDetail, TrainingSessionFilter, TrainingSessionListItem } from '../models/training-session.model';

/**
 * Coach Session read/action API (requirement.md 9.2, todo.md 4.5/4.7/4.15).
 * Start/Complete/Submit throw HttpErrorResponse(400) with ApiErrorBody.message
 * on an invalid status transition or incomplete-record rule.
 */
@Injectable({ providedIn: 'root' })
export class TrainingSessionService {
  private readonly baseUrl = `${environment.apiBaseUrl}/training-sessions`;

  constructor(private readonly http: HttpClient) {}

  async list(filter: TrainingSessionFilter): Promise<PagedResult<TrainingSessionListItem>> {
    let params = new HttpParams().set('page', filter.page).set('pageSize', filter.pageSize);

    if (filter.coachId !== null) {
      params = params.set('coachId', filter.coachId);
    }
    if (filter.trainingType !== null) {
      params = params.set('trainingType', filter.trainingType);
    }
    if (filter.status !== null) {
      params = params.set('status', filter.status);
    }
    if (filter.dateFrom) {
      params = params.set('dateFrom', filter.dateFrom);
    }
    if (filter.dateTo) {
      params = params.set('dateTo', filter.dateTo);
    }

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PagedResult<TrainingSessionListItem>>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async getById(trainingSessionId: number): Promise<TrainingSessionDetail> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<TrainingSessionDetail>>(`${this.baseUrl}/${trainingSessionId}`),
    );
    return response.data;
  }

  async start(trainingSessionId: number, actualStartDateTime: string | null = null): Promise<TrainingSessionDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<TrainingSessionDetail>>(`${this.baseUrl}/${trainingSessionId}/start`, {
        actualStartDateTime,
      }),
    );
    return response.data;
  }

  async complete(trainingSessionId: number, actualEndDateTime: string | null = null): Promise<TrainingSessionDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<TrainingSessionDetail>>(`${this.baseUrl}/${trainingSessionId}/complete`, {
        actualEndDateTime,
      }),
    );
    return response.data;
  }

  async submit(trainingSessionId: number): Promise<TrainingSessionDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<{ session: TrainingSessionDetail }>>(`${this.baseUrl}/${trainingSessionId}/submit`, {}),
    );
    return response.data.session;
  }
}
