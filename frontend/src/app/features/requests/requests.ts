import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, signal, untracked } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { AppRole, RoleService } from '../../core/role.service';
import { PresentationAnalysis, RequestDetail, RequestSummary, RequestsService, ReviewInput } from './requests.service';

type ExpertView = 'review' | 'questions' | 'feedback';

@Component({
  selector: 'app-requests',
  imports: [FormsModule],
  templateUrl: './requests.html',
  styleUrl: './requests.css'
})
export class Requests {
  protected readonly roles = inject(RoleService);
  private readonly api = inject(RequestsService);
  private readonly destroyRef = inject(DestroyRef);
  private generation = 0;
  private selectionVersion = 0;
  protected readonly requests = signal<RequestSummary[]>([]);
  protected readonly selected = signal<RequestDetail | null>(null);
  protected readonly creating = signal(false);
  protected readonly loading = signal(false);
  protected readonly saving = signal(false);
  protected readonly analyzing = signal(false);
  protected readonly analysis = signal<PresentationAnalysis | null>(null);
  protected readonly error = signal('');
  protected readonly notice = signal('');
  protected readonly expertView = signal<ExpertView>('review');
  protected candidateName = '';
  protected targetLevel = 'A3';
  protected presentation: File | null = null;
  protected review: ReviewInput = { questions: '', transcript: '', feedback: '', reviewed: false };

  constructor() {
    effect(() => {
      const role = this.roles.role();
      const generation = ++this.generation;
      this.selectionVersion++;
      this.selected.set(null);
      this.requests.set([]);
      this.creating.set(false);
      this.expertView.set('review');
      this.saving.set(false);
      this.analyzing.set(false);
      this.analysis.set(null);
      this.error.set('');
      this.notice.set('');
      untracked(() => this.load(role, generation));
    });
  }

  protected newRequest(): void {
    this.selectionVersion++;
    this.selected.set(null);
    this.analyzing.set(false);
    this.analysis.set(null);
    this.candidateName = '';
    this.targetLevel = 'A3';
    this.presentation = null;
    this.error.set('');
    this.notice.set('');
    this.creating.set(true);
  }

  protected selectFile(event: Event): void {
    this.presentation = (event.target as HTMLInputElement).files?.item(0) ?? null;
  }

  protected createRequest(): void {
    const file = this.presentation;
    if (!this.candidateName.trim() || !file || !/\.pptx?$/i.test(file.name) || file.size === 0 || file.size > 10 * 1024 * 1024) {
      this.error.set('Enter a candidate name and choose a .ppt or .pptx file up to 10 MB.');
      return;
    }
    const generation = this.generation;
    this.error.set('');
    this.saving.set(true);
    this.api.create(this.candidateName.trim(), this.targetLevel, file).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (request) => {
        if (generation !== this.generation) return;
        this.saving.set(false);
        this.creating.set(false);
        this.load('applicant', generation);
        this.openRequest(request);
        this.notice.set('Request submitted.');
      },
      error: (error: HttpErrorResponse) => {
        if (generation !== this.generation) return;
        this.saving.set(false);
        this.error.set(this.errorMessage(error, 'Could not submit the request.'));
      }
    });
  }

  protected openRequest(request: RequestSummary): void {
    const generation = this.generation;
    const selection = ++this.selectionVersion;
    this.creating.set(false);
    this.expertView.set('review');
    this.selected.set(null);
    this.analyzing.set(false);
    this.analysis.set(null);
    this.error.set('');
    this.notice.set('');
    this.api.get(request.id, this.roles.role()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (detail) => {
        if (generation !== this.generation || selection !== this.selectionVersion) return;
        this.review = { questions: detail.questions ?? '', transcript: detail.transcript ?? '', feedback: detail.feedback ?? '', reviewed: detail.request.reviewed };
        this.selected.set(detail);
      },
      error: (error: HttpErrorResponse) => {
        if (generation === this.generation && selection === this.selectionVersion) this.error.set(this.errorMessage(error, 'Could not load the request.'));
      }
    });
  }

  protected navigateExpertTabs(event: KeyboardEvent): void {
    const views: ExpertView[] = ['review', 'questions', 'feedback'];
    const current = views.indexOf(this.expertView());
    let next = current;
    if (event.key === 'ArrowRight') next = (current + 1) % views.length;
    else if (event.key === 'ArrowLeft') next = (current + views.length - 1) % views.length;
    else if (event.key === 'Home') next = 0;
    else if (event.key === 'End') next = views.length - 1;
    else return;
    event.preventDefault();
    this.expertView.set(views[next]);
    (event.currentTarget as HTMLElement).parentElement?.querySelector<HTMLElement>(`#tab-${views[next]}`)?.focus();
  }

  protected analyzePresentation(): void {
    const detail = this.selected();
    if (!detail || this.roles.role() !== 'expert' || this.analyzing()) return;
    const generation = this.generation;
    const selection = this.selectionVersion;
    this.analyzing.set(true);
    this.error.set('');
    this.notice.set('');
    this.api.analyzePresentation(detail.request.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (draft) => {
        if (generation !== this.generation || selection !== this.selectionVersion) return;
        this.analysis.set(draft);
        this.analyzing.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (generation !== this.generation || selection !== this.selectionVersion) return;
        this.analyzing.set(false);
        this.error.set(this.errorMessage(error, 'Could not analyze the presentation.'));
      }
    });
  }

  protected saveReview(): void {
    const detail = this.selected();
    if (!detail || this.roles.role() !== 'expert') return;
    if (this.review.reviewed && !this.review.feedback.trim()) {
      this.error.set('Feedback is required to mark a request reviewed.');
      return;
    }
    const generation = this.generation;
    const selection = this.selectionVersion;
    const review = { ...this.review };
    this.saving.set(true);
    this.error.set('');
    this.notice.set('');
    this.api.saveReview(detail.request.id, review).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        if (generation !== this.generation) return;
        this.saving.set(false);
        this.load('expert', generation);
        if (selection === this.selectionVersion) {
          this.selected.set({ ...detail, request: { ...detail.request, reviewed: review.reviewed }, ...review });
          this.notice.set('Review saved.');
        }
      },
      error: (error: HttpErrorResponse) => {
        if (generation !== this.generation) return;
        this.saving.set(false);
        this.error.set(this.errorMessage(error, 'Could not save the review.'));
      }
    });
  }

  protected downloadPresentation(): void {
    const detail = this.selected();
    if (!detail) return;
    const generation = this.generation;
    this.api.download(detail.request.id, this.roles.role()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (file) => {
        if (generation !== this.generation) return;
        const url = URL.createObjectURL(file);
        const link = document.createElement('a');
        link.href = url;
        link.download = detail.request.presentationName;
        link.hidden = true;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1000);
      },
      error: () => { if (generation === this.generation) this.error.set('Could not download the presentation.'); }
    });
  }

  private load(role: AppRole, generation: number): void {
    this.loading.set(true);
    this.api.list(role).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (requests) => {
        if (generation !== this.generation) return;
        this.requests.set(requests);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (generation !== this.generation) return;
        this.loading.set(false);
        this.error.set(this.errorMessage(error, 'Could not load requests.'));
      }
    });
  }

  private errorMessage(error: HttpErrorResponse, fallback: string): string {
    return typeof error.error === 'string' && error.error.length < 300 ? error.error : fallback;
  }
}