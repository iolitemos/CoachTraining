import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { AthleteService } from '../../../services/athlete.service';
import { DateInput } from '../../../shared/date-input/date-input';
import { AthleteType } from '../../../models/athlete.model';

@Component({
  selector: 'app-athlete-form',
  imports: [ReactiveFormsModule, RouterLink, PageHeader, LoadingIndicator, DateInput],
  templateUrl: './athlete-form.html',
  styleUrl: './athlete-form.css',
})
export class AthleteForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly athleteService = inject(AthleteService);

  athleteId = signal<number | null>(null);
  isEditMode = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);

  form = this.fb.group({
    athleteCode: ['', [Validators.required, Validators.maxLength(30)]],
    athleteType: this.fb.control<AthleteType>('Affiliated', { nonNullable: true, validators: Validators.required }),
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    nickname: ['', Validators.maxLength(100)],
    dateOfBirth: [''],
    phoneNumber: ['', Validators.maxLength(30)],
    parentName: ['', Validators.maxLength(200)],
    parentPhoneNumber: ['', Validators.maxLength(30)],
    athleteLevel: ['', Validators.maxLength(100)],
    joinDate: [''],
    remarks: [''],
  });

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    const isEdit = idParam !== null;
    this.isEditMode.set(isEdit);

    if (isEdit) {
      this.athleteId.set(Number(idParam));
    }

    try {
      if (isEdit) {
        const athlete = await this.athleteService.getById(this.athleteId()!);
        this.form.patchValue({
          athleteCode: athlete.athleteCode,
          athleteType: athlete.athleteType,
          fullName: athlete.fullName,
          nickname: athlete.nickname ?? '',
          dateOfBirth: athlete.dateOfBirth ?? '',
          phoneNumber: athlete.phoneNumber ?? '',
          parentName: athlete.parentName ?? '',
          parentPhoneNumber: athlete.parentPhoneNumber ?? '',
          athleteLevel: athlete.athleteLevel ?? '',
          joinDate: athlete.joinDate ?? '',
          remarks: athlete.remarks ?? '',
        });
        this.form.controls.athleteCode.disable();
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
      athleteType: value.athleteType!,
      fullName: value.fullName!,
      nickname: value.nickname || null,
      dateOfBirth: value.dateOfBirth || null,
      phoneNumber: value.phoneNumber || null,
      parentName: value.parentName || null,
      parentPhoneNumber: value.parentPhoneNumber || null,
      athleteLevel: value.athleteLevel || null,
      joinDate: value.joinDate || null,
      remarks: value.remarks || null,
    };

    try {
      if (this.isEditMode()) {
        await this.athleteService.update(this.athleteId()!, payload);
      } else {
        await this.athleteService.create({ athleteCode: value.athleteCode!, ...payload });
      }

      await this.router.navigateByUrl('/athletes');
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? (error.error as ApiErrorBody | undefined) : undefined;
      this.errorMessage.set(body?.message ?? 'บันทึกข้อมูลไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
    } finally {
      this.submitting.set(false);
    }
  }
}
