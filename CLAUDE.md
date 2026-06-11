# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SIG-ES** is a multi-system integration platform for financial closures and business process management. .NET 10 + Angular 21. Integrations: Bizneo, SGPV, Celero, Intratime, PayHawk, TravelPerk, A3 Innuva, **Galán** (warehouse), **Mediapost** (distribution).

## Tech Stack

**Backend**: ASP.NET Core 10, PostgreSQL 16 (port 5433), EF Core 9, Clean Architecture
**Frontend**: Angular 21, Material 21, RxJS, Prettier
**Infrastructure**: Docker Compose, API (5180), Frontend (4200), CORS on localhost:4200

## Data Source: Galán & Mediapost (✅ Integrated 11 June 2026)

**LOCAL FILE-BASED SYSTEMS** — No external APIs, read monthly from local files:

### Galán (Warehouse/Logistics)
- **Source**: CSV files from `Mediapost/Mediapost/Documentación/`
- **Parser**: Flexible CSV parsing with `NormalizarKeys()` (removes accents, spaces, periods, numeric prefixes)
- **Data sync**: `MediapostExcelClient.GetEntradasAsync()`, GetSalidasAsync(), GetStockAsync()
- **Result**: 1,520 registros (entradas, salidas, stock)
- **Endpoints**: `/api/galan/{entradas|salidas|stock}` + dashboard
- **Auth**: NO authentication required (local files)

### Mediapost (Distribution/Delivery)  
- **Source**: Excel files from `Mediapost/Mediapost/Documentación/`
  - `infpedsit11_*.xlsx` (Pedidos/Orders)
  - `infrecep07_*.xlsx` (Recepciones/Receptions)
- **Parser**: Multi-worksheet support (reads "Report" worksheet, not "Parametros")
  - **Pedidos**: Headers row 4, data row 5+. Cols: A=Nº doc, L=Fecha, M=Estado
  - **Recepciones**: Headers row 5, data row 6+. Cols: A=Nº Recepción, G=Fecha, E=Tipo
- **Data sync**: `MediapostExcelClient.GetPedidosAsync()`, GetRecepcionesAsync()
- **Result**: 151 registros (142 pedidos + 9 recepciones)
- **Endpoints**: `/api/mediapost/{pedidos|recepciones}` + dashboard
- **Auth**: NO authentication required (local files)

### Authorization Pattern (SyncController)
- `/api/sync/galan`, `/api/sync/mediapost` → **No auth** (local files)
- All other `/api/sync/{system}` → Requires **Administrator** or **Admin SIG** role

## Architecture & Structure

**Backend Layers** (Clean Architecture):
1. **Domain** (SIG.Domain): Entities, value objects, domain logic
2. **Application** (SIG.Application): Interfaces, services, DTOs, validation
3. **Infrastructure** (SIG.Infrastructure): DB, APIs, implementations
4. **API** (SIG.API): Controllers, middleware, HTTP

**Key patterns**: 
- Soft-delete via global query filters (`IsDeleted`, `DeletedAt`)
- Staging tables (`staging_*`) for external API syncs
- `DataProcessorService` migrates staging → productive
- JWT with custom claims (see `Program.cs`)
- FluentValidation on all request DTOs
- Named HttpClient pattern with retry policies

**Database**: PostgreSQL 16, 33+ tables, migrations auto-applied on startup
- Creds: Host=localhost, Port=5433, DB=sig_plataforma_dev, User=postgres, Pass=SigEs@2026
- Local dev: `docker-compose up -d`
- Apply migrations: `cd backend/SIG.API && dotnet ef database update`
- Auto-seed demo data in Development (user=demo, pass=Demo#2026!)

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

## Key Services

**Backend**: AuthService, ClientService, ProjectService, ConceptService, PeriodService, ClosureService, ApprovalService, DataProcessorService, CalculationEngine, ExcelExporter, SyncService
**Frontend**: ApiService (HTTP + JWT injection), AuthService (token storage)

## Testing

**Backend** (xUnit): Unit tests mock deps, integration tests use real PostgreSQL via appsettings.Testing.json
**Frontend**: Karma + Jasmine for units (`*.spec.ts`), Playwright for E2E (in `frontend/e2e/`)

## When to Ask User

Ask before: (1) Refactoring domain/app/infra layers, (2) Adding external APIs, (3) Modifying JWT/auth, (4) Major DB schema changes, (5) Formula/approval changes, (6) Frontend routing changes, (7) UI/UX changes
