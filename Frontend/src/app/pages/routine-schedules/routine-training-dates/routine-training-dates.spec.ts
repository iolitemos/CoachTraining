import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ParentRoutinePlanService } from '../../../services/parent-routine-plan.service';
import { CompetitionMatchService } from '../../../services/competition-match.service';
import { CalendarNoteService } from '../../../services/calendar-note.service';
import { RoutineTrainingDates } from './routine-training-dates';

class ParentRoutinePlanServiceStub {
  dates = [{ routineTrainingDateId: 1, trainingDate: '2026-09-05' }];
  async listTrainingDates() { return [...this.dates]; }
  async addTrainingDate(trainingDate: string) { const item = { routineTrainingDateId: 2, trainingDate }; this.dates.push(item); return item; }
  async removeTrainingDate(trainingDate: string) { this.dates = this.dates.filter(item => item.trainingDate !== trainingDate); }
}
class CompetitionMatchServiceStub {
  async listAll() { return [{ competitionMatchId: 1, name: 'รายการทดสอบ', province: 'กรุงเทพฯ', startDate: '2026-09-06', endDate: '2026-09-07', coaches: [] }]; }
}
class CalendarNoteServiceStub {
  async list() { return [{ calendarNoteId: 1, noteDate: '2026-09-06', content: 'หมายเหตุทดสอบ' }]; }
}

describe('RoutineTrainingDates', () => {
  let fixture: ComponentFixture<RoutineTrainingDates>;
  let component: RoutineTrainingDates;
  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [RoutineTrainingDates], providers: [provideRouter([]), { provide: ParentRoutinePlanService, useClass: ParentRoutinePlanServiceStub }, { provide: CompetitionMatchService, useClass: CompetitionMatchServiceStub }, { provide: CalendarNoteService, useClass: CalendarNoteServiceStub }] }).compileComponents();
    fixture = TestBed.createComponent(RoutineTrainingDates);
    component = fixture.componentInstance;
    component.visibleMonth.set(new Date(2026, 8, 1));
    await component.load();
  });
  it('adds a date independently when an empty calendar date is selected', async () => {
    const day = component.calendarDays().find(item => item.isoDate === '2026-09-06')!;
    await component.toggle(day);
    expect(component.dates().some(item => item.trainingDate === '2026-09-06')).toBe(true);
  });
  it('removes an existing Routine availability date', async () => {
    const day = component.calendarDays().find(item => item.isoDate === '2026-09-05')!;
    await component.toggle(day);
    expect(component.pendingRemoval()?.isoDate).toBe('2026-09-05');
    await component.confirmRemoval();
    expect(component.dates().some(item => item.trainingDate === '2026-09-05')).toBe(false);
  });
  it('marks competition and Note dates in the calendar', () => {
    const day = component.calendarDays().find(item => item.isoDate === '2026-09-06')!;
    expect(day.competitionMatches).toHaveLength(1);
    expect(day.note?.content).toBe('หมายเหตุทดสอบ');
  });
});
