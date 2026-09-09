import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody, PagedResult } from '../models/paged-result.model';
import {
  AthleteCreateRequest,
  AthleteDetail,
  AthleteListItem,
  AthleteOption,
  AthleteType,
  AthleteUpdateRequest,
} from '../models/athlete.model';

@Injectable({ providedIn: 'root' })
export class AthleteService {
  private readonly baseUrl = `${environment.apiBaseUrl}/athletes`;

  constructor(private readonly http: HttpClient) {}

  async list(page: number, pageSize: number, search: string, athleteType: AthleteType): Promise<PagedResult<AthleteListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('search', search ?? '')
      .set('athleteType', athleteType);

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PagedResult<AthleteListItem>>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async getById(athleteId: number): Promise<AthleteDetail> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<AthleteDetail>>(`${this.baseUrl}/${athleteId}`),
    );
    return response.data;
  }

  async create(request: AthleteCreateRequest): Promise<AthleteDetail> {
    const response = await firstValueFrom(this.http.post<ApiSuccessBody<AthleteDetail>>(this.baseUrl, request));
    return response.data;
  }

  async update(athleteId: number, request: AthleteUpdateRequest): Promise<AthleteDetail> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<AthleteDetail>>(`${this.baseUrl}/${athleteId}`, request),
    );
    return response.data;
  }

  async setStatus(athleteId: number, isActive: boolean): Promise<void> {
    await firstValueFrom(this.http.patch(`${this.baseUrl}/${athleteId}/status`, { isActive }));
  }

  /** Active athletes only — used by Routine attendance selection and Private
   * Training athlete assignment. */
  async searchActive(search: string): Promise<AthleteOption[]> {
    const params = new HttpParams().set('search', search ?? '');
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<AthleteOption[]>>(`${this.baseUrl}/search`, { params }),
    );
    return response.data;
  }
}
