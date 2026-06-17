# PROGRESO_BACKEND

## Environment probe
- dotnet --list-sdks: 10.0.103, 10.0.104
- dotnet --list-runtimes: .NET 10.0.4+
- PostgreSQL: 16.12 (detected in ENVIRONMENT.md)
- DB password from ENVIRONMENT.md: **admin** (confirmed)

## Fast mode
Backend solution already existed with all 65 endpoints implemented. Fast mode activated.

## Backend build
**PASS** — 5 projects compiled successfully (0 errors, 0 warnings)

## Migrations
Existing migrations applied automatically at startup via `Database.MigrateAsync()`

Migraciones clave del refactor PPT y closure alerts:
- `20260612071833_RenameProjectActionToService` — rename data-preserving de las tablas/columnas Project/Action → Service (services, service_concepts, service_users, service_cost_centers; `Concept.ProjectId` → `ServiceId`; `Closure.ProjectId` → `ServiceId`). No hay pérdida de datos.
- `20260612121215_AddClosureAlertasAndA3Contratos` — añade la entidad/tabla `ClosureAlerta` (alertas de cierre) y los contratos A3.

## Refactor Project → Service (PPT) — COMPLETADO

- Eliminadas las entidades internas `Project` y `Action`. La entidad de dominio es ahora **`Service`** (tabla `services`), con la jerarquía **Cliente → Servicio → Concepto**.
- Service absorbe el vínculo directo a `Client` y las relaciones que colgaban de Project/Action: `ServiceConcept`, `ServiceUser` (ownership), `ServiceCostCenter`, `Closure`, `TarifaServicio`, `PresupuestoServicio`.
- `Concept.ServiceId` y `Closure.ServiceId` (antes `ProjectId`).
- Endpoints: `/api/services` (eliminados `/api/projects` y `/api/actions`). Tarifas/presupuestos cuelgan de `api/services/{serviceId}/...`.
- Capa Application/Infrastructure: `IServiceService`/`ServiceService`, `IServiceRepository`/`ServiceRepository` (firma `GetByIdAndUsuarioIdAsync(int id, int usuarioId)`); DTOs `ServiceListItemDto`/`ServiceDetailDto`/`ServiceCreateRequest`/`ServiceUpdateRequest`; enum `EstadoServicio { Activo, Inactivo }`.
- Fidelidad externa: los campos de sistemas ajenos (Intratime `PROJECT_ID`/`INOUT_PROJECT_NAME`, etc.) NO se renombran.

## Closure alerts — IMPLEMENTADO

- Entidad `ClosureAlerta` (`TipoAlerta { Bloqueante, Advertencia }`, código, descripción, detalle, flag `Confirmada` con usuario y fecha).
- DTO `ClosureAlertaDto`; `ClosureDetailDto` expone `Alertas`.
- Alertas bloqueantes no confirmadas impiden avanzar el cierre.

## Staging canónico

- Modelo canónico: `staging_galan_{entradas,salidas,stocks}` y `staging_mediapost_{pedidos,recepciones}`. Las tablas `staging_mdp_*` ya NO existen.

## Smoke test
**PASS** — API started and listening on http://localhost:5180

BACKEND_PORT_HTTPS=5181
BACKEND_PORT_HTTP=5180

## Seed users
N/A (seed auto-run depends on Seed:AutoRun=true in Development)

## Build fixes applied
1. `DashboardCalcSyncAudit.cs:294` — Fixed nullable TimeSpan access (`.TotalHours` via `.Value`)
2. `DashboardCalcSyncAudit.cs:305` — Fixed nullable DateTime access to `SpecifyKind`
3. `IServices.cs` — Added `ValidarDiscrepanciasIntratimeAsync` to `IDataProcessorService` interface
