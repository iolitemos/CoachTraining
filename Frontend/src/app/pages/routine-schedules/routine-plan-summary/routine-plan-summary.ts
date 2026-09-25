import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RoutineParticipationPlanAthlete, RoutineParticipationPlanSummary } from '../../../models/parent-routine-plan.model';
import { ParentRoutinePlanService } from '../../../services/parent-routine-plan.service';
import { DisplayDatePipe } from '../../../shared/display-date/display-date.pipe';
import { PageHeader } from '../../../shared/page-header/page-header';

@Component({ selector: 'app-routine-plan-summary', imports: [RouterLink, DisplayDatePipe, PageHeader], templateUrl: './routine-plan-summary.html' })
export class RoutinePlanSummary implements OnInit {
  visibleMonth = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  summaries = signal<RoutineParticipationPlanSummary[]>([]); athletes = signal<RoutineParticipationPlanAthlete[]>([]);
  selectedDate = signal<string | null>(null); loading = signal(true); detailLoading = signal(false); error = signal<string | null>(null);
  constructor(private readonly service: ParentRoutinePlanService) {}
  ngOnInit(): void { void this.load(); }
  monthLabel(): string { return new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(this.visibleMonth()); }
  moveMonth(offset: number): void { const month = this.visibleMonth(); this.visibleMonth.set(new Date(month.getFullYear(), month.getMonth() + offset, 1)); this.selectedDate.set(null); this.athletes.set([]); void this.load(); }
  async load(): Promise<void> { this.loading.set(true); this.error.set(null); const [start, end] = monthRange(this.visibleMonth()); try { this.summaries.set(await this.service.getSummary(start, end)); } catch { this.error.set('ไม่สามารถโหลดข้อมูลแผนเข้าซ้อมได้'); } finally { this.loading.set(false); } }
  async selectDate(date: string): Promise<void> { this.selectedDate.set(date); this.detailLoading.set(true); try { this.athletes.set(await this.service.getAthletes(date)); } catch { this.error.set('ไม่สามารถโหลดรายชื่อนักกีฬาได้'); } finally { this.detailLoading.set(false); } }
}
function monthRange(month: Date): [string, string] { const last = new Date(month.getFullYear(), month.getMonth() + 1, 0); return [toIso(month), toIso(last)]; }
function toIso(date: Date): string { return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`; }
