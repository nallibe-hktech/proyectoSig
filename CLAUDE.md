# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## ⚠️ CRITICAL WORKFLOW RULE

**NO COMMITS OR PRs WITHOUT USER CONFIRMATION** — Before executing `git commit`, `git push`, or creating/merging PRs, ALWAYS show the user what changes are proposed and wait for explicit approval. Do not assume permission from prior conversations.

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
# Building & Running
dotnet build backend                                    # Build solution
cd backend/SIG.API && dotnet run                        # Start API on :5180 with hot reload
dotnet build backend --configuration Release            # Production build

# Testing
dotnet test backend                                     # Run all tests
dotnet test backend --filter "ClassName"                # Run tests in specific class
dotnet test backend --filter "TestMethodName"           # Run specific test
dotnet test backend --logger "console;verbosity=detailed"  # Verbose output
dotnet test backend --collect:"XPlat Code Coverage"     # Run with coverage

# Migrations & Database
dotnet ef migrations add MigrationName --project ../SIG.Infrastructure  # Create migration
dotnet ef database update                               # Apply pending migrations
dotnet ef database update --connection "Host=localhost;Port=5433;Database=sig_plataforma_dev;Username=postgres;Password=SigEs@2026"  # Force DB reset
```

**Frontend**:
```bash
# Development & Building
npm start                    # Dev server :4200 with auto-reload
npm run build                # Production build to dist/
npm run watch                # Build watch mode (for debugging builds)

# Testing & Quality
npm test                     # Unit tests (Karma, watch mode)
npm test -- --watch=false --code-coverage  # Single run with coverage report
npx playwright test          # E2E tests (requires backend + frontend running)
npx playwright test --ui     # E2E with UI inspector
npx prettier --check src/    # Check formatting
npx prettier --write src/    # Auto-format code
```

**Database**:
```bash
docker-compose up -d         # Start PostgreSQL (from project root)
docker-compose down          # Stop PostgreSQL
psql -h localhost -p 5433 -U postgres -d sig_plataforma_dev  # Connect directly
```

## IDE Setup

**Backend (Visual Studio 2022 / VS Code)**:
- Open `backend/SIG.slnx` in Visual Studio 2022 (modern solution format)
- Projects load in order: Domain → Application → Infrastructure → API, Tests
- Set `SIG.API` as startup project for F5 debugging
- Use Debug > Start Debugging to launch with breakpoints on port :5180

**Frontend (VS Code / WebStorm / Angular IDE)**:
- Open `frontend/` folder in IDE
- Install Prettier extension for auto-formatting on save
- Set up Angular Language Service for template intellisense

## Debugging

**Backend (.NET)**:
- Set breakpoints in any layer (Domain, Application, Infrastructure, API)
- F5 to debug; breakpoints work across all project boundaries
- Use Debug Console to inspect local variables
- `appsettings.Development.json` enables detailed logging and stack traces

**Frontend (Angular)**:
- Chrome DevTools: F12 while running `npm start`
- Sources tab: Set breakpoints in `.ts` files (source maps provided)
- Angular DevTools extension for component inspection
- Network tab to inspect API calls to `http://localhost:5180`

## Configuration Files

**Backend** (`backend/SIG.API/appsettings*.json`):
- `appsettings.json` → Base config
- `appsettings.Development.json` → Local dev (auto-seed enabled, verbose logging)
- `appsettings.Testing.json` → Test DB
- Key vars: `ConnectionStrings:DefaultConnection`, `JwtSettings:SigningKey`, `Integrations` (API creds), `Seed:AutoRun`, `Logging:LogLevel`

