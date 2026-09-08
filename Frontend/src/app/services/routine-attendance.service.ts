import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import {
  RoutineAttendanceCreateRequest,
  RoutineAttendanceItem,
  RoutineAttendanceUpdateRequest,
} from '../models/routine-attendance.model';

/**
 * Routine Attendance (requirement.md 9.3, todo.md 4.8/5.7) — no pre-assigned
 * roster: only athletes the Coach actively adds get a record.
 */
@Injectable({ providedIn: 'root' })
export class RoutineAttendanceService {
  private readonly baseUrl = (trainingSessionId: number) =>
    `${environment.apiBaseUrl}/training-sessions/${trainingSessionId}/routine-attendance`;

  constructor(private readonly http: HttpClient) {}

  async list(trainingSessionId: number): Promise<RoutineAttendanceItem[]> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<RoutineAttendanceItem[]>>(this.baseUrl(trainingSessionId)),
    );
    return response.data;
  }

  async add(trainingSessionId: number, request: RoutineAttendanceCreateRequest): Promise<RoutineAttendanceItem> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<RoutineAttendanceItem>>(this.baseUrl(trainingSessionId), request),
    );
    return response.data;
  }

  async update(
    trainingSessionId: number,
    attendanceId: number,
    request: RoutineAttendanceUpdateRequest,
  ): Promise<RoutineAttendanceItem> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<RoutineAttendanceItem>>(`${this.baseUrl(trainingSessionId)}/${attendanceId}`, request),
    );
    return response.data;
  }

  async remove(trainingSessionId: number, attendanceId: number): Promise<void> {
    await firstValueFrom(this.http.delete(`${this.baseUrl(trainingSessionId)}/${attendanceId}`));
  }
}
