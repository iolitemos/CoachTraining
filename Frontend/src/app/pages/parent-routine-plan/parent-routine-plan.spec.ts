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
});
