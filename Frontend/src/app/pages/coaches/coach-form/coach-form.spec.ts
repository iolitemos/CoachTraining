import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { CoachForm } from './coach-form';

describe('CoachForm', () => {
  let component: CoachForm;
  let fixture: ComponentFixture<CoachForm>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CoachForm],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CoachForm);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create in "add" mode with no requests', async () => {
    await component.ngOnInit();

    expect(component).toBeTruthy();
    expect(component.isEditMode()).toBe(false);
    expect(component.loading()).toBe(false);
  });

  it('should require coach code and full name before submit', async () => {
    await component.ngOnInit();

    await component.onSubmit();

    expect(component.form.controls.coachCode.invalid).toBe(true);
    expect(component.form.controls.fullName.invalid).toBe(true);
    expect(component.submitting()).toBe(false);
  });
});
