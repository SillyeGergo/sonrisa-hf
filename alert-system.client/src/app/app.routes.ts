import { Routes } from '@angular/router';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { UserDashboardComponent } from './user-dashboard.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'user'
  },
  {
    path: 'user',
    component: UserDashboardComponent
  },
  {
    path: 'admin',
    component: AdminDashboardComponent
  },
  {
    path: '**',
    redirectTo: 'user'
  }
];
