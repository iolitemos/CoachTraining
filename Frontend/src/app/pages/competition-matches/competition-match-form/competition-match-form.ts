import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { CompetitionMatchService } from '../../../services/competition-match.service';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { PageHeader } from '../../../shared/page-header/page-header';
import { DateInput } from '../../../shared/date-input/date-input';

@Component({ selector: 'app-competition-match-form', imports: [ReactiveFormsModule, RouterLink, PageHeader, LoadingIndicator, DateInput], templateUrl: './competition-match-form.html' })
export class CompetitionMatchForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly service = inject(CompetitionMatchService);
  matchId = signal<number | null>(null);
  isEditMode = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    province: ['', [Validators.required, Validators.maxLength(100)]],
    startDate: ['', Validators.required],
    endDate: ['', Validators.required],
  });

  async ngOnInit(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(id !== null);
    if (id) this.matchId.set(Number(id));
    try {
      if (id) this.form.patchValue(await this.service.getById(Number(id)));
    } catch { this.errorMessage.set('ไม่สามารถโหลดข้อมูลได้'); }
    finally { this.loading.set(false); }
  }

  dateRangeInvalid(): boolean {
    const { startDate, endDate } = this.form.getRawValue();
    return !!startDate && !!endDate && endDate < startDate;
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid || this.dateRangeInvalid() || this.submitting()) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true); this.errorMessage.set(null);
    const value = this.form.getRawValue();
    const request = { name: value.name!.trim(), province: value.province!.trim(), startDate: value.startDate!, endDate: value.endDate! };
    try {
      if (this.isEditMode()) await this.service.update(this.matchId()!, request); else await this.service.create(request);
      await this.router.navigateByUrl('/competition-matches');
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? error.error as ApiErrorBody | undefined : undefined;
      this.errorMessage.set(body?.message ?? 'บันทึกข้อมูลไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
    } finally { this.submitting.set(false); }
  }
}
