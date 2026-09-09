import { Injectable } from '@angular/core';
import { ActivatedRoute, ParamMap, Router } from '@angular/router';
import { AuthService } from './auth.service';

export type FilterState = Record<string, string | number | null>;

/** Keeps non-sensitive filter state in the URL and, per user, for the current browser tab. */
@Injectable({ providedIn: 'root' })
export class FilterStateService {
  constructor(private readonly authService: AuthService, private readonly router: Router) {}

  restore<T extends FilterState>(key: string, defaults: T, query: ParamMap, numberKeys: string[] = []): T {
    const result: FilterState = { ...defaults, ...this.read(key) };
    for (const field of Object.keys(defaults)) {
      if (!query.has(field)) continue;
      const raw = query.get(field);
      result[field] = numberKeys.includes(field) ? this.numberOrNull(raw) : (raw ?? '');
    }
    return result as T;
  }

  save(key: string, state: FilterState, route: ActivatedRoute, urlState: FilterState = state): void {
    try {
      sessionStorage.setItem(this.storageKey(key), JSON.stringify(state));
    } catch {
      // The URL remains the fallback when session storage is unavailable.
    }
    const queryParams = Object.fromEntries(
      Object.entries(urlState).map(([field, value]) => [field, value === '' || value === null ? null : value]),
    );
    void this.router.navigate([], { relativeTo: route, queryParams, replaceUrl: true });
  }

  private read(key: string): FilterState {
    try {
      const raw = sessionStorage.getItem(this.storageKey(key));
      return raw ? (JSON.parse(raw) as FilterState) : {};
    } catch {
      return {};
    }
  }

  private storageKey(key: string): string {
    return `coach-training:filters:${this.authService.currentUser()?.userId ?? 'anonymous'}:${key}`;
  }

  private numberOrNull(raw: string | null): number | null {
    if (!raw) return null;
    const value = Number(raw);
    return Number.isFinite(value) ? value : null;
  }
}
