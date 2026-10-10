import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AthleteOption } from '../../models/athlete.model';
import { RoutineAttendanceCreateRequest, RoutineAttendanceItem } from '../../models/routine-attendance.model';
import { AthleteService } from '../../services/athlete.service';
import { RoutineAttendanceService } from '../../services/routine-attendance.service';
import { RoutineAttendance } from './routine-attendance';

const plannedAthlete: AthleteOption = {
  athleteId: 12,
  athleteCode: 'A012',
  fullName: 'นักกีฬาทดสอบ',
  nickname: 'ปิง',
};

class AthleteServiceStub {
  async searchActive(): Promise<AthleteOption[]> { return []; }
}

class RoutineAttendanceServiceStub {
  addRequests: RoutineAttendanceCreateRequest[] = [];

  async list(): Promise<RoutineAttendanceItem[]> { return []; }
  async listPlannedAthletes(): Promise<AthleteOption[]> { return [plannedAthlete]; }
  async add(_trainingSessionId: number, request: RoutineAttendanceCreateRequest): Promise<RoutineAttendanceItem> {
    this.addRequests.push(request);
    return {
      attendanceId: 99,
      athleteId: plannedAthlete.athleteId,
      athleteCode: plannedAthlete.athleteCode,
      fullName: plannedAthlete.fullName,
      nickname: plannedAthlete.nickname,
      status: request.status,
      arrivalTime: request.arrivalTime,
      remark: request.remark,
      recordedDate: '2026-10-08T10:00:00Z',
    };
  }
  async update(): Promise<RoutineAttendanceItem> { throw new Error('Not used'); }
  async remove(): Promise<void> {}
}

describe('RoutineAttendance', () => {
  let fixture: ComponentFixture<RoutineAttendance>;
  let component: RoutineAttendance;
  let service: RoutineAttendanceServiceStub;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RoutineAttendance],
      providers: [
        { provide: AthleteService, useClass: AthleteServiceStub },
        { provide: RoutineAttendanceService, useClass: RoutineAttendanceServiceStub },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RoutineAttendance);
    component = fixture.componentInstance;
    service = TestBed.inject(RoutineAttendanceService) as unknown as RoutineAttendanceServiceStub;
    fixture.componentRef.setInput('trainingSessionId', 42);
    fixture.detectChanges();
    await fixture.whenStable();
    await component.load();
    fixture.detectChanges();
  });

  it('pre-fills planned athletes as options without recording attendance', () => {
    expect(component.plannedAthletes()).toEqual([plannedAthlete]);
    expect(component.attendees()).toEqual([]);
    expect(service.addRequests).toEqual([]);

    const section = fixture.nativeElement.querySelector('[aria-labelledby="planned-athletes-heading"]') as HTMLElement;
    expect(section.textContent).toContain('นักกีฬาที่ลงแผนเข้าซ้อมวันนี้');
    expect(section.textContent).toContain('ปิง - นักกีฬาทดสอบ');
    expect(section.textContent).toContain('ยืนยันว่ามา');
  });

  it('creates Present attendance only after the Coach confirms the planned athlete', async () => {
    await component.confirmPlannedAthlete(plannedAthlete);

    expect(service.addRequests).toEqual([{
      athleteId: plannedAthlete.athleteId,
      status: 'Present',
      arrivalTime: null,
      remark: null,
    }]);
    expect(component.attendees()).toHaveLength(1);
    expect(component.isAdded(plannedAthlete.athleteId)).toBe(true);
  });
});
