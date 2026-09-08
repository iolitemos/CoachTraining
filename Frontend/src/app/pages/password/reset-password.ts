import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ApiErrorBody } from '../../models/paged-result.model';
import { AuthService } from '../../services/auth.service';

@Component({ selector: 'app-reset-password', imports: [ReactiveFormsModule, RouterLink], templateUrl: './reset-password.html', styleUrl: './password-page.css' })
export class ResetPassword {
  private readonly fb = inject(FormBuilder); private readonly auth = inject(AuthService); private readonly route = inject(ActivatedRoute);
  readonly token = new URLSearchParams(this.route.snapshot.fragment ?? '').get('token') ?? '';
  form = this.fb.nonNullable.group({ newPassword: ['', [Validators.required, Validators.minLength(8)]], confirmPassword: ['', Validators.required] });
  submitting = signal(false); completed = signal(false); error = signal<string | null>(this.token ? null : 'ลิงก์ตั้งรหัสผ่านไม่ถูกต้อง');
  async submit(): Promise<void> {
    this.error.set(null);
    if (!this.token || this.form.invalid || this.form.value.newPassword !== this.form.value.confirmPassword || this.submitting()) {
      this.form.markAllAsTouched(); if (this.form.value.newPassword !== this.form.value.confirmPassword) this.error.set('รหัสผ่านและการยืนยันไม่ตรงกัน'); return;
    }
    this.submitting.set(true);
    try { await this.auth.resetPassword({ token: this.token, newPassword: this.form.value.newPassword! }); this.completed.set(true); }
    catch (e) { this.error.set(e instanceof HttpErrorResponse ? (e.error as ApiErrorBody)?.message ?? 'ไม่สามารถตั้งรหัสผ่านใหม่ได้' : 'ไม่สามารถตั้งรหัสผ่านใหม่ได้'); }
    finally { this.submitting.set(false); }
  }
}
