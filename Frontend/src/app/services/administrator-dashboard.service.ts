import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { AdministratorDashboardFilter, AdministratorDashboardResponse } from '../models/administrator-dashboard.model';

/** Administrator Dashboard (requirement.md 9, FR-ADASH-001–004, todo.md 4.17/5.14). */
@Injectable({ providedIn: 'root' })
export class AdministratorDashboardService {
  private readonly baseUrl = `${environment.apiBaseUrl}/dashboard/administrator`;

  constructor(private readonly http: HttpClient) {}

  async get(filter: AdministratorDashboardFilter): Promise<AdministratorDashboardResponse> {
    let params = new HttpParams();

    if (filter.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter.endDate) {
      params = params.set('endDate', filter.endDate);
    }
    if (filter.coachId !== null) {
      params = params.set('coachId', filter.coachId);
    }
    if (filter.trainingType !== null) {
      params = params.set('trainingType', filter.trainingType);
    }

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<AdministratorDashboardResponse>>(this.baseUrl, { params }),
    );
    return response.data;
  }
}
