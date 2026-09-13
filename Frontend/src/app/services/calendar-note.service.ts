import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { CalendarNote } from '../models/calendar-note.model';
import { ApiSuccessBody } from '../models/paged-result.model';

@Injectable({ providedIn: 'root' })
export class CalendarNoteService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/calendar-notes`;

  async list(startDate: string, endDate: string): Promise<CalendarNote[]> {
    const params = new HttpParams().set('startDate', startDate).set('endDate', endDate);
    return (await firstValueFrom(this.http.get<ApiSuccessBody<CalendarNote[]>>(this.baseUrl, { params }))).data;
  }

  async save(noteDate: string, content: string): Promise<CalendarNote> {
    return (await firstValueFrom(this.http.put<ApiSuccessBody<CalendarNote>>(`${this.baseUrl}/${noteDate}`, { content }))).data;
  }

  async delete(noteDate: string): Promise<void> {
    await firstValueFrom(this.http.delete<void>(`${this.baseUrl}/${noteDate}`));
  }
}
