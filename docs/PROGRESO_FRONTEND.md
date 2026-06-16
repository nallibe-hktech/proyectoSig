# Informe del Frontend — SIG-ES

| Campo | Valor |
|-------|-------|
| Fecha | 8 Junio 2026 |
| Stack aplicado | Angular 21 + Angular Material 21 (M3) |
| Diseño del Designer implementado | OK |
| Frontend build | PASS |

## Estado del proyecto

El frontend está **completamente implementado** con los 16 módulos funcionales:

| Módulo | Estado | Componentes | data-testid |
|--------|--------|-------------|-------------|
| Login | OK | LoginComponent | ✅ |
| Dashboard | OK | DashboardComponent | ✅ |
| Clientes | OK | ClientsList, ClientForm, ClientDetail | ✅ |
| Servicios | OK | ServicesList, ServiceForm, ServiceDetail (+ Tarifas, Presupuestos) | ✅ |
| Conceptos | OK | ConceptsList, ConceptForm, ConceptDetail, FormulaEditor | ✅ |
| Variables | OK | VariablesList, VariableForm | ✅ |
| Periodos | OK | PeriodsList, PeriodForm | ✅ |
| Aprobaciones | OK | ApprovalsComponent | ✅ |
| Cierres | OK | ClosuresList, ClosureForm, ClosureDetail (con panel de alertas de cierre), RejectDialog | ✅ |
| Cálculos | OK | CalculationDetailComponent | ✅ |
| Auditoría | OK | AuditComponent | ✅ |
| Sincronización | OK | SyncComponent | ✅ |
| Reportes | OK | ReportsComponent | ✅ |
| Roles | OK | RolesListComponent | ✅ |
| Usuarios | OK | UsersList, UserForm, UserDetail | ✅ |
| CECOs | OK | CostCentersList, CostCenterForm | ✅ |
| Departamentos | OK | DepartmentsList, DepartmentForm | ✅ |
| Celero Visitas | OK | CeleroVisitasComponent | ✅ |

## Bugs [FRONTEND-BUG] de iteraciones anteriores resueltos

- **N/A** — No había bugs de frontend en docs/BLOQUEANTES.md

## Issues SonarQube resueltos

- **N/A** — Todos los issues en SONAR_ISSUES.md son de backend

## Bloqueantes nuevos

- **0** — Sin bloqueantes nuevos

## Refactor Project → Service (PPT) — COMPLETADO

- Las antiguas features `projects/` y `actions/` se **eliminaron y consolidaron** en `features/services/` (jerarquía Cliente → Servicio → Concepto).
- Componentes: `services-list.component.ts`, `service-form.component.ts`, `service-detail.component.ts` (+ subcarpetas `tarifas/` y `presupuestos/`).
- Servicio HTTP: `core/api/services.service.ts` → clase `ServiceService` apuntando a `/api/services` (sustituye a ProjectsService + ActionsService).
- Modelos TS renombrados: `ServiceListItem`/`ServiceDetail` (antes Project*/Action*), `EstadoServicio` (antes EstadoProyecto/EstadoAccion); Concept usa `serviceIds`.
- Menú lateral: entradas "Projects"/"Actions" sustituidas por una única "Servicios".

## Closure alerts — IMPLEMENTADO

- `ClosureDetail` muestra panel de alertas de cierre (modelo `ClosureAlerta`: tipo Bloqueante/Advertencia, código, descripción, confirmación).
- Las alertas bloqueantes no confirmadas inhabilitan el avance del cierre en la UI.

## Cambios realizados

1. **SyncComponent** — Corregido error TS2345: el tipo `System` incluía `'intratime-empleados'` que no está en la unión de `SyncService.sync()`. Se eliminó del type y del array de sistemas.

## Build

- `ng build --configuration development` → PASS (18.7s)
- Lazy chunks generados correctamente para todos los módulos
- Sin warnings
