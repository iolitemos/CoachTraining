import { Component, computed, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { LucideSearch, LucideX } from '@lucide/angular';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { AthleteOption, athletePickerLabel } from '../../../models/athlete.model';
import {
  AthleteAttendanceRecord,
  AthleteAttendanceReportItem,
  AthleteAttendanceReportResponse,
} from '../../../models/athlete-attendance-report.model';
import { AthleteService } from '../../../services/athlete.service';
import { AthleteAttendanceReportService } from '../../../services/athlete-attendance-report.service';
import { DateInput } from '../../../shared/date-input/date-input';
import { FilterStateService } from '../../../services/filter-state.service';

type ViewState = 'loading' | 'error' | 'ready';

interface AttendanceCalendarDay {
  isoDate: string;
  dayNumber: number;
  inCurrentMonth: boolean;
  records: AthleteAttendanceRecord[];
}

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
 * Athlete Attendance Report (requirement.md 6.19, FR-RPT-ATH-001–007,
 * todo.md 5.16). This page reports actual participation only: Present and Late
 * records count as attendance, while Absent and Excused are not displayed.
 */
@Component({
  selector: 'app-athlete-attendance-report',
  imports: [FormsModule, PageHeader, LoadingIndicator, EmptyState, ErrorState, LucideSearch, LucideX, DateInput],
  templateUrl: './athlete-attendance-report.html',
  styleUrl: './athlete-attendance-report.css',
})
export class AthleteAttendanceReport implements OnInit {
  readonly athletePickerLabel = athletePickerLabel;
  state = signal<ViewState>('loading');
  report = signal<AthleteAttendanceReportResponse | null>(null);
  calendarAthlete = signal<AthleteAttendanceReportItem | null>(null);
  calendarMonth = signal(new Date());
  readonly weekdayLabels = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];
  calendarDays = computed<AttendanceCalendarDay[]>(() => {
    const athlete = this.calendarAthlete();
    const month = this.calendarMonth();
    const year = month.getFullYear();
    const monthIndex = month.getMonth();
    const firstGridDate = new Date(year, monthIndex, 1 - new Date(year, monthIndex, 1).getDay());

    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(firstGridDate.getFullYear(), firstGridDate.getMonth(), firstGridDate.getDate() + index);
      const isoDate = this.formatLocalDate(date);
      return {
        isoDate,
        dayNumber: date.getDate(),
        inCurrentMonth: date.getMonth() === monthIndex,
        records: athlete?.records.filter((record) => record.sessionDate === isoDate) ?? [],
      };
    });
  });

  selectedAthlete = signal<AthleteOption | null>(null);
  athleteSearchTerm = '';
  athleteSearchResults = signal<AthleteOption[]>([]);
  athleteSearching = signal(false);
  athleteSearchError = signal<string | null>(null);

  startDate = currentMonthRange().startDate;
  endDate = currentMonthRange().endDate;

  constructor(
    private readonly athleteService: AthleteService,
    private readonly reportService: AthleteAttendanceReportService,
    private readonly filterState: FilterStateService,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    const defaults = currentMonthRange();
    const filters = this.filterState.restore<{ athleteId: number | null; athleteLabel: string; startDate: string; endDate: string }>('athlete-attendance-report', {
      athleteId: null, athleteLabel: '', startDate: defaults.startDate, endDate: defaults.endDate,
    }, this.route.snapshot.queryParamMap, ['athleteId']);
    this.startDate = filters.startDate;
    this.endDate = filters.endDate;
    if (filters.athleteId && filters.athleteLabel) {
      this.selectedAthlete.set({ athleteId: filters.athleteId, athleteCode: '', fullName: filters.athleteLabel, nickname: null });
      void this.load();
    } else if (filters.athleteId) {
      void this.restoreAthlete(filters.athleteId);
    } else {
      void this.load();
    }
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.report.set(
        await this.reportService.get({
          athleteId: this.selectedAthlete()?.athleteId ?? null,
          startDate: this.startDate || null,
          endDate: this.endDate || null,
        }),
      );
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  applyFilters(): void {
    const athlete = this.selectedAthlete();
    const state = {
      athleteId: athlete?.athleteId ?? null,
      athleteLabel: athlete ? athletePickerLabel(athlete) : '',
      startDate: this.startDate,
      endDate: this.endDate,
    };
    this.filterState.save('athlete-attendance-report', state, this.route, {
      athleteId: state.athleteId, startDate: state.startDate, endDate: state.endDate,
    });
    void this.load();
  }

  async searchAthletes(): Promise<void> {
    this.athleteSearching.set(true);
    this.athleteSearchError.set(null);
    try {
      this.athleteSearchResults.set(await this.athleteService.searchActive(this.athleteSearchTerm.trim()));
    } catch {
      this.athleteSearchError.set('ไม่สามารถค้นหานักกีฬาได้');
    } finally {
      this.athleteSearching.set(false);
    }
  }

  selectAthlete(athlete: AthleteOption): void {
    this.selectedAthlete.set(athlete);
    this.athleteSearchResults.set([]);
    this.athleteSearchTerm = '';
  }

  clearAthlete(): void {
    this.selectedAthlete.set(null);
  }

  private async restoreAthlete(athleteId: number): Promise<void> {
    try {
      const athlete = await this.athleteService.getById(athleteId);
      this.selectedAthlete.set({
        athleteId: athlete.athleteId,
        athleteCode: athlete.athleteCode,
        fullName: athlete.fullName,
        nickname: athlete.nickname,
      });
    } catch {
      this.selectedAthlete.set(null);
    } finally {
      void this.load();
    }
  }

  openCalendar(item: AthleteAttendanceReportItem): void {
    this.calendarAthlete.set(item);
    const initialDate = item.records[0]?.sessionDate ?? this.startDate;
    const [year, month] = initialDate.split('-').map(Number);
    this.calendarMonth.set(new Date(year, month - 1, 1));
  }

  closeCalendar(): void {
    this.calendarAthlete.set(null);
  }

  changeCalendarMonth(offset: number): void {
    const current = this.calendarMonth();
    this.calendarMonth.set(new Date(current.getFullYear(), current.getMonth() + offset, 1));
  }

  calendarMonthLabel(): string {
    return new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(this.calendarMonth());
  }

  routineCount(records: AthleteAttendanceRecord[]): number {
    return records.filter((record) => record.trainingType === 'Routine').length;
  }

  privateCount(records: AthleteAttendanceRecord[]): number {
    return records.filter((record) => record.trainingType === 'Private').length;
  }

  private formatLocalDate(date: Date): string {
    const monthPart = String(date.getMonth() + 1).padStart(2, '0');
    const dayPart = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${monthPart}-${dayPart}`;
  }

}
