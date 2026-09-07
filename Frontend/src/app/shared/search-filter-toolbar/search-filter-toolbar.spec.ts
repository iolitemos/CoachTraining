import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SearchFilterToolbar } from './search-filter-toolbar';

describe('SearchFilterToolbar', () => {
  let component: SearchFilterToolbar;
  let fixture: ComponentFixture<SearchFilterToolbar>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SearchFilterToolbar],
    }).compileComponents();

    fixture = TestBed.createComponent(SearchFilterToolbar);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
