import { Component, OnChanges, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideSearch, LucideTrash2, LucideUserPlus } from '@lucide/angular';
import { LoadingIndicator } from '../loading-indicator/loading-indicator';
import { ErrorState } from '../error-state/error-state';
import { ConfirmationDialog } from '../confirmation-dialog/confirmation-dialog';
import { AthleteOption, athletePickerLabel } from '../../models/athlete.model';
import { RoutineAttendanceItem, RoutineAttendanceStatus } from '../../models/routine-attendance.model';
import { AthleteService } from '../../services/athlete.service';
import { RoutineAttendanceService } from '../../services/routine-attendance.service';
import { DisplayDateTimePipe } from '../display-date-time/display-date-time.pipe';

type ViewState = 'loading' | 'error' | 'ready';

/**
 * Routine Attendance (requirement.md 9.3, todo.md 5.7). No pre-assigned
 * roster: the Coach searches active athletes and adds only those who
 * attended — unselected athletes are never implied as Absent (FR-RATT-002/003).
 */
@Component({
  selector: 'app-routine-attendance',
  imports: [FormsModule, LoadingIndicator, ErrorState, ConfirmationDialog, LucideSearch, LucideUserPlus, LucideTrash2, DisplayDateTimePipe],
  templateUrl: './routine-attendance.html',
  styleUrl: './routine-attendance.css',
})
export class RoutineAttendance implements OnChanges {
  readonly athletePickerLabel = athletePickerLabel;
  trainingSessionId = input.required<number>();
  readOnly = input(false);

  state = signal<ViewState>('loading');
  attendees = signal<RoutineAttendanceItem[]>([]);
  savingIds = signal<Set<number>>(new Set());
  actionError = signal<string | null>(null);

  searchTerm = '';
  searching = signal(false);
  searchError = signal<string | null>(null);
  searchResults = signal<AthleteOption[]>([]);
  adding = signal(false);

  removeTarget = signal<RoutineAttendanceItem | null>(null);
  removing = signal(false);

  constructor(
    private readonly athleteService: AthleteService,
    private readonly routineAttendanceService: RoutineAttendanceService,
  ) {}

  ngOnChanges(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      this.attendees.set(await this.routineAttendanceService.list(this.trainingSessionId()));
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  isAdded(athleteId: number): boolean {
    return this.attendees().some((a) => a.athleteId === athleteId);
  }

  async search(): Promise<void> {
    this.searching.set(true);
    this.searchError.set(null);
    try {
      this.searchResults.set(await this.athleteService.searchActive(this.searchTerm.trim()));
    } catch {
      this.searchError.set('ไม่สามารถค้นหานักกีฬาได้');
    } finally {
      this.searching.set(false);
    }
  }

  async addAthlete(athlete: AthleteOption): Promise<void> {
    if (this.isAdded(athlete.athleteId) || this.adding()) {
      return;
    }

    this.adding.set(true);
    this.actionError.set(null);
    try {
      const created = await this.routineAttendanceService.add(this.trainingSessionId(), {
        athleteId: athlete.athleteId,
        status: 'Present',
        arrivalTime: null,
        remark: null,
      });
      this.attendees.update((current) => [...current, created]);
    } catch {
      this.actionError.set('ไม่สามารถเพิ่มนักกีฬาได้');
    } finally {
      this.adding.set(false);
    }
  }

  async updateStatus(item: RoutineAttendanceItem, status: RoutineAttendanceStatus): Promise<void> {
    await this.save({ ...item, status, arrivalTime: status === 'Late' ? item.arrivalTime : null });
  }

  async updateArrivalTime(item: RoutineAttendanceItem, arrivalTime: string): Promise<void> {
    await this.save({ ...item, arrivalTime: arrivalTime || null });
  }

  async updateRemark(item: RoutineAttendanceItem, remark: string): Promise<void> {
    await this.save({ ...item, remark: remark || null });
  }

  private async save(item: RoutineAttendanceItem): Promise<void> {
    this.setSaving(item.attendanceId, true);
    this.actionError.set(null);
    try {
      const updated = await this.routineAttendanceService.update(this.trainingSessionId(), item.attendanceId, {
        status: item.status,
        arrivalTime: item.arrivalTime,
        remark: item.remark,
      });
      this.attendees.update((current) => current.map((a) => (a.attendanceId === updated.attendanceId ? updated : a)));
    } catch {
      this.actionError.set('ไม่สามารถบันทึกข้อมูลการเข้าร่วมได้');
    } finally {
      this.setSaving(item.attendanceId, false);
    }
  }

  requestRemove(item: RoutineAttendanceItem): void {
    this.removeTarget.set(item);
  }

  cancelRemove(): void {
    this.removeTarget.set(null);
  }

  async confirmRemove(): Promise<void> {
    const target = this.removeTarget();
    if (!target) {
      return;
    }

    this.removing.set(true);
    try {
      await this.routineAttendanceService.remove(this.trainingSessionId(), target.attendanceId);
      this.attendees.update((current) => current.filter((a) => a.attendanceId !== target.attendanceId));
      this.removeTarget.set(null);
    } catch {
      this.actionError.set('ไม่สามารถลบการเข้าร่วมได้');
    } finally {
      this.removing.set(false);
    }
  }

  isSaving(attendanceId: number): boolean {
    return this.savingIds().has(attendanceId);
  }

  private setSaving(attendanceId: number, value: boolean): void {
    this.savingIds.update((current) => {
      const next = new Set(current);
      if (value) {
        next.add(attendanceId);
      } else {
        next.delete(attendanceId);
      }
      return next;
    });
  }
}
