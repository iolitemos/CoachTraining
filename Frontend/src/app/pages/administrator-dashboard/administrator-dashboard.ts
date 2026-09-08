import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  LucideCalendarCheck2,
  LucideCalendarX,
  LucideCircleCheckBig,
  LucideDumbbell,
  LucideHourglass,
  LucideRepeat,
  LucideUsers,
} from '@lucide/angular';
import { PageHeader } from '../../shared/page-header/page-header';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { ErrorState } from '../../shared/error-state/error-state';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { CoachOption } from '../../models/coach.model';
import { TrainingType } from '../../models/training-session.model';
import { AdministratorDashboardResponse } from '../../models/administrator-dashboard.model';
import { CoachService } from '../../services/coach.service';
import { AdministratorDashboardService } from '../../services/administrator-dashboard.service';

type ViewState = 'loading' | 'error' | 'ready';

/**
 * Administrator Dashboard (requirement.md 9, FR-ADASH-001–004, todo.md 5.14).
 * Also available read-only to Management/Viewer (CLAUDE.md section 11) — the
 * page itself has no write actions, so no role branching is needed here.
 */
@Component({
  selector: 'app-administrator-dashboard',
  imports: [
    FormsModule,
    RouterLink,
    PageHeader,
    LoadingIndicator,
    ErrorState,
    EmptyState,
    LucideCalendarCheck2,
    LucideCalendarX,
    LucideCircleCheckBig,
    LucideDumbbell,
    LucideHourglass,
    LucideRepeat,
    LucideUsers,
  ],
  templateUrl: './administrator-dashboard.html',
  styleUrl: './administrator-dashboard.css',
})
export class AdministratorDashboard implements OnInit {
  state = signal<ViewState>('loading');
  dashboard = signal<AdministratorDashboardResponse | null>(null);
  coachOptions = signal<CoachOption[]>([]);

  startDate = '';
  endDate = '';
  coachId: number | null = null;
  trainingType: TrainingType | '' = '';

  constructor(
    private readonly dashboardService: AdministratorDashboardService,
    private readonly coachService: CoachService,
  ) {}

  ngOnInit(): void {
    void this.coachService.getActiveOptions().then((options) => this.coachOptions.set(options));
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.dashboard.set(
        await this.dashboardService.get({
          startDate: this.startDate || null,
          endDate: this.endDate || null,
          coachId: this.coachId,
          trainingType: this.trainingType || null,
        }),
      );
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  applyFilters(): void {
    void this.load();
  }
}