**Frontend** (`frontend/src/environments/`):
- `environment.ts` → Dev (apiUrl=http://localhost:5180, debug enabled)
- `environment.prod.ts` → Production (optimized, minified)

**Formatting** (`frontend/`):
- `.prettierrc` (if present) or default Prettier rules (2-space indent, no semicolons)
- Run `npm run format` to auto-fix all files

## Middleware & Request Pipeline

**Exception Handling** (`ExceptionHandlingMiddleware`):
- Global error handler that catches all exceptions before returning to client
- Maps domain exceptions to HTTP status codes (400, 403, 404, 500)
- Returns `{message: string, details?: object}` JSON responses
- Never expose internal stack traces in production

**Request Validation** (`ValidationFilter`):
- FluentValidation runs automatically on all DTOs before controller action
- Returns 400 BadRequest with field-level error messages
- No need to check `ModelState` manually

**JWT Authentication** (`Program.cs`):
- Custom claims mapping: `MapInboundClaims = false` prevents auto-mapping
- Tokens include `sub` (user ID), `role`, and custom claims
- All API endpoints except `/api/auth/login` require `[Authorize]`
- Role-based access: `[Authorize(Roles = "Administrator,Admin SIG")]`

## Dependency Injection & Services

**Service Registration** (in `Infrastructure.cs`):
- All services registered in `AddInfrastructure(config)` extension in `Program.cs`
- Follows Clean Architecture: interfaces in Application layer, implementations in Infrastructure
- Scoped services for EF DbContext and business logic
- Singleton services for caching (if used)

**Adding New Services**:
1. Create interface in `SIG.Application/Interfaces/Services/`
2. Create implementation in `SIG.Infrastructure/Services/`
3. Register in `SIG.Infrastructure/DependencyInjection.cs`: `services.AddScoped<IMyService, MyService>()`
4. Inject into controllers or other services via constructor

## Important Patterns

1. **JWT Auth**: Custom claims mapping in `Program.cs` — check before modifying
2. **Validation**: FluentValidation on all DTOs, caught by `ValidationFilter`
3. **Soft deletes**: `.IgnoreQueryFilters()` to override
4. **Migrations**: Create new, never modify applied. Delete unapplied with `Remove-Migration`
5. **External APIs**: Staging tables + `DataProcessorService` → productive
6. **Entity Configs**: `SIG.Infrastructure/Persistence/Configurations/`
7. **DateTime & PostgreSQL** (**CRITICAL**): When persisting external data to `timestamp with time zone` columns, ALWAYS wrap DateTime with `DateTime.SpecifyKind(value, DateTimeKind.Utc)`. Npgsql requires Kind=Utc or sync will fail with "Cannot write DateTime with Kind=Unspecified". This applies to all sync operations in `SyncService` (DashboardCalcSyncAudit.cs).

## Adding New Entities & Migrations

**Create Entity** (in `SIG.Domain/Entities/`):
```csharp
public class MyEntity : BaseEntity
{
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

**Create Configuration** (in `SIG.Infrastructure/Persistence/Configurations/`):
```csharp
public class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.ToTable("my_entities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).IsRequired().HasMaxLength(200);
    }
}
```

**Register in DbContext** (in `SIG.Infrastructure/Persistence/AppDbContext.cs`):
```csharp
modelBuilder.ApplyConfiguration(new MyEntityConfiguration());
```

**Create & Apply Migration**:
```bash
cd backend/SIG.Infrastructure
dotnet ef migrations add AddMyEntity
cd ../SIG.API
dotnet ef database update
```

## Angular Code Generation

**Generate Component**:
```bash
ng generate component features/my-feature/components/my-component
# Creates: component.ts, .html, .css, .spec.ts
```

**Generate Service**:
```bash
ng generate service core/api/my-feature
# Creates: my-feature.service.ts and .spec.ts
```

**Generate Module** (if not standalone):
```bash
ng generate module features/my-feature
```

All generated code follows the existing project structure (features → components/services).

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

## Code Style & Conventions

**Backend (.NET)**:
- Clean Architecture: Keep business logic in Domain/Application, avoid mixing with Infrastructure
- PascalCase for class/method names, camelCase for local variables and properties
- Use `async/await` for all I/O (database, HTTP calls)
- Exception handling: Catch specific exceptions, not bare `catch (Exception)`
- Naming: Services end with `Service`, Validators with `Validator`, Clients with `Client`

**Frontend (Angular)**:
- Prettier auto-formats on save (2-space indent, semicolons)
- Use standalone components (no NgModule) for new features
- RxJS: Use `takeUntil()` pattern to prevent memory leaks in subscriptions
- Reactive forms for complex forms, template-driven for simple ones
- Material components for UI (no custom CSS unless necessary)
- Store HTTP calls in services, not components

## Common Gotchas & Tips

**Backend**:
- DateTime fields must be `DateTime.UtcNow` in UTC always (not `DateTime.Now`)
- When syncing external data, ALWAYS use `DateTime.SpecifyKind(..., DateTimeKind.Utc)` before saving
- Migrations are immutable once applied — never edit, always create new ones
- DbContext is scoped per request; don't share across threads
- `IgnoreQueryFilters()` bypasses soft-delete filter — use sparingly

**Frontend**:
- Import from `core/notify.service` not `core/services/notify.service` (path is important!)
- Always unsubscribe from observables or use `async` pipe to prevent memory leaks
- Material DatePicker expects `Date` object, not ISO string
- CORS is enabled only for `localhost:4200` — will fail in other origins without backend changes
- Tests use Karma (unit) and Playwright (E2E) — run both before committing

**Database**:
- PostgreSQL 16 with UTC timezone — always verify timezone on timestamp columns
- Migrations apply automatically on app startup in Development mode
- Demo user auto-seeds on first run if `Seed:AutoRun` is true

## Testing Checklist Before PR

- [ ] `dotnet test backend` passes all tests
- [ ] `npm test -- --watch=false` passes all unit tests
- [ ] No console errors in browser DevTools (frontend running)
- [ ] Swagger API docs load at `http://localhost:5180/swagger`
- [ ] Database seeding works (delete DB and let it recreate)
- [ ] E2E tests pass: `npx playwright test` (requires frontend + backend running)
- [ ] Code formatted: `npx prettier --write frontend/src/` (backend auto-formatted by IDE)

## When to Ask User

Ask before: (1) Refactoring domain/app/infra layers, (2) Adding external APIs, (3) Modifying JWT/auth, (4) Major DB schema changes, (5) Formula/approval changes, (6) Frontend routing changes, (7) UI/UX changes, (8) Changing data source imports or sync patterns
