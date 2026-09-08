import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { RescheduleResponse, RescheduleSessionRequest } from '../models/reschedule.model';

/** Rescheduling (requirement.md 4.7/9.7, FR-CR-004–007, todo.md 4.13/5.12) — Administrator-only. */
@Injectable({ providedIn: 'root' })
export class RescheduleService {
  private readonly baseUrl = `${environment.apiBaseUrl}/training-sessions`;

  constructor(private readonly http: HttpClient) {}

  /** Throws HttpErrorResponse(409) with ApiErrorBody.errors on schedule conflict. */
  async reschedule(trainingSessionId: number, request: RescheduleSessionRequest): Promise<RescheduleResponse> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RescheduleResponse>>(`${this.baseUrl}/${trainingSessionId}/reschedule`, request),
    );
    return response.data;
  }
}
