import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { ParentRoutinePlanCalendar, ParentRoutinePlanLinkCreated, ParentRoutinePlanLinkStatus, RoutineParticipationPlanAthlete, RoutineParticipationPlanSummary, RoutineTrainingDate } from '../models/parent-routine-plan.model';

@Injectable({ providedIn: 'root' })
export class ParentRoutinePlanService {
  private readonly baseUrl = `${environment.apiBaseUrl}/parent-routine-plans`;
  constructor(private readonly http: HttpClient) {}

  async getLinkStatus(athleteId: number): Promise<ParentRoutinePlanLinkStatus> {
    return (await firstValueFrom(this.http.get<ApiSuccessBody<ParentRoutinePlanLinkStatus>>(`${this.baseUrl}/admin/athletes/${athleteId}/link`))).data;
  }
  async rotateLink(athleteId: number): Promise<ParentRoutinePlanLinkCreated> {
    return (await firstValueFrom(this.http.post<ApiSuccessBody<ParentRoutinePlanLinkCreated>>(`${this.baseUrl}/admin/athletes/${athleteId}/link`, {}))).data;
  }
  async setAccess(athleteId: number, isEnabled: boolean): Promise<ParentRoutinePlanLinkStatus> {
    return (await firstValueFrom(this.http.patch<ApiSuccessBody<ParentRoutinePlanLinkStatus>>(`${this.baseUrl}/admin/athletes/${athleteId}/link/access`, { isEnabled }))).data;
  }
  async getCalendar(token: string, startDate: string, endDate: string): Promise<ParentRoutinePlanCalendar> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return (await firstValueFrom(this.http.get<ApiSuccessBody<ParentRoutinePlanCalendar>>(`${this.baseUrl}/public/${encodeURIComponent(token)}`, { params }))).data;
  }
  async save(token: string, startDate: string, endDate: string, selectedDates: string[]): Promise<ParentRoutinePlanCalendar> {
    return (await firstValueFrom(this.http.put<ApiSuccessBody<ParentRoutinePlanCalendar>>(`${this.baseUrl}/public/${encodeURIComponent(token)}`, { startDate, endDate, selectedDates }))).data;
  }
  async getSummary(startDate: string, endDate: string): Promise<RoutineParticipationPlanSummary[]> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return (await firstValueFrom(this.http.get<ApiSuccessBody<RoutineParticipationPlanSummary[]>>(`${this.baseUrl}/admin/summary`, { params }))).data;
  }
  async getAthletes(trainingDate: string): Promise<RoutineParticipationPlanAthlete[]> {
    return (await firstValueFrom(this.http.get<ApiSuccessBody<RoutineParticipationPlanAthlete[]>>(`${this.baseUrl}/admin/dates/${trainingDate}`))).data;
  }
  async listTrainingDates(startDate: string, endDate: string): Promise<RoutineTrainingDate[]> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return (await firstValueFrom(this.http.get<ApiSuccessBody<RoutineTrainingDate[]>>(`${environment.apiBaseUrl}/routine-training-dates`, { params }))).data;
  }
  async addTrainingDate(trainingDate: string): Promise<RoutineTrainingDate> {
    return (await firstValueFrom(this.http.post<ApiSuccessBody<RoutineTrainingDate>>(`${environment.apiBaseUrl}/routine-training-dates`, { trainingDate }))).data;
  }
  async removeTrainingDate(trainingDate: string): Promise<void> {
    await firstValueFrom(this.http.delete(`${environment.apiBaseUrl}/routine-training-dates/${trainingDate}`));
  }
}
