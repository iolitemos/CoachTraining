import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { AthleteList } from './athlete-list';

describe('AthleteList', () => {
  let component: AthleteList;
  let fixture: ComponentFixture<AthleteList>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AthleteList],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    fixture = TestBed.createComponent(AthleteList);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should load the first page of athletes', async () => {
    const loadPromise = component.load();

    httpMock
      .expectOne((r) => r.url.endsWith('/athletes'))
      .flush({ message: 'Success', data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } });

    await loadPromise;

    expect(component).toBeTruthy();
    expect(component.state()).toBe('ready');
  });

  it('should show the error state when loading fails', async () => {
    const loadPromise = component.load();

    httpMock
      .expectOne((r) => r.url.endsWith('/athletes'))
      .flush({ message: 'error' }, { status: 500, statusText: 'Server Error' });

    await loadPromise;

    expect(component.state()).toBe('error');
  });
});
