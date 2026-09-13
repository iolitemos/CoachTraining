import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { CoachOption, coachPickerLabel } from '../../../models/coach.model';
import { AthleteOption, athletePickerLabel } from '../../../models/athlete.model';
import { PrivateSessionAthlete } from '../../../models/private-session.model';
import { TrainingSessionStatus } from '../../../models/training-session-status.model';
import { CoachService } from '../../../services/coach.service';
import { AthleteService } from '../../../services/athlete.service';
import { PrivateSessionService } from '../../../services/private-session.service';
import { DateInput } from '../../../shared/date-input/date-input';

@Component({
  selector: 'app-private-session-form',
  imports: [ReactiveFormsModule, FormsModule, RouterLink, PageHeader, LoadingIndicator, StatusBadge, DateInput],
  templateUrl: './private-session-form.html',
  styleUrl: './private-session-form.css',
})
export class PrivateSessionForm implements OnInit {
  readonly weekDays = [
    { value: 1, label: 'จันทร์' }, { value: 2, label: 'อังคาร' },
    { value: 3, label: 'พุธ' }, { value: 4, label: 'พฤหัสบดี' },
    { value: 5, label: 'ศุกร์' }, { value: 6, label: 'เสาร์' },
    { value: 0, label: 'อาทิตย์' },
  ];
  readonly athletePickerLabel = athletePickerLabel;
  readonly coachPickerLabel = coachPickerLabel;
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly coachService = inject(CoachService);
  private readonly athleteService = inject(AthleteService);
  private readonly privateSessionService = inject(PrivateSessionService);

  trainingSessionId = signal<number | null>(null);
  isEditMode = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  conflictMessages = signal<string[]>([]);
  scheduleMode = signal<'single' | 'range'>('single');
  rangePattern = signal<'everyDay' | 'weekdays'>('everyDay');
  selectedDaysOfWeek = signal<number[]>([]);
  endDate = this.fb.control('');

  status = signal<TrainingSessionStatus | null>(null);
  /** Only Scheduled Private sessions may be edited (FR-PRIVATE-009). */
  readOnly = signal(false);

  coachOptions = signal<CoachOption[]>([]);
  selectedAthletes = signal<PrivateSessionAthlete[]>([]);
  athleteSearchTerm = '';
  athleteSearchResults = signal<AthleteOption[]>([]);
  athleteSearching = signal(false);
  athleteSearchError = signal<string | null>(null);
  guestName = '';
  guestPhone = '';
  guestRemark = '';

  form = this.fb.group({
    coachId: this.fb.control<number | null>(null, Validators.required),
    sessionDate: ['', Validators.required],
    startTime: ['', Validators.required],
    endTime: ['', Validators.required],
    location: ['', Validators.maxLength(200)],
    remarks: [''],
  });

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    const isEdit = idParam !== null;
    this.isEditMode.set(isEdit);

    if (isEdit) {
      this.trainingSessionId.set(Number(idParam));
    } else {
      this.prefillFromCalendar(this.route.snapshot.queryParamMap?.get('date'));
    }

