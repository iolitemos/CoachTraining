import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { CompetitionMatch, CompetitionMatchRequest } from '../models/competition-match.model';
import { ApiSuccessBody, PagedResult } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class CompetitionMatchService {
  private readonly baseUrl = `${environment.apiBaseUrl}/competition-matches`;

  constructor(private readonly http: HttpClient) {}

  async list(page: number, pageSize: number, search: string): Promise<PagedResult<CompetitionMatch>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize).set('search', search);
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<PagedResult<CompetitionMatch>>>(this.baseUrl, { params }));
    return response.data;
  }

  async getById(id: number): Promise<CompetitionMatch> {
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<CompetitionMatch>>(`${this.baseUrl}/${id}`));
    return response.data;
  }

  async create(request: CompetitionMatchRequest): Promise<CompetitionMatch> {
    const response = await firstValueFrom(this.http.post<ApiSuccessBody<CompetitionMatch>>(this.baseUrl, request));
    return response.data;
  }

  async update(id: number, request: CompetitionMatchRequest): Promise<CompetitionMatch> {
    const response = await firstValueFrom(this.http.put<ApiSuccessBody<CompetitionMatch>>(`${this.baseUrl}/${id}`, request));
    return response.data;
  }

  async delete(id: number): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${id}`));
  }
}
