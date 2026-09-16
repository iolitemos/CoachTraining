import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PrivateSessionCalendar } from './private-session-calendar';

describe('PrivateSessionCalendar', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PrivateSessionCalendar],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.match((request) => request.url.endsWith('/calendar-notes'))
      .forEach((request) => request.flush({ message: 'Success', data: [] }));
    httpMock.verify();
  });

  it('loads the complete visible calendar range', async () => {
    const component = TestBed.createComponent(PrivateSessionCalendar).componentInstance;
    const loadPromise = component.load();
    const request = httpMock.expectOne((req) => req.url.endsWith('/private-sessions/calendar'));

    expect(request.request.params.get('startDate')).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(request.request.params.get('endDate')).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    request.flush({ message: 'Success', data: [] });
    httpMock.expectOne((req) => req.url.endsWith('/calendar-notes')).flush({ message: 'Success', data: [] });
    await loadPromise;

    expect(component.state()).toBe('ready');
  });

  it('uses blue for session dates and gray only for an empty selected date', () => {
    const component = TestBed.createComponent(PrivateSessionCalendar).componentInstance;
    component.visibleMonth.set(new Date(2026, 0, 1));
    const day = component.calendarDays().find((item) => item.isoDate === '2026-01-05')!;

    day.sessions = [{ trainingSessionId: 1 } as never];
    expect(component.calendarDayBackground(day, true)).toBe('#dbeafe');

    day.sessions = [];
    expect(component.calendarDayBackground(day, true)).toBe('#e5e7eb');
  });

  it('shows participant names on calendar session entries', async () => {
    const fixture = TestBed.createComponent(PrivateSessionCalendar);
    const component = fixture.componentInstance;
    const sessionDate = component.selectedDate();
    fixture.detectChanges();
    httpMock.expectOne((request) => request.url.endsWith('/private-sessions/calendar')).flush({
      message: 'Success',
      data: [{
        trainingSessionId: 1,
        sessionDate,
        startTime: '17:00:00',
        endTime: '18:00:00',
        coachCode: 'C001',
        coachFullName: 'Coach One',
        coachNickname: 'โค้ชหนึ่ง',
        coachColorHex: '#10B981',
        location: null,
        status: 'Scheduled',
        athleteCount: 2,
        participantNames: ['Ace', 'Guest Player'],
      }],
    });
    httpMock.expectOne((request) => request.url.endsWith('/calendar-notes')).flush({ message: 'Success', data: [] });
    await new Promise((resolve) => setTimeout(resolve, 0));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Ace, Guest Player');
  });

  it('deletes a session in any status and removes it from the calendar', async () => {
    const component = TestBed.createComponent(PrivateSessionCalendar).componentInstance;
    const session = { trainingSessionId: 12, status: 'Scheduled' } as never;
    component.sessions.set([session]);
    component.requestDelete(session);

    const deletePromise = component.confirmDelete();
    const request = httpMock.expectOne((req) => req.url.endsWith('/training-sessions/12'));
    expect(request.request.method).toBe('DELETE');
    request.flush(null);
    await deletePromise;

    expect(component.sessions()).toEqual([]);
    expect(component.deleteTarget()).toBeNull();
  });
});
