import { Component, computed, input } from '@angular/core';
import { getStatusDisplay, TrainingSessionStatus } from '../../models/training-session-status.model';

@Component({
  selector: 'app-status-badge',
  imports: [],
  templateUrl: './status-badge.html',
  styleUrl: './status-badge.css',
})
export class StatusBadge {
  status = input.required<TrainingSessionStatus>();

  display = computed(() => getStatusDisplay(this.status()));
}
