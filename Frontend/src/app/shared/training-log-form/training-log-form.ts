import { Component, OnChanges, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorBody } from '../../models/paged-result.model';
import { TrainingLog, TrainingLogUpsertRequest } from '../../models/training-session.model';
import { TrainingLogService } from '../../services/training-log.service';

/**
 * Training Log form (requirement.md 9.2 step 4, FR-LOG-001–004, todo.md 5.9).
 * No field is individually required — content is left to the Coach's discretion.
 */
@Component({
  selector: 'app-training-log-form',
  imports: [FormsModule],
  templateUrl: './training-log-form.html',
  styleUrl: './training-log-form.css',
})
export class TrainingLogForm implements OnChanges {
  trainingSessionId = input.required<number>();
  initialLog = input<TrainingLog | null>(null);
  readOnly = input(false);

  saved = output<TrainingLog>();

  form: TrainingLogUpsertRequest = this.emptyForm();
  saving = signal(false);
  savedFlag = signal(false);
  errorMessage = signal<string | null>(null);

  private lastSessionId: number | null = null;

  constructor(private readonly trainingLogService: TrainingLogService) {}

  ngOnChanges(): void {
    if (this.trainingSessionId() !== this.lastSessionId) {
      this.lastSessionId = this.trainingSessionId();
      this.resetForm();
    }
  }

  async save(): Promise<void> {
    this.saving.set(true);
    this.errorMessage.set(null);
    this.savedFlag.set(false);
    try {
      const updated = await this.trainingLogService.upsert(this.trainingSessionId(), this.form);
      this.saved.emit(updated);
      this.savedFlag.set(true);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.saving.set(false);
    }
  }

  private resetForm(): void {
    const log = this.initialLog();
    this.form = {
      topic: log?.topic ?? null,
      objective: log?.objective ?? null,
      exerciseDrill: log?.exerciseDrill ?? null,
      focus: log?.focus ?? null,
      intensity: log?.intensity ?? null,
      coachNotes: log?.coachNotes ?? null,
      athleteNotes: log?.athleteNotes ?? null,
      generalRemarks: log?.generalRemarks ?? null,
    };
    this.savedFlag.set(false);
    this.errorMessage.set(null);
  }

  private emptyForm(): TrainingLogUpsertRequest {
    return {
      topic: null,
      objective: null,
      exerciseDrill: null,
      focus: null,
      intensity: null,
      coachNotes: null,
      athleteNotes: null,
      generalRemarks: null,
    };
  }

  private extractErrorMessage(error: unknown): string {
    if (error instanceof HttpErrorResponse) {
      const body = error.error as ApiErrorBody | undefined;
      return body?.message ?? 'ไม่สามารถบันทึกบันทึกการฝึกซ้อมได้';
    }
    return 'ไม่สามารถบันทึกบันทึกการฝึกซ้อมได้';
  }
}
