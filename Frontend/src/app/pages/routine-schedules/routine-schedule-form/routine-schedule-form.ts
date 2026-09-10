import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { CoachOption } from '../../../models/coach.model';
import { CoachService } from '../../../services/coach.service';
import { RoutineScheduleService } from '../../../services/routine-schedule.service';
import { DateInput } from '../../../shared/date-input/date-input';

@Component({
  selector: 'app-routine-schedule-form',
  imports: [ReactiveFormsModule, RouterLink, PageHeader, LoadingIndicator, DateInput],
  templateUrl: './routine-schedule-form.html',
  styleUrl: './routine-schedule-form.css',
})
export class RoutineScheduleForm implements OnInit {
  readonly weekDays = [
    { value: 1, label: 'จันทร์' }, { value: 2, label: 'อังคาร' },
    { value: 3, label: 'พุธ' }, { value: 4, label: 'พฤหัสบดี' },
    { value: 5, label: 'ศุกร์' }, { value: 6, label: 'เสาร์' },
    { value: 0, label: 'อาทิตย์' },
  ];
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly coachService = inject(CoachService);
  private readonly routineScheduleService = inject(RoutineScheduleService);

  routineScheduleId = signal<number | null>(null);
  isEditMode = signal(false);
  isSelfService = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  conflictMessages = signal<string[]>([]);
  scheduleMode = signal<'single' | 'range'>('single');
  rangePattern = signal<'everyDay' | 'weekdays'>('everyDay');
  selectedDaysOfWeek = signal<number[]>([]);
  effectiveEndDate = this.fb.control(getTodayIsoDate());

  coachOptions = signal<CoachOption[]>([]);
  form = this.fb.group({
    coachId: this.fb.control<number | null>(null, Validators.required),
    startTime: ['18:30', Validators.required],
    endTime: ['20:30', Validators.required],
    effectiveStartDate: [getTodayIsoDate(), Validators.required],
    remarks: [''],
  });

  selectedOccurrenceCount(): number {
    if (this.scheduleMode() === 'single') return 1;
    const start = parseIsoDate(this.form.controls.effectiveStartDate.value);
    const end = parseIsoDate(this.effectiveEndDate.value);
    if (!start || !end || end < start) return 0;
    const selectedDays = new Set(this.effectiveDaysOfWeek());
    let count = 0;
    for (const date = new Date(start); date <= end; date.setDate(date.getDate() + 1)) {
      if (selectedDays.has(date.getDay())) count++;
    }
    return count;
  }

  async ngOnInit(): Promise<void> {
    const selfService = this.route.snapshot.data?.['selfService'] === true;
    this.isSelfService.set(selfService);
    if (selfService) {
      this.form.controls.coachId.clearValidators();
      this.form.controls.coachId.updateValueAndValidity();
    }
    const idParam = this.route.snapshot.paramMap.get('id');
    const isEdit = !selfService && idParam !== null;
    this.isEditMode.set(isEdit);

    if (isEdit) {
      this.routineScheduleId.set(Number(idParam));
    } else {
      this.prefillFromCalendar(this.route.snapshot.queryParamMap?.get('date'));
    }

    try {
      const [coachOptions, schedule] = await Promise.all([
        selfService ? Promise.resolve([]) : this.coachService.getActiveOptions(),
        isEdit
          ? this.routineScheduleService.getById(this.routineScheduleId()!)
          : Promise.resolve(null),
      ]);

      this.coachOptions.set(coachOptions);

      if (schedule) {
        this.form.patchValue({
          coachId: schedule.coachId,
          startTime: schedule.startTime.slice(0, 5),
          endTime: schedule.endTime.slice(0, 5),
          effectiveStartDate: schedule.effectiveStartDate,
          remarks: schedule.remarks ?? '',
        });
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

    this.form.patchValue({
      effectiveStartDate: date,
    });
    this.effectiveEndDate.setValue(date);
    this.selectedDaysOfWeek.set([selectedDate.getDay()]);
  }

  setScheduleMode(mode: 'single' | 'range'): void {
    this.scheduleMode.set(mode);
    if (mode === 'range' && this.selectedDaysOfWeek().length === 0) {
      const date = parseIsoDate(this.form.controls.effectiveStartDate.value);
      this.selectedDaysOfWeek.set([date?.getDay() ?? new Date().getDay()]);
    }
  }

  toggleDay(day: number): void {
    this.selectedDaysOfWeek.update((days) =>
      days.includes(day) ? days.filter((value) => value !== day) : [...days, day],
    );
  }

  setRangePattern(pattern: 'everyDay' | 'weekdays'): void {
    this.rangePattern.set(pattern);
    if (pattern === 'weekdays' && this.selectedDaysOfWeek().length === 0) {
      const date = parseIsoDate(this.form.controls.effectiveStartDate.value);
      this.selectedDaysOfWeek.set([date?.getDay() ?? new Date().getDay()]);
    }
  }

  effectiveDaysOfWeek(): number[] {
    return this.rangePattern() === 'everyDay' ? [0, 1, 2, 3, 4, 5, 6] : this.selectedDaysOfWeek();
  }

  async onSubmit(): Promise<void> {
    if (this.isSelfService() && !this.isEditMode() && this.scheduleMode() === 'range') {
      const startDate = this.form.controls.effectiveStartDate.value!;
      const endDate = this.effectiveEndDate.value!;
      if (!endDate || endDate < startDate || this.effectiveDaysOfWeek().length === 0 || this.selectedOccurrenceCount() === 0) {
        this.errorMessage.set('กรุณาเลือกช่วงวันที่และวันในสัปดาห์ให้ถูกต้อง');
        this.form.markAllAsTouched();
        return;
      }
    }

    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.conflictMessages.set([]);

    const value = this.form.getRawValue();
    const payload = {
      coachId: value.coachId!,
      startTime: value.startTime!,
      endTime: value.endTime!,
      effectiveStartDate: value.effectiveStartDate!,
      remarks: value.remarks || null,
    };

    try {
      if (this.isEditMode()) {
        await this.routineScheduleService.update(this.routineScheduleId()!, payload);
      } else if (this.isSelfService()) {
        if (this.scheduleMode() === 'range') {
          await this.routineScheduleService.createOwnBatch({
            startTime: payload.startTime,
            endTime: payload.endTime,
            startDate: payload.effectiveStartDate,
            endDate: this.effectiveEndDate.value!,
            daysOfWeek: this.effectiveDaysOfWeek(),
            remarks: payload.remarks,
          });
        } else {
          await this.routineScheduleService.createOwn({
            startTime: payload.startTime,
            endTime: payload.endTime,
            effectiveStartDate: payload.effectiveStartDate,
            remarks: payload.remarks,
          });
        }
      } else {
        await this.routineScheduleService.create(payload);
      }

      await this.router.navigateByUrl(this.isSelfService() ? '/coach/home?view=calendar' : '/routine-schedules');
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

function getTodayIsoDate(): string {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, '0');
  const day = String(today.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function parseIsoDate(value: string | null): Date | null {
  if (!value || !/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
  const [year, month, day] = value.split('-').map(Number);
  const date = new Date(year, month - 1, day);
  return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day
    ? date
    : null;
}
