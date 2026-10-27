import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CoachHome } from './coach-home';
import { AuthService } from '../../services/auth.service';
import { RoutineScheduleRequest } from '../../models/routine-schedule-request.model';

describe('CoachHome calendar', () => {
  let component: CoachHome;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CoachHome],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    component = TestBed.createComponent(CoachHome).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.match((request) => request.url.endsWith('/calendar-notes'))
      .forEach((request) => request.flush({ message: 'Success', data: [] }));
    httpMock.verify();
  });

  it('loads the visible calendar range through the coach-scoped session API', async () => {
    const loadPromise = component.loadCalendar();
    const request = httpMock.expectOne((r) => r.url.endsWith('/training-sessions'));

    expect(request.request.params.get('dateFrom')).toBeTruthy();
    expect(request.request.params.get('dateTo')).toBeTruthy();
    expect(request.request.params.has('coachId')).toBe(false);
    const colleaguesRequest = httpMock.expectOne((r) =>
      r.url.endsWith('/dashboard/coach/calendar-colleagues'),
    );
    const scheduleRequestsRequest = httpMock.expectOne((r) =>
      r.url.endsWith('/coach/routine-schedules/requests'),
    );
    expect(scheduleRequestsRequest.request.params.get('dateFrom')).toBeTruthy();
    expect(scheduleRequestsRequest.request.params.get('dateTo')).toBeTruthy();
    const competitionRequest = httpMock.expectOne((r) => r.url.endsWith('/competition-matches'));

    request.flush({
      message: 'Success',
      data: {
        items: [{ trainingSessionId: 1, participantNames: ['น้องเอ'] }],
        page: 1,
        pageSize: 100,
        totalCount: 1,
        totalPages: 1,
      },
    });
    scheduleRequestsRequest.flush({ message: 'Success', data: [] });
    colleaguesRequest.flush({ message: 'Success', data: [] });
    competitionRequest.flush({
      message: 'Success',
      data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 },
    });
    httpMock.expectOne((r) => r.url.endsWith('/calendar-notes')).flush({ message: 'Success', data: [] });
    await loadPromise;

    expect(component.calendarState()).toBe('ready');
    expect(component.calendarDays()).toHaveLength(42);
    expect(component.calendarSessions()[0].participantNames).toEqual(['น้องเอ']);
  });

  it('marks each day in a competition range', () => {
    component.visibleMonth.set(new Date(2026, 0, 1));
    component.calendarCompetitionMatches.set([
      {
        competitionMatchId: 1,
        coaches: [],
        name: 'ชิงแชมป์ประเทศไทย',
        province: 'เชียงใหม่',
        startDate: '2026-01-05',
        endDate: '2026-01-07',
      },
    ]);

    expect(component.calendarDays().find((day) => day.isoDate === '2026-01-04')?.competitionMatches).toHaveLength(0);
    expect(component.calendarDays().find((day) => day.isoDate === '2026-01-05')?.competitionMatches).toHaveLength(1);
    expect(component.calendarDays().find((day) => day.isoDate === '2026-01-07')?.competitionMatches).toHaveLength(1);
    expect(component.calendarDays().find((day) => day.isoDate === '2026-01-08')?.competitionMatches).toHaveLength(0);
  });

  it('switches between overview and calendar tabs', () => {
    expect(component.activeTab()).toBe('calendar');

    component.selectTab('overview');
    expect(component.activeTab()).toBe('overview');

    component.selectTab('calendar');

    expect(component.activeTab()).toBe('calendar');
  });

  it('restores and remembers the last viewed calendar month per signed-in user', () => {
    const auth = TestBed.inject(AuthService);
    const storageKey = `coach-training:filters:${auth.currentUser()?.userId ?? 'anonymous'}:coach-home-calendar`;
    sessionStorage.setItem(storageKey, JSON.stringify({ month: '2026-11-01', date: '2026-11-18' }));

    (component as any).restoreCalendarState();

    expect(component.visibleMonth().getFullYear()).toBe(2026);
    expect(component.visibleMonth().getMonth()).toBe(10);
    expect(component.selectedDate()).toBe('2026-11-18');

    component.selectTab('calendar');
    expect(sessionStorage.getItem(storageKey)).toContain('2026-11-01');
    sessionStorage.removeItem(storageKey);
  });

  it('hides overdue sessions by default and toggles their visibility', () => {
    expect(component.overdueSessionsExpanded()).toBe(false);

    component.toggleOverdueSessions();
    expect(component.overdueSessionsExpanded()).toBe(true);

    component.toggleOverdueSessions();
    expect(component.overdueSessionsExpanded()).toBe(false);
  });

  it('shows every upcoming session returned for the current month', () => {
    component.dashboard.set({
      upcomingSessions: [
        { trainingSessionId: 1, sessionDate: '2026-09-11' },
        { trainingSessionId: 2, sessionDate: '2026-09-11' },
        { trainingSessionId: 3, sessionDate: '2026-09-12' },
        { trainingSessionId: 4, sessionDate: '2026-09-13' },
        { trainingSessionId: 5, sessionDate: '2026-09-14' },
      ],
    } as never);

    expect(component.upcomingSessionsThisMonth().map((session) => session.trainingSessionId)).toEqual([
      1, 2, 3, 4, 5,
    ]);
  });

  it('formats compact session date and duration details with a Gregorian year', () => {
    const session = {
      sessionDate: '2026-09-12',
      scheduledStartDateTime: '2026-09-12T18:30:00',
      scheduledEndDateTime: '2026-09-12T20:00:00',
    } as never;

    expect(component.sessionDay(session)).toBe('12');
    expect(component.sessionMonthYear(session)).toBe('ก.ย. 2026');
    expect(component.sessionWeekday(session)).toBe('เสาร์');
    expect(component.sessionDuration(session)).toBe('1 ชม. 30 นาที');
  });

  it('immediately submits a 16:30–20:30 request by tapping a date in edit mode', async () => {
    const day = component.calendarDays()[10];

    await component.handleCalendarDayClick(day);
    httpMock.expectNone((item) => item.method === 'POST' && item.url.endsWith('/coach/routine-schedules'));

    component.toggleCalendarEditMode();
    const createPromise = component.handleCalendarDayClick(day);
    const createRequest = httpMock.expectOne((item) =>
      item.method === 'POST' && item.url.endsWith('/coach/routine-schedules'),
    );
    expect(createRequest.request.body).toEqual({
      effectiveStartDate: day.isoDate,
      startTime: '16:30',
      endTime: '20:30',
      remarks: null,
    });
    createRequest.flush({
      message: 'Success',
      data: {
        routineScheduleRequestId: 90,
        proposedDate: day.isoDate,
        proposedStartTime: '16:30:00',
        proposedEndTime: '20:30:00',
        status: 'Pending',
      },
    });
    await createPromise;

    expect(component.calendarRequests()[0].routineScheduleRequestId).toBe(90);
    expect(component.calendarActionSuccess()).toContain('16:30–20:30');
  });

  it('opens deletion confirmation by tapping a pending date and cancels the request', async () => {
    const request = {
      routineScheduleRequestId: 91,
      proposedDate: '2026-10-12',
      proposedStartTime: '16:30:00',
      proposedEndTime: '20:30:00',
      status: 'Pending',
    } as RoutineScheduleRequest;
    component.visibleMonth.set(new Date(2026, 9, 1));
    component.calendarRequests.set([request]);
    component.toggleCalendarEditMode();
    const day = component.calendarDays().find((item) => item.isoDate === request.proposedDate)!;
    await component.handleCalendarDayClick(day);
    expect(component.pendingRequestDelete()).toBe(request);

    const deletePromise = component.confirmRequestDelete();
    httpMock.expectOne((item) =>
      item.method === 'DELETE' && item.url.endsWith('/coach/routine-schedules/requests/91'),
    ).flush({ message: 'Success', data: { ...request, status: 'Cancelled' } });
    await deletePromise;

    expect(component.pendingRequestDelete()).toBeNull();
    expect(component.calendarRequests()[0].status).toBe('Cancelled');
    expect(component.calendarActionSuccess()).toBe('ลบคำขอฝึกซ้อมประจำแล้ว');
  });

  it('detects Routine and Private session types for calendar color markers', () => {
    const day = component.calendarDays()[0];
    day.sessions = [
      { trainingType: 'Routine' } as never,
      { trainingType: 'Private' } as never,
    ];

    expect(component.hasTrainingType(day, 'Routine')).toBe(true);
    expect(component.hasTrainingType(day, 'Private')).toBe(true);
    expect(component.calendarDayBackground(day)).toContain('linear-gradient');
  });

  it('uses gray for an empty selected day but preserves training colors for populated days', () => {
    const emptyDay = component.calendarDays()[0];
    emptyDay.sessions = [];
    expect(component.calendarDayBackground(emptyDay, true)).toBe('#e5e7eb');

    emptyDay.sessions = [{ trainingType: 'Routine' } as never];
    expect(component.calendarDayBackground(emptyDay, true)).toBe('#d1fae5');
  });

  it('shows pending Coach requests on their proposed calendar date without counting them as sessions', () => {
    component.visibleMonth.set(new Date(2026, 0, 1));
    component.calendarRequests.set([
      {
        routineScheduleRequestId: 91,
        proposedDate: '2026-01-12',
        proposedStartTime: '18:00:00',
        proposedEndTime: '20:00:00',
        status: 'Pending',
      } as never,
    ]);

    const day = component.calendarDays().find((item) => item.isoDate === '2026-01-12')!;
    expect(day.requests).toHaveLength(1);
    expect(day.sessions).toHaveLength(0);
    expect(component.currentMonthSessionCount()).toBe(0);
    expect(component.currentMonthPendingRequestCount()).toBe(1);
    expect(component.calendarDayBackground(day)).toBe('#fef3c7');
    expect(component.requestStatusLabel(day.requests[0])).toBe('รออนุมัติ');
  });

});
