import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { getRoleLabel } from '../../../models/auth.model';
import { CoachOption, RoleOption } from '../../../models/user.model';
import { ApiErrorBody } from '../../../models/paged-result.model';
import { UserService } from '../../../services/user.service';

@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule, RouterLink, PageHeader, LoadingIndicator],
  templateUrl: './user-form.html',
  styleUrl: './user-form.css',
})
export class UserForm implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly userService = inject(UserService);

  userId = signal<number | null>(null);
  isEditMode = signal(false);
  loading = signal(true);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  roleOptions = signal<RoleOption[]>([]);
  coachOptions = signal<CoachOption[]>([]);
  getRoleLabel = getRoleLabel;

  form = this.fb.group({
    username: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    fullName: ['', Validators.required],
    password: [''],
    roleIds: this.fb.control<number[]>([]),
    coachId: this.fb.control<number | null>(null),
  });

  async ngOnInit(): Promise<void> {
    const idParam = this.route.snapshot.paramMap.get('id');
    const isEdit = idParam !== null;
    this.isEditMode.set(isEdit);

    if (isEdit) {
      this.userId.set(Number(idParam));
      this.form.controls.username.disable();
      this.form.controls.password.clearValidators();
    } else {
      this.form.controls.password.setValidators([Validators.required, Validators.minLength(8)]);
    }

    try {
      const [roleOptions, coachOptions, user] = await Promise.all([
        this.userService.getRoleOptions(),
        this.userService.getCoachOptions(),
        isEdit ? this.userService.getById(this.userId()!) : Promise.resolve(null),
      ]);

      this.roleOptions.set(roleOptions);
      this.coachOptions.set(coachOptions);

      if (user) {
        this.form.patchValue({
          username: user.username,
          email: user.email,
          fullName: user.fullName,
          roleIds: user.roleIds,
          coachId: user.coachId,
        });
      }
    } catch {
      this.errorMessage.set('ไม่สามารถโหลดข้อมูลได้');
    } finally {
      this.loading.set(false);
    }
  }

  toggleRole(roleId: number, checked: boolean): void {
    const current = this.form.controls.roleIds.value ?? [];
    this.form.controls.roleIds.setValue(
      checked ? [...current, roleId] : current.filter((id) => id !== roleId),
    );
  }

  isRoleChecked(roleId: number): boolean {
    return (this.form.controls.roleIds.value ?? []).includes(roleId);
  }

  async onSubmit(): Promise<void> {
    if (this.form.invalid || this.submitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const value = this.form.getRawValue();

    try {
      if (this.isEditMode()) {
        const userId = this.userId()!;
        await this.userService.update(userId, { email: value.email!, fullName: value.fullName! });
        await this.userService.assignRoles(userId, value.roleIds ?? []);
        await this.userService.setCoachLink(userId, value.coachId ?? null);
      } else {
        await this.userService.create({
          username: value.username!,
          email: value.email!,
          fullName: value.fullName!,
          password: value.password!,
          roleIds: value.roleIds ?? [],
        });
      }

      this.successMessage.set('บันทึกข้อมูลสำเร็จ');
      await this.router.navigateByUrl('/users');
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? (error.error as ApiErrorBody | undefined) : undefined;
      this.errorMessage.set(body?.message ?? 'บันทึกข้อมูลไม่สำเร็จ กรุณาลองใหม่อีกครั้ง');
    } finally {
      this.submitting.set(false);
    }
  }
}
