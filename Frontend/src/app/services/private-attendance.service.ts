import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { PrivateAttendanceRoster, PrivateAttendanceSetRequest } from '../models/private-attendance.model';

/**
 * Private Attendance (requirement.md 9.4, todo.md 4.9/5.8) — the roster is
 * fixed by the session's assigned athletes; attendance is recorded per
 * athlete, never added/removed independently of that assignment.
 */
@Injectable({ providedIn: 'root' })
export class PrivateAttendanceService {
  private readonly baseUrl = (trainingSessionId: number) =>
    `${environment.apiBaseUrl}/training-sessions/${trainingSessionId}/private-attendance`;

  constructor(private readonly http: HttpClient) {}

  async getRoster(trainingSessionId: number): Promise<PrivateAttendanceRoster> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PrivateAttendanceRoster>>(this.baseUrl(trainingSessionId)),
    );
    return response.data;
  }

  async set(
    trainingSessionId: number,
    athleteId: number,
    request: PrivateAttendanceSetRequest,
  ): Promise<PrivateAttendanceRoster> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<{ rosterComplete: boolean }>>(`${this.baseUrl(trainingSessionId)}/${athleteId}`, request),
    );
    // Refetch the full roster: the Set endpoint only returns the changed row
    // plus a completeness flag (PrivateAttendanceActionResult), not the whole list.
    void response;
    return this.getRoster(trainingSessionId);
  }
}
