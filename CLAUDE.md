# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SIG-ES** ("Sistema Integral de Gestión - Empresa Específica") is a multi-system integration platform for financial closures and business process management. It connects multiple external APIs (Bizneo, SGPV, Celero, Intratime, PayHawk, TravelPerk, A3 Innuva) with a custom calculation engine and approval workflow.

**Status**: Active development. Uses .NET 10 + Angular 21. Most integrations complete. **Recently added**: Galán (warehouse/logistics) and Mediapost (distribution/delivery) modules with full CRUD, dashboards, and UI.

## Tech Stack

### Backend
- **Framework**: ASP.NET Core 10 (C#)
- **Database**: PostgreSQL 16 (local dev on port 5433)
- **ORM**: Entity Framework Core 9
- **Architecture**: Clean Architecture (Domain/Application/Infrastructure/API layers)
- **Authentication**: JWT Bearer tokens (user-defined claims mapping)
- **Validation**: FluentValidation
- **API Documentation**: Swagger/OpenAPI

### Frontend
- **Framework**: Angular 21
- **UI Library**: Angular Material 21
- **HTTP**: RxJS 7.8
- **Charts**: Chart.js 4.5
- **Testing**: Jasmine + Karma, Playwright E2E
- **Code Style**: Prettier

### Infrastructure
- **Containerization**: Docker Compose (PostgreSQL 16)
- **Port Assignments**:
  - Backend API: 5180
  - Frontend dev server: 4200
  - PostgreSQL: 5433
- **CORS**: Configured for localhost:4200

## Galán & Mediapost Integration (NEW — June 2026)

### Overview
Two new operational modules for warehouse/logistics (Galán) and distribution/delivery (Mediapost) management.

### Backend Endpoints

**Galán** (`/api/galan`):
- `GET /entradas?page=1&pageSize=50&search=...` — Warehouse entries (paginated, searchable)
- `GET /salidas?page=1&pageSize=50&search=...` — Warehouse exits/dispatches (paginated, searchable)
- `GET /stock` — Full inventory snapshot
- `GET /entradas/{id}`, `GET /salidas/{id}`, `GET /stock/{id}` — Detail endpoints
- `GET /dashboard?desde=YYYY-MM-DD&hasta=YYYY-MM-DD` — KPI dashboard (stock value, logistics cost, low-stock alerts)

**Mediapost** (`/api/mediapost`):
- `GET /pedidos?page=1&pageSize=50&search=...&estado=...` — Orders (paginated, filtrable by estado)
- `GET /recepciones?page=1&pageSize=50&search=...` — Receptions (paginated, searchable)
- `GET /pedidos/{id}`, `GET /recepciones/{id}` — Detail endpoints
- `GET /dashboard?desde=YYYY-MM-DD&hasta=YYYY-MM-DD` — KPI dashboard (delivery rate, damage tracking, pending orders)

### Implementation Files

**Backend**:
- `SIG.Infrastructure/Services/GalanService.cs` — Data access layer (EF Core queries, pagination, search)
- `SIG.Infrastructure/Services/MediapostService.cs` — Data access layer (EF Core queries, pagination, search)
- `SIG.API/Controllers/GalanController.cs` — HTTP endpoints
- `SIG.API/Controllers/MediapostController.cs` — HTTP endpoints
- `SIG.Infrastructure/DependencyInjection.cs` — Service registration

**Frontend**:
- `src/app/features/galan/components/galan-dashboard.component.ts` — Dashboard with KPI cards + 4 tabs
- `src/app/features/galan/components/galan-entradas-list.component.ts` — Entradas list (search + pagination)
- `src/app/features/galan/components/galan-salidas-list.component.ts` — Salidas list (search + pagination)
- `src/app/features/galan/components/galan-stock-list.component.ts` — Stock list (full, no pagination)
- `src/app/features/galan/services/galan.service.ts` — HTTP client
- `src/app/features/mediapost/components/mediapost-dashboard.component.ts` — Dashboard with 8 KPI cards + 3 tabs
- `src/app/features/mediapost/components/mediapost-pedidos-list.component.ts` — Pedidos list (search + estado filter + pagination)
- `src/app/features/mediapost/components/mediapost-recepciones-list.component.ts` — Recepciones list (search + pagination)
- `src/app/features/mediapost/services/mediapost.service.ts` — HTTP client

### Routes
```
/galan           → Galán dashboard + warehouse management
/mediapost       → Mediapost dashboard + delivery tracking
```

### Key Features
- **Search**: Full-text with debounce(300ms) on all lists
- **Pagination**: Material paginator with 10/25/50 options
- **Date Filtering**: Dashboard KPIs by date range (desde/hasta)
- **Status Tracking**: Color-coded delivery/movement status
- **Alerts**: Low stock warnings (< 5 units), damage tracking in recepciones
- **All Components**: Standalone (modern Angular pattern)

### Architecture Notes
- Service implementations in **Infrastructure** layer (depend on EF Core + AppDbContext)
- Service interfaces in **Application** layer (abstraction boundary)
- Controllers in **API** layer (HTTP concerns only)
- Pagination uses existing `PagedResult<T>` pattern from `SIG.Application/Common/PagedResult.cs`
- Dashboard KPI calculations done in-memory after querying staging tables (not SQL aggregates)

### Next Steps
- **SharePoint Integration** (Step 5): Replace local CSV/Excel file paths with SharePoint SDK for real data sync
- Chart visualizations (currently removed to focus on CRUD + KPIs; can be re-added with ng2-charts if needed)

## Code Style & Formatting

### Frontend (Angular)
- **Formatter**: Prettier (configured in `frontend/.prettierrc`)
  - Single quotes: `true`
  - Print width: `100` characters
  - HTML parser: Angular (`*.html` files)
- **Component generation**: By default skips tests (configure in `angular.json`: `schematics.@schematics/angular:component.skipTests: true`)
- **Styles**: SCSS (configured in `angular.json`). Generate components with: `ng generate component name --skip-tests`

### Backend (.NET)
- Follow standard C# conventions (PascalCase for public members, camelCase for locals)
- EditorConfig enforced across solution (check `.editorconfig` if present)
- Format on save is recommended (Visual Studio default)

## Development Commands

### Backend (.NET)

**Solution file**: `backend/SIG.slnx` (5 projects: API, Application, Domain, Infrastructure, Tests)

```bash
# Restore dependencies
dotnet restore backend

# Build
dotnet build backend

# Build with warnings-as-errors (for CI/pre-commit)
dotnet build backend -warnaserror

# Run API server (listens on port 5180)
cd backend/SIG.API && dotnet run

# Run all tests
dotnet test backend

# Run specific test class
dotnet test backend/SIG.Tests --filter "FullyQualifiedName~SIG.Tests.Unit.Services.AuthServiceTests"

# Run specific test method
dotnet test backend/SIG.Tests --filter "Name=TestMethodName"

# Run tests with coverage
dotnet test backend --logger "trx;LogFileName=results.trx" /p:CollectCoverage=true

# Run integration tests only
dotnet test backend/SIG.Tests --filter "Category=Integration"

# Entity Framework: apply pending migrations
cd backend/SIG.API && dotnet ef database update

# Entity Framework: create new migration
cd backend/SIG.API && dotnet ef migrations add MigrationName --project ../SIG.Infrastructure

# List pending migrations
cd backend/SIG.API && dotnet ef migrations list
```

### Frontend (Angular)

```bash
cd frontend

# Install dependencies
npm install

# Development server (http://localhost:4200)
npm start

# Build for production
npm run build

# Watch mode during development (triggers rebuild on file changes)
npm run watch
```

**Unit Tests** (Karma + Jasmine):
```bash
cd frontend

# Run tests once
npm test

# Run tests in watch mode
npm test -- --watch

# Run tests with coverage
npm test -- --code-coverage

# Run specific test file
npm test -- --browsers=Chrome --include="**/auth.service.spec.ts"
```

**E2E Tests** (Playwright):
```bash
# Prerequisites: backend running (port 5180), frontend dev server running (port 4200)
cd frontend

# Install Playwright browsers (first time only)
npx playwright install chromium

# Run all E2E tests
npx playwright test

# Run tests in headed mode (see browser)
npx playwright test --headed

# Run specific test file
npx playwright test e2e/auth.spec.ts

# Debug mode (interactive)
npx playwright test --debug
```

### Database

```bash
# Start PostgreSQL container (SIG-ES database)
docker-compose up -d

# Stop PostgreSQL
docker-compose down

# View logs
docker-compose logs -f postgres

# Connect directly (from host)
psql -h localhost -p 5433 -U postgres -d sig_plataforma_dev
# Password: SigEs@2026
```

## Project Structure

### Backend

```
backend/
├── SIG.API/                    # ASP.NET Core 10 API server
│   ├── Controllers/            # API endpoints (Auth, Clients, Projects, etc.)
│   ├── Filters/                # ValidationFilter (request/response interceptors)
│   ├── Middleware/             # ExceptionHandling, CORS, JWT
│   ├── Program.cs              # Service registration, middleware setup
│   └── appsettings.json        # Configuration (JWT keys, DB connection)
│
├── SIG.Application/            # Application services & DTOs
│   ├── Interfaces/             # Service interfaces (DI contracts)
│   ├── Services/               # Business logic (not domain-specific)
│   ├── DTOs/                   # Request/Response models
│   └── Validators/             # FluentValidation rules
│
├── SIG.Domain/                 # Domain models (entities, value objects)
│   ├── Entities/               # Core entities (User, Client, Project, Concept, etc.)
│   ├── ValueObjects/           # Immutable value types
│   └── Enums/                  # Domain enums (ApprovalStatus, ActionType, etc.)
│
├── SIG.Infrastructure/         # Data access, external APIs, configs
│   ├── Persistence/            # AppDbContext, migrations
│   ├── Migrations/             # EF Core migration history
│   ├── Services/               # Integration clients (CeleroPostgresClient, BizAC3API, etc.)
│   ├── Repositories/           # Repository pattern (if used)
│   └── ServiceCollectionExtensions.cs  # DI registration
│
├── SIG.Tests/                  # xUnit + Moq tests
│   ├── Unit/                   # Service logic tests
│   ├── Integration/            # Database + API tests (uses real DbContext)
│   └── Fixtures/               # Shared test data
│
└── SIG.slnx                    # Solution file
```

### Frontend

```
frontend/
├── src/
│   ├── app/
│   │   ├── auth/               # Login feature
│   │   ├── core/               # Singleton services
│   │   │   ├── api/            # HTTP service for backend communication
│   │   │   └── auth/           # JWT token storage/retrieval
│   │   ├── features/           # Feature modules (lazy-loaded)
│   │   │   ├── dashboard/
│   │   │   ├── clients/
│   │   │   ├── projects/
│   │   │   ├── actions/
│   │   │   ├── concepts/
│   │   │   ├── periods/
│   │   │   ├── closures/
│   │   │   ├── approvals/
│   │   │   ├── audit/
│   │   │   └── celero-visitas/
│   │   └── shared/             # Shared components, pipes, directives
│   ├── styles/                 # Global CSS/SCSS
│   └── main.ts                 # Bootstrap Angular app
│
├── public/                     # Static assets
├── angular.json                # Angular CLI configuration
├── tsconfig.json               # TypeScript configuration
├── playwright.config.ts        # E2E test configuration
└── package.json                # Dependencies
```

## Architecture Patterns

### Clean Architecture Layers

The backend follows **Clean Architecture** with strict dependency flow (outer → inner only):

1. **Domain Layer** (SIG.Domain): Pure C#, zero dependencies. Entity definitions, domain logic.
2. **Application Layer** (SIG.Application): Service interfaces, DTOs, validation, business orchestration.
3. **Infrastructure Layer** (SIG.Infrastructure): Database, external APIs, concrete service implementations.
4. **API Layer** (SIG.API): Controllers, middleware, HTTP concerns.

**Key rule**: Controllers depend on services, services depend on interfaces defined in Application, implementations live in Infrastructure.

### Database Design

- **Soft-delete**: Global query filters exclude deleted records; `IsDeleted` + `DeletedAt` timestamps on all entities.
- **Ownership**: Some entities tied to specific users or organizations via `OwnerId`.
- **Audit trail**: Immutable `AuditLog` table captures all significant changes (user, timestamp, entity, action).
- **Timestamps**: `CreatedAt` / `UpdatedAt` set automatically via EF interceptors or DbContext.SaveChanges().
- **Migrations**: Numbered sequentially; applied at startup via `dotnet ef database update`.

### External Integrations

The system connects to 7 external APIs via HTTP clients:

| System | Type | Purpose | Status |
|--------|------|---------|--------|
| **Bizneo** | HTTP (OAuth2) | HR/HCM - employees, hours | Integrated (needs creds) |
| **SGPV** | HTTP (Basic auth) | Products catalog | Integrated (needs creds) |
| **Celero** | PostgreSQL direct | Sales/clients data | Integrated (local) |
| **Intratime** | HTTP | Time tracking (fichajes) | Integrated (needs creds) |
| **PayHawk** | HTTP | Expense management | Integrated (needs creds) |
| **TravelPerk** | HTTP | Travel bookings | Integrated (needs creds) |
| **A3 Innuva** | HTTP | ERP data | Integrated (needs creds) |

**Integration pattern**: Each external system has a staging table (`staging_*`) where sync results land. A `DataProcessorService` then migrates staging records to productive tables.

### Key Services

**Backend core services** (SIG.Application + SIG.Infrastructure):

- `AuthService` — JWT generation, password hashing (BCrypt)
- `ClientService` — CRUD + Celero sync
- `ProjectService` — CRUD, user assignment
- `ConceptService` — Concept management + formula evaluation
- `PeriodService` — Period CRUD + recalculation
- `ClosureService` — Integral closures with multi-line support
- `ApprovalService` — Multi-role approval flow (Gestor → FICO → Director)
- `DataProcessorService` — Sync staging tables to productive data
- `CalculationEngine` — Complex formula evaluation (MathNet.Numerics)
- `ExcelExporter` — Multi-format export (ClosedXML)
- `SyncService` — External API clients (Bizneo, SGPV, Celero, etc.)

**Frontend core services** (in `src/app/core/`):

- `ApiService` — HTTP wrapper, JWT token injection, error handling
- `AuthService` — Token storage, login/logout
- `AppState` — Optional state management (if using NGRX or custom)

## Database Connection & Initialization

**Local development** (Docker Compose):
```
Host: localhost
Port: 5433
Database: sig_plataforma_dev
Username: postgres
Password: SigEs@2026
```

**EF Core**: Connection string in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5433;Database=sig_plataforma_dev;Username=postgres;Password=SigEs@2026;"
}
```

**Database Setup**:
1. Start PostgreSQL: `docker-compose up -d`
2. Apply migrations: `cd backend/SIG.API && dotnet ef database update`
3. Seeding: In **Development** environment, the API auto-seeds demo data on startup (configured in `appsettings.Development.json` with `Seed.AutoRun: true`). Demo user password is `Demo#2026!`.

