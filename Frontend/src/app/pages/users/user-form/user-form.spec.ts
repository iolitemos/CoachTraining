import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { UserForm } from './user-form';

describe('UserForm', () => {
  let component: UserForm;
  let fixture: ComponentFixture<UserForm>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UserForm],
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

    fixture = TestBed.createComponent(UserForm);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create in "add" mode and load role/coach options', async () => {
    const initPromise = component.ngOnInit();

    httpMock.expectOne((r) => r.url.endsWith('/users/role-options')).flush({ message: 'Success', data: [] });
    httpMock.expectOne((r) => r.url.endsWith('/users/coach-options')).flush({ message: 'Success', data: [] });

    await initPromise;

    expect(component).toBeTruthy();
    expect(component.isEditMode()).toBe(false);
    expect(component.loading()).toBe(false);
  });
});
