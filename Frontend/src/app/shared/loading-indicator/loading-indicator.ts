import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-indicator',
  imports: [],
  templateUrl: './loading-indicator.html',
  styleUrl: './loading-indicator.css',
})
export class LoadingIndicator {
  /** Text shown next to the spinner, e.g. "กำลังโหลดข้อมูล...". */
  label = input('กำลังโหลดข้อมูล...');
}