    try {
      const [coachOptions, session] = await Promise.all([
        this.coachService.getActiveOptions(),
        isEdit ? this.privateSessionService.getById(this.trainingSessionId()!) : Promise.resolve(null),
      ]);

      this.coachOptions.set(coachOptions);

      if (session) {
        this.status.set(session.status);
        this.readOnly.set(session.status !== 'Scheduled');
        this.selectedAthletes.set(session.athletes);
        this.form.patchValue({
          coachId: session.coachId,
          sessionDate: session.sessionDate,
          startTime: session.startTime.slice(0, 5),
          endTime: session.endTime.slice(0, 5),
          location: session.location ?? '',
          remarks: session.remarks ?? '',
        });

        if (this.readOnly()) {
          this.form.disable();
        }
      }
    } catch {
      this.errorMessage.set('ไม่สามารถโหลดข้อมูลได้');
    } finally {
      this.loading.set(false);
    }
  }

  private prefillFromCalendar(date: string | null | undefined): void {
    if (!date || !/^\d{4}-\d{2}-\d{2}$/.test(date)) {
      return;
    }

    const [year, month, day] = date.split('-').map(Number);
    const selectedDate = new Date(year, month - 1, day);
    if (
      Number.isNaN(selectedDate.getTime()) ||
      selectedDate.getFullYear() !== year ||
      selectedDate.getMonth() !== month - 1 ||
      selectedDate.getDate() !== day
    ) {
      return;
    }

    this.form.patchValue({ sessionDate: date });
    this.endDate.setValue(date);
    this.selectedDaysOfWeek.set([selectedDate.getDay()]);
  }

  setScheduleMode(mode: 'single' | 'range'): void {
    this.scheduleMode.set(mode);
    if (mode === 'range' && !this.endDate.value) this.endDate.setValue(this.form.controls.sessionDate.value);
  }

  setRangePattern(pattern: 'everyDay' | 'weekdays'): void {
    this.rangePattern.set(pattern);
    if (pattern === 'weekdays' && this.selectedDaysOfWeek().length === 0) {
      this.selectedDaysOfWeek.set([parseIsoDate(this.form.controls.sessionDate.value)?.getDay() ?? new Date().getDay()]);
    }
  }

  toggleDay(day: number): void {
    this.selectedDaysOfWeek.update((days) => days.includes(day) ? days.filter((item) => item !== day) : [...days, day]);
  }

  effectiveDaysOfWeek(): number[] {
    return this.rangePattern() === 'everyDay' ? [0, 1, 2, 3, 4, 5, 6] : this.selectedDaysOfWeek();
  }

  selectedOccurrenceCount(): number {
    if (this.scheduleMode() === 'single') return 1;
    const start = parseIsoDate(this.form.controls.sessionDate.value);
    const end = parseIsoDate(this.endDate.value);
    if (!start || !end || end < start) return 0;
    const selectedDays = new Set(this.effectiveDaysOfWeek());
    let count = 0;
    for (const date = new Date(start); date <= end; date.setDate(date.getDate() + 1)) {
      if (selectedDays.has(date.getDay())) count++;
    }
    return count;
  }

  async searchAthletes(): Promise<void> {
    this.athleteSearching.set(true);
    this.athleteSearchError.set(null);
    try {
      this.athleteSearchResults.set(await this.athleteService.searchActive(this.athleteSearchTerm.trim()));
    } catch {
      this.athleteSearchError.set('ไม่สามารถค้นหานักกีฬาได้');
    } finally {
      this.athleteSearching.set(false);
    }
  }

  isAthleteSelected(athleteId: number): boolean {
    return this.selectedAthletes().some((a) => a.athleteId === athleteId);
  }

  addAthlete(athlete: AthleteOption): void {
    if (this.isAthleteSelected(athlete.athleteId)) {
      return;
    }
    this.selectedAthletes.update((current) => [
      ...current,
      { privateSessionAthleteId: 0, athleteId: athlete.athleteId, isGuest: false, athleteCode: athlete.athleteCode, fullName: athlete.fullName, guestPhone: null, guestRemark: null, clientKey: `athlete-${athlete.athleteId}` },
    ]);
  }

  addGuest(): void {
    const fullName = this.guestName.trim();
    if (!fullName) {
      this.errorMessage.set('กรุณากรอกชื่อผู้เรียนชั่วคราว');
      return;
    }
    this.selectedAthletes.update((current) => [...current, {
      privateSessionAthleteId: 0, athleteId: null, isGuest: true, athleteCode: 'ผู้เรียนชั่วคราว', fullName,
      guestPhone: this.guestPhone.trim() || null, guestRemark: this.guestRemark.trim() || null,
      clientKey: `guest-${Date.now()}-${Math.random()}`,
    }]);
    this.guestName = '';
    this.guestPhone = '';
    this.guestRemark = '';
    this.errorMessage.set(null);
  }

  participantKey(participant: PrivateSessionAthlete): string {
    return participant.clientKey ?? `saved-${participant.privateSessionAthleteId}`;
  }

  removeParticipant(key: string): void {
    this.selectedAthletes.update((current) => current.filter((a) => this.participantKey(a) !== key));
  }

  async onSubmit(): Promise<void> {
    if (this.readOnly() || this.submitting()) {
      return;
    }

    if (this.form.invalid || this.selectedAthletes().length === 0) {
      this.form.markAllAsTouched();
      if (this.selectedAthletes().length === 0) {
        this.errorMessage.set('กรุณาเพิ่มผู้เข้าร่วมอย่างน้อยหนึ่งคน');
      }
      return;
    }

    if (!this.isEditMode() && this.scheduleMode() === 'range') {
      const endDate = this.endDate.value;
      if (!endDate || endDate < this.form.controls.sessionDate.value! || this.effectiveDaysOfWeek().length === 0 || this.selectedOccurrenceCount() === 0) {
        this.errorMessage.set('กรุณาเลือกช่วงวันที่และรูปแบบวันให้ถูกต้อง');
        return;
      }
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.conflictMessages.set([]);

    const value = this.form.getRawValue();
    const payload = {
      coachId: value.coachId!,
      sessionDate: value.sessionDate!,
      startTime: value.startTime!,
      endTime: value.endTime!,
      location: value.location || null,
      remarks: value.remarks || null,
      athleteIds: this.selectedAthletes().filter((a) => a.athleteId !== null).map((a) => a.athleteId as number),
      guestParticipants: this.selectedAthletes().filter((a) => a.isGuest).map((a) => ({ fullName: a.fullName, phone: a.guestPhone, remark: a.guestRemark })),
    };

    try {
      if (this.isEditMode()) {
        await this.privateSessionService.update(this.trainingSessionId()!, payload);
      } else if (this.scheduleMode() === 'range') {
        await this.privateSessionService.createBatch({
          coachId: payload.coachId,
          startDate: payload.sessionDate,
          endDate: this.endDate.value!,
          daysOfWeek: this.effectiveDaysOfWeek(),
          startTime: payload.startTime,
          endTime: payload.endTime,
          location: payload.location,
          remarks: payload.remarks,
          athleteIds: payload.athleteIds,
          guestParticipants: payload.guestParticipants,
        });
      } else {
        await this.privateSessionService.create(payload);
      }

      await this.router.navigateByUrl('/private-sessions');
    } catch (error) {
      this.handleSaveError(error);
    } finally {
      this.submitting.set(false);
    }
  }

  private handleSaveError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as ApiErrorBody | undefined;

      if (error.status === 409) {
        this.errorMessage.set(body?.message ?? 'พบตารางฝึกซ้อมที่ขัดแย้งกัน');
        this.conflictMessages.set((body?.errors ?? []).map((e) => e.message));
        return;
      }

      this.errorMessage.set(body?.message ?? 'บันทึกข้อมูลไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
      return;
    }

    this.errorMessage.set('บันทึกข้อมูลไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
  }
}

function parseIsoDate(value: string | null): Date | null {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null;
}
