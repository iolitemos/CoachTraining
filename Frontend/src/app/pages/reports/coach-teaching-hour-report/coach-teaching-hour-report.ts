import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { CoachOption } from '../../../models/coach.model';
import { TrainingType } from '../../../models/training-session.model';
import { CoachTeachingHourReportItem, CoachTeachingHourReportResponse } from '../../../models/coach-teaching-hour-report.model';
import { CoachService } from '../../../services/coach.service';
import { CoachTeachingHourReportService } from '../../../services/coach-teaching-hour-report.service';
import { DateInput } from '../../../shared/date-input/date-input';
import { FilterStateService } from '../../../services/filter-state.service';
import { CoachNamePipe } from '../../../shared/coach-name/coach-name.pipe';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';

type ViewState = 'loading' | 'error' | 'ready';
type ReportTab = TrainingType | 'Competition';
type TeachingSummaryItem = CoachTeachingHourReportItem & { competitionDays: number; combinedDays: number };

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
  imports: [FormsModule, PageHeader, LoadingIndicator, EmptyState, ErrorState, DateInput, CoachNamePipe, DisplayDatePipe],
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
  activeTrainingType = signal<ReportTab>('Routine');
  teachingSummaryItems = computed<TeachingSummaryItem[]>(() => {
    const report = this.report();
    if (!report) return [];

    const competitionByCoach = new Map(report.competitionAssignments.map((item) => [item.coachId, item]));
    const items = report.items.map((item) => {
      const competitionDays = this.activeTrainingType() === 'Routine'
        ? (competitionByCoach.get(item.coachId)?.assignedDays ?? 0)
        : 0;
      competitionByCoach.delete(item.coachId);
      return { ...item, competitionDays, combinedDays: item.actualDays + competitionDays };
    });

    if (this.activeTrainingType() === 'Routine') {
      for (const competition of competitionByCoach.values()) {
        items.push({
          coachId: competition.coachId,
          coachCode: competition.coachCode,
          coachFullName: competition.coachFullName,
          coachNickname: competition.coachNickname,
          coachColorHex: competition.coachColorHex,
          sessionCount: 0,
          plannedSessionCount: 0,
          actualSessionCount: 0,
          plannedDays: 0,
          actualDays: 0,
          routineDays: 0,
          privateDays: 0,
          totalDays: 0,
          competitionDays: competition.assignedDays,
          combinedDays: competition.assignedDays,
        });
      }
    }

    return items.sort((a, b) => b.combinedDays - a.combinedDays || a.coachCode.localeCompare(b.coachCode));
  });

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
    this.activeTrainingType.set(filters.trainingType === 'Private' || filters.trainingType === 'Competition' ? filters.trainingType : 'Routine');
    void this.coachService.getActiveOptions().then((options) => this.coachOptions.set(options));
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const activeTab = this.activeTrainingType();
      this.report.set(
        await this.reportService.get({
          coachId: this.coachId,
          startDate: this.startDate || null,
          endDate: this.endDate || null,
          trainingType: activeTab === 'Competition' ? null : activeTab,
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

  selectTrainingType(trainingType: ReportTab): void {
    if (this.activeTrainingType() === trainingType) return;
    this.activeTrainingType.set(trainingType);
    this.applyFilters();
  }
}
