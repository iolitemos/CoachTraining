import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';

import { CoachHome } from './coach-home';

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

  afterEach(() => httpMock.verify());

  it('loads the visible calendar range through the coach-scoped session API', async () => {
    const loadPromise = component.loadCalendar();
    const request = httpMock.expectOne((r) => r.url.endsWith('/training-sessions'));

    expect(request.request.params.get('dateFrom')).toBeTruthy();
    expect(request.request.params.get('dateTo')).toBeTruthy();
    expect(request.request.params.has('coachId')).toBe(false);
    const colleaguesRequest = httpMock.expectOne((r) =>
      r.url.endsWith('/dashboard/coach/calendar-colleagues'),
    );

    request.flush({
      message: 'Success',
      data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 },
    });
    colleaguesRequest.flush({ message: 'Success', data: [] });
    await loadPromise;

    expect(component.calendarState()).toBe('ready');
    expect(component.calendarDays()).toHaveLength(42);
  });

  it('switches between overview and calendar tabs', () => {
    expect(component.activeTab()).toBe('overview');

    component.selectTab('calendar');

    expect(component.activeTab()).toBe('calendar');
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

  it('opens the self-service create page with the held calendar date', () => {
    vi.useFakeTimers();
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const day = component.calendarDays()[10];

    component.startDateLongPress(day);
    vi.advanceTimersByTime(600);

    expect(navigate).toHaveBeenCalledWith(['/coach/routine-schedules/new'], {
      queryParams: { date: day.isoDate },
    });
    vi.useRealTimers();
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

});
