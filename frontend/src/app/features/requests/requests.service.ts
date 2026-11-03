import { HttpClient, HttpHeaders } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AppRole } from '../../core/role.service';

export interface RequestSummary {
  id: string;
  candidateName: string;
  targetLevel: string;
  presentationName: string;
  createdAt: string;
  reviewed: boolean;
}

export interface RequestDetail {
  request: RequestSummary;
  questions: string | null;
  transcript: string | null;
  feedback: string | null;
}

export interface ReviewInput {
  questions: string;
  transcript: string;
  feedback: string;
  reviewed: boolean;
}

export interface PresentationAnalysis {
  categories: { name: string; status: 'Pass' | 'Thin' | 'Insufficient'; notes: string }[];
  gaps: { category: string; skill: string; reason: string }[];
  candidateFeedback: string[];
  decision: 'Approve' | 'SendBack';
  decisionReason: string;
}

@Injectable({ providedIn: 'root' })
export class RequestsService {
  private readonly http = inject(HttpClient);
  private headers(role: AppRole): HttpHeaders { return new HttpHeaders({ 'X-Demo-Role': role }); }

  list(role: AppRole) {
    return this.http.get<RequestSummary[]>('/api/requests/', { headers: this.headers(role) });
  }

  get(id: string, role: AppRole) {
    return this.http.get<RequestDetail>(`/api/requests/${id}`, { headers: this.headers(role) });
  }

  create(candidateName: string, targetLevel: string, presentation: File) {
    const form = new FormData();
    form.append('candidateName', candidateName);
    form.append('targetLevel', targetLevel);
    form.append('presentation', presentation);
    return this.http.post<RequestSummary>('/api/requests/', form, { headers: this.headers('applicant') });
  }

  saveReview(id: string, review: ReviewInput) {
    return this.http.put<void>(`/api/requests/${id}/review`, review, { headers: this.headers('expert') });
  }

  analyzePresentation(id: string) {
    return this.http.post<PresentationAnalysis>(`/api/requests/${id}/presentation/analysis`, null, { headers: this.headers('expert') });
  }

  download(id: string, role: AppRole) {
    return this.http.get(`/api/requests/${id}/presentation`, { headers: this.headers(role), responseType: 'blob' });
  }
}