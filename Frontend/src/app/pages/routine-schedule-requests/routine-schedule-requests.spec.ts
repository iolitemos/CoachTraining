import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { RoutineScheduleRequest } from '../../models/routine-schedule-request.model';
import { RoutineScheduleRequests } from './routine-schedule-requests';

function request(
  id: number,
  status: RoutineScheduleRequest['status'],
  overrides: Partial<RoutineScheduleRequest> = {},
): RoutineScheduleRequest {
  return {
    routineScheduleRequestId: id,
    submissionBatchId: `batch-${id}`,
    coachId: 1,
    coachCode: 'C001',
    coachName: 'โค้ชทดสอบ',
    coachNickname: 'โค้ชเอ',
    coachColorHex: '#10B981',
    proposedDate: '2026-11-03',
    proposedStartTime: '18:30:00',
    proposedEndTime: '20:30:00',
    proposedRemarks: null,
    status,
    approvedDate: null,
    approvedStartTime: null,
    approvedEndTime: null,
    approvedRemarks: null,
    reviewReason: null,
    reviewedDate: null,
    routineScheduleId: null,
    currentScheduleCoachId: null,
    currentScheduleCoachCode: null,
    currentScheduleCoachName: null,
    currentScheduleCoachNickname: null,
    currentScheduleCoachColorHex: null,
    currentScheduleDate: null,
    currentScheduleStartTime: null,
    currentScheduleEndTime: null,
    currentScheduleRemarks: null,
    createdDate: '2026-10-17T10:00:00Z',
    ...overrides,
  };
}

