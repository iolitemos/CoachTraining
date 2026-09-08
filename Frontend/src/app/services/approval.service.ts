import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { TrainingApprovalActionResponse, TrainingApprovalHistory } from '../models/approval.model';

/**
 * Submit/Approve/Reject/RequestRevision/Unlock workflow (requirement.md 6.15,
 * todo.md 4.15/5.13). Submit itself lives on TrainingSessionService — every
 * other action here is Administrator-only.
 */
@Injectable({ providedIn: 'root' })
export class ApprovalService {
  private readonly baseUrl = (trainingSessionId: number) =>
    `${environment.apiBaseUrl}/training-sessions/${trainingSessionId}`;

  constructor(private readonly http: HttpClient) {}

  async approve(trainingSessionId: number, reason: string | null): Promise<TrainingApprovalActionResponse> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<TrainingApprovalActionResponse>>(`${this.baseUrl(trainingSessionId)}/approve`, {
        reason,
      }),
    );
    return response.data;
  }

  async reject(trainingSessionId: number, reason: string): Promise<TrainingApprovalActionResponse> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<TrainingApprovalActionResponse>>(`${this.baseUrl(trainingSessionId)}/reject`, {
        reason,
      }),
    );
    return response.data;
  }

  async requestRevision(trainingSessionId: number, reason: string): Promise<TrainingApprovalActionResponse> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<TrainingApprovalActionResponse>>(
        `${this.baseUrl(trainingSessionId)}/request-revision`,
        { reason },
      ),
    );
    return response.data;
  }

  async unlock(trainingSessionId: number, reason: string): Promise<TrainingApprovalActionResponse> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<TrainingApprovalActionResponse>>(`${this.baseUrl(trainingSessionId)}/unlock`, {
        reason,
      }),
    );
    return response.data;
  }

  async listHistory(trainingSessionId: number): Promise<TrainingApprovalHistory[]> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<TrainingApprovalHistory[]>>(`${this.baseUrl(trainingSessionId)}/history/approvals`),
    );
    return response.data;
  }
}
