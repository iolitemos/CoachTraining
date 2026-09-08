import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';

/**
 * Fallback shown only when an authenticated account has no supported role.
 */
@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.html',
  styleUrl: './home.css',
})
export class Home {
  constructor(readonly authService: AuthService) {}

  logout(): void {
    this.authService.logout();
  }
}
