# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SIG-ES** ("Sistema Integral de Gestión - Empresa Específica") is a multi-system integration platform for financial closures and business process management. It connects multiple external APIs (Bizneo, SGPV, Celero, Intratime, PayHawk, TravelPerk, A3 Innuva) with a custom calculation engine and approval workflow.

**Status**: Active development (4-5 week project, currently week 2). Backend 85% complete, frontend 75% complete. Uses .NET 10 + Angular 21.

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

# Run specific test file
dotnet test backend/SIG.Tests --filter "TestClassName=ClassName"

# Run all tests with coverage
dotnet test backend --logger "trx;LogFileName=results.trx" /p:CollectCoverage=true

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

# Run unit tests (Jasmine + Karma)
npm test

# Run E2E tests (Playwright)
npm run e2e  # or: npx playwright test

# Watch mode during development
npm run watch
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

## Database Connection

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

**Schema**: 33+ tables including entities, staging tables, audit logs.

## Testing

### Backend (xUnit, Moq, InMemoryDatabase)

- **Unit tests**: Service logic with mocked dependencies
- **Integration tests**: Against InMemoryDatabase or real PostgreSQL
- Run: `dotnet test backend`
- Coverage: Use `/p:CollectCoverage=true` flag

### Frontend (Jasmine, Karma, Playwright)

- **Unit tests**: Components, services, pipes
- **Karma (UI)**: `npm test` (watches for changes)
- **Playwright (E2E)**: `npm run e2e` (full user flows in real browser)

## Configuration & Secrets

### Backend (appsettings.json)
- `ConnectionStrings:DefaultConnection` — PostgreSQL connection
- `JwtSettings:SigningKey` — JWT secret (must be >32 chars)
- `JwtSettings:Issuer` — Token issuer claim
- `JwtSettings:Audience` — Token audience claim
- External API credentials (Bizneo, SGPV, etc.) — stored in `user-secrets` for dev, environment variables in production

### Frontend (environment.ts, environment.prod.ts)
- `apiUrl` — Backend API base URL (e.g., `http://localhost:5180`)

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

## When to Ask the User

- If a refactoring affects architectural layers (e.g., moving logic from Domain to Application)
- If adding new external integrations (requires understanding of API auth + staging schema)
- If modifying authentication/JWT claim handling
- If making database schema changes beyond simple column additions
- If changing the approval workflow or calculation logic
