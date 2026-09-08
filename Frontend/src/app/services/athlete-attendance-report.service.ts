import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { AthleteAttendanceReportFilter, AthleteAttendanceReportResponse } from '../models/athlete-attendance-report.model';

/** Athlete Attendance Report (requirement.md 6.19, todo.md 4.19/5.16) — Administrator and Management/Viewer. */
@Injectable({ providedIn: 'root' })
export class AthleteAttendanceReportService {
  private readonly baseUrl = `${environment.apiBaseUrl}/reports/athlete-attendance`;

  constructor(private readonly http: HttpClient) {}

  async get(filter: AthleteAttendanceReportFilter): Promise<AthleteAttendanceReportResponse> {
    let params = new HttpParams();

    if (filter.athleteId !== null) {
      params = params.set('athleteId', filter.athleteId);
    }
    if (filter.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate);
    }

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<AthleteAttendanceReportResponse>>(this.baseUrl, { params }),
    );
    return response.data;
  }
}