**Schema**: 33+ tables including entities, staging tables, audit logs. Migrations are applied at startup; no manual migration scripts needed for local development.

## Testing Strategy

### Backend (xUnit, Moq, InMemoryDatabase)

- **Location**: `backend/SIG.Tests/` (Unit and Integration subdirectories)
- **Unit tests**: Service logic with mocked dependencies (no database)
- **Integration tests**: Against real PostgreSQL (uses test database via `appsettings.Testing.json`)
- **Fixtures**: Shared test data in `Fixtures/` directory
- **Test runner**: xUnit (via `dotnet test`)

### Frontend (Karma + Jasmine for units, Playwright for E2E)

- **Unit tests**: Located as `*.spec.ts` files adjacent to source
  - Test runner: Karma (headless Chrome by default)
  - Framework: Jasmine
  - Coverage: Use `npm test -- --code-coverage` 
  - Components skip test generation by default (edit `angular.json` to change)

- **E2E tests**: Located in `frontend/e2e/` directory
  - Test runner: Playwright (Chrome, Firefox, Safari configured)
  - Prerequisites: Both backend (5180) and frontend dev server (4200) must be running
  - Timeout: 30 seconds per test, 1 automatic retry on failure
  
**Note**: The Frontend README mentions Vitest, but the project uses Karma + Jasmine. Ignore Vitest references in generated documentation.