describe('RoutineScheduleRequests', () => {
  let component: RoutineScheduleRequests;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineScheduleRequests],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    component = TestBed.createComponent(RoutineScheduleRequests).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads all statuses so the summary and calendar share one data source', async () => {
    const load = component.load();
    const apiRequest = httpMock.expectOne((item) =>
      item.url.endsWith('/routine-schedule-requests'),
    );

    expect(apiRequest.request.params.has('status')).toBe(false);
    apiRequest.flush({
      message: 'Success',
      data: [request(1, 'Pending'), request(2, 'Approved'), request(3, 'Cancelled')],
    });
    httpMock
      .expectOne((item) => item.url.endsWith('/competition-matches'))
      .flush({
        message: 'Success',
        data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 },
      });
    httpMock
      .expectOne((item) => item.url.endsWith('/parent-routine-plans/admin/summary'))
      .flush({ message: 'Success', data: [] });
    await load;

    expect(component.state()).toBe('ready');
    expect(component.pendingCount()).toBe(1);
    expect(component.approvedCount()).toBe(1);
    expect(component.closedCount()).toBe(1);
  });

  it('places filtered requests on their proposed calendar date', () => {
    component.visibleMonth.set(new Date(2026, 10, 1));
    component.requests.set([
      request(1, 'Pending'),
      request(2, 'Approved', { coachId: 2 }),
      request(3, 'Pending', { proposedDate: '2026-11-04', coachId: 3 }),
    ]);

    const pendingReviewDay = component.calendarDays().find((day) => day.isoDate === '2026-11-03')!;
    expect(pendingReviewDay.requests.map((item) => item.routineScheduleRequestId)).toEqual([1, 2]);
    expect(component.coachCount(pendingReviewDay)).toBe(2);

    component.selectStatus('Approved');
    expect(
      component
        .calendarDays()
        .find((day) => day.isoDate === '2026-11-03')
        ?.requests.map((item) => item.routineScheduleRequestId),
    ).toEqual([2]);

    component.selectStatus('');
    expect(
      component
        .calendarDays()
        .find((day) => day.isoDate === '2026-11-03')
        ?.requests.map((item) => item.routineScheduleRequestId),
    ).toEqual([1, 2]);

    component.selectCoach('3');
    expect(component.filteredRequests().map((item) => item.routineScheduleRequestId)).toEqual([3]);
  });

  it('switches views and keeps calendar navigation synchronized with the selected date', () => {
    component.visibleMonth.set(new Date(2026, 10, 1));
    component.selectedDate.set('2026-11-03');

    component.setViewMode('list');
    expect(component.viewMode()).toBe('list');

    component.moveMonth(1);
    httpMock
      .expectOne((item) => item.url.endsWith('/parent-routine-plans/admin/summary'))
      .flush({ message: 'Success', data: [] });
    expect(component.visibleMonth().getMonth()).toBe(11);
    expect(component.selectedDate()).toBe('2026-12-01');
  });

  it('moves an approved request to the current Routine Schedule date after an admin edit', () => {
    component.visibleMonth.set(new Date(2026, 10, 1));
    const approved = request(8, 'Approved', {
      approvedDate: '2026-11-04',
      approvedStartTime: '18:30:00',
      approvedEndTime: '20:30:00',
      currentScheduleDate: '2026-11-05',
      currentScheduleStartTime: '19:00:00',
      currentScheduleEndTime: '21:00:00',
      currentScheduleCoachId: 9,
      currentScheduleCoachCode: 'C009',
      currentScheduleCoachName: 'โค้ชคนใหม่',
      currentScheduleCoachNickname: 'นิว',
      currentScheduleCoachColorHex: '#2563EB',
    });
    component.requests.set([approved]);

    expect(
      component.calendarDays().find((day) => day.isoDate === '2026-11-04')?.requests,
    ).toHaveLength(0);
    expect(
      component
        .calendarDays()
        .find((day) => day.isoDate === '2026-11-05')
        ?.requests.map((item) => item.routineScheduleRequestId),
    ).toEqual([8]);
    expect(component.requestDate(approved)).toBe('2026-11-05');
    expect(component.requestCoachId(approved)).toBe(9);
    expect(component.requestCoachName(approved)).toBe('นิว');
    expect(component.requestCoachCode(approved)).toBe('C009');
    expect(component.requestCoachColor(approved)).toBe('#2563EB');
    expect(
      component.coachCount(component.calendarDays().find((day) => day.isoDate === '2026-11-05')!),
    ).toBe(1);
    expect(component.coachChangedAfterApproval(approved)).toBe(true);
    expect(component.scheduleChangedAfterApproval(approved)).toBe(true);

    component.selectCoach('9');
    expect(
      component.calendarDays().find((day) => day.isoDate === '2026-11-05')?.requests,
    ).toHaveLength(1);
    component.selectCoach('1');
    expect(
      component.calendarDays().find((day) => day.isoDate === '2026-11-05')?.requests,
    ).toHaveLength(0);
  });

  it('shows competition ranges and planned athlete counts on each calendar day', () => {
    component.visibleMonth.set(new Date(2026, 10, 1));
    component.competitionMatches.set([
      {
        competitionMatchId: 1,
        name: 'รายการทดสอบ',
        province: 'กรุงเทพฯ',
        startDate: '2026-11-03',
        endDate: '2026-11-05',
        coaches: [],
      },
    ]);
    component.participationSummaries.set([{ trainingDate: '2026-11-04', athleteCount: 7 }]);

    expect(
      component.calendarDays().find((day) => day.isoDate === '2026-11-03')?.competitionMatches,
    ).toHaveLength(1);
    expect(
      component.calendarDays().find((day) => day.isoDate === '2026-11-05')?.competitionMatches,
    ).toHaveLength(1);
    const plannedDay = component.calendarDays().find((day) => day.isoDate === '2026-11-04');
    expect(plannedDay?.isRoutineTrainingDate).toBe(true);
    expect(plannedDay?.plannedAthleteCount).toBe(7);
  });

  it('requires an override reason before approving a conflicting schedule', async () => {
    component.open(request(1, 'Pending'));
    component.overrideConflict = true;
    component.reason = '';

    await component.approve();

    expect(component.error()).toContain('กรุณาระบุเหตุผล');
    expect(component.processing()).toBe(false);
  });

  it('selects only pending requests and approves the selection as one batch', async () => {
    const first = request(1, 'Pending');
    const second = request(2, 'Pending', { proposedDate: '2026-11-04' });
    component.requests.set([first, second, request(3, 'Approved')]);

    component.toggleVisiblePending();
    expect(
      component.selectedPendingRequests().map((item) => item.routineScheduleRequestId),
    ).toEqual([1, 2]);

    const approve = component.confirmBatchApprove();
    const approveRequest = httpMock.expectOne((item) =>
      item.url.endsWith('/routine-schedule-requests/batch-approve'),
    );
    expect(approveRequest.request.body).toEqual({ requestIds: [1, 2] });
    approveRequest.flush({
      message: 'Success',
      data: [
        { ...first, status: 'Approved' },
        { ...second, status: 'Approved' },
      ],
    });
    await new Promise<void>((resolve) => setTimeout(resolve, 0));
    const reloadRequests = httpMock.match(
      (item) => item.method === 'GET' && item.url.endsWith('/routine-schedule-requests'),
    );
    expect(reloadRequests.length).toBeGreaterThan(0);
    reloadRequests.forEach((item) => item.flush({ message: 'Success', data: [] }));
    httpMock
      .match((item) => item.url.endsWith('/competition-matches'))
      .forEach((item) =>
        item.flush({
          message: 'Success',
          data: { items: [], page: 1, pageSize: 100, totalCount: 0, totalPages: 0 },
        }),
      );
    httpMock
      .match((item) => item.url.endsWith('/parent-routine-plans/admin/summary'))
      .forEach((item) => item.flush({ message: 'Success', data: [] }));
    await approve;

    expect(component.selectedPendingRequests()).toHaveLength(0);
    expect(component.success()).toContain('2 รายการ');
  });
});
