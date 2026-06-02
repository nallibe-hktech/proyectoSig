# PROGRESO_BACKEND.md — v2.1 (verificado 2026-06-02)

## Environment probe
- .NET SDK: 10.0.103, 10.0.104 (using 10.0.104)
- PostgreSQL: Detectado en localhost:5432
- Build: PASS (0 errors)
- Migraciones pendientes: No (modelo al día)

## Connection strings (coinciden con ENVIRONMENT.md)
- `appsettings.json`: `Password=__SET_VIA_ENVIRONMENT__` ✅
- `appsettings.Development.json`: `Host=localhost;Port=5432;Database=sig_plataforma_dev;Username=postgres;Password=admin` ✅
- `appsettings.Testing.json`: `Host=localhost;Port=5432;Database=sig_plataforma_test;Username=postgres;Password=admin` ✅
- `appsettings.E2E.json`: `Host=localhost;Port=5432;Database=sig_plataforma_e2e;Username=postgres;Password=admin` ✅

## Build
- `dotnet build`: PASS (0 errors, 8 warnings — solo NU1903 transitivo)

## Smoke test (2026-06-02)
- API starts: PASS (listening on http://localhost:5180)
- DB migrations: PASS (no pending changes)
- DB seed: CONFIGURADO (Seed:AutoRun=true en Development)

## Puertos
BACKEND_PORT_HTTPS=5181
BACKEND_PORT_HTTP=5180

## Controllers verificados (FAST MODE) — 26 controllers, ~80+ endpoints
- AuthController: login, refresh, logout, me ✅
- ClientsController: list, get, create, update, delete ✅
- ProjectsController: list, get, create, update, delete ✅
- ActionsController: list, get, create, update, delete ✅
- ConceptsController: list, get, create, update, delete, validar-formula ✅
- UsersController: list, get, create, update, password, delete ✅
- RolesController: list ✅
- DepartmentsController: list, create, update, delete ✅
- CostCentersController: list, create, update, delete ✅
- PeriodsController: list, activo, get, create, update, cerrar, reabrir ✅
- DashboardController: kpis, avisos, mis-proyectos ✅
- ClosuresController: list, get, create, recalcular, aprobar, rechazar ✅
- ApprovalsController: list, pendientes, historial, batch-aprobar, batch-rechazar ✅
- CalculationsController: get ✅
- AuditController: list ✅
- SyncController: sync, celero-stats ✅
- ExportsController: a3-innuva, a3-erp ✅
- DevController: regenerar-seed ✅
- HealthController: get ✅
- VariablesController: list, get, create, update, delete ✅
- CeleroVisitasController: list, get, update ✅
- TarifasController: list, get, create, update, delete ✅
- PresupuestosController: list, get, create, update, delete ✅
- CeleroResourceMappingsController: list, create, delete ✅
- CeleroServiceMappingsController: list, create, delete ✅
- CeleroMissionMappingsController: list, create, delete ✅

## Bloqueantes
- Ninguno activo.
- Warnings EF Core 10622 (global query filters en required navigations) — no bloqueante, comportamiento conocido.

---

## Informe del Backend

- Stack aplicado: PostgreSQL 16.12 + JWT (BCrypt) + .NET 10
- Environment probe: OK
- Database password de ENVIRONMENT.md: SI (Password=admin)
- Backend build: PASS
- Smoke test: PASS
- Puerto HTTP: 5180 | Puerto HTTPS: 5181
- Seed usuarios: CONFIGURADO (Seed:AutoRun=true)
- Bloqueantes: 0