## Configuration & Environment Files

### Backend (appsettings.*.json)

Located in `backend/SIG.API/`:
- `appsettings.json` — Base configuration (committed)
- `appsettings.Development.json` — Local dev overrides (includes demo credentials, auto-seeding)
- `appsettings.Testing.json` — Test database connection
- `appsettings.E2E.json` — E2E test configuration

**Key settings**:
- `ConnectionStrings:DefaultConnection` — PostgreSQL connection
- `JwtSettings:SigningKey` — JWT secret (must be >32 chars)
- `Integrations` — External API credentials (Bizneo, SGPV, PayHawk, etc.)
- `Seed:AutoRun` — Enable auto-seeding on startup (true in Development)
- `Features:AllowSeedRegeneration` — Allow re-running seeds via API

For local development, appsettings.Development.json is applied automatically when `ASPNETCORE_ENVIRONMENT=Development`. In production, use environment variables.

### Frontend (environment.ts, environment.prod.ts)

Located in `frontend/src/environments/`:
- `environment.ts` — Development configuration
- `environment.prod.ts` — Production configuration

**Key settings**:
- `apiUrl` — Backend API base URL (e.g., `http://localhost:5180` for dev)
- Any feature flags or configuration needed by services

**Note**: Environment configurations are swapped during build based on the target configuration (see `angular.json` `fileReplacements`).

