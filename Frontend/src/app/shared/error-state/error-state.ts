import { Component, input, output } from '@angular/core';
import { LucideRefreshCw, LucideTriangleAlert } from '@lucide/angular';

@Component({
  selector: 'app-error-state',
  imports: [LucideTriangleAlert, LucideRefreshCw],
  templateUrl: './error-state.html',
  styleUrl: './error-state.css',
})
export class ErrorState {
  message = input('เกิดข้อผิดพลาด ไม่สามารถโหลดข้อมูลได้');
  /** Hide the retry button when the caller does not support retrying. */
  retryable = input(true);

  retry = output<void>();
}
