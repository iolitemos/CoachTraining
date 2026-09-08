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
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly coachService = inject(CoachService);
  private readonly routineScheduleService = inject(RoutineScheduleService);

  routineScheduleId = signal<number | null>(null);
  isEditMode = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  conflictMessages = signal<string[]>([]);

  coachOptions = signal<CoachOption[]>([]);
  form = this.fb.group({
    coachId: this.fb.control<number | null>(null, Validators.required),
    startTime: ['18:30', Validators.required],
    endTime: ['20:30', Validators.required],
    effectiveStartDate: [getTodayIsoDate(), Validators.required],
    remarks: [''],
  });

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    const isEdit = idParam !== null;
    this.isEditMode.set(isEdit);

    if (isEdit) {
      this.routineScheduleId.set(Number(idParam));
    } else {
      this.prefillFromCalendar(this.route.snapshot.queryParamMap?.get('date'));
    }

    try {
      const [coachOptions, schedule] = await Promise.all([
        this.coachService.getActiveOptions(),
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
  }

  async onSubmit(): Promise<void> {
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
      } else {
        await this.routineScheduleService.create(payload);
      }

      await this.router.navigateByUrl('/routine-schedules');
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
