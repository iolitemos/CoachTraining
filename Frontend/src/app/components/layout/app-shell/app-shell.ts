import { Component, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  LucideCalendarHeart,
  LucideDumbbell,
  LucideLayoutDashboard,
  LucideLogOut,
  LucideMenu,
  LucideRepeat,
  LucideUserCheck,
  LucideUsers,
  LucideX,
} from '@lucide/angular';
import { AppRole } from '../../../models/auth.model';
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
    LucideDumbbell,
    LucideRepeat,
    LucideCalendarHeart,
    LucideLogOut,
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
}
