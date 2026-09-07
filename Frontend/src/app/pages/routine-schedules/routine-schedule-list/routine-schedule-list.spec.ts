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
      .flush({ message: 'Success', data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } });

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
});
