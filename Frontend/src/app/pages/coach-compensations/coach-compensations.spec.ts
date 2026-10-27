import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { CoachCompensationService } from '../../services/coach-compensation.service';
import { CoachService } from '../../services/coach.service';
import { CoachCompensations } from './coach-compensations';
import { createCompensationStatementCanvas } from './compensation-statement-image';

describe('CoachCompensations', () => {
  let fixture: ComponentFixture<CoachCompensations>;
  let component: CoachCompensations;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CoachCompensations],
      providers: [
        provideRouter([]),
        { provide: CoachService, useValue: { list: vi.fn().mockResolvedValue({ items: [] }) } },
        { provide: CoachCompensationService, useValue: {} },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CoachCompensations);
    component = fixture.componentInstance;
  });

  it('requires a coach before calculation', async () => {
    await component.ngOnInit();
    await component.calculate();

    expect(component.errorMessage()).toBe('กรุณาเลือกโค้ชและเดือนที่ต้องการ');
  });

  it('labels statement workflow statuses in Thai', () => {
    expect(component.statusLabel('Draft')).toBe('รอตรวจสอบ');
    expect(component.statusLabel('Confirmed')).toBe('ยืนยันแล้ว');
    expect(component.statusLabel('Paid')).toBe('จ่ายแล้ว');
  });

  it('shows the coach bank account details on the compensation statement', () => {
    component.statement.set({
      coachCompensationStatementId: 1,
      coachId: 7,
      coachCode: 'C007',
      coachFullName: 'สมชาย ใจดี',
      coachNickname: 'ชาย',
      bankName: 'ธนาคารกสิกรไทย',
      bankAccountNumber: '123-4-56789-0',
      bankAccountName: 'นายสมชาย ใจดี',
      year: 2026,
      month: 10,
      status: 'Draft',
      baseSalary: 15000,
      privateSessionCount: 0,
      privateCompensation: 0,
      adjustmentAmount: 0,
      adjustmentReason: null,
      netAmount: 15000,
      confirmedDate: null,
      paidDate: null,
      items: [],
    });

    fixture.detectChanges();

    const content = fixture.nativeElement.textContent;
    expect(content).toContain('ข้อมูลบัญชีรับเงิน');
    expect(content).toContain('ธนาคารกสิกรไทย');
    expect(content).toContain('123-4-56789-0');
    expect(content).toContain('นายสมชาย ใจดี');
  });

  it('creates a shareable compensation image with the statement summary', () => {
    const fillText = vi.fn();
    const context = {
      arc: vi.fn(),
      beginPath: vi.fn(),
      createLinearGradient: vi.fn().mockReturnValue({ addColorStop: vi.fn() }),
      fill: vi.fn(),
      fillRect: vi.fn(),
      fillText,
      measureText: vi.fn().mockImplementation((value: string) => ({ width: value.length * 12 })),
      moveTo: vi.fn(),
      lineTo: vi.fn(),
      roundRect: vi.fn(),
      stroke: vi.fn(),
    };
    vi.spyOn(HTMLCanvasElement.prototype, 'getContext').mockReturnValue(
      context as unknown as CanvasRenderingContext2D,
    );

    const canvas = createCompensationStatementCanvas({
      coachCompensationStatementId: 1,
      coachId: 7,
      coachCode: 'C007',
      coachFullName: 'สมชาย ใจดี',
      coachNickname: 'ชาย',
      bankName: 'ธนาคารกสิกรไทย',
      bankAccountNumber: '123-4-56789-0',
      bankAccountName: 'นายสมชาย ใจดี',
      year: 2026,
      month: 10,
      status: 'Confirmed',
      baseSalary: 15000,
      privateSessionCount: 1,
      privateCompensation: 500,
      adjustmentAmount: 100,
      adjustmentReason: 'ค่าเดินทาง',
      netAmount: 15600,
      confirmedDate: '2026-10-31T12:00:00',
      paidDate: null,
      items: [
        {
          coachCompensationItemId: 1,
          trainingSessionId: 99,
          sessionDate: '2026-10-15',
          scheduledStartDateTime: '2026-10-15T10:00:00',
          scheduledEndDateTime: '2026-10-15T11:00:00',
          coachName: 'สมชาย ใจดี',
          participantNames: 'น้องเอ',
          privateRate: 500,
          amount: 500,
        },
      ],
    });

    expect(canvas.width).toBe(1600);
    expect(canvas.height).toBeGreaterThan(900);
    expect(fillText).toHaveBeenCalledWith('สรุปค่าตอบแทนโค้ช', 84, 150);
    expect(fillText).toHaveBeenCalledWith('C007  •  ตุลาคม 2026', 84, 280);
    expect(fillText).toHaveBeenCalledWith('ธนาคาร: ธนาคารกสิกรไทย', 112, 466);
    expect(fillText).toHaveBeenCalledWith('เลขบัญชี: 123-4-56789-0', 580, 466);
    expect(fillText).toHaveBeenCalledWith('ชื่อบัญชี: นายสมชาย ใจดี', 1055, 466);
  });
});
