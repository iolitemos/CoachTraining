import { Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  imports: [],
  templateUrl: './page-header.html',
  styleUrl: './page-header.css',
})
export class PageHeader {
  title = input.required<string>();
  subtitle = input('');
}
