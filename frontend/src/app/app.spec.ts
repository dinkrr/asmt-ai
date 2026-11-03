import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { RoleService } from './core/role.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    TestBed.inject(HttpTestingController).expectOne('/api/health').flush({ status: 'ok' });
    expect(app).toBeTruthy();
  });

  it('should render the API status', async () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController).expectOne('/api/health').flush({ status: 'ok' });
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[role="status"]')?.textContent).toContain('API connected');
  });

  it('shows when the API is unavailable', async () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController).expectOne('/api/health').flush('Unavailable', { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[role="status"]')?.textContent).toContain('API unavailable');
  });

  it('switches the local role', async () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController).expectOne('/api/health').flush({ status: 'ok' });
    await fixture.whenStable();
    const select = (fixture.nativeElement as HTMLElement).querySelector('select')!;
    select.value = 'expert';
    select.dispatchEvent(new Event('change'));
    expect(TestBed.inject(RoleService).role()).toBe('expert');
  });
});
