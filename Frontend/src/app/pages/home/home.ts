import { Component } from '@angular/core';
import { PageHeader } from '../../shared/page-header/page-header';

/**
 * Placeholder landing page for Project Setup. Coach Home (todo.md 5.5) and
 * the Administrator Dashboard (todo.md 5.14) replace this once Authentication
 * (todo.md section 3) can redirect signed-in users by role.
 */
@Component({
  selector: 'app-home',
  imports: [PageHeader],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {}
