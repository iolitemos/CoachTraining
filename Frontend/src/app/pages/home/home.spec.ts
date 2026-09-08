import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Home } from './home';
import { AuthService } from '../../services/auth.service';

describe('Home', () => {
  let component: Home;
  let fixture: ComponentFixture<Home>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Home],
      providers: [{ provide: AuthService, useValue: { logout: vi.fn() } }],
    }).compileComponents();

    fixture = TestBed.createComponent(Home);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows the no-role fallback', () => {
    expect(fixture.nativeElement.textContent).toContain('บัญชียังไม่มีสิทธิ์ใช้งาน');
  });
});
