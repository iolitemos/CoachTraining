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
          useValue: {
            snapshot: { paramMap: convertToParamMap({}), queryParamMap: convertToParamMap({}) },
          },
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

    httpMock
      .expectOne((r) => r.url.endsWith('/coaches/options'))
      .flush({ message: 'Success', data: [] });

    await initPromise;

    expect(component).toBeTruthy();
    expect(component.isEditMode()).toBe(false);
    expect(component.loading()).toBe(false);
    expect(component.form.controls.startTime.value).toBe('18:30');
    expect(component.form.controls.endTime.value).toBe('20:30');
  });

  it('should require coach, time, and effective start date before submit', async () => {
    const initPromise = component.ngOnInit();
    httpMock
      .expectOne((r) => r.url.endsWith('/coaches/options'))
      .flush({ message: 'Success', data: [] });
    await initPromise;

    component.form.patchValue({ startTime: '', endTime: '', effectiveStartDate: '' });
    await component.onSubmit();

    expect(component.form.controls.coachId.invalid).toBe(true);
    expect(component.form.controls.startTime.invalid).toBe(true);
    expect(component.form.controls.endTime.invalid).toBe(true);
    expect(component.form.controls.effectiveStartDate.invalid).toBe(true);
    expect(component.submitting()).toBe(false);
  });

  it('should hide system-managed schedule fields from the form', async () => {
    fixture.detectChanges();
    httpMock
      .expectOne((r) => r.url.endsWith('/coaches/options'))
      .flush({ message: 'Success', data: [] });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('#name')).toBeNull();
    expect(fixture.nativeElement.querySelector('#dayOfWeek')).toBeNull();
    expect(fixture.nativeElement.querySelector('#effectiveEndDate')).toBeNull();
    expect(fixture.nativeElement.querySelector('#recurrencePattern')).toBeNull();
  });

  it('should derive hidden create values from the selected date', async () => {
    const initPromise = component.ngOnInit();
    httpMock
      .expectOne((r) => r.url.endsWith('/coaches/options'))
      .flush({ message: 'Success', data: [] });
    await initPromise;

    component.form.patchValue({ coachId: 1, effectiveStartDate: '2026-01-06' });
    const submitPromise = component.onSubmit();
    const request = httpMock.expectOne((r) => r.url.endsWith('/routine-schedules'));

    expect(request.request.body).toMatchObject({
      startTime: '18:30',
      endTime: '20:30',
      effectiveStartDate: '2026-01-06',
    });
    expect(request.request.body).not.toHaveProperty('name');
    expect(request.request.body).not.toHaveProperty('dayOfWeek');
    expect(request.request.body).not.toHaveProperty('effectiveEndDate');
    expect(request.request.body).not.toHaveProperty('recurrencePattern');

    request.flush({ message: 'Success', data: { schedule: null, error: null, conflicts: [] } });
    await submitPromise;
  });

  it('should prefill the effective date selected from the calendar', async () => {
    (
      TestBed.inject(ActivatedRoute).snapshot as {
        queryParamMap: ReturnType<typeof convertToParamMap>;
      }
    ).queryParamMap = convertToParamMap({ date: '2026-01-05' });

    const initPromise = component.ngOnInit();
    httpMock
      .expectOne((r) => r.url.endsWith('/coaches/options'))
      .flush({ message: 'Success', data: [] });
    await initPromise;

    expect(component.form.controls.effectiveStartDate.value).toBe('2026-01-05');
  });

  it('should show schedule-conflict messages on a 409 response', async () => {
    const initPromise = component.ngOnInit();
    httpMock
      .expectOne((r) => r.url.endsWith('/coaches/options'))
      .flush({ message: 'Success', data: [] });
    await initPromise;

    component.form.setValue({
      coachId: 1,
      startTime: '09:00',
      endTime: '10:00',
      effectiveStartDate: '2026-01-01',
      remarks: '',
    });

    const submitPromise = component.onSubmit();

    httpMock
      .expectOne((r) => r.url.endsWith('/routine-schedules'))
      .flush(
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

  it('should create a Coach self-service schedule without loading or sending a coach id', async () => {
    (
      TestBed.inject(ActivatedRoute).snapshot as {
        data?: Record<string, unknown>;
      }
    ).data = { selfService: true };

    await component.ngOnInit();
    component.form.patchValue({
      startTime: '18:00',
      endTime: '20:00',
      effectiveStartDate: '2026-09-10',
    });

    const submitPromise = component.onSubmit();
    const request = httpMock.expectOne((r) => r.url.endsWith('/coach/routine-schedules'));

    expect(request.request.body).toEqual({
      startTime: '18:00',
      endTime: '20:00',
      effectiveStartDate: '2026-09-10',
      remarks: null,
    });
    request.flush({ message: 'Success', data: { schedule: null, error: null, conflicts: [] } });
    await submitPromise;

    expect(component.isSelfService()).toBe(true);
  });
});
