import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StatusBadge } from './status-badge';

describe('StatusBadge', () => {
  let component: StatusBadge;
  let fixture: ComponentFixture<StatusBadge>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatusBadge],
    }).compileComponents();

    fixture = TestBed.createComponent(StatusBadge);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('status', 'Scheduled');
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render the Thai label for the given status', () => {
    fixture.detectChanges();
    const text = (fixture.nativeElement as HTMLElement).textContent?.trim();
    expect(text).toBe('กำหนดการ');
  });
});
