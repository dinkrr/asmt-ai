import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Requests } from './requests';
import { RoleService } from '../../core/role.service';

describe('Requests', () => {
  it('renders the requests workspace', async () => {
    await TestBed.configureTestingModule({ imports: [Requests], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(Requests);
    fixture.detectChanges();
    TestBed.inject(HttpTestingController).expectOne('/api/requests/').flush([]);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('h1')?.textContent).toContain('Requests');
  });

  it('switches from applicant actions to the expert queue', async () => {
    await TestBed.configureTestingModule({ imports: [Requests], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    const fixture = TestBed.createComponent(Requests);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    http.expectOne('/api/requests/').flush([]);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('New request');
    TestBed.inject(RoleService).select('expert');
    fixture.detectChanges();
    const request = http.expectOne('/api/requests/');
    expect(request.request.headers.get('X-Demo-Role')).toBe('expert');
    request.flush([]);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Review queue');
    expect((fixture.nativeElement as HTMLElement).textContent).not.toContain('New request');
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('AI Review');
    expect((fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.expert-tabs button')?.disabled).toBe(true);
  });

  it('shows expert AI views and retains manual review data across tabs', async () => {
    await TestBed.configureTestingModule({ imports: [Requests], providers: [provideHttpClient(), provideHttpClientTesting()] }).compileComponents();
    TestBed.inject(RoleService).select('expert');
    const fixture = TestBed.createComponent(Requests);
    const http = TestBed.inject(HttpTestingController);
    const summary = { id: '123', candidateName: 'Candidate', targetLevel: 'A3', presentationName: 'slides.pptx', createdAt: '2026-01-01', reviewed: false };
    fixture.detectChanges();
    http.expectOne('/api/requests/').flush([summary]);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;
    element.querySelector<HTMLButtonElement>('.request-item')!.click();
    http.expectOne('/api/requests/123').flush({ request: summary, questions: 'Existing question', transcript: 'Existing transcript', feedback: 'Existing feedback' });
    await fixture.whenStable();
    expect(element.querySelector('#tab-review')?.getAttribute('aria-selected')).toBe('true');
    expect(element.querySelector<HTMLButtonElement>('#panel-review button')?.disabled).toBe(true);
    element.querySelector<HTMLButtonElement>('#tab-questions')!.click();
    await fixture.whenStable();
    expect(element.querySelector<HTMLTextAreaElement>('textarea[name="questions"]')?.value).toBe('Existing question');
    element.querySelector<HTMLButtonElement>('#tab-feedback')!.click();
    await fixture.whenStable();
    expect(element.querySelector<HTMLTextAreaElement>('textarea[name="transcript"]')?.value).toBe('Existing transcript');
    expect(element.querySelector<HTMLTextAreaElement>('textarea[name="feedback"]')?.value).toBe('Existing feedback');
    expect(element.querySelector<HTMLButtonElement>('#panel-feedback button')?.disabled).toBe(true);
    element.querySelector<HTMLButtonElement>('.form-actions button')!.click();
    const save = http.expectOne('/api/requests/123/review');
    expect(save.request.body).toEqual({ questions: 'Existing question', transcript: 'Existing transcript', feedback: 'Existing feedback', reviewed: false });
    save.flush(null);
    http.expectOne('/api/requests/').flush([summary]);
    http.verify();
  });
});