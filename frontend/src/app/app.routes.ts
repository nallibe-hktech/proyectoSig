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

  // A3 INNUVA OAuth Callback (public, from Wolters Kluwer redirect)
  {
    path: 'a3-innuva/oauth-callback',
    loadComponent: () =>
      import('./features/a3-innuva/a3-innuva-oauth-callback.component').then((m) => m.A3InnuvaOAuthCallbackComponent),
    title: 'OAuth Callback — SIG',
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

      // Services
      { path: 'services', loadComponent: () => import('./features/services/services-list.component').then((m) => m.ServicesListComponent), title: 'Servicios — SIG' },
      { path: 'services/nuevo', loadComponent: () => import('./features/services/service-form.component').then((m) => m.ServiceFormComponent), title: 'Nuevo Servicio — SIG' },
      { path: 'services/:id', loadComponent: () => import('./features/services/service-detail.component').then((m) => m.ServiceDetailComponent), title: 'Detalle Servicio — SIG' },
      { path: 'services/:id/editar', loadComponent: () => import('./features/services/service-form.component').then((m) => m.ServiceFormComponent), title: 'Editar Servicio — SIG' },
      { path: 'services/:id/tarifas', loadComponent: () => import('./features/services/tarifas/tarifas-list.component').then((m) => m.TarifasListComponent), title: 'Tarifas — SIG' },
      { path: 'services/:id/presupuestos', loadComponent: () => import('./features/services/presupuestos/presupuestos-list.component').then((m) => m.PresupuestosListComponent), title: 'Presupuestos — SIG' },

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

      // Cierres de Costes (Ola 3b #10) — mensual, evalúa conceptos de Pago.
      { path: 'cierres-costes', loadComponent: () => import('./features/closures/closures-list.component').then((m) => m.ClosuresListComponent), data: { tipoCierre: 'Costes' }, title: 'Cierres de Costes — SIG' },
      { path: 'cierres-costes/nuevo', loadComponent: () => import('./features/closures/closure-form.component').then((m) => m.ClosureFormComponent), data: { tipoCierre: 'Costes' }, title: 'Nuevo Cierre de Costes — SIG' },
      { path: 'cierres-costes/:id', loadComponent: () => import('./features/closures/closure-detail.component').then((m) => m.ClosureDetailComponent), data: { tipoCierre: 'Costes' }, title: 'Detalle Cierre de Costes — SIG' },

      // Cierres de Facturación (Ola 3b #10) — plurianual, evalúa conceptos de Factura.
      { path: 'cierres-facturacion', loadComponent: () => import('./features/closures/closures-list.component').then((m) => m.ClosuresListComponent), data: { tipoCierre: 'Facturacion' }, title: 'Cierres de Facturación — SIG' },
      { path: 'cierres-facturacion/nuevo', loadComponent: () => import('./features/closures/closure-form.component').then((m) => m.ClosureFormComponent), data: { tipoCierre: 'Facturacion' }, title: 'Nuevo Cierre de Facturación — SIG' },
      { path: 'cierres-facturacion/:id', loadComponent: () => import('./features/closures/closure-detail.component').then((m) => m.ClosureDetailComponent), data: { tipoCierre: 'Facturacion' }, title: 'Detalle Cierre de Facturación — SIG' },

      // Compat: la antigua ruta /closures redirige a Cierres de Costes.
      { path: 'closures', redirectTo: 'cierres-costes', pathMatch: 'full' },

      // Alertas de Cierre
      { path: 'alertas', loadComponent: () => import('./features/closures/alerts-list.component').then((m) => m.AlertsListComponent), title: 'Alertas de Cierre — SIG' },

      // Contratos de un día (Ola 2 #2)
      { path: 'contratos-un-dia', loadComponent: () => import('./features/contratos/contratos-un-dia.component').then((m) => m.ContratosUnDiaComponent), canActivate: [roleGuard], data: { roles: ['Administrator', 'Backoffice'] }, title: 'Contratos de un día — SIG' },

      // Calculations
      { path: 'calculations/:closureLineId', loadComponent: () => import('./features/calculations/calculation-detail.component').then((m) => m.CalculationDetailComponent), title: 'Detalle de cálculo — SIG' },

      // Audit (Administrator, Auditor)
      { path: 'audit', loadComponent: () => import('./features/audit/audit.component').then((m) => m.AuditComponent), canActivate: [roleGuard], data: { roles: ['Administrator', 'Auditor'] }, title: 'Audit Log — SIG' },

      // Sync (Administrator)
      { path: 'sync', loadComponent: () => import('./features/sync/sync.component').then((m) => m.SyncComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Sincronizaciones — SIG' },

      // Celero Visitas (Administrator)
      { path: 'celero-visitas', loadComponent: () => import('./features/celero-visitas/celero-visitas.component').then((m) => m.CeleroVisitasComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Visitas Celero — SIG' },
      { path: 'celero-mapeos', loadComponent: () => import('./features/celero-visitas/celero-mapeos.component').then((m) => m.CeleroMapeosComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Gestión de Mapeos Celero — SIG' },

      // Galán — Logística e Inventario
      { path: 'galan', loadComponent: () => import('./features/galan/components/galan-dashboard.component').then((m) => m.GalanDashboardComponent), title: 'Logística Galán — SIG' },

      // Mediapost — Distribución y Entregas
      { path: 'mediapost', loadComponent: () => import('./features/mediapost/components/mediapost-dashboard.component').then((m) => m.MediapostDashboardComponent), title: 'Distribución Mediapost — SIG' },

      // Bizneo — Gestión RRHH
      { path: 'bizneo', loadComponent: () => import('./features/bizneo/components/bizneo-dashboard.component').then((m) => m.BizneoDashboardComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Bizneo RRHH — SIG' },

      // Intratime — Control de Fichajes
      { path: 'intratime', loadComponent: () => import('./features/intratime/components/intratime-dashboard.component').then((m) => m.IntratimedashboardComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'Intratime Fichajes — SIG' },

      // PayHawk — Gestión de Gastos
      { path: 'payhawk', loadComponent: () => import('./features/payhawk/components/payhawk-dashboard.component').then((m) => m.PayHawkDashboardComponent), canActivate: [roleGuard], data: { roles: ['Administrator', 'Fico'] }, title: 'PayHawk Gastos — SIG' },

      // Reports
      { path: 'reports', loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent), title: 'Reports — SIG' },

      // Forecast (PPT slide 36)
      { path: 'forecast', loadComponent: () => import('./features/forecast/forecast-resumen.component').then((m) => m.ForecastResumenComponent), title: 'Forecast — SIG' },

      // A3 INNUVA Nóminas (Testing & Integration)
      { path: 'a3-innuva', loadComponent: () => import('./features/a3-innuva/a3-innuva.component').then((m) => m.A3InnuvaComponent), canActivate: [roleGuard], data: { roles: ['Administrator'] }, title: 'A3 INNUVA Nóminas — SIG' },

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
