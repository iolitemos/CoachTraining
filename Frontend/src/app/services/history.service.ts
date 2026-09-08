import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { AuditLogEntry, ConflictOverrideHistoryEntry } from '../models/history.model';

/**
 * Session business history / audit trail (requirement.md 6.20, FR-AUDIT-001–003,
 * todo.md 4.20/5.17). Approval and Substitution history live on their own
 * services (ApprovalService, SubstituteCoachService) — this covers the
 * remaining two history sources.
 */
@Injectable({ providedIn: 'root' })
export class HistoryService {
  private readonly baseUrl = (trainingSessionId: number) =>
    `${environment.apiBaseUrl}/training-sessions/${trainingSessionId}/history`;

  constructor(private readonly http: HttpClient) {}

  async getAuditLog(trainingSessionId: number): Promise<AuditLogEntry[]> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<AuditLogEntry[]>>(this.baseUrl(trainingSessionId)),
    );
    return response.data;
  }

  async getConflictOverrideHistory(trainingSessionId: number): Promise<ConflictOverrideHistoryEntry[]> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<ConflictOverrideHistoryEntry[]>>(
        `${this.baseUrl(trainingSessionId)}/conflict-overrides`,
      ),
    );
    return response.data;
  }
}
