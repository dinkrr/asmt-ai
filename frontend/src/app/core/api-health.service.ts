import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

export interface ApiHealth {
  status: string;
}

@Injectable({ providedIn: 'root' })
export class ApiHealthService {
  private readonly http = inject(HttpClient);

  check() {
    return this.http.get<ApiHealth>('/api/health');
  }
}