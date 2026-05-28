import { Routes } from '@angular/router';
import { authGuard, roleGuard } from './core/auth/auth.guard';

// IMPORTANT: redirect '' a '/dashboard' DEBE estar antes que el path '' del shell
// (gotcha orden de rutas Angular).
export const routes: Routes = [
  // Redirect desde raíz
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },

  // Login (público — sin shell)
  {
    path: 'login',
    loadComponent: () =>
      import('./auth/login/login.component').then((m) => m.LoginComponent),
    title: 'Iniciar sesión — SIG',
  },

  // Smoke test (solo Development)
  {
    path: '_smoke',
    loadComponent: () =>
      import('./_smoke/smoke.component').then((m) => m.SmokeComponent),
    title: 'Smoke Test — SIG',
  },

  // Shell autenticado (AppBar + Sidenav)
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      // Dashboard
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent), title: 'Dashboard — SIG' },

      // Clients
      { path: 'clients', loadComponent: () => import('./features/clients/clients-list.component').then((m) => m.ClientsListComponent), title: 'Clients — SIG' },
      { path: 'clients/nuevo', loadComponent: () => import('./features/clients/client-form.component').then((m) => m.ClientFormComponent), title: 'Nuevo Client — SIG' },
      { path: 'clients/:id', loadComponent: () => import('./features/clients/client-detail.component').then((m) => m.ClientDetailComponent), title: 'Detalle Client — SIG' },
      { path: 'clients/:id/editar', loadComponent: () => import('./features/clients/client-form.component').then((m) => m.ClientFormComponent), title: 'Editar Client — SIG' },

      // Projects
      { path: 'projects', loadComponent: () => import('./features/projects/projects-list.component').then((m) => m.ProjectsListComponent), title: 'Projects — SIG' },
      { path: 'projects/nuevo', loadComponent: () => import('./features/projects/project-form.component').then((m) => m.ProjectFormComponent), title: 'Nuevo Project — SIG' },
      { path: 'projects/:id', loadComponent: () => import('./features/projects/project-detail.component').then((m) => m.ProjectDetailComponent), title: 'Detalle Project — SIG' },
      { path: 'projects/:id/editar', loadComponent: () => import('./features/projects/project-form.component').then((m) => m.ProjectFormComponent), title: 'Editar Project — SIG' },

      // Actions
      { path: 'actions', loadComponent: () => import('./features/actions/actions-list.component').then((m) => m.ActionsListComponent), title: 'Actions — SIG' },
      { path: 'actions/nuevo', loadComponent: () => import('./features/actions/action-form.component').then((m) => m.ActionFormComponent), title: 'Nueva Action — SIG' },
      { path: 'actions/:id', loadComponent: () => import('./features/actions/action-detail.component').then((m) => m.ActionDetailComponent), title: 'Detalle Action — SIG' },
      { path: 'actions/:id/editar', loadComponent: () => import('./features/actions/action-form.component').then((m) => m.ActionFormComponent), title: 'Editar Action — SIG' },

      // Concepts
      { path: 'concepts', loadComponent: () => import('./features/concepts/concepts-list.component').then((m) => m.ConceptsListComponent), title: 'Concepts — SIG' },
      { path: 'concepts/nuevo', loadComponent: () => import('./features/concepts/concept-form.component').then((m) => m.ConceptFormComponent), title: 'Nuevo Concept — SIG' },
      { path: 'concepts/:id', loadComponent: () => import('./features/concepts/concept-detail.component').then((m) => m.ConceptDetailComponent), title: 'Detalle Concept — SIG' },
      { path: 'concepts/:id/editar', loadComponent: () => import('./features/concepts/concept-form.component').then((m) => m.ConceptFormComponent), title: 'Editar Concept — SIG' },
      { path: 'concepts/:id/formula', loadComponent: () => import('./features/concepts/formula-editor.component').then((m) => m.FormulaEditorComponent), title: 'Editor de Fórmula — SIG' },

      // Variables
      { path: 'variables', loadComponent: () => import('./features/variables/variables-list.component').then((m) => m.VariablesListComponent), title: 'Variables — SIG' },
      { path: 'variables/nueva', loadComponent: () => import('./features/variables/variable-form.component').then((m) => m.VariableFormComponent), title: 'Nueva Variable — SIG' },
      { path: 'variables/:id/editar', loadComponent: () => import('./features/variables/variable-form.component').then((m) => m.VariableFormComponent), title: 'Editar Variable — SIG' },

      // Periods
      { path: 'periods', loadComponent: () => import('./features/periods/periods-list.component').then((m) => m.PeriodsListComponent), title: 'Periods — SIG' },
      { path: 'periods/nuevo', loadComponent: () => import('./features/periods/period-form.component').then((m) => m.PeriodFormComponent), title: 'Nuevo Period — SIG' },
      { path: 'periods/:id/editar', loadComponent: () => import('./features/periods/period-form.component').then((m) => m.PeriodFormComponent), title: 'Editar Period — SIG' },

      // Approvals
      { path: 'approvals', loadComponent: () => import('./features/approvals/approvals.component').then((m) => m.ApprovalsComponent), title: 'Panel de aprobaciones — SIG' },
      { path: 'approvals/pendientes', loadComponent: () => import('./features/approvals/approvals.component').then((m) => m.ApprovalsComponent), title: 'Mis pendientes — SIG', data: { onlyPendientes: true } },

      // Closures
      { path: 'closures', loadComponent: () => import('./features/closures/closures-list.component').then((m) => m.ClosuresListComponent), title: 'Cierres — SIG' },
      { path: 'closures/nuevo', loadComponent: () => import('./features/closures/closure-form.component').then((m) => m.ClosureFormComponent), title: 'Nuevo Cierre — SIG' },
      { path: 'closures/:id', loadComponent: () => import('./features/closures/closure-detail.component').then((m) => m.ClosureDetailComponent), title: 'Detalle Cierre — SIG' },

      // Calculations
      { path: 'calculations/:closureLineId', loadComponent: () => import('./features/calculations/calculation-detail.component').then((m) => m.CalculationDetailComponent), title: 'Detalle de cálculo — SIG' },

      // Audit (Administrator, Auditor)
      { path: 'audit', loadComponent: () => import('./features/audit/audit.component').then((m) => m.AuditComponent), canActivate: [roleGuard], data: { roles: ['Administrator', 'Auditor'] }, title: 'Audit Log — SIG' },

      // Sync (Administrator)
      { path: 'sync', loadComponent: () => import('./features/sync/sync.component').then((m) => m.SyncComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Sincronizaciones — SIG' },

      // Reports
      { path: 'reports', loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent), title: 'Reports — SIG' },

      // Administración — CostCenters
      { path: 'cost-centers', loadComponent: () => import('./features/cost-centers/cost-centers-list.component').then((m) => m.CostCentersListComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Centros de Coste — SIG' },
      { path: 'cost-centers/nuevo', loadComponent: () => import('./features/cost-centers/cost-center-form.component').then((m) => m.CostCenterFormComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Nuevo CECO — SIG' },
      { path: 'cost-centers/:id/editar', loadComponent: () => import('./features/cost-centers/cost-center-form.component').then((m) => m.CostCenterFormComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Editar CECO — SIG' },

      // Administración — Departments
      { path: 'departments', loadComponent: () => import('./features/departments/departments-list.component').then((m) => m.DepartmentsListComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Departments — SIG' },
      { path: 'departments/nuevo', loadComponent: () => import('./features/departments/department-form.component').then((m) => m.DepartmentFormComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Nuevo Department — SIG' },
      { path: 'departments/:id/editar', loadComponent: () => import('./features/departments/department-form.component').then((m) => m.DepartmentFormComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Editar Department — SIG' },

      // Administración — Roles
      { path: 'roles', loadComponent: () => import('./features/roles/roles-list.component').then((m) => m.RolesListComponent), canActivate: [roleGuard], data: { roles: ['Administrator', 'Auditor'] }, title: 'Roles — SIG' },

      // Administración — Users
      { path: 'users', loadComponent: () => import('./features/users/users-list.component').then((m) => m.UsersListComponent), canActivate: [roleGuard], data: { roles: ['Administrator', 'Auditor'] }, title: 'Users — SIG' },
      { path: 'users/nuevo', loadComponent: () => import('./features/users/user-form.component').then((m) => m.UserFormComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Nuevo User — SIG' },
      { path: 'users/:id', loadComponent: () => import('./features/users/user-detail.component').then((m) => m.UserDetailComponent), canActivate: [roleGuard], data: { roles: ['Administrator', 'Auditor'] }, title: 'Detalle User — SIG' },
      { path: 'users/:id/editar', loadComponent: () => import('./features/users/user-form.component').then((m) => m.UserFormComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Editar User — SIG' },
    ],
  },

  // Fallback
  { path: '**', redirectTo: '/dashboard' },
];
