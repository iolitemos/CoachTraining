import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnChanges, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiErrorBody } from '../../models/paged-result.model';
import { CoachOption } from '../../models/coach.model';
import { SubstituteCoachResponse } from '../../models/substitute-coach.model';
import { CoachService } from '../../services/coach.service';
import { SubstituteCoachService } from '../../services/substitute-coach.service';
import { CoachNamePipe } from '../coach-name/coach-name.pipe';

/**
 * Substitute Coach dialog (requirement.md 4.4/9.7, FR-SUB-001–005, todo.md 5.10).
 * Administrator-only; reused wherever an eligible session screen offers the action.
 */
@Component({
  selector: 'app-substitute-coach-dialog',
  imports: [FormsModule, CoachNamePipe],
  templateUrl: './substitute-coach-dialog.html',
  styleUrl: './substitute-coach-dialog.css',
})
export class SubstituteCoachDialog implements OnChanges {
  trainingSessionId = input.required<number>();
  open = input(false);
  /** The original assigned coach — excluded from the substitute list client-side. */
  excludeCoachId = input<number | null>(null);

  assigned = output<SubstituteCoachResponse>();
  cancelled = output<void>();

  coachOptions = signal<CoachOption[]>([]);
  loadingOptions = signal(false);
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  conflictMessages = signal<string[]>([]);
  showOverride = signal(false);

  substituteCoachId: number | null = null;
  reason = '';
  overrideReason = '';

  constructor(
    private readonly coachService: CoachService,
    private readonly substituteCoachService: SubstituteCoachService,
  ) {}

  ngOnChanges(): void {
    if (this.open()) {
      this.resetForm();
      void this.loadCoachOptions();
    }
  }

  eligibleCoaches(): CoachOption[] {
    return this.coachOptions().filter((c) => c.coachId !== this.excludeCoachId());
  }

  async submit(): Promise<void> {
    if (!this.substituteCoachId || !this.reason.trim()) {
      this.errorMessage.set('กรุณาเลือกโค้ชตัวแทนและระบุเหตุผล');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    this.conflictMessages.set([]);

    try {
      const result = await this.substituteCoachService.assign(this.trainingSessionId(), {
        substituteCoachId: this.substituteCoachId,
        reason: this.reason.trim(),
        overrideConflict: this.showOverride(),
        overrideReason: this.showOverride() ? this.overrideReason.trim() || null : null,
      });
      this.assigned.emit(result);
    } catch (error) {
      this.handleError(error);
    } finally {
      this.submitting.set(false);
    }
  }

  cancel(): void {
    this.cancelled.emit();
  }

  private async loadCoachOptions(): Promise<void> {
    this.loadingOptions.set(true);
    try {
      this.coachOptions.set(await this.coachService.getActiveOptions());
    } catch {
      this.errorMessage.set('ไม่สามารถโหลดรายชื่อโค้ชได้');
    } finally {
      this.loadingOptions.set(false);
    }
  }

  private resetForm(): void {
    this.substituteCoachId = null;
    this.reason = '';
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

      this.errorMessage.set(body?.message ?? 'ไม่สามารถกำหนดโค้ชตัวแทนได้');
      return;
    }

    this.errorMessage.set('ไม่สามารถกำหนดโค้ชตัวแทนได้');
  }
}
