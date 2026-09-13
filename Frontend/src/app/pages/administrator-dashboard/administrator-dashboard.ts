import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import {
  LucideUsers,
} from '@lucide/angular';
import { PageHeader } from '../../shared/page-header/page-header';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { ErrorState } from '../../shared/error-state/error-state';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { CoachOption } from '../../models/coach.model';
import { TrainingType } from '../../models/training-session.model';
import { AdministratorDashboardResponse, CoachTeachingToday } from '../../models/administrator-dashboard.model';
import { CoachService } from '../../services/coach.service';
import { AdministratorDashboardService } from '../../services/administrator-dashboard.service';
import { DateInput } from '../../shared/date-input/date-input';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';
import { FilterStateService } from '../../services/filter-state.service';
import { CoachNamePipe } from '../../shared/coach-name/coach-name.pipe';

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
    LucideUsers,
    DateInput,
    DisplayDatePipe,
    CoachNamePipe,
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
    private readonly filterState: FilterStateService,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const today = new Date();
    const filters = this.filterState.restore<{ startDate: string; endDate: string; coachId: number | null; trainingType: string }>('administrator-dashboard', {
      startDate: this.toDateInputValue(new Date(today.getFullYear(), today.getMonth(), 1)),
      endDate: this.toDateInputValue(new Date(today.getFullYear(), today.getMonth() + 1, 0)),
      coachId: null, trainingType: '',
    }, this.route.snapshot.queryParamMap, ['coachId']);
    this.startDate = filters.startDate;
    this.endDate = filters.endDate;
    this.coachId = filters.coachId;
    this.trainingType = filters.trainingType as TrainingType | '';
    void this.coachService.getActiveOptions().then((options) => this.coachOptions.set(options));
    void this.load();
  }

  coachesByType(trainingType: TrainingType | string): CoachTeachingToday[] {
    return this.dashboard()?.coachesTeachingToday.filter((coach) => coach.trainingType === trainingType) ?? [];
  }

  attendanceCount(attendances: { athleteId: number; attendanceCount: number }[], athleteId: number): number {
    return attendances.find((attendance) => attendance.athleteId === athleteId)?.attendanceCount ?? 0;
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
    this.filterState.save('administrator-dashboard', {
      startDate: this.startDate, endDate: this.endDate, coachId: this.coachId, trainingType: this.trainingType,
    }, this.route);
    void this.load();
  }

  private toDateInputValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
