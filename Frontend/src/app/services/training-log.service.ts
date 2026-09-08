import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { TrainingLog, TrainingLogUpsertRequest } from '../models/training-session.model';

/** Training Log (requirement.md 9.2 step 4, todo.md 4.10) — Coach or Administrator. */
@Injectable({ providedIn: 'root' })
export class TrainingLogService {
  private readonly baseUrl = (trainingSessionId: number) =>
    `${environment.apiBaseUrl}/training-sessions/${trainingSessionId}/training-log`;

  constructor(private readonly http: HttpClient) {}

  async get(trainingSessionId: number): Promise<TrainingLog> {
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<TrainingLog>>(this.baseUrl(trainingSessionId)));
    return response.data;
  }

  async upsert(trainingSessionId: number, request: TrainingLogUpsertRequest): Promise<TrainingLog> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<TrainingLog>>(this.baseUrl(trainingSessionId), request),
    );
    return response.data;
  }
}
