import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { CoachTeachingHourReportFilter, CoachTeachingHourReportResponse } from '../models/coach-teaching-hour-report.model';

/** Coach Teaching-Hour Report (requirement.md 6.18, todo.md 4.18/5.15) — Administrator and Management/Viewer. */
@Injectable({ providedIn: 'root' })
export class CoachTeachingHourReportService {
  private readonly baseUrl = `${environment.apiBaseUrl}/reports/coach-teaching-hours`;

  constructor(private readonly http: HttpClient) {}

  async get(filter: CoachTeachingHourReportFilter): Promise<CoachTeachingHourReportResponse> {
    let params = new HttpParams();

    if (filter.coachId !== null) {
      params = params.set('coachId', filter.coachId);
    }
    if (filter.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate);
    }
    if (filter.trainingType !== null) {
      params = params.set('trainingType', filter.trainingType);
    }

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<CoachTeachingHourReportResponse>>(this.baseUrl, { params }),
    );
    return response.data;
  }
}
