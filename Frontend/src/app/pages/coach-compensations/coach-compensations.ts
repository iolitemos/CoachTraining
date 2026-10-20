import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CoachCompensationStatement } from '../../models/coach-compensation.model';
import { CoachListItem } from '../../models/coach.model';
import { ApiErrorBody } from '../../models/paged-result.model';
import { CoachCompensationService } from '../../services/coach-compensation.service';
import { CoachService } from '../../services/coach.service';
import { CoachNamePipe } from '../../shared/coach-name/coach-name.pipe';
import { ConfirmationDialog } from '../../shared/confirmation-dialog/confirmation-dialog';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { LoadingIndicator } from '../../shared/loading-indicator/loading-indicator';
import { PageHeader } from '../../shared/page-header/page-header';
import { createCompensationStatementCanvas } from './compensation-statement-image';

type PendingAction = 'confirm' | 'paid' | null;

function currentPeriod(): string {
  const today = new Date();
  return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}`;
}

@Component({
  selector: 'app-coach-compensations',
  imports: [
    FormsModule,
    RouterLink,
    CurrencyPipe,
    PageHeader,
    LoadingIndicator,
    EmptyState,
    ConfirmationDialog,
    DisplayDatePipe,
    CoachNamePipe,
  ],
  templateUrl: './coach-compensations.html',
})
export class CoachCompensations implements OnInit {
  coaches = signal<CoachListItem[]>([]);
  statement = signal<CoachCompensationStatement | null>(null);
  loading = signal(false);
  processing = signal(false);
  exportingImage = signal(false);
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  pendingAction = signal<PendingAction>(null);

  coachId: number | null = null;
  period = currentPeriod();
  adjustmentAmount = 0;
  adjustmentReason = '';

  readonly supportsNativeShare =
    typeof navigator !== 'undefined' && typeof navigator.share === 'function';

  constructor(
    private readonly coachService: CoachService,
    private readonly compensationService: CoachCompensationService,
  ) {}

  async ngOnInit(): Promise<void> {
    try {
      const result = await this.coachService.list(1, 100, '');
      this.coaches.set(result.items);
    } catch {
      this.errorMessage.set('ไม่สามารถโหลดรายชื่อโค้ชได้');
    }
  }

  async load(): Promise<void> {
    const request = this.periodRequest();
    if (!request) return;

    this.loading.set(true);
    this.clearFeedback();
    try {
      const statement = await this.compensationService.get(request);
      this.setStatement(statement);
    } catch (error) {
      if (error instanceof HttpErrorResponse && error.status === 404) {
        this.statement.set(null);
      } else {
        this.errorMessage.set(this.apiMessage(error, 'ไม่สามารถโหลดสรุปค่าตอบแทนได้'));
      }
    } finally {
      this.loading.set(false);
    }
  }

  selectionChanged(): void {
    this.statement.set(null);
    this.adjustmentAmount = 0;
    this.adjustmentReason = '';
    this.clearFeedback();
  }

  async calculate(): Promise<void> {
    const request = this.periodRequest();
    if (!request || this.processing()) return;

    this.processing.set(true);
    this.clearFeedback();
    try {
      this.setStatement(await this.compensationService.calculate(request));
      this.successMessage.set('คำนวณค่าตอบแทนเรียบร้อยแล้ว');
    } catch (error) {
      this.errorMessage.set(this.apiMessage(error, 'ไม่สามารถคำนวณค่าตอบแทนได้'));
    } finally {
      this.processing.set(false);
    }
  }

  async exportImage(preferShare = false): Promise<void> {
    const statement = this.statement();
    if (!statement || this.exportingImage()) return;

    this.exportingImage.set(true);
    this.clearFeedback();
    try {
      await document.fonts?.ready;
      const canvas = createCompensationStatementCanvas(statement);
      const blob = await canvasToBlob(canvas);
      const fileName = `coach-compensation-${statement.coachCode}-${statement.year}-${String(statement.month).padStart(2, '0')}.png`;
      const file = new File([blob], fileName, { type: 'image/png' });

      if (preferShare && navigator.canShare?.({ files: [file] })) {
        await navigator.share({
          files: [file],
          title: `สรุปค่าตอบแทน ${this.monthLabel(statement.year, statement.month)}`,
        });
        this.successMessage.set('เปิดเมนูแชร์รูปสรุปค่าตอบแทนแล้ว');
      } else {
        downloadBlob(blob, fileName);
        this.successMessage.set(
          preferShare
            ? 'อุปกรณ์นี้ไม่รองรับการแชร์โดยตรง ระบบจึงบันทึกรูปให้แทน'
            : 'บันทึกรูปสรุปค่าตอบแทนแล้ว',
        );
      }
    } catch (error) {
      if (isShareCancellation(error)) return;
      this.errorMessage.set('ไม่สามารถสร้างรูปสรุปค่าตอบแทนได้ กรุณาลองใหม่อีกครั้ง');
    } finally {
      this.exportingImage.set(false);
    }
  }

  async saveAdjustment(): Promise<void> {
    const statement = this.statement();
    if (!statement || statement.status !== 'Draft' || this.processing()) return;

    if (this.adjustmentAmount !== 0 && !this.adjustmentReason.trim()) {
      this.errorMessage.set('กรุณาระบุเหตุผลของรายการปรับเพิ่ม/ลด');
      return;
    }

    this.processing.set(true);
    this.clearFeedback();
    try {
      this.setStatement(
        await this.compensationService.updateAdjustment(
          statement.coachCompensationStatementId,
          this.adjustmentAmount,
          this.adjustmentReason.trim() || null,
        ),
      );
      this.successMessage.set('บันทึกรายการปรับเพิ่ม/ลดแล้ว');
    } catch (error) {
      this.errorMessage.set(this.apiMessage(error, 'ไม่สามารถบันทึกรายการปรับเพิ่ม/ลดได้'));
    } finally {
      this.processing.set(false);
    }
  }

  requestAction(action: Exclude<PendingAction, null>): void {
    this.pendingAction.set(action);
  }

  cancelAction(): void {
    this.pendingAction.set(null);
  }

  async confirmAction(): Promise<void> {
    const statement = this.statement();
    const action = this.pendingAction();
    if (!statement || !action || this.processing()) return;

    this.processing.set(true);
    this.clearFeedback();
    try {
      const updated =
        action === 'confirm'
          ? await this.compensationService.confirm(statement.coachCompensationStatementId)
          : await this.compensationService.markPaid(statement.coachCompensationStatementId);
      this.setStatement(updated);
      this.successMessage.set(
        action === 'confirm' ? 'ยืนยันสรุปค่าตอบแทนแล้ว' : 'บันทึกสถานะจ่ายแล้ว',
      );
      this.pendingAction.set(null);
    } catch (error) {
      this.errorMessage.set(this.apiMessage(error, 'ไม่สามารถเปลี่ยนสถานะรายการได้'));
    } finally {
      this.processing.set(false);
    }
  }

  statusLabel(status: string): string {
    return { Draft: 'รอตรวจสอบ', Confirmed: 'ยืนยันแล้ว', Paid: 'จ่ายแล้ว' }[status] ?? status;
  }

  statusClass(status: string): string {
    if (status === 'Paid') return 'bg-success-surface text-success';
    if (status === 'Confirmed') return 'bg-primary-50 text-primary-700';
    return 'bg-warning-surface text-warning';
  }

  monthLabel(year: number, month: number): string {
    return `${THAI_MONTHS[month - 1] ?? ''} ${year}`.trim();
  }

  time(value: string): string {
    const match = /T(\d{2}:\d{2})/.exec(value);
    return match?.[1] ?? '-';
  }

  actionTitle(): string {
    return this.pendingAction() === 'paid' ? 'ยืนยันว่าจ่ายค่าตอบแทนแล้ว' : 'ยืนยันสรุปค่าตอบแทน';
  }

  actionMessage(): string {
    return this.pendingAction() === 'paid'
      ? 'หลังบันทึกเป็นจ่ายแล้ว รายการนี้จะไม่สามารถแก้ไขหรือคำนวณใหม่ได้'
      : 'หลังยืนยันแล้ว ยอดและรายละเอียด Session จะถูกล็อกและไม่สามารถคำนวณใหม่ได้';
  }

  private periodRequest(): { coachId: number; year: number; month: number } | null {
    if (!this.coachId || !/^\d{4}-\d{2}$/.test(this.period)) {
      this.errorMessage.set('กรุณาเลือกโค้ชและเดือนที่ต้องการ');
      return null;
    }

    const [year, month] = this.period.split('-').map(Number);
    return { coachId: this.coachId, year, month };
  }

  private setStatement(statement: CoachCompensationStatement): void {
    this.statement.set(statement);
    this.adjustmentAmount = statement.adjustmentAmount;
    this.adjustmentReason = statement.adjustmentReason ?? '';
  }

  private clearFeedback(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  private apiMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      return (error.error as ApiErrorBody | undefined)?.message ?? fallback;
    }
    return fallback;
  }
}

const THAI_MONTHS = [
  'มกราคม',
  'กุมภาพันธ์',
  'มีนาคม',
  'เมษายน',
  'พฤษภาคม',
  'มิถุนายน',
  'กรกฎาคม',
  'สิงหาคม',
  'กันยายน',
  'ตุลาคม',
  'พฤศจิกายน',
  'ธันวาคม',
];

function canvasToBlob(canvas: HTMLCanvasElement): Promise<Blob> {
  return new Promise((resolve, reject) => {
    canvas.toBlob(
      (blob) => (blob ? resolve(blob) : reject(new Error('Unable to create image'))),
      'image/png',
    );
  });
}

function downloadBlob(blob: Blob, fileName: string): void {
  const objectUrl = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = objectUrl;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(objectUrl);
}

function isShareCancellation(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}
