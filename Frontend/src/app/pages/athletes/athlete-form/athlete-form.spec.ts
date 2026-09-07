import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { AthleteForm } from './athlete-form';

describe('AthleteForm', () => {
  let component: AthleteForm;
  let fixture: ComponentFixture<AthleteForm>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AthleteForm],
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

    fixture = TestBed.createComponent(AthleteForm);
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

  it('should require athlete code and full name before submit', async () => {
    await component.ngOnInit();

    await component.onSubmit();

    expect(component.form.controls.athleteCode.invalid).toBe(true);
    expect(component.form.controls.fullName.invalid).toBe(true);
    expect(component.submitting()).toBe(false);
  });
});
