import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: 'prijava',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'registracija',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register.component').then(m => m.RegisterComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then(m => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'nadzorna-ploca' },
      {
        path: 'nadzorna-ploca',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'upiti',
        loadComponent: () => import('./features/tickets/ticket-list.component').then(m => m.TicketListComponent)
      },
      {
        path: 'upiti/novi',
        canActivate: [roleGuard('student')],
        loadComponent: () => import('./features/tickets/ticket-create.component').then(m => m.TicketCreateComponent)
      },
      {
        path: 'upiti/:id',
        loadComponent: () => import('./features/tickets/ticket-detail.component').then(m => m.TicketDetailComponent)
      },
      {
        path: 'administracija/kategorije',
        canActivate: [roleGuard('admin')],
        loadComponent: () => import('./features/admin/categories-admin.component').then(m => m.CategoriesAdminComponent)
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
