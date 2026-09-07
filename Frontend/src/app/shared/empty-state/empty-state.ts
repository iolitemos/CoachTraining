import { Component, input } from '@angular/core';
import { LucideInbox } from '@lucide/angular';

@Component({
  selector: 'app-empty-state',
  imports: [LucideInbox],
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.css',
})
export class EmptyState {
  title = input('ไม่พบข้อมูล');
  description = input('ยังไม่มีข้อมูลให้แสดงในขณะนี้');
}
