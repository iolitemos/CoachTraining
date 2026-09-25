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
import { ParentRoutinePlanLinkStatus } from '../../../models/parent-routine-plan.model';
import { ParentRoutinePlanService } from '../../../services/parent-routine-plan.service';

@Component({
  selector: 'app-athlete-form',
  imports: [ReactiveFormsModule, RouterLink, PageHeader, LoadingIndicator, DateInput],
  templateUrl: './athlete-form.html',
  styleUrl: './athlete-form.css',
})
export class AthleteForm implements OnInit {
  readonly currentYear = new Date().getFullYear();
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly athleteService = inject(AthleteService);
  private readonly parentRoutinePlanService = inject(ParentRoutinePlanService);

  athleteId = signal<number | null>(null);
  isEditMode = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  planLinkStatus = signal<ParentRoutinePlanLinkStatus | null>(null);
  planLinkLoading = signal(false);
  planLinkProcessing = signal(false);
  planLinkMessage = signal<string | null>(null);

  form = this.fb.group({
    athleteCode: ['', [Validators.required, Validators.maxLength(30)]],
    athleteType: this.fb.control<AthleteType>('Affiliated', { nonNullable: true, validators: Validators.required }),
    fullName: ['', [Validators.required, Validators.maxLength(200)]],
    nickname: ['', Validators.maxLength(100)],
    dateOfBirth: [''],
    birthYear: this.fb.control<number | null>(null, [Validators.min(1900), Validators.max(this.currentYear)]),
    phoneNumber: ['', Validators.maxLength(30)],
    parentName: ['', Validators.maxLength(200)],
    parentPhoneNumber: ['', Validators.maxLength(30)],
    athleteLevel: ['', Validators.maxLength(100)],
    joinDate: [''],
    province: ['', Validators.maxLength(100)],
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
          birthYear: athlete.birthYear,
          phoneNumber: athlete.phoneNumber ?? '',
          parentName: athlete.parentName ?? '',
          parentPhoneNumber: athlete.parentPhoneNumber ?? '',
          athleteLevel: athlete.athleteLevel ?? '',
          joinDate: athlete.joinDate ?? '',
          province: athlete.province ?? '',
          remarks: athlete.remarks ?? '',
        });
        this.form.controls.athleteCode.disable();
        await this.loadPlanLink();
      }
    } catch {
      this.errorMessage.set('ไม่สามารถโหลดข้อมูลได้');
    } finally {
      this.loading.set(false);
    }
  }

  planLinkUrl(): string | null {
    const token = this.planLinkStatus()?.token;
    return token ? `${window.location.origin}/parent/routine-plan/${token}` : null;
  }

  async loadPlanLink(): Promise<void> {
    const athleteId = this.athleteId();
    if (!athleteId) return;
    this.planLinkLoading.set(true);
    try { this.planLinkStatus.set(await this.parentRoutinePlanService.getLinkStatus(athleteId)); }
    catch { this.planLinkMessage.set('ไม่สามารถโหลดข้อมูลลิงก์แผนเข้าซ้อมได้'); }
    finally { this.planLinkLoading.set(false); }
  }

  async rotatePlanLink(): Promise<void> {
    const athleteId = this.athleteId();
    if (!athleteId || this.planLinkProcessing()) return;
    if (this.planLinkStatus()?.exists && !window.confirm('ลิงก์เดิมจะใช้งานไม่ได้ทันที ต้องการสร้างลิงก์ใหม่หรือไม่?')) return;
    this.planLinkProcessing.set(true); this.planLinkMessage.set(null);
    try {
      const result = await this.parentRoutinePlanService.rotateLink(athleteId);
      this.planLinkStatus.set({ exists: true, isEnabled: true, ...result });
      this.planLinkMessage.set('สร้างลิงก์แผนเข้าซ้อมเรียบร้อยแล้ว');
    } catch { this.planLinkMessage.set('ไม่สามารถสร้างลิงก์แผนเข้าซ้อมได้'); }
    finally { this.planLinkProcessing.set(false); }
  }

  async togglePlanLinkAccess(): Promise<void> {
    const athleteId = this.athleteId();
    const current = this.planLinkStatus();
    if (!athleteId || !current || this.planLinkProcessing()) return;
    this.planLinkProcessing.set(true); this.planLinkMessage.set(null);
    try {
      this.planLinkStatus.set(await this.parentRoutinePlanService.setAccess(athleteId, !current.isEnabled));
      this.planLinkMessage.set(current.isEnabled ? 'ปิดการเข้าถึงลิงก์ชั่วคราวแล้ว' : 'เปิดการเข้าถึงลิงก์แล้ว');
    } catch { this.planLinkMessage.set('ไม่สามารถเปลี่ยนสถานะลิงก์ได้'); }
    finally { this.planLinkProcessing.set(false); }
  }

  async copyPlanLink(): Promise<void> {
    const url = this.planLinkUrl();
    if (!url) return;
    try { await navigator.clipboard.writeText(url); this.planLinkMessage.set('คัดลอกลิงก์แล้ว'); }
    catch { this.planLinkMessage.set('กรุณาเลือกลิงก์และคัดลอกด้วยตนเอง'); }
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
      birthYear: value.birthYear,
      phoneNumber: value.phoneNumber || null,
      parentName: value.parentName || null,
      parentPhoneNumber: value.parentPhoneNumber || null,
      athleteLevel: value.athleteLevel || null,
      joinDate: value.joinDate || null,
      province: value.province || null,
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
