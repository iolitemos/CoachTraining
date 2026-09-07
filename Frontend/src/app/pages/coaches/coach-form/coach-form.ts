import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { CoachService } from '../../../services/coach.service';

@Component({
  selector: 'app-coach-form',
  imports: [ReactiveFormsModule, RouterLink, PageHeader, LoadingIndicator],
  templateUrl: './coach-form.html',
  styleUrl: './coach-form.css',
})
export class CoachForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly coachService = inject(CoachService);

  coachId = signal<number | null>(null);
  isEditMode = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  linkedUsername = signal<string | null>(null);

  form = this.fb.group({
    coachCode: ['', [Validators.required, Validators.maxLength(30)]],
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    nickname: ['', Validators.maxLength(100)],
    phoneNumber: ['', Validators.maxLength(30)],
    email: ['', [Validators.email, Validators.maxLength(200)]],
    coachType: ['', Validators.maxLength(100)],
    specialization: ['', Validators.maxLength(200)],
    remarks: [''],
  });

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    const isEdit = idParam !== null;
    this.isEditMode.set(isEdit);

    if (isEdit) {
      this.coachId.set(Number(idParam));
    }

    try {
      if (isEdit) {
        const coach = await this.coachService.getById(this.coachId()!);
        this.form.patchValue({
          coachCode: coach.coachCode,
          fullName: coach.fullName,
          nickname: coach.nickname ?? '',
          phoneNumber: coach.phoneNumber ?? '',
          email: coach.email ?? '',
          coachType: coach.coachType ?? '',
          specialization: coach.specialization ?? '',
          remarks: coach.remarks ?? '',
        });
        this.linkedUsername.set(coach.linkedUsername);
        this.form.controls.coachCode.disable();
      }
    } catch {
      this.errorMessage.set('ไม่สามารถโหลดข้อมูลได้');
    } finally {
      this.loading.set(false);
    }
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    const value = this.form.getRawValue();
    const payload = {
      fullName: value.fullName!,
      nickname: value.nickname || null,
      phoneNumber: value.phoneNumber || null,
      email: value.email || null,
      coachType: value.coachType || null,
      specialization: value.specialization || null,
      remarks: value.remarks || null,
    };

    try {
      if (this.isEditMode()) {
        await this.coachService.update(this.coachId()!, payload);
      } else {
        await this.coachService.create({ coachCode: value.coachCode!, ...payload });
      }

      await this.router.navigateByUrl('/coaches');
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? (error.error as ApiErrorBody | undefined) : undefined;
      this.errorMessage.set(body?.message ?? 'บันทึกข้อมูลไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
    } finally {
      this.submitting.set(false);
    }
  }
}
