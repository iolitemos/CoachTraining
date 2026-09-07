import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideListFilter, LucideSearch } from '@lucide/angular';

/**
 * Shared search/filter toolbar per skill.md "Data Listing UI Standards":
 * search input, search button, and filter button stay in one row; advanced
 * filters are opened in a modal/drawer/popup by the host page.
 */
@Component({
  selector: 'app-search-filter-toolbar',
  imports: [FormsModule, LucideSearch, LucideListFilter],
  templateUrl: './search-filter-toolbar.html',
  styleUrl: './search-filter-toolbar.css',
})
export class SearchFilterToolbar {
  placeholder = input('ค้นหา...');
  /** Number of advanced filters currently applied, shown as a badge on the filter button. */
  activeFilterCount = input(0);

  searchTerm = '';

  search = output<string>();
  filterClick = output<void>();

  onSubmit(): void {
    this.search.emit(this.searchTerm.trim());
  }
}
