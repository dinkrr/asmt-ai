import { Routes } from '@angular/router';

export const routes: Routes = [
	{ path: '', pathMatch: 'full', redirectTo: 'requests' },
	{ path: 'requests', loadComponent: () => import('./features/requests/requests').then(module => module.Requests) }
];
