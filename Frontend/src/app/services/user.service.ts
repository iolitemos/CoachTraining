import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody, PagedResult } from '../models/paged-result.model';
import {
  CoachOption,
  RoleOption,
  UserCreateRequest,
  UserDetail,
  UserListItem,
  UserUpdateRequest,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly baseUrl = `${environment.apiBaseUrl}/users`;

  constructor(private readonly http: HttpClient) {}

  async list(page: number, pageSize: number, search: string): Promise<PagedResult<UserListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('search', search ?? '');

    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<PagedResult<UserListItem>>>(this.baseUrl, { params }),
    );
    return response.data;
  }

  async getById(userId: number): Promise<UserDetail> {
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<UserDetail>>(`${this.baseUrl}/${userId}`));
    return response.data;
  }

  async create(request: UserCreateRequest): Promise<UserDetail> {
    const response = await firstValueFrom(this.http.post<ApiSuccessBody<UserDetail>>(this.baseUrl, request));
    return response.data;
  }

  async update(userId: number, request: UserUpdateRequest): Promise<UserDetail> {
    const response = await firstValueFrom(
      this.http.put<ApiSuccessBody<UserDetail>>(`${this.baseUrl}/${userId}`, request),
    );
    return response.data;
  }

  async setStatus(userId: number, isActive: boolean): Promise<void> {
    await firstValueFrom(this.http.patch(`${this.baseUrl}/${userId}/status`, { isActive }));
  }

  async assignRoles(userId: number, roleIds: number[]): Promise<void> {
    await firstValueFrom(this.http.put(`${this.baseUrl}/${userId}/roles`, { roleIds }));
  }

  async setCoachLink(userId: number, coachId: number | null): Promise<void> {
    await firstValueFrom(this.http.put(`${this.baseUrl}/${userId}/coach-link`, { coachId }));
  }

  async getRoleOptions(): Promise<RoleOption[]> {
    const response = await firstValueFrom(this.http.get<ApiSuccessBody<RoleOption[]>>(`${this.baseUrl}/role-options`));
    return response.data;
  }

  async getCoachOptions(): Promise<CoachOption[]> {
    const response = await firstValueFrom(
      this.http.get<ApiSuccessBody<CoachOption[]>>(`${this.baseUrl}/coach-options`),
    );
    return response.data;
  }
}
