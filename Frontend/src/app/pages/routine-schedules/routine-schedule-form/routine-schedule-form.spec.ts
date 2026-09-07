import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { RoutineScheduleForm } from './routine-schedule-form';

describe('RoutineScheduleForm', () => {
  let component: RoutineScheduleForm;
  let fixture: ComponentFixture<RoutineScheduleForm>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineScheduleForm],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RoutineScheduleForm);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create in "add" mode and load active coach options', async () => {
    const initPromise = component.ngOnInit();

    httpMock.expectOne((r) => r.url.endsWith('/coaches/options')).flush({ message: 'Success', data: [] });

    await initPromise;

    expect(component).toBeTruthy();
    expect(component.isEditMode()).toBe(false);
    expect(component.loading()).toBe(false);
  });

  it('should require coach, day, time, and effective start date before submit', async () => {
    const initPromise = component.ngOnInit();
    httpMock.expectOne((r) => r.url.endsWith('/coaches/options')).flush({ message: 'Success', data: [] });
    await initPromise;

    await component.onSubmit();

    expect(component.form.controls.coachId.invalid).toBe(true);
    expect(component.form.controls.dayOfWeek.invalid).toBe(true);
    expect(component.form.controls.startTime.invalid).toBe(true);
    expect(component.form.controls.effectiveStartDate.invalid).toBe(true);
    expect(component.submitting()).toBe(false);
  });

  it('should show schedule-conflict messages on a 409 response', async () => {
    const initPromise = component.ngOnInit();
    httpMock.expectOne((r) => r.url.endsWith('/coaches/options')).flush({ message: 'Success', data: [] });
    await initPromise;

    component.form.setValue({
      name: '',
      coachId: 1,
      dayOfWeek: 'Monday',
      startTime: '09:00',
      endTime: '10:00',
      effectiveStartDate: '2026-01-01',
      effectiveEndDate: '',
      recurrencePattern: 'Weekly',
      remarks: '',
    });

    const submitPromise = component.onSubmit();

    httpMock.expectOne((r) => r.url.endsWith('/routine-schedules')).flush(
      {
        message: 'พบตารางฝึกซ้อมที่ขัดแย้งกัน',
        errors: [{ field: 'coachId', message: 'โค้ชมีตารางฝึกซ้อมในเวลานี้แล้ว' }],
      },
      { status: 409, statusText: 'Conflict' },
    );

    await submitPromise;

    expect(component.conflictMessages()).toEqual(['โค้ชมีตารางฝึกซ้อมในเวลานี้แล้ว']);
    expect(component.submitting()).toBe(false);
  });
});
