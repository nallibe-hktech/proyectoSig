# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SIG-ES** is a multi-system integration platform for financial closures, payroll, and business process management. .NET 10 + Angular 21. Integrations complete for: Bizneo (HR), SGPV (Products), Celero (Sales/Projects), Intratime (Time tracking), PayHawk (Expenses), TravelPerk (Travel), A3 Innuva (ERP), **Galán** (warehouse), **Mediapost** (distribution).

## Tech Stack

**Backend**: ASP.NET Core 10, PostgreSQL 16, EF Core 9, Clean Architecture, JWT auth
**Frontend**: Angular 21, Material 21, RxJS, Prettier, Karma+Jasmine, Playwright
**Infrastructure**: Docker Compose (PostgreSQL), API (:5180), Frontend (:4200), CORS on localhost:4200

## Database (PostgreSQL)

**Local dev connection**:
```
Host: localhost | Port: 5433 | Database: sig_plataforma_dev
Username: postgres | Password: SigEs@2026
```
**Startup**: `docker-compose up -d`
**Apply migrations**: `cd backend/SIG.API && dotnet ef database update`
**Auto-seed demo**: Development env seeds demo user (demo/Demo#2026!) on first run
**Schema**: 33+ tables (entities, staging, audit logs). Migrations auto-applied on startup.

## External Integrations (Existing)

| System | Type | Purpose | Client | Status |
|--------|------|---------|--------|--------|
| **Bizneo** | HTTP OAuth2 | HR/HCM — employees, hours, absences | DMS | ✅ Integrated |
| **SGPV** | HTTP Basic Auth | Product catalog | DMS | ✅ Integrated |
| **Celero** | PostgreSQL direct | Sales/projects/visits/clients | CRM | ✅ Integrated |
| **Intratime** | HTTP (Token) | Time tracking — fichajes, attendance | DMS | ✅ Integrated |
| **PayHawk** | HTTP API | Expense management — receipts, employee expenses | Finance | ✅ Integrated |
| **TravelPerk** | HTTP API | Travel bookings — flights, hotels, costs | Finance | ⏳ Pending API Key |
| **A3 Innuva** | HTTP OAuth2 | ERP — invoicing, GL, customer data | Finance | ⏳ Pending Conectia registration |

**Pattern**: Each system has staging table (`staging_*`). `DataProcessorService` syncs staging → productive tables.
**Sync endpoint**: `POST /api/sync/{system}` (requires Administrator or Admin SIG role for external APIs)

## Data Sources: Galán & Mediapost (✅ Integrated 11 June 2026)

**LOCAL FILE-BASED SYSTEMS** — No external APIs, monthly file updates:

### Galán (Warehouse/Logistics)
- **Source**: CSV from `C:\Projects\workspaces\SIG-es\Mediapost\Mediapost\Documentación\`
- **Parser**: `GalanCsvClient` — flexible header parsing with `NormalizarKeys()` (handles accents, spaces, periods, numeric prefixes)
- **Data**: 1,520 registros (entradas, salidas, stock)
- **Sync**: `POST /api/sync/galan` → calls `GalanCsvClient.GetEntradasAsync()`, GetSalidasAsync(), GetStockAsync()
- **Endpoints**: `/api/galan/entradas`, `/api/galan/salidas`, `/api/galan/stock` + `/api/galan/dashboard`
- **Auth**: ❌ NO authentication (local files)

### Mediapost (Distribution/Delivery)  
- **Source**: Excel from same `Documentación/` folder:
  - `infpedsit11_*.xlsx` → Pedidos (orders), 206 rows
  - `infrecep07_*.xlsx` → Recepciones (receptions), 15 rows
- **Parser**: `MediapostExcelClient` — multi-worksheet support
  - Reads "Report" worksheet (not "Parametros" which is metadata)
  - **Pedidos**: Headers row 4, data row 5+. Cols: A=Nº doc, L=Fecha expedición, M=Estado
  - **Recepciones**: Headers row 5, data row 6+. Cols: A=Nº Recepción, G=Fecha, E=Tipo Recepción
- **Data**: 151 registros (142 pedidos + 9 recepciones)
- **Sync**: `POST /api/sync/mediapost` → calls `MediapostExcelClient.GetPedidosAsync()`, GetRecepcionesAsync()
- **Endpoints**: `/api/mediapost/pedidos`, `/api/mediapost/recepciones` + `/api/mediapost/dashboard`
- **Auth**: ❌ NO authentication (local files)

### Authorization (SyncController)
- `/api/sync/galan` + `/api/sync/mediapost` → **No auth required** (local files)
- All other `/api/sync/{system}` → **Administrator** or **Admin SIG** role required
- See: `backend/SIG.API/Controllers/OtherControllers.cs` line 84-92

## Architecture & Core Services

**Backend Layers** (Clean Architecture):
1. **Domain** (SIG.Domain): Entities, enums, domain logic
2. **Application** (SIG.Application): Service interfaces, DTOs, validation
3. **Infrastructure** (SIG.Infrastructure): DB, external APIs, implementations
4. **API** (SIG.API): Controllers, middleware, JWT

**Key Patterns**: 
- Soft-delete: Global query filters exclude deleted records; `.IgnoreQueryFilters()` to override
- Staging tables (`staging_*`) for external syncs → `DataProcessorService` migrates to productive
- JWT: Custom claims mapping in `Program.cs`; validate before modifying auth
- FluentValidation on all request DTOs; `ValidationFilter` catches errors
- Named HttpClient pattern with retry policies

**Core Services** (SIG.Application + SIG.Infrastructure):
- `AuthService` — JWT generation, BCrypt hashing
- `ClientService` — CRUD + Celero sync
- `ProjectService` — CRUD, user assignment
- `ConceptService` — Formula evaluation (MathNet.Numerics)
- `PeriodService` — Period CRUD + recalc
- `ClosureService` — Multi-line closures
- `ApprovalService` — Multi-role workflow (Gestor → FICO → Director)
- `SyncService` — Orchestrates all external API syncs
- `DataProcessorService` — Staging → productive migration
- `ExcelExporter` — ClosedXML multi-format export
- `GalanCsvClient`, `MediapostExcelClient` — Local file parsers

## Development Commands

**Backend**:
```bash
dotnet build backend                                    # Build
dotnet run (in backend/SIG.API)                        # Start API on :5180
dotnet test backend                                     # Run all tests
dotnet ef migrations add MigrationName --project ../SIG.Infrastructure  # Create migration
dotnet ef database update                               # Apply migrations
```

**Frontend**:
```bash
npm start                    # Dev server :4200
npm test                     # Unit tests (Karma)
npx playwright test          # E2E tests (need backend + frontend running)
```

**Database**:
```bash
docker-compose up -d         # Start PostgreSQL
docker-compose down          # Stop
psql -h localhost -p 5433 -U postgres -d sig_plataforma_dev  # Connect directly
```

## Configuration Files

**Backend** (`backend/SIG.API/appsettings*.json`):
- `appsettings.json` → Base config
- `appsettings.Development.json` → Local dev (auto-seed enabled)
- `appsettings.Testing.json` → Test DB
- Key vars: `ConnectionStrings:DefaultConnection`, `JwtSettings:SigningKey`, `Integrations` (API creds), `Seed:AutoRun`

**Frontend** (`frontend/src/environments/`):
- `environment.ts` → Dev (apiUrl=http://localhost:5180)
- `environment.prod.ts` → Production

## Important Patterns

1. **JWT Auth**: Custom claims mapping in `Program.cs` — check before modifying
2. **Validation**: FluentValidation on all DTOs, caught by `ValidationFilter`
3. **Soft deletes**: `.IgnoreQueryFilters()` to override
4. **Migrations**: Create new, never modify applied. Delete unapplied with `Remove-Migration`
5. **External APIs**: Staging tables + `DataProcessorService` → productive
6. **Entity Configs**: `SIG.Infrastructure/Persistence/Configurations/`
7. **DateTime & PostgreSQL** (**CRITICAL**): When persisting external data to `timestamp with time zone` columns, ALWAYS wrap DateTime with `DateTime.SpecifyKind(value, DateTimeKind.Utc)`. Npgsql requires Kind=Utc or sync will fail with "Cannot write DateTime with Kind=Unspecified". This applies to all sync operations in `SyncService` (DashboardCalcSyncAudit.cs).

## Frontend: Auto-Sync on File Upload (11 June 2026)

**Galán & Mediapost dashboards now auto-trigger sync after file upload** — users upload file → sync runs → data visible in tabs.

**Implementation**:
- Dashboard components inject `SyncService` (from `core/api/misc.service`) and `NotifyService` (from `core/notify.service`)
- On successful upload, `uploadFile()` calls `syncManual()` which invokes `syncSvc.sync('galan'|'mediapost')`
- Spinner appears during sync, toast shows "Sincronizado: X registros nuevos"
- Manual "Sincronizar" button in page header allows re-sync without uploading
- Files modified: `frontend/src/app/features/galan/components/galan-dashboard.component.ts` + same for mediapost
- **Import paths**: NotifyService is at `core/notify.service` (NOT `core/services/notify.service`)

## Key Services

**Backend**: AuthService, ClientService, ProjectService, ConceptService, PeriodService, ClosureService, ApprovalService, DataProcessorService, CalculationEngine, ExcelExporter, SyncService
**Frontend**: ApiService (HTTP + JWT injection), AuthService (token storage)

## Testing

**Backend** (xUnit): Unit tests mock deps, integration tests use real PostgreSQL via appsettings.Testing.json
**Frontend**: Karma + Jasmine for units (`*.spec.ts`), Playwright for E2E (in `frontend/e2e/`)

## When to Ask User

Ask before: (1) Refactoring domain/app/infra layers, (2) Adding external APIs, (3) Modifying JWT/auth, (4) Major DB schema changes, (5) Formula/approval changes, (6) Frontend routing changes, (7) UI/UX changes
