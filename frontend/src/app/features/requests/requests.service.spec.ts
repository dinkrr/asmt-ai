import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RequestsService } from './requests.service';

describe('RequestsService', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  it('sends an applicant request with its presentation', () => {
    const service = TestBed.inject(RequestsService);
    const http = TestBed.inject(HttpTestingController);
    const file = new File(['slides'], 'slides.pptx');
    service.create('Candidate', 'A3', file).subscribe();
    const request = http.expectOne('/api/requests/');
    expect(request.request.headers.get('X-Demo-Role')).toBe('applicant');
    expect(request.request.body.get('presentation')).toBe(file);
    request.flush({});
    http.verify();
  });

  it('sends expert role for review changes', () => {
    const service = TestBed.inject(RequestsService);
    const http = TestBed.inject(HttpTestingController);
    service.saveReview('123', { questions: 'Question', transcript: '', feedback: '', reviewed: false }).subscribe();
    const request = http.expectOne('/api/requests/123/review');
    expect(request.request.headers.get('X-Demo-Role')).toBe('expert');
    request.flush(null);
    http.verify();
  });
});