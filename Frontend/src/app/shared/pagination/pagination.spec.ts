import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Pagination } from './pagination';

describe('Pagination', () => {
  let component: Pagination;
  let fixture: ComponentFixture<Pagination>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Pagination],
    }).compileComponents();

    fixture = TestBed.createComponent(Pagination);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('page', 1);
    fixture.componentRef.setInput('pageSize', 20);
    fixture.componentRef.setInput('totalCount', 45);
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should compute total pages from page size and total count', () => {
    expect(component.totalPages()).toBe(3);
  });

  it('should emit the next page when goToNext is called', () => {
    const emitted: number[] = [];
    component.pageChange.subscribe((page) => emitted.push(page));

    component.goToNext();

    expect(emitted).toEqual([2]);
  });
});
