import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { PrivateSessionForm } from './private-session-form';

describe('PrivateSessionForm', () => {
  let component: PrivateSessionForm;
  let fixture: ComponentFixture<PrivateSessionForm>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PrivateSessionForm],
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

    fixture = TestBed.createComponent(PrivateSessionForm);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  async function initInAddMode(): Promise<void> {
    const initPromise = component.ngOnInit();
    httpMock.expectOne((r) => r.url.endsWith('/coaches/options')).flush({ message: 'Success', data: [] });
    await initPromise;
  }

  it('should create in "add" mode and load active coach options', async () => {
    await initInAddMode();

    expect(component).toBeTruthy();
    expect(component.isEditMode()).toBe(false);
    expect(component.loading()).toBe(false);
  });

  it('formats coach options as nickname followed by full name', () => {
    expect(
      component.coachPickerLabel({ nickname: 'มอส', fullName: 'สมชาย ใจดี' }),
    ).toBe('มอส - สมชาย ใจดี');
    expect(
      component.coachPickerLabel({ nickname: null, fullName: 'สมหญิง ใจดี' }),
    ).toBe('สมหญิง ใจดี');
  });

  it('should require at least one participant before submit even when the rest of the form is valid', async () => {
    await initInAddMode();

    component.form.setValue({
      coachId: 1,
      sessionDate: '2026-01-10',
      startTime: '09:00',
      endTime: '10:00',
      location: '',
      remarks: '',
    });

    await component.onSubmit();

    expect(component.errorMessage()).toBe('กรุณาเพิ่มผู้เข้าร่วมอย่างน้อยหนึ่งคน');
    expect(component.submitting()).toBe(false);
  });

  it('should create a session with a temporary participant without an Athlete Master id', async () => {
    await initInAddMode();
    component.guestName = 'ผู้เรียนทดลอง';
    component.guestPhone = '0812345678';
    component.guestRemark = 'ทดลองเรียน';
    component.addGuest();
    component.form.setValue({
      coachId: 1,
      sessionDate: '2026-01-10',
      startTime: '09:00',
      endTime: '10:00',
      location: '',
      remarks: '',
    });

    const submitPromise = component.onSubmit();
    const request = httpMock.expectOne((r) => r.url.endsWith('/private-sessions'));
    expect(request.request.body.athleteIds).toEqual([]);
    expect(request.request.body.guestParticipants).toEqual([
      { fullName: 'ผู้เรียนทดลอง', phone: '0812345678', remark: 'ทดลองเรียน' },
    ]);
    request.flush({ message: 'Success', data: {} });
    await submitPromise;
  });

  it('should not add the same athlete twice', async () => {
    await initInAddMode();

    const athlete = { athleteId: 1, athleteCode: 'A001', fullName: 'นักกีฬาทดสอบ', nickname: null };
    component.addAthlete(athlete);
    component.addAthlete(athlete);

    expect(component.selectedAthletes().length).toBe(1);
  });

  it('should show schedule-conflict messages on a 409 response', async () => {
    await initInAddMode();

    component.addAthlete({ athleteId: 1, athleteCode: 'A001', fullName: 'นักกีฬาทดสอบ', nickname: null });
    component.form.setValue({
      coachId: 1,
      sessionDate: '2026-01-10',
      startTime: '09:00',
      endTime: '10:00',
      location: '',
      remarks: '',
    });

    const submitPromise = component.onSubmit();

    httpMock.expectOne((r) => r.url.endsWith('/private-sessions')).flush(
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

  it('should create Private Training on every date in a selected range', async () => {
    await initInAddMode();
    component.addAthlete({ athleteId: 1, athleteCode: 'A001', fullName: 'นักกีฬาทดสอบ', nickname: null });
    component.setScheduleMode('range');
    component.setRangePattern('everyDay');
    component.form.setValue({
      coachId: 1,
      sessionDate: '2026-09-01',
      startTime: '09:00',
      endTime: '10:00',
      location: 'สนาม A',
      remarks: '',
    });
    component.endDate.setValue('2026-09-03');

    expect(component.selectedOccurrenceCount()).toBe(3);
    const submitPromise = component.onSubmit();
    const request = httpMock.expectOne((r) => r.url.endsWith('/private-sessions/batch'));
    expect(request.request.body).toMatchObject({
      startDate: '2026-09-01',
      endDate: '2026-09-03',
      daysOfWeek: [0, 1, 2, 3, 4, 5, 6],
      athleteIds: [1],
    });
    request.flush({ message: 'Success', data: { createdCount: 3, createdDates: ['2026-09-01', '2026-09-02', '2026-09-03'] } });
    await submitPromise;
  });

  it('should create only selected weekdays in a Private Training date range', async () => {
    await initInAddMode();
    component.addAthlete({ athleteId: 1, athleteCode: 'A001', fullName: 'นักกีฬาทดสอบ', nickname: null });
    component.setScheduleMode('range');
    component.setRangePattern('weekdays');
    component.selectedDaysOfWeek.set([1, 5]);
    component.form.patchValue({ coachId: 1, sessionDate: '2026-09-01', startTime: '09:00', endTime: '10:00' });
    component.endDate.setValue('2026-09-10');

    expect(component.selectedOccurrenceCount()).toBe(2);
    const submitPromise = component.onSubmit();
    const request = httpMock.expectOne((r) => r.url.endsWith('/private-sessions/batch'));
    expect(request.request.body.daysOfWeek).toEqual([1, 5]);
    request.flush({ message: 'Success', data: { createdCount: 2, createdDates: ['2026-09-04', '2026-09-07'] } });
    await submitPromise;
  });
});
