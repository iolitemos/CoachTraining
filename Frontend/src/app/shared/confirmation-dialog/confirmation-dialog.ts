import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-confirmation-dialog',
  imports: [],
  templateUrl: './confirmation-dialog.html',
  styleUrl: './confirmation-dialog.css',
})
export class ConfirmationDialog {
  open = input(false);
  title = input('ยืนยันการทำรายการ');
  message = input('คุณต้องการดำเนินการนี้ใช่หรือไม่?');
  confirmLabel = input('ยืนยัน');
  cancelLabel = input('ยกเลิก');
  /** Use the danger style for destructive actions (e.g. cancel session, deactivate). */
  danger = input(false);
  /** Disables both actions and shows a processing state on the confirm button. */
  processing = input(false);

  confirmed = output<void>();
  cancelled = output<void>();
}
