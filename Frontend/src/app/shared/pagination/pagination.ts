import { Component, computed, input, output } from '@angular/core';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';

@Component({
  selector: 'app-pagination',
  imports: [LucideChevronLeft, LucideChevronRight],
  templateUrl: './pagination.html',
  styleUrl: './pagination.css',
})
export class Pagination {
  page = input.required<number>();
  pageSize = input.required<number>();
  totalCount = input.required<number>();

  pageChange = output<number>();

  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / Math.max(1, this.pageSize()))));

  rangeStart = computed(() => (this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1));
  rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.totalCount()));

  goToPrevious(): void {
    if (this.page() > 1) {
      this.pageChange.emit(this.page() - 1);
    }
  }

  goToNext(): void {
    if (this.page() < this.totalPages()) {
      this.pageChange.emit(this.page() + 1);
    }
  }
}
