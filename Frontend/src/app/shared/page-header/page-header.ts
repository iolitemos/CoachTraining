import { Component, ViewEncapsulation, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  imports: [],
  templateUrl: './page-header.html',
  styleUrl: './page-header.css',
  encapsulation: ViewEncapsulation.None,
})
export class PageHeader {
  title = input.required<string>();
  subtitle = input('');
}
