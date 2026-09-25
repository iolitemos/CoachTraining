import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { ParentRoutinePlanService } from './parent-routine-plan.service';

describe('ParentRoutinePlanService', () => {
  let service: ParentRoutinePlanService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(ParentRoutinePlanService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('uses athlete-scoped admin link endpoint', async () => {
    const promise = service.getLinkStatus(12);
    const request = http.expectOne(`${environment.apiBaseUrl}/parent-routine-plans/admin/athletes/12/link`);
    expect(request.request.method).toBe('GET');
    request.flush({ message: 'Success', data: { exists: false, isEnabled: false, token: null, tokenHint: null, createdDate: null } });
    expect((await promise).exists).toBe(false);
  });

  it('loads administrator summary for the requested range', async () => {
    const promise = service.getSummary('2026-09-01', '2026-09-30');
    const request = http.expectOne(req => req.url.endsWith('/admin/summary') && req.params.get('startDate') === '2026-09-01');
    request.flush({ message: 'Success', data: [{ trainingDate: '2026-09-26', athleteCount: 3 }] });
    expect((await promise)[0].athleteCount).toBe(3);
  });

  it('manages Routine availability dates through their own endpoint', async () => {
    const promise = service.addTrainingDate('2026-09-26');
    const request = http.expectOne(`${environment.apiBaseUrl}/routine-training-dates`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ trainingDate: '2026-09-26' });
    request.flush({ message: 'Success', data: { routineTrainingDateId: 1, trainingDate: '2026-09-26' } });
    expect((await promise).trainingDate).toBe('2026-09-26');
  });
});
