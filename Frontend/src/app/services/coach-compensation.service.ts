import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CoachCompensationPeriodRequest,
  CoachCompensationStatement,
} from '../models/coach-compensation.model';
import { ApiSuccessBody } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class CoachCompensationService {
  private readonly baseUrl = `${environment.apiBaseUrl}/coach-compensations/statements`;

  constructor(private readonly http: HttpClient) {}

  async get(request: CoachCompensationPeriodRequest): Promise<CoachCompensationStatement> {
    const params = new HttpParams()
      .set('coachId', request.coachId)
      .set('year', request.year)
      .set('month', request.month);
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<CoachCompensationStatement>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async calculate(request: CoachCompensationPeriodRequest): Promise<CoachCompensationStatement> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<CoachCompensationStatement>>(
        `${this.baseUrl}/calculate`,
        request,
      ),
    );
    return response.data;
  }

  async updateAdjustment(
    statementId: number,
    amount: number,
    reason: string | null,
  ): Promise<CoachCompensationStatement> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<CoachCompensationStatement>>(
        `${this.baseUrl}/${statementId}/adjustment`,
        { amount, reason },
      ),
    );
    return response.data;
  }

  async confirm(statementId: number): Promise<CoachCompensationStatement> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<CoachCompensationStatement>>(
        `${this.baseUrl}/${statementId}/confirm`,
        {},
      ),
    );
    return response.data;
  }

  async markPaid(statementId: number): Promise<CoachCompensationStatement> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<CoachCompensationStatement>>(
        `${this.baseUrl}/${statementId}/paid`,
        {},
      ),
    );
    return response.data;
  }
}
