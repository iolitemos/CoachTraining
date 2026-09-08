import { SlicePipe } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideSearch, LucideX } from '@lucide/angular';
import { PageHeader } from '../../../shared/page-header/page-header';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { AthleteOption } from '../../../models/athlete.model';
import {
  AthleteAttendanceRecord,
  AthleteAttendanceReportResponse,
  ReportAttendanceStatus,
} from '../../../models/athlete-attendance-report.model';
import { TrainingType } from '../../../models/training-session.model';
import { AthleteService } from '../../../services/athlete.service';
import { AthleteAttendanceReportService } from '../../../services/athlete-attendance-report.service';

type ViewState = 'loading' | 'error' | 'ready';

/**
 * Athlete Attendance Report (requirement.md 6.19, FR-RPT-ATH-001–007,
 * todo.md 5.16). Routine attendance lists only what was explicitly recorded
 * (Present/Late) — never an inferred Absent for athletes who weren't selected
 * (FR-RPT-ATH-006). Private attendance summarizes all four statuses.
 */
@Component({
  selector: 'app-athlete-attendance-report',
  imports: [FormsModule, SlicePipe, PageHeader, LoadingIndicator, EmptyState, ErrorState, LucideSearch, LucideX],
  templateUrl: './athlete-attendance-report.html',
  styleUrl: './athlete-attendance-report.css',
})
export class AthleteAttendanceReport implements OnInit {
  state = signal<ViewState>('loading');
  report = signal<AthleteAttendanceReportResponse | null>(null);

  selectedAthlete = signal<AthleteOption | null>(null);
  athleteSearchTerm = '';
  athleteSearchResults = signal<AthleteOption[]>([]);
  athleteSearching = signal(false);
  athleteSearchError = signal<string | null>(null);

  startDate = '';
  endDate = '';

  constructor(
    private readonly athleteService: AthleteService,
    private readonly reportService: AthleteAttendanceReportService,
  ) {}

  ngOnInit(): void {
    void this.load();
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

  trainingTypeLabel(type: TrainingType): string {
    return type === 'Routine' ? 'ฝึกซ้อมประจำ' : 'ฝึกซ้อมส่วนตัว';
  }

  routineRecords(records: AthleteAttendanceRecord[]): AthleteAttendanceRecord[] {
    return records.filter((r) => r.trainingType === 'Routine');
  }

  privateRecords(records: AthleteAttendanceRecord[]): AthleteAttendanceRecord[] {
    return records.filter((r) => r.trainingType === 'Private');
  }

  statusLabel(status: ReportAttendanceStatus): string {
    return STATUS_LABELS_TH[status];
  }
}

const STATUS_LABELS_TH: Record<ReportAttendanceStatus, string> = {
  Present: 'มาเรียน',
  Absent: 'ขาด',
  Late: 'มาสาย',
  Excused: 'ลา',
};
