import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnChanges, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiErrorBody } from '../../models/paged-result.model';
import { ApprovalActionType, TrainingApprovalActionResponse } from '../../models/approval.model';
import { ApprovalService } from '../../services/approval.service';

interface ApprovalActionConfig {
  title: string;
  description: string;
  reasonLabel: string;
  reasonRequired: boolean;
  confirmLabel: string;
  danger: boolean;
}

const CONFIGS: Record<Exclude<ApprovalActionType, 'Submit'>, ApprovalActionConfig> = {
  Approve: {
    title: 'อนุมัติเซสชัน',
    description: 'เซสชันจะถูกอนุมัติและล็อกทันที ไม่สามารถแก้ไขข้อมูลได้อีก',
    reasonLabel: 'ความคิดเห็น (ถ้ามี)',
    reasonRequired: false,
    confirmLabel: 'อนุมัติ',
    danger: false,
  },
  Reject: {
    title: 'ตีกลับเซสชัน',
    description: 'เซสชันจะกลับไปอยู่ในสถานะเสร็จสิ้น ให้โค้ชแก้ไขและส่งตรวจใหม่',
    reasonLabel: 'เหตุผลในการตีกลับ',
    reasonRequired: true,
    confirmLabel: 'ตีกลับ',
    danger: true,
  },
  RequestRevision: {
    title: 'ขอให้แก้ไขข้อมูล',
    description: 'เซสชันจะกลับไปอยู่ในสถานะเสร็จสิ้น ให้โค้ชแก้ไขและส่งตรวจใหม่',
    reasonLabel: 'เหตุผลในการขอแก้ไข',
    reasonRequired: true,
    confirmLabel: 'ส่งคำขอแก้ไข',
    danger: false,
  },
  Unlock: {
    title: 'ปลดล็อกเซสชัน',
    description: 'เซสชันจะกลับไปแก้ไขได้อีกครั้ง การปลดล็อกจะถูกบันทึกไว้เพื่อการตรวจสอบ',
    reasonLabel: 'เหตุผลในการปลดล็อก',
    reasonRequired: true,
    confirmLabel: 'ปลดล็อก',
    danger: true,
  },
};

/**
 * Approve/Reject/Request Revision/Unlock dialog (requirement.md 6.15,
 * FR-APPROVAL-001–007, todo.md 5.13). One reusable dialog driven by `action`;
 * `null` keeps it closed. Reject/RequestRevision/Unlock require a reason;
 * Approve's comment is optional (ApprovalCommentRequest).
 */
@Component({
  selector: 'app-approval-action-dialog',
  imports: [FormsModule],
  templateUrl: './approval-action-dialog.html',
  styleUrl: './approval-action-dialog.css',
})
export class ApprovalActionDialog implements OnChanges {
  trainingSessionId = input.required<number>();
  action = input<Exclude<ApprovalActionType, 'Submit'> | null>(null);

  completed = output<TrainingApprovalActionResponse>();
  dismissed = output<void>();

  reason = '';
  submitting = signal(false);
  errorMessage = signal<string | null>(null);

  config = computed<ApprovalActionConfig | null>(() => {
    const action = this.action();
    return action ? CONFIGS[action] : null;
  });

  constructor(private readonly approvalService: ApprovalService) {}

  ngOnChanges(): void {
    if (this.action()) {
      this.reason = '';
      this.errorMessage.set(null);
    }
  }

  async confirm(): Promise<void> {
    const action = this.action();
    const config = this.config();
    if (!action || !config) {
      return;
    }

    if (config.reasonRequired && !this.reason.trim()) {
      this.errorMessage.set('กรุณาระบุเหตุผล');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    try {
      const reason = this.reason.trim() || null;
      const sessionId = this.trainingSessionId();
      let result: TrainingApprovalActionResponse;

      switch (action) {
        case 'Approve':
          result = await this.approvalService.approve(sessionId, reason);
          break;
        case 'Reject':
          result = await this.approvalService.reject(sessionId, reason!);
          break;
        case 'RequestRevision':
          result = await this.approvalService.requestRevision(sessionId, reason!);
          break;
        case 'Unlock':
          result = await this.approvalService.unlock(sessionId, reason!);
          break;
      }

      this.completed.emit(result);
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
      return body?.message ?? 'ไม่สามารถดำเนินการได้';
    }
    return 'ไม่สามารถดำเนินการได้';
  }
}
