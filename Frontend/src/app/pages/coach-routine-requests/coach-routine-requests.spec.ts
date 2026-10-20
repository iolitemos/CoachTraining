import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { RoutineScheduleRequest } from '../../models/routine-schedule-request.model';
import { CoachRoutineRequests } from './coach-routine-requests';

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

describe('CoachRoutineRequests', () => {
  let component: CoachRoutineRequests;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CoachRoutineRequests],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    component = TestBed.createComponent(CoachRoutineRequests).componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads coach requests and groups them for the summary and filters', async () => {
    const load = component.load();
    const apiRequest = httpMock.expectOne((item) =>
      item.url.endsWith('/coach/routine-schedules/requests'),
    );
    apiRequest.flush({
      message: 'Success',
      data: [
        request(1, 'Pending'),
        request(2, 'RevisionRequested'),
        request(3, 'Approved'),
        request(4, 'Cancelled'),
        request(5, 'Rejected'),
      ],
    });
    await load;

    expect(component.state()).toBe('ready');
    expect(component.pendingCount()).toBe(1);
    expect(component.revisionCount()).toBe(1);
    expect(component.approvedCount()).toBe(1);
    expect(component.closedCount()).toBe(2);

    component.selectFilter('Closed');
    expect(component.filteredRequests().map((item) => item.status)).toEqual([
      'Cancelled',
      'Rejected',
    ]);
  });

  it('formats the date, time duration, and status text for request cards', () => {
    const item = request(1, 'Pending');

    expect(component.day(item.proposedDate)).toBe('3');
    expect(component.monthYear(item.proposedDate)).toBe('พ.ย. 2569');
    expect(component.weekday(item.proposedDate)).toBe('อังคาร');
    expect(component.duration(item)).toBe('2 ชม.');
    expect(component.statusLabel(item.status)).toBe('รอตรวจสอบ');
  });

  it('detects when the admin approved different schedule information', () => {
    const unchanged = request(1, 'Approved', {
      approvedDate: '2026-11-03',
      approvedStartTime: '18:30:00',
      approvedEndTime: '20:30:00',
    });
    const changed = request(2, 'Approved', {
      approvedDate: '2026-11-04',
      approvedStartTime: '18:30:00',
      approvedEndTime: '20:30:00',
    });

    expect(component.approvedChanged(unchanged)).toBe(false);
    expect(component.approvedChanged(changed)).toBe(true);
  });

  it('shows the current schedule after an admin edits an approved request schedule', () => {
    const item = request(3, 'Approved', {
      approvedDate: '2026-11-03',
      approvedStartTime: '18:30:00',
      approvedEndTime: '20:30:00',
      currentScheduleDate: '2026-11-05',
      currentScheduleStartTime: '19:00:00',
      currentScheduleEndTime: '21:00:00',
      currentScheduleCoachId: 9,
      currentScheduleCoachCode: 'C009',
      currentScheduleCoachName: 'โค้ชคนใหม่',
      currentScheduleCoachNickname: 'นิว',
    });

    expect(component.requestDate(item)).toBe('2026-11-05');
    expect(component.requestStartTime(item)).toBe('19:00:00');
    expect(component.requestCoachName(item)).toBe('นิว');
    expect(component.coachChangedAfterApproval(item)).toBe(true);
    expect(component.scheduleChangedAfterApproval(item)).toBe(true);
  });
});
