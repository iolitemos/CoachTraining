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

  it('imports a valid workbook and reloads the athlete list', async () => {
    const file = new File(['workbook'], 'athletes.xlsx', { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const event = { target: { files: [file], value: file.name } } as unknown as Event;

    const importPromise = component.onImportFileSelected(event);
    httpMock.expectOne((r) => r.url.endsWith('/athletes/import') && r.method === 'POST')
      .flush({ message: 'Success', data: { importedCount: 2, totalRows: 2 } });
    await new Promise((resolve) => setTimeout(resolve, 0));
    const reloadRequests = httpMock.match((r) => r.url.endsWith('/athletes') && r.method === 'GET');
    expect(reloadRequests.length).toBeGreaterThan(0);
    reloadRequests.forEach((request) => request.flush({ message: 'Success', data: { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 } }));
    await importPromise;

    expect(component.importMessage()).toBe('นำเข้านักกีฬา 2 รายการสำเร็จ');
    expect(component.importErrors()).toEqual([]);
  });

  it('shows row-level validation errors returned by import API', async () => {
    const file = new File(['workbook'], 'athletes.xlsx');
    const event = { target: { files: [file], value: file.name } } as unknown as Event;

    const importPromise = component.onImportFileSelected(event);
    httpMock.expectOne((r) => r.url.endsWith('/athletes/import'))
      .flush({ message: 'ข้อมูลในไฟล์นำเข้าไม่ถูกต้อง', errors: [{ row: 4, field: 'athleteCode', message: 'รหัสซ้ำ' }] }, { status: 400, statusText: 'Bad Request' });
    await importPromise;

    expect(component.importErrors()).toEqual([{ row: 4, field: 'athleteCode', message: 'รหัสซ้ำ' }]);
  });
});
