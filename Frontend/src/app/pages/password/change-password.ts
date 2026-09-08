import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiErrorBody } from '../../models/paged-result.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-change-password', imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './change-password.html', styleUrl: './password-page.css',
})
export class ChangePassword {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  form = this.fb.nonNullable.group({
    currentPassword: ['', Validators.required], newPassword: ['', [Validators.required, Validators.minLength(8)]], confirmPassword: ['', Validators.required],
  });
  submitting = signal(false); message = signal<string | null>(null); error = signal<string | null>(null);

  async submit(): Promise<void> {
    this.error.set(null); this.message.set(null);
    if (this.form.invalid || this.form.value.newPassword !== this.form.value.confirmPassword || this.submitting()) {
      this.form.markAllAsTouched();
      if (this.form.value.newPassword !== this.form.value.confirmPassword) this.error.set('รหัสผ่านใหม่และการยืนยันไม่ตรงกัน');
      return;
    }
    this.submitting.set(true);
    try {
      await this.auth.changePassword({ currentPassword: this.form.value.currentPassword!, newPassword: this.form.value.newPassword! });
      this.form.reset(); this.message.set('เปลี่ยนรหัสผ่านสำเร็จ');
    } catch (e) {
      this.error.set(e instanceof HttpErrorResponse ? (e.error as ApiErrorBody)?.message ?? 'เปลี่ยนรหัสผ่านไม่สำเร็จ' : 'เปลี่ยนรหัสผ่านไม่สำเร็จ');
    } finally { this.submitting.set(false); }
  }
}
