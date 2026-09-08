import { Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import {
  LucideArrowRight,
  LucideCalendarDays,
  LucideChartNoAxesColumnIncreasing,
  LucideEye,
  LucideEyeOff,
  LucideLockKeyhole,
  LucideUserRound,
  LucideUsers,
} from '@lucide/angular';
import { AuthService } from '../../services/auth.service';
import { ApiErrorBody } from '../../models/paged-result.model';

@Component({
  selector: 'app-login',
  imports: [
    ReactiveFormsModule,
    LucideArrowRight,
    LucideCalendarDays,
    LucideChartNoAxesColumnIncreasing,
    LucideEye,
    LucideEyeOff,
    LucideLockKeyhole,
    LucideUserRound,
    LucideUsers,
  ],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  form = this.fb.group({
    username: ['', Validators.required],
    password: ['', Validators.required],
  });

  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  passwordVisible = signal(false);

  togglePasswordVisibility(): void {
    this.passwordVisible.update((visible) => !visible);
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    try {
      await this.authService.login({
        username: this.form.value.username!.trim(),
        password: this.form.value.password!,
      });
      await this.router.navigateByUrl('/');
    } catch (error) {
      const body =
        error instanceof HttpErrorResponse ? (error.error as ApiErrorBody | undefined) : undefined;
      this.errorMessage.set(body?.message ?? 'เข้าสู่ระบบไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
    } finally {
      this.submitting.set(false);
    }
  }
}
