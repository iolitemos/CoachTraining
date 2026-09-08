import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { CoachOption } from '../../../models/coach.model';
import { TrainingType } from '../../../models/training-session.model';
import { CoachTeachingHourReportResponse } from '../../../models/coach-teaching-hour-report.model';
import { CoachService } from '../../../services/coach.service';
import { CoachTeachingHourReportService } from '../../../services/coach-teaching-hour-report.service';

type ViewState = 'loading' | 'error' | 'ready';

/**
 * Coach Teaching-Hour Report (requirement.md 6.18, FR-RPT-COACH-001–009,
 * todo.md 5.15). Hours are credited to the coach who actually taught each
 * session — a substitute coach's hours, not the originally assigned coach's
 * (FR-SUB, requirement.md 4.4) — noted in the page so the distinction reads
 * clearly even though the report itself aggregates by actual coach only.
 */
@Component({
  selector: 'app-coach-teaching-hour-report',
  imports: [FormsModule, PageHeader, LoadingIndicator, EmptyState, ErrorState],
  templateUrl: './coach-teaching-hour-report.html',
  styleUrl: './coach-teaching-hour-report.css',
})
export class CoachTeachingHourReport implements OnInit {
  state = signal<ViewState>('loading');
  report = signal<CoachTeachingHourReportResponse | null>(null);
  coachOptions = signal<CoachOption[]>([]);

  coachId: number | null = null;
  startDate = '';
  endDate = '';
  trainingType: TrainingType | '' = '';

  constructor(
    private readonly reportService: CoachTeachingHourReportService,
    private readonly coachService: CoachService,
  ) {}

  ngOnInit(): void {
    void this.coachService.getActiveOptions().then((options) => this.coachOptions.set(options));
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.report.set(
        await this.reportService.get({
          coachId: this.coachId,
          startDate: this.startDate || null,
          endDate: this.endDate || null,
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
