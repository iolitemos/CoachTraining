import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { RouterLink } from '@angular/router';

import { Login } from './login';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    await fixture.whenStable();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should mark the form touched and not submit when fields are empty', async () => {
    await component.onSubmit();
    expect(component.form.controls.username.touched).toBe(true);
    httpMock.expectNone(() => true);
  });

  it('should toggle password visibility without changing the form value', () => {
    component.form.controls.password.setValue('secret-password');

    component.togglePasswordVisibility();

    expect(component.passwordVisible()).toBe(true);
    expect(component.form.controls.password.value).toBe('secret-password');
  });

  it('should only link the forgot-password button to the reset flow', () => {
    fixture.detectChanges();

    const passwordToggle = fixture.nativeElement.querySelector(
      'button[aria-label="แสดงรหัสผ่าน"]',
    ) as HTMLButtonElement;
    const linkedButtons = fixture.debugElement.queryAll(By.directive(RouterLink));

    expect(linkedButtons).toHaveLength(1);
    expect(linkedButtons[0].nativeElement).not.toBe(passwordToggle);
    expect(linkedButtons[0].nativeElement.textContent).toContain('ลืมรหัสผ่าน');
    expect(linkedButtons[0].injector.get(RouterLink).urlTree?.toString()).toBe('/forgot-password');
  });
});
