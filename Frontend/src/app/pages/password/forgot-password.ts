import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({ selector: 'app-forgot-password', imports: [ReactiveFormsModule, RouterLink], templateUrl: './forgot-password.html', styleUrl: './password-page.css' })
export class ForgotPassword {
  private readonly fb = inject(FormBuilder); private readonly auth = inject(AuthService);
  form = this.fb.nonNullable.group({ email: ['', [Validators.required, Validators.email]] });
  submitting = signal(false); sent = signal(false); error = signal<string | null>(null);
  async submit(): Promise<void> {
    if (this.form.invalid || this.submitting()) { this.form.markAllAsTouched(); return; }
    this.submitting.set(true); this.error.set(null);
    try { await this.auth.forgotPassword({ email: this.form.value.email! }); this.sent.set(true); }
    catch (e) { this.error.set(e instanceof HttpErrorResponse && e.status === 429 ? 'ส่งคำขอบ่อยเกินไป กรุณารอแล้วลองใหม่' : 'ไม่สามารถส่งคำขอได้ กรุณาลองใหม่'); }
    finally { this.submitting.set(false); }
  }
}
