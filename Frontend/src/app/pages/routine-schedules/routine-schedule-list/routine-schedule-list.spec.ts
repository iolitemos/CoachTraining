import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { RoutineScheduleList } from './routine-schedule-list';

describe('RoutineScheduleList', () => {
  let component: RoutineScheduleList;
  let fixture: ComponentFixture<RoutineScheduleList>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineScheduleList],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(RoutineScheduleList);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the first page of routine schedules', async () => {
    const loadPromise = component.load();

    httpMock
      .expectOne((r) => r.url.endsWith('/routine-schedules'))
      .flush({
        message: 'Success',
        data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
      });

    await loadPromise;

    expect(component).toBeTruthy();
    expect(component.state()).toBe('ready');
  });

  it('should show the error state when loading fails', async () => {
    const loadPromise = component.load();

    httpMock
      .expectOne((r) => r.url.endsWith('/routine-schedules'))
      .flush({ message: 'error' }, { status: 500, statusText: 'Server Error' });

    await loadPromise;

    expect(component.state()).toBe('error');
  });

  it('should delete a selected schedule and reload the list', async () => {
    component.pendingDelete.set({
      routineScheduleId: 7,
      coachId: 1,
      coachCode: 'C001',
      coachFullName: 'Coach One',
      coachNickname: 'หนึ่ง',
      coachColorHex: '#0EA5E9',
      startTime: '18:30:00',
      endTime: '20:30:00',
      effectiveStartDate: '2026-09-02',
      isActive: true,
    });

    const deletePromise = component.confirmDelete();
    httpMock
      .expectOne((r) => r.url.endsWith('/routine-schedules/7') && r.method === 'DELETE')
      .flush({
        message: 'ลบตารางฝึกซ้อมสำเร็จ',
        data: {},
      });
    await new Promise((resolve) => setTimeout(resolve));
    const reloadRequests = httpMock.match(
      (r) => r.url.endsWith('/routine-schedules') && r.method === 'GET',
    );
    expect(reloadRequests.length).toBeGreaterThan(0);
    reloadRequests.forEach((request) =>
      request.flush({
        message: 'Success',
        data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 },
      }),
    );
    await deletePromise;

    expect(component.pendingDelete()).toBeNull();
    expect(component.state()).toBe('ready');
  });
});