## Important Patterns & Constraints

1. **JWT claims**: Uses custom claims mapping (`JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()`). Refer to `Program.cs` before modifying auth.

2. **Validation**: All request DTOs validated via FluentValidation. Validation errors caught by `ValidationFilter` and returned as structured responses.

3. **Soft deletes**: Queries automatically exclude deleted records via EF global query filters. Override with `.IgnoreQueryFilters()` when needed.

4. **CORS**: Only `http://localhost:4200` allowed by default. Update in `Program.cs` if adding new client origins.

5. **Database migrations**: Always create new migrations; never modify applied migrations. Unapplied migrations can be deleted, but applied ones must use `Remove-Migration` (if not pushed) or new migrations.

6. **External APIs**: Use named HttpClient pattern with retry policies. See `SIG.Infrastructure/Services/` for examples (CeleroPostgresClient, BizAC3API, etc.).

7. **Entity Framework**: Use `ef database update` for dev, `ef migrations script` for deployment scripts. Entity configurations in `SIG.Infrastructure/Persistence/Configurations/`.

## Common Development Tasks

### Adding a new API endpoint

1. Create entity in `SIG.Domain/Entities/`
2. Define service interface in `SIG.Application/Interfaces/`
3. Implement service in `SIG.Infrastructure/Services/`
4. Create request/response DTOs in `SIG.Application/DTOs/`
5. Add validation in `SIG.Application/Validators/`
6. Create controller action in `SIG.API/Controllers/`
7. Create migration for new table: `dotnet ef migrations add AddNewEntity`
8. Test via Swagger or frontend

