import { Component, OnChanges, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideCircleAlert } from '@lucide/angular';
import { LoadingIndicator } from '../loading-indicator/loading-indicator';
import { ErrorState } from '../error-state/error-state';
import { PrivateAttendanceRosterItem, PrivateAttendanceStatus } from '../../models/private-attendance.model';
import { PrivateAttendanceService } from '../../services/private-attendance.service';
import { DisplayDateTimePipe } from '../display-date-time/display-date-time.pipe';

type ViewState = 'loading' | 'error' | 'ready';

/**
 * Private Attendance (requirement.md 9.4, todo.md 5.8). The roster always
 * lists every assigned athlete; missing attendance status is highlighted and
 * blocks submission (FR-PATT-001/002).
 */
@Component({
  selector: 'app-private-attendance',
  imports: [FormsModule, LoadingIndicator, ErrorState, LucideCircleAlert, DisplayDateTimePipe],
  templateUrl: './private-attendance.html',
  styleUrl: './private-attendance.css',
})
export class PrivateAttendance implements OnChanges {
  trainingSessionId = input.required<number>();
  readOnly = input(false);

  /** FR-PATT-002 — lets the host page (Coach Session) block Submit until every
   * assigned athlete has a recorded attendance status. */
  completeChange = output<boolean>();

  state = signal<ViewState>('loading');
  athletes = signal<PrivateAttendanceRosterItem[]>([]);
  isComplete = signal(false);
  savingIds = signal<Set<number>>(new Set());
  actionError = signal<string | null>(null);

  constructor(private readonly privateAttendanceService: PrivateAttendanceService) {}

  ngOnChanges(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const roster = await this.privateAttendanceService.getRoster(this.trainingSessionId());
      this.athletes.set(roster.athletes);
      this.setComplete(roster.isComplete);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  needsRemark(status: PrivateAttendanceStatus | null): boolean {
    return status === 'Absent' || status === 'Excused';
  }

  async updateStatus(item: PrivateAttendanceRosterItem, status: PrivateAttendanceStatus): Promise<void> {
    await this.save({ ...item, status, arrivalTime: status === 'Late' ? item.arrivalTime : null });
  }

  async updateArrivalTime(item: PrivateAttendanceRosterItem, arrivalTime: string): Promise<void> {
    await this.save({ ...item, arrivalTime: arrivalTime || null });
  }

  async updateRemark(item: PrivateAttendanceRosterItem, remark: string): Promise<void> {
    await this.save({ ...item, remark: remark || null });
  }

  private async save(item: PrivateAttendanceRosterItem): Promise<void> {
    if (!item.status) {
      return;
    }

    this.setSaving(item.privateSessionAthleteId, true);
    this.actionError.set(null);
    try {
      const roster = await this.privateAttendanceService.set(this.trainingSessionId(), item.privateSessionAthleteId, {
        status: item.status,
        arrivalTime: item.arrivalTime,
        remark: item.remark,
      });
      this.athletes.set(roster.athletes);
      this.setComplete(roster.isComplete);
    } catch {
      this.actionError.set('ไม่สามารถบันทึกข้อมูลการเข้าร่วมได้');
    } finally {
      this.setSaving(item.privateSessionAthleteId, false);
    }
  }

  private setComplete(value: boolean): void {
    this.isComplete.set(value);
    this.completeChange.emit(value);
  }

  isSaving(athleteId: number): boolean {
    return this.savingIds().has(athleteId);
  }

  private setSaving(athleteId: number, value: boolean): void {
    this.savingIds.update((current) => {
      const next = new Set(current);
      if (value) {
        next.add(athleteId);
      } else {
        next.delete(athleteId);
      }
      return next;
    });
  }
}
