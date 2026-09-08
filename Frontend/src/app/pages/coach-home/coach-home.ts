import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCalendarCheck2,
  LucideCircleCheckBig,
  LucideClipboardCheck,
  LucideDumbbell,
  LucideHourglass,
  LucideRepeat,
} from '@lucide/angular';
import { PageHeader } from '../../shared/page-header/page-header';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { ErrorState } from '../../shared/error-state/error-state';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { StatusBadge } from '../../shared/status-badge/status-badge';
import { CoachDashboardResponse, CoachDashboardSession } from '../../models/coach-dashboard.model';
import { CoachDashboardService } from '../../services/coach-dashboard.service';

type ViewState = 'loading' | 'error' | 'ready';

/**
 * Coach Home (requirement.md 9.1, FR-CDASH-001–003, todo.md 5.5). Prioritizes
 * today's/upcoming sessions, training type, status, and the required next
 * action; every session card opens the session directly (FR-CDASH-001).
 */
@Component({
  selector: 'app-coach-home',
  imports: [
    RouterLink,
    PageHeader,
    LoadingIndicator,
    ErrorState,
    EmptyState,
    StatusBadge,
    LucideArrowRight,
    LucideCalendarCheck2,
    LucideCircleCheckBig,
    LucideClipboardCheck,
    LucideDumbbell,
    LucideHourglass,
    LucideRepeat,
  ],
  templateUrl: './coach-home.html',
  styleUrl: './coach-home.css',
})
export class CoachHome implements OnInit {
  state = signal<ViewState>('loading');
  dashboard = signal<CoachDashboardResponse | null>(null);

  constructor(private readonly coachDashboardService: CoachDashboardService) {}

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.dashboard.set(await this.coachDashboardService.get());
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  trainingTypeLabel(session: CoachDashboardSession): string {
    return session.trainingType === 'Routine' ? 'ฝึกซ้อมประจำ' : 'ฝึกซ้อมส่วนตัว';
  }

  timeRange(session: CoachDashboardSession): string {
    const start = new Date(session.scheduledStartDateTime);
    const end = new Date(session.scheduledEndDateTime);
    const fmt = (d: Date) => d.toLocaleTimeString('th-TH', { hour: '2-digit', minute: '2-digit' });
    return `${fmt(start)} - ${fmt(end)}`;
  }
}
