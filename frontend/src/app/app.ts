import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ApiHealthService } from './core/api-health.service';
import { RoleService } from './core/role.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly roles = inject(RoleService);
  protected readonly apiStatus = signal<'checking' | 'online' | 'offline'>('checking');
  protected readonly statusLabel = computed(() => ({
    checking: 'Connecting', online: 'API connected', offline: 'API unavailable'
  })[this.apiStatus()]);

  protected selectRole(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === 'applicant' || value === 'expert') this.roles.select(value);
  }

  constructor() {
    inject(ApiHealthService).check().subscribe({
      next: (response) => this.apiStatus.set(response.status === 'ok' ? 'online' : 'offline'),
      error: () => this.apiStatus.set('offline')
    });
  }
}
