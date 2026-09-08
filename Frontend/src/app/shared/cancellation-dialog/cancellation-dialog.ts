import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnChanges, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiErrorBody } from '../../models/paged-result.model';
import { TrainingSessionDetail } from '../../models/training-session.model';
import { CancellationService } from '../../services/cancellation.service';

/**
 * Cancellation confirmation dialog (requirement.md 4.6/9.7, FR-CR-001–003,
 * todo.md 5.11). Administrator-only; a reason is always required.
 */
@Component({
  selector: 'app-cancellation-dialog',
  imports: [FormsModule],
  templateUrl: './cancellation-dialog.html',
  styleUrl: './cancellation-dialog.css',
})
export class CancellationDialog implements OnChanges {
  trainingSessionId = input.required<number>();
  open = input(false);

  confirmed = output<TrainingSessionDetail>();
  dismissed = output<void>();

  reason = '';
  submitting = signal(false);
  errorMessage = signal<string | null>(null);

  constructor(private readonly cancellationService: CancellationService) {}

  ngOnChanges(): void {
    if (this.open()) {
      this.reason = '';
      this.errorMessage.set(null);
    }
  }

  async confirmCancel(): Promise<void> {
    if (!this.reason.trim()) {
      this.errorMessage.set('กรุณาระบุเหตุผลในการยกเลิก');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const session = await this.cancellationService.cancel(this.trainingSessionId(), { reason: this.reason.trim() });
      this.confirmed.emit(session);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.submitting.set(false);
    }
  }

  dismiss(): void {
    this.dismissed.emit();
  }

  private extractErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as ApiErrorBody | undefined;
      return body?.message ?? 'ไม่สามารถยกเลิกเซสชันฝึกซ้อมได้';
    }
    return 'ไม่สามารถยกเลิกเซสชันฝึกซ้อมได้';
  }
}