### Adding external API integration

1. Create HTTP client interface in `SIG.Infrastructure/Services/`
2. Implement with real API calls (use `HttpClient` from DI)
3. Create staging table migration
4. Wire into `SyncService` or create new sync controller
5. Add credentials to `appsettings.json` (or `user-secrets` for dev)
6. Test sync with real API data

### Debugging frontend

- Use `ng serve` for hot reload
- Open browser DevTools (F12) for console logs
- Check Network tab to inspect API calls and response payloads
- Use Angular DevTools extension for component tree inspection

## Development Workflows & Debugging

### Full Local Stack Startup

```bash
# Terminal 1: Start PostgreSQL
docker-compose up -d

# Terminal 2: Start backend API (auto-applies migrations, auto-seeds on first run)
cd backend/SIG.API
ASPNETCORE_ENVIRONMENT=Development dotnet run  # Listens on http://localhost:5180

# Terminal 3: Start frontend dev server (with hot reload)
cd frontend
npm start  # Listens on http://localhost:4200

# Terminal 4 (optional): Run tests
cd frontend && npm test
# OR
cd backend && dotnet test
```

### Backend Debugging

- **Swagger UI**: http://localhost:5180/swagger (test endpoints directly)
- **EF Core logging**: Check `appsettings.Development.json` for `Logging:LogLevel` (set Microsoft.EntityFrameworkCore.Database.Command to Information to see SQL)
- **Exception details**: Enabled in Development by default; turned off in production
- **Database state**: Use `psql` or pgAdmin to inspect tables directly (password: `SigEs@2026`)

### Frontend Debugging

- **Browser DevTools**: Press F12 (Console, Network, Sources tabs useful)
- **Angular DevTools extension**: Inspect component tree, change properties at runtime
- **Network tab**: See API requests/responses, check JWT tokens in headers
- **Hot reload**: Changes to `.ts` and `.html` files auto-reload during `npm start`
- **Environment check**: Verify `environment.ts` `apiUrl` points to running backend

### Running Tests During Development

```bash
# Backend: Run tests in watch mode (re-runs on code changes)
cd backend && dotnet watch test

# Frontend: Run unit tests in watch mode
cd frontend && npm test -- --watch

# Frontend: Debug single test in browser
cd frontend && npm test -- --browsers=Chrome --watch
```

### Stopping & Cleaning Up

```bash
# Stop backend (Ctrl+C in terminal)
# Stop frontend (Ctrl+C in terminal)

# Stop PostgreSQL container
docker-compose down

# (Optional) Reset database completely
docker-compose down -v    # -v removes volumes
docker-compose up -d      # Re-create empty database
dotnet ef database update # Re-apply migrations
```

## When to Ask the User

Before making significant changes, ask if:

1. **Architecture**: Refactoring across Domain/Application/Infrastructure layers (affects DI and contracts)
2. **Integrations**: Adding new external APIs (requires understanding auth patterns, staging tables, sync flow)
3. **Authentication**: Modifying JWT claims, token refresh logic, or role-based authorization
4. **Database**: Schema changes beyond simple column additions (affects migrations and may break integration tests)
5. **Calculation/Approval**: Changes to formula evaluation or the multi-role approval workflow
6. **Frontend routing or state**: Major refactoring of feature modules or service dependencies
7. **UI/UX**: Significant layout or interaction pattern changes (test in browser before reporting done)

If unsure about scope, ask rather than assuming.
