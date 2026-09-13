import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PublicCompetitionMatch, PublicRoutineCalendarItem } from '../../models/public-routine-calendar.model';
import { PublicRoutineCalendarService } from '../../services/public-routine-calendar.service';
import { DisplayDatePipe } from '../../shared/display-date/display-date.pipe';

type ViewState = 'loading' | 'ready' | 'error' | 'invalid';
interface CalendarDay { isoDate: string; dayNumber: number; isCurrentMonth: boolean; items: PublicRoutineCalendarItem[]; competitionMatches: PublicCompetitionMatch[]; }
const DAY_HEADERS = ['อา.', 'จ.', 'อ.', 'พ.', 'พฤ.', 'ศ.', 'ส.'];

@Component({
  selector: 'app-public-routine-calendar',
  imports: [DisplayDatePipe],
  templateUrl: './public-routine-calendar.html',
})
export class PublicRoutineCalendar implements OnInit {
  state = signal<ViewState>('loading');
  items = signal<PublicRoutineCalendarItem[]>([]);
  competitionMatches = signal<PublicCompetitionMatch[]>([]);
  visibleMonth = signal(startOfMonth(new Date()));
  selectedDate = signal(toIsoDate(new Date()));
  readonly dayHeaders = DAY_HEADERS;

  monthLabel = computed(() => new Intl.DateTimeFormat('th-TH', { month: 'long', year: 'numeric' }).format(this.visibleMonth()));
  calendarDays = computed<CalendarDay[]>(() => {
    const month = this.visibleMonth();
    const gridStart = new Date(month.getFullYear(), month.getMonth(), 1 - month.getDay());
    return Array.from({ length: 42 }, (_, index) => {
      const date = addDays(gridStart, index);
      const isoDate = toIsoDate(date);
      return {
        isoDate,
        dayNumber: date.getDate(),
        isCurrentMonth: date.getMonth() === month.getMonth(),
        items: this.items().filter(item => item.trainingDate === isoDate),
        competitionMatches: this.competitionMatches().filter(match => match.startDate <= isoDate && isoDate <= match.endDate),
      };
    });
  });
  selectedItems = computed(() => this.items().filter(item => item.trainingDate === this.selectedDate()));
  selectedCompetitionMatches = computed(() => this.competitionMatches().filter(match => match.startDate <= this.selectedDate() && this.selectedDate() <= match.endDate));

  private readonly token: string;
  constructor(route: ActivatedRoute, private readonly service: PublicRoutineCalendarService) {
    this.token = route.snapshot.paramMap.get('token') ?? '';
  }

  ngOnInit(): void { void this.load(); }
  async load(): Promise<void> {
    if (!this.token) { this.state.set('invalid'); return; }
    this.state.set('loading');
    const first = this.calendarDays()[0].isoDate;
    const last = this.calendarDays()[41].isoDate;
    try {
      const data = await this.service.getCalendar(this.token, first, last);
      this.items.set(data.schedules);
      this.competitionMatches.set(data.competitionMatches);
      this.state.set('ready');
    }
    catch (error: unknown) {
      const status = typeof error === 'object' && error !== null && 'status' in error ? (error as { status: number }).status : 0;
      this.state.set(status === 404 ? 'invalid' : 'error');
    }
  }
  moveMonth(offset: number): void {
    const current = this.visibleMonth();
    const next = new Date(current.getFullYear(), current.getMonth() + offset, 1);
    this.visibleMonth.set(next); this.selectedDate.set(toIsoDate(next)); void this.load();
  }
  selectDay(day: CalendarDay): void { this.selectedDate.set(day.isoDate); }
}

function startOfMonth(date: Date): Date { return new Date(date.getFullYear(), date.getMonth(), 1); }
function addDays(date: Date, days: number): Date { return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days); }
function toIsoDate(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
