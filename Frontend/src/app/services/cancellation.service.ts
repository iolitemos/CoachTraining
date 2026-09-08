import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { CancelSessionRequest } from '../models/cancellation.model';
import { TrainingSessionDetail } from '../models/training-session.model';

/** Cancellation (requirement.md 4.6/9.7, FR-CR-001–003, todo.md 4.12/5.11) — Administrator-only. */
@Injectable({ providedIn: 'root' })
export class CancellationService {
  private readonly baseUrl = `${environment.apiBaseUrl}/training-sessions`;

  constructor(private readonly http: HttpClient) {}

  async cancel(trainingSessionId: number, request: CancelSessionRequest): Promise<TrainingSessionDetail> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<TrainingSessionDetail>>(`${this.baseUrl}/${trainingSessionId}/cancel`, request),
    );
    return response.data;
  }
}
