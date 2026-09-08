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

  afterEach(() => httpMock.verify());

  it('loads the complete visible calendar range', async () => {
    const component = TestBed.createComponent(PrivateSessionCalendar).componentInstance;
    const loadPromise = component.load();
    const request = httpMock.expectOne((req) => req.url.endsWith('/private-sessions/calendar'));

    expect(request.request.params.get('startDate')).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    expect(request.request.params.get('endDate')).toMatch(/^\d{4}-\d{2}-\d{2}$/);
    request.flush({ message: 'Success', data: [] });
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
});
