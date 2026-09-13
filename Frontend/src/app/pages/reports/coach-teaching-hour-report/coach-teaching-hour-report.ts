import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { CoachOption } from '../../../models/coach.model';
import { TrainingType } from '../../../models/training-session.model';
import { CoachTeachingHourReportResponse } from '../../../models/coach-teaching-hour-report.model';
import { CoachService } from '../../../services/coach.service';
import { CoachTeachingHourReportService } from '../../../services/coach-teaching-hour-report.service';
import { DateInput } from '../../../shared/date-input/date-input';
import { FilterStateService } from '../../../services/filter-state.service';
import { CoachNamePipe } from '../../../shared/coach-name/coach-name.pipe';

type ViewState = 'loading' | 'error' | 'ready';

function currentMonthRange(): { startDate: string; endDate: string } {
  const today = new Date();
  const year = today.getFullYear();
  const month = today.getMonth();
  const format = (date: Date): string => {
    const monthPart = String(date.getMonth() + 1).padStart(2, '0');
    const dayPart = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${monthPart}-${dayPart}`;
  };

  return {
    startDate: format(new Date(year, month, 1)),
    endDate: format(new Date(year, month + 1, 0)),
  };
}

/**
 * Coach teaching-day report. Days are credited to the coach who actually taught each
 * session — a substitute coach's days, not the originally assigned coach's
 * (FR-SUB, requirement.md 4.4) — noted in the page so the distinction reads
 * clearly even though the report itself aggregates by actual coach only.
 */
@Component({
  selector: 'app-coach-teaching-hour-report',
  imports: [FormsModule, PageHeader, LoadingIndicator, EmptyState, ErrorState, DateInput, CoachNamePipe],
  templateUrl: './coach-teaching-hour-report.html',
  styleUrl: './coach-teaching-hour-report.css',
})
export class CoachTeachingHourReport implements OnInit {
  state = signal<ViewState>('loading');
  report = signal<CoachTeachingHourReportResponse | null>(null);
  coachOptions = signal<CoachOption[]>([]);

  coachId: number | null = null;
  startDate = currentMonthRange().startDate;
  endDate = currentMonthRange().endDate;
  activeTrainingType = signal<TrainingType>('Routine');

  constructor(
    private readonly reportService: CoachTeachingHourReportService,
    private readonly coachService: CoachService,
    private readonly filterState: FilterStateService,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const defaults = currentMonthRange();
    const filters = this.filterState.restore<{ coachId: number | null; startDate: string; endDate: string; trainingType: string }>('coach-teaching-report', {
      coachId: null, startDate: defaults.startDate, endDate: defaults.endDate, trainingType: 'Routine',
    }, this.route.snapshot.queryParamMap, ['coachId']);
    this.coachId = filters.coachId;
    this.startDate = filters.startDate;
    this.endDate = filters.endDate;
    this.activeTrainingType.set(filters.trainingType === 'Private' ? 'Private' : 'Routine');
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
          trainingType: this.activeTrainingType(),
        }),
      );
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  applyFilters(): void {
    this.filterState.save('coach-teaching-report', {
      coachId: this.coachId, startDate: this.startDate, endDate: this.endDate, trainingType: this.activeTrainingType(),
    }, this.route);
    void this.load();
  }

  selectTrainingType(trainingType: TrainingType): void {
    if (this.activeTrainingType() === trainingType) return;
    this.activeTrainingType.set(trainingType);
    this.applyFilters();
  }
}
