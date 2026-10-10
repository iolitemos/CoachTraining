import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { ParentRoutinePlanCalendar } from '../../models/parent-routine-plan.model';
import { ParentRoutinePlanService } from '../../services/parent-routine-plan.service';
import { ParentRoutinePlan } from './parent-routine-plan';

class ParentRoutinePlanServiceStub {
  savedDates: string[] = [];
  readonly calendar: ParentRoutinePlanCalendar = {
    athleteId: 1,
    athleteNickname: 'ปิง',
    athleteFullName: 'นักกีฬาหนึ่ง',
    dates: [{
      trainingDate: '2026-09-26',
      isSelected: false,
    }],
    competitionMatches: [{ name: 'ชิงแชมป์เยาวชน', province: 'กรุงเทพฯ', startDate: '2026-09-25', endDate: '2026-09-27' }],
    privateSessions: [{
      sessionDate: '2026-09-28',
      startTime: '17:00:00',
      endTime: '18:00:00',
      coachNickname: 'หนึ่ง',
      location: 'โต๊ะ 1',
      status: 'Scheduled',
    }],
  };
  async getCalendar(): Promise<ParentRoutinePlanCalendar> { return structuredClone(this.calendar); }
  async save(_token: string, _startDate: string, _endDate: string, selectedDates: string[]): Promise<ParentRoutinePlanCalendar> {
    this.savedDates = selectedDates;
    return { ...structuredClone(this.calendar), dates: this.calendar.dates.map(date => ({ ...date, isSelected: selectedDates.includes(date.trainingDate) })) };
  }
}

describe('ParentRoutinePlan', () => {
  let fixture: ComponentFixture<ParentRoutinePlan>;
  let component: ParentRoutinePlan;
  let service: ParentRoutinePlanServiceStub;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ParentRoutinePlan],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'secret-token' } } } },
        { provide: ParentRoutinePlanService, useClass: ParentRoutinePlanServiceStub },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(ParentRoutinePlan);
    component = fixture.componentInstance;
    service = TestBed.inject(ParentRoutinePlanService) as unknown as ParentRoutinePlanServiceStub;
    component.visibleMonth.set(new Date(2026, 8, 1));
    await component.load();
  });

  it('shows only available dates and saves selected dates', async () => {
    expect(component.dates()).toHaveLength(1);
    await component.selectDate('2026-09-26');
    expect(service.savedDates).toEqual(['2026-09-26']);
    expect(component.selectedDayCount()).toBe(1);
    expect(component.message()).toContain('บันทึก');
    expect(component.messageType()).toBe('success');
  });

  it('shows selected dates in the calendar while preserving the monthly summary', () => {
    component.dates.set([{ ...service.calendar.dates[0], isSelected: true }]);
    expect(component.selectedDayCount()).toBe(1);
    expect(component.calendarDays().find(day => day.isoDate === '2026-09-26')?.planDate?.isSelected).toBe(true);
  });

  it('requires confirmation before clearing a selected date', async () => {
    component.dates.set([{ ...service.calendar.dates[0], isSelected: true }]);
    await component.selectDate('2026-09-26');
    expect(component.pendingCancellationDate()).toBe('2026-09-26');
    expect(service.savedDates).toEqual([]);
    await component.confirmCancellation();
    expect(service.savedDates).toEqual([]);
    expect(component.pendingCancellationDate()).toBeNull();
    expect(component.message()).toContain('ยกเลิก');
  });

  it('marks every calendar date covered by a competition', async () => {
    expect(component.calendarDays().find(day => day.isoDate === '2026-09-25')?.competitionMatches).toHaveLength(1);
    expect(component.calendarDays().find(day => day.isoDate === '2026-09-27')?.competitionMatches).toHaveLength(1);
    expect(component.calendarDays().find(day => day.isoDate === '2026-09-28')?.competitionMatches).toHaveLength(0);
    component.state.set('ready');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.competition-trophy').length).toBe(3);
  });

  it('shows assigned Private Training as read-only calendar information', async () => {
    const privateDay = component.calendarDays().find(day => day.isoDate === '2026-09-28');
    expect(privateDay?.privateSessions).toHaveLength(1);

    component.state.set('ready');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const privateSummary = fixture.nativeElement.querySelector('[aria-labelledby="private-session-heading"]') as HTMLElement;
    expect(privateSummary.textContent).toContain('ตารางเรียนส่วนตัวเดือนนี้');
    expect(privateSummary.textContent).toContain('สำหรับตรวจสอบตารางเรียนส่วนตัวเท่านั้น');
    expect(privateSummary.textContent).toContain('17:00–18:00');
    expect(privateSummary.textContent).toContain('โค้ชหนึ่ง');
    expect(privateSummary.textContent).not.toContain('ชื่อเต็มโค้ช');
    expect(privateSummary.querySelector('button')).toBeNull();
  });

  it('uses a purple symbol without the Private label in mobile calendar cells', async () => {
    component.state.set('ready');
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const mobileMarker = fixture.nativeElement.querySelector('.private-session-mobile-marker') as HTMLElement;
    const desktopLabel = fixture.nativeElement.querySelector('.private-session-desktop-label') as HTMLElement;
    expect(mobileMarker).not.toBeNull();
    expect(mobileMarker.textContent?.trim()).toBe('');
    expect(mobileMarker.classList).toContain('bg-orange-500');
    expect(mobileMarker.classList).not.toContain('bg-violet-600');
    expect(mobileMarker.classList).toContain('sm:hidden');
    expect(desktopLabel.textContent).toContain('ส่วนตัว 1');
    expect(desktopLabel.classList).toContain('hidden');
    expect(desktopLabel.classList).toContain('sm:block');
    expect(desktopLabel.classList).toContain('bg-orange-700');
  });
});
