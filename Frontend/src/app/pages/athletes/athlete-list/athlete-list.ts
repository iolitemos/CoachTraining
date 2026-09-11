import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnInit, signal, viewChild } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeader } from '../../../shared/page-header/page-header';
import { SearchFilterToolbar } from '../../../shared/search-filter-toolbar/search-filter-toolbar';
import { LoadingIndicator } from '../../../shared/loading-indicator/loading-indicator';
import { EmptyState } from '../../../shared/empty-state/empty-state';
import { ErrorState } from '../../../shared/error-state/error-state';
import { Pagination } from '../../../shared/pagination/pagination';
import { ConfirmationDialog } from '../../../shared/confirmation-dialog/confirmation-dialog';
import { AthleteImportError, AthleteListItem, AthleteType, athleteTypeLabel } from '../../../models/athlete.model';
import { AthleteService } from '../../../services/athlete.service';
import { FilterStateService } from '../../../services/filter-state.service';

type ViewState = 'loading' | 'error' | 'ready';

@Component({
  selector: 'app-athlete-list',
  imports: [
    RouterLink,
    PageHeader,
    SearchFilterToolbar,
    LoadingIndicator,
    EmptyState,
    ErrorState,
    Pagination,
    ConfirmationDialog,
  ],
  templateUrl: './athlete-list.html',
  styleUrl: './athlete-list.css',
})
export class AthleteList implements OnInit {
  readonly athleteTypeLabel = athleteTypeLabel;
  state = signal<ViewState>('loading');
  athletes = signal<AthleteListItem[]>([]);
  page = signal(1);
  pageSize = signal(20);
  totalCount = signal(0);
  search = signal('');
  athleteType = signal<AthleteType>('Affiliated');
  age = signal<number | null>(null);

  pendingStatusChange = signal<AthleteListItem | null>(null);
  statusChangeProcessing = signal(false);
  importFileInput = viewChild<ElementRef<HTMLInputElement>>('importFileInput');
  updateFileInput = viewChild<ElementRef<HTMLInputElement>>('updateFileInput');
  downloadingTemplate = signal(false);
  importing = signal(false);
  downloadingUpdateTemplate = signal(false);
  importingUpdates = signal(false);
  importMessage = signal<string | null>(null);
  importErrors = signal<AthleteImportError[]>([]);

  constructor(private readonly athleteService: AthleteService, private readonly filterState: FilterStateService, private readonly route: ActivatedRoute) {}

  ngOnInit(): void {
    const filters = this.filterState.restore<{ search: string; page: number; athleteType: string; age: number | null }>('athletes', { search: '', page: 1, athleteType: 'Affiliated', age: null }, this.route.snapshot.queryParamMap, ['page', 'age']);
    this.search.set(filters.search);
    this.page.set(filters.page ?? 1);
    this.athleteType.set(filters.athleteType === 'General' ? 'General' : 'Affiliated');
    this.age.set(filters.age === null || filters.age === undefined ? null : Number(filters.age));
    void this.load();
  }

  async load(): Promise<void> {
    this.state.set('loading');
    try {
      const result = await this.athleteService.list(this.page(), this.pageSize(), this.search(), this.athleteType(), this.age());
      this.athletes.set(result.items);
      this.totalCount.set(result.totalCount);
      this.state.set('ready');
    } catch {
      this.state.set('error');
    }
  }

  onSearch(term: string): void {
    this.search.set(term);
    this.page.set(1);
    this.rememberFilters();
    void this.load();
  }

  onAgeInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.age.set(value === '' ? null : Number(value));
  }

  onAgeSearch(): void {
    this.page.set(1);
    this.rememberFilters();
    void this.load();
  }

  onPageChange(page: number): void {
    this.page.set(page);
    this.rememberFilters();
    void this.load();
  }

  onTypeChange(athleteType: AthleteType): void {
    if (athleteType === this.athleteType()) return;
    this.athleteType.set(athleteType);
    this.page.set(1);
    this.rememberFilters();
    void this.load();
  }

  private rememberFilters(): void {
    this.filterState.save('athletes', { search: this.search(), page: this.page(), athleteType: this.athleteType(), age: this.age() }, this.route);
  }

  requestStatusChange(athlete: AthleteListItem): void {
    this.pendingStatusChange.set(athlete);
  }

  cancelStatusChange(): void {
    this.pendingStatusChange.set(null);
  }

  async confirmStatusChange(): Promise<void> {
    const athlete = this.pendingStatusChange();
    if (!athlete) {
      return;
    }

    this.statusChangeProcessing.set(true);
    try {
      await this.athleteService.setStatus(athlete.athleteId, !athlete.isActive);
      this.pendingStatusChange.set(null);
      await this.load();
    } finally {
      this.statusChangeProcessing.set(false);
    }
  }

  async downloadTemplate(): Promise<void> {
    this.downloadingTemplate.set(true);
    this.importMessage.set(null);
    try {
      const blob = await this.athleteService.downloadImportTemplate();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'athlete-import-template.xlsx';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      this.importMessage.set('ไม่สามารถดาวน์โหลดเทมเพลตได้ กรุณาลองใหม่');
    } finally {
      this.downloadingTemplate.set(false);
    }
  }

  chooseImportFile(): void {
    this.importFileInput()?.nativeElement.click();
  }

  async downloadUpdateTemplate(): Promise<void> {
    this.downloadingUpdateTemplate.set(true);
    this.importMessage.set(null);
    try {
      const blob = await this.athleteService.downloadUpdateTemplate();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = 'athlete-bulk-update.xlsx';
      anchor.click();
      URL.revokeObjectURL(url);
    } catch {
      this.importMessage.set('ไม่สามารถดาวน์โหลดแบบฟอร์มอัปเดตได้ กรุณาลองใหม่');
    } finally {
      this.downloadingUpdateTemplate.set(false);
    }
  }

  chooseUpdateFile(): void {
    this.updateFileInput()?.nativeElement.click();
  }

  async onUpdateFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.importMessage.set(null);
    this.importErrors.set([]);
    if (!file.name.toLowerCase().endsWith('.xlsx') || file.size > 5 * 1024 * 1024) {
      this.importMessage.set('รองรับเฉพาะไฟล์ .xlsx ขนาดไม่เกิน 5 MB');
      return;
    }
    this.importingUpdates.set(true);
    try {
      const result = await this.athleteService.importUpdates(file);
      this.importMessage.set(`อัปเดตนักกีฬา ${result.importedCount} รายการสำเร็จ`);
      await this.load();
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? error.error : null;
      this.importErrors.set(Array.isArray(body?.errors) ? body.errors : []);
      this.importMessage.set(body?.message ?? 'ไม่สามารถอัปเดตข้อมูลนักกีฬาได้');
    } finally {
      this.importingUpdates.set(false);
    }
  }

  async onImportFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;

    this.importMessage.set(null);
    this.importErrors.set([]);
    if (!file.name.toLowerCase().endsWith('.xlsx') || file.size > 5 * 1024 * 1024) {
      this.importMessage.set('รองรับเฉพาะไฟล์ .xlsx ขนาดไม่เกิน 5 MB');
      return;
    }

    this.importing.set(true);
    try {
      const result = await this.athleteService.import(file);
      this.importMessage.set(`นำเข้านักกีฬา ${result.importedCount} รายการสำเร็จ`);
      await this.load();
    } catch (error) {
      const body = error instanceof HttpErrorResponse ? error.error : null;
      this.importErrors.set(Array.isArray(body?.errors) ? body.errors : []);
      this.importMessage.set(body?.message ?? 'ไม่สามารถนำเข้ารายชื่อนักกีฬาได้');
    } finally {
      this.importing.set(false);
    }
  }
}
