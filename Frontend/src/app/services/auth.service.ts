import { HttpClient } from '@angular/common/http';
import { Injectable, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiSuccessBody } from '../models/paged-result.model';
import { AppRole, ChangePasswordRequest, CurrentUser, ForgotPasswordRequest, LoginRequest, LoginResponse, ResetPasswordRequest } from '../models/auth.model';

const TOKEN_KEY = 'coachtraining.token';
const USER_KEY = 'coachtraining.user';

/**
 * Frontend authentication state (todo.md 3.3). Persists the JWT and current
 * user to localStorage so a page refresh doesn't sign the user out; the JWT
 * interceptor reads the token via getToken() on every request.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUserSignal = signal<CurrentUser | null>(this.readStoredUser());

  readonly currentUser = this.currentUserSignal.asReadonly();
  readonly isAuthenticated = computed(() => this.currentUserSignal() !== null);
  readonly roles = computed(() => this.currentUserSignal()?.roles ?? []);

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router,
  ) {}

  async login(request: LoginRequest): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<ApiSuccessBody<LoginResponse>>(`${environment.apiBaseUrl}/auth/login`, request),
    );

    localStorage.setItem(TOKEN_KEY, response.data.token);
    localStorage.setItem(USER_KEY, JSON.stringify(response.data.user));
    this.currentUserSignal.set(response.data.user);
  }

  async changePassword(request: ChangePasswordRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${environment.apiBaseUrl}/auth/change-password`, request));
  }

  async forgotPassword(request: ForgotPasswordRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${environment.apiBaseUrl}/auth/forgot-password`, request));
  }

  async resetPassword(request: ResetPasswordRequest): Promise<void> {
    await firstValueFrom(this.http.post(`${environment.apiBaseUrl}/auth/reset-password`, request));
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUserSignal.set(null);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch {
      return null;
    }
  }

  hasRole(...roles: AppRole[]): boolean {
    const current = this.currentUserSignal();
    return current !== null && roles.some((role) => current.roles.includes(role));
  }

  private readStoredUser(): CurrentUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      return raw ? (JSON.parse(raw) as CurrentUser) : null;
    } catch {
      return null;
    }
  }
}
