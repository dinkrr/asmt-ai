import { Injectable, signal } from '@angular/core';

export type AppRole = 'applicant' | 'expert';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly selectedRole = signal<AppRole>('applicant');
  readonly role = this.selectedRole.asReadonly();

  select(role: AppRole): void {
    this.selectedRole.set(role);
  }
}