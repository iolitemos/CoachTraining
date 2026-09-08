import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { RoutineScheduleCalendar } from './routine-schedule-calendar';

describe('RoutineScheduleCalendar', () => {
  let component: RoutineScheduleCalendar;
  let fixture: ComponentFixture<RoutineScheduleCalendar>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineScheduleCalendar],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(RoutineScheduleCalendar);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    component.visibleMonth.set(new Date(2026, 0, 1));
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should display a schedule only on its selected date', async () => {
    const loadPromise = component.load();
    httpMock
      .expectOne((request) => request.url.endsWith('/routine-schedules'))
      .flush({
        message: 'Success',
        data: {
          items: [
            {
              routineScheduleId: 1,
              coachId: 1,
              coachCode: 'C001',
              coachFullName: 'โค้ชหนึ่ง',
              coachNickname: 'หนึ่ง',
              coachColorHex: '#0EA5E9',
              startTime: '09:00:00',
              endTime: '10:00:00',
              effectiveStartDate: '2026-01-05',
              isActive: true,
            },
          ],
          page: 1,
          pageSize: 100,
          totalCount: 1,
          totalPages: 1,
        },
      });
    await loadPromise;

    expect(
      component.calendarDays().find((day) => day.isoDate === '2026-01-05')?.schedules.length,
    ).toBe(1);
    expect(
      component.calendarDays().find((day) => day.isoDate === '2026-01-12')?.schedules.length,
    ).toBe(0);
    expect(component.currentMonthOccurrenceCount()).toBe(1);
    expect(component.state()).toBe('ready');
  });

  it('should move between months and select the first day', () => {
    component.moveMonth(1);

    expect(component.visibleMonth().getMonth()).toBe(1);
    expect(component.selectedDate()).toBe('2026-02-01');
  });

  it('should render only the coach nickname with the configured color and no time', async () => {
    component.visibleMonth.set(new Date(2026, 0, 1));
    fixture.detectChanges();
    httpMock
      .expectOne((request) => request.url.endsWith('/routine-schedules'))
      .flush({
        message: 'Success',
        data: {
          items: [
            {
              routineScheduleId: 1,
              coachId: 1,
              coachCode: 'C001',
              coachFullName: 'โค้ชหนึ่ง นามสกุล',
              coachNickname: 'หนึ่ง',
              coachColorHex: '#0EA5E9',
              startTime: '09:00:00',
              endTime: '10:00:00',
              effectiveStartDate: '2026-01-05',
              isActive: true,
            },
          ],
          page: 1,
          pageSize: 100,
          totalCount: 1,
          totalPages: 1,
        },
      });
    await new Promise((resolve) => setTimeout(resolve));
    fixture.detectChanges();

    const calendarEntry = fixture.nativeElement.querySelector('.coach-event') as HTMLElement;
    expect(calendarEntry.textContent).toContain('หนึ่ง');
    expect(calendarEntry.textContent).not.toContain('09:00');
    expect(calendarEntry.textContent).not.toContain('C001');
    expect(calendarEntry.textContent).not.toContain('โค้ชหนึ่ง นามสกุล');
    expect(calendarEntry.style.getPropertyValue('--coach-color')).toBe('#0EA5E9');
  });

  it('should show the error state when schedules cannot be loaded', async () => {
    const loadPromise = component.load();
    httpMock
      .expectOne((request) => request.url.endsWith('/routine-schedules'))
      .flush({ message: 'Error' }, { status: 500, statusText: 'Server Error' });
    await loadPromise;

    expect(component.state()).toBe('error');
  });
});
