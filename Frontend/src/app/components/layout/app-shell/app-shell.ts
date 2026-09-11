import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  LucideClipboardCheck,
  LucideClipboardList,
  LucideFileBarChart,
  LucideLayoutDashboard,
  LucideKeyRound,
  LucideLogOut,
  LucideMenu,
  LucideUserCheck,
  LucideUsers,
  LucideX,
  LucideCalendarDays,
  LucideUserPlus,
  LucideCalendarPlus2,
  LucideCalendarClock,
  LucideTrophy,
} from '@lucide/angular';
import { AppRole, getRoleLabel } from '../../../models/auth.model';
import { AuthService } from '../../../services/auth.service';

/**
 * Responsive application shell: top bar + side navigation + routed content.
 * Only rendered for authenticated routes (see app.routes.ts); navigation
 * items are hidden per role (CLAUDE.md section 9 — backend still enforces
 * authorization independently).
 */
@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    LucideMenu,
    LucideX,
    LucideLayoutDashboard,
    LucideUsers,
    LucideUserCheck,
    LucideClipboardCheck,
    LucideFileBarChart,
    LucideClipboardList,
    LucideLogOut,
    LucideKeyRound,
    LucideCalendarDays,
    LucideUserPlus,
    LucideCalendarPlus2,
    LucideCalendarClock,
    LucideTrophy,
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.css',
})
export class AppShell {
  readonly appRole = AppRole;

  sidebarOpen = signal(false);

  constructor(readonly authService: AuthService) {}

  toggleSidebar(): void {
    this.sidebarOpen.update((value) => !value);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  logout(): void {
    this.authService.logout();
  }

  userInitial(fullName: string | undefined): string {
    return fullName?.trim().charAt(0).toUpperCase() ?? '';
  }

  roleLabel(role: string): string {
    return getRoleLabel(role);
  }
}
