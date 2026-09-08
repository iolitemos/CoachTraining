import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import {
  CoachSubstitutionHistory,
  SubstituteCoachRequest,
  SubstituteCoachResponse,
} from '../models/substitute-coach.model';

/**
 * Substitute Coach (requirement.md 4.4/9.7, todo.md 4.11/5.10) —
 * Administrator-only assignment; reused history read from the History /
 * Audit API (todo.md 4.20), scoped by role like every other session view.
 */
@Injectable({ providedIn: 'root' })
export class SubstituteCoachService {
  private readonly baseUrl = (trainingSessionId: number) =>
    `${environment.apiBaseUrl}/training-sessions/${trainingSessionId}`;

  constructor(private readonly http: HttpClient) {}

  /** Throws HttpErrorResponse(409) with ApiErrorBody.errors on schedule conflict. */
  async assign(trainingSessionId: number, request: SubstituteCoachRequest): Promise<SubstituteCoachResponse> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<SubstituteCoachResponse>>(`${this.baseUrl(trainingSessionId)}/substitute`, request),
    );
    return response.data;
  }

  async listHistory(trainingSessionId: number): Promise<CoachSubstitutionHistory[]> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<CoachSubstitutionHistory[]>>(`${this.baseUrl(trainingSessionId)}/history/substitutions`),
    );
    return response.data;
  }
}
