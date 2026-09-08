import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnChanges, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiErrorBody } from '../../models/paged-result.model';
import { RescheduleResponse } from '../../models/reschedule.model';
import { RescheduleService } from '../../services/reschedule.service';
import { DateInput } from '../date-input/date-input';

/**
 * Reschedule dialog (requirement.md 4.7/9.7, FR-CR-004–007, todo.md 5.12).
 * Administrator-only; preserves the original session and creates a linked
 * replacement, validated for coach/athlete conflicts.
 */
@Component({
  selector: 'app-reschedule-dialog',
  imports: [FormsModule, DateInput],
  templateUrl: './reschedule-dialog.html',
  styleUrl: './reschedule-dialog.css',
})
export class RescheduleDialog implements OnChanges {
  trainingSessionId = input.required<number>();
  open = input(false);

  rescheduled = output<RescheduleResponse>();
  dismissed = output<void>();

  sessionDate = '';
  startTime = '';
  endTime = '';
  remarks = '';
  overrideReason = '';

  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  conflictMessages = signal<string[]>([]);
  showOverride = signal(false);

  constructor(private readonly rescheduleService: RescheduleService) {}

  ngOnChanges(): void {
    if (this.open()) {
      this.resetForm();
    }
  }

  async submit(): Promise<void> {
    if (!this.sessionDate || !this.startTime || !this.endTime) {
      this.errorMessage.set('กรุณากรอกวันที่และเวลาให้ครบถ้วน');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.conflictMessages.set([]);

    try {
      const result = await this.rescheduleService.reschedule(this.trainingSessionId(), {
        sessionDate: this.sessionDate,
        startTime: this.startTime,
        endTime: this.endTime,
        remarks: this.remarks.trim() || null,
        overrideConflict: this.showOverride(),
        overrideReason: this.showOverride() ? this.overrideReason.trim() || null : null,
      });
      this.rescheduled.emit(result);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.submitting.set(false);
    }
  }

  dismiss(): void {
    this.dismissed.emit();
  }

  private resetForm(): void {
    this.sessionDate = '';
    this.startTime = '';
    this.endTime = '';
    this.remarks = '';
    this.overrideReason = '';
    this.showOverride.set(false);
    this.errorMessage.set(null);
    this.conflictMessages.set([]);
  }

  private handleError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as ApiErrorBody | undefined;

      if (error.status === 409) {
        this.errorMessage.set(body?.message ?? 'พบตารางฝึกซ้อมที่ขัดแย้งกัน');
        this.conflictMessages.set((body?.errors ?? []).map((e) => e.message));
        this.showOverride.set(true);
        return;
      }

      this.errorMessage.set(body?.message ?? 'ไม่สามารถเลื่อนเซสชันฝึกซ้อมได้');
      return;
    }

    this.errorMessage.set('ไม่สามารถเลื่อนเซสชันฝึกซ้อมได้');
  }
}
