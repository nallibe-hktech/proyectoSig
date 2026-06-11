# Skill Registry — SIG-ES

**Generated**: 2026-06-08  
**Scope**: SIG-ES (sig-es) project  
**Persistence**: engram

## Project Standards (auto-resolved)

### Architecture & Design

**Rule**: Clean Architecture layers — strict dependency flow.

**Compact rule**:
```
Backend must follow Clean Architecture:
Domain (zero deps) → Application (interfaces) → Infrastructure (impl) → API (HTTP).
Controllers depend on services (DI). Services depend on Application interfaces.
Entity configurations in SIG.Infrastructure/Persistence/Configurations/.
```

**Applies to**: Backend C# changes, new services, new entities, new API endpoints.

---

### Testing & TDD

**Rule**: Strict TDD Mode enabled. All changes require tests before implementation.

**Compact rule**:
```
STRICT TDD MODE ACTIVE.
Backend: xUnit + NSubstitute. Test command: dotnet test backend
Frontend: Jasmine + Karma. Test command: npm test
E2E: Playwright. Test command: npm run e2e

Test structure: Unit → Integration → E2E (where applicable).
Do NOT implement without tests. Write test FIRST, then code.
```

**Applies to**: Backend C# implementation, frontend Angular/TypeScript, E2E user flows.

---

### Validation & FluentValidation

**Rule**: All request DTOs validated via FluentValidation before service layer.

**Compact rule**:
```
Backend: Every request DTO must have a FluentValidator in SIG.Application/Validators/.
ValidationFilter intercepts requests and returns 400 Bad Request for validation failures.
Do NOT add business logic validation in controllers — use validators.
```

**Applies to**: Backend API controllers, new request DTOs.

---

### Database & Entity Framework

**Rule**: Soft deletes, EF migrations, global query filters.

**Compact rule**:
```
Entities: Include IsDeleted + DeletedAt fields. EF global query filters auto-exclude deleted records.
Timestamps: CreatedAt / UpdatedAt set automatically (via interceptors or SaveChanges override).
Ownership: Use OwnerId for access control where needed.
Migrations: Create new migrations, never modify applied ones. Command: dotnet ef migrations add MigrationName
Apply: dotnet ef database update (applied at startup via AppDbContext.Database.Migrate())
```

**Applies to**: Backend database changes, new entities, new migrations.

---

### External Integrations

**Rule**: Integration pattern with staging tables.

**Compact rule**:
```
External data → staging_* tables → DataProcessorService → productive tables.
HTTP clients use named HttpClient pattern with retry policies.
API credentials in appsettings.json (dev) or user-secrets / environment vars (prod).
Integrated systems: Bizneo, SGPV, Celero, Intratime, PayHawk, TravelPerk, A3 Innuva.
```

**Applies to**: New external API integrations, sync services, staging table changes.

---

### Frontend Angular

**Rule**: Lazy-loaded feature modules, RxJS with proper cleanup.

**Compact rule**:
```
Feature modules lazy-loaded (dashboard, clients, projects, actions, concepts, periods, closures, approvals, audit, celero-visitas).
Services: Provide at root (providedIn: 'root') or in shared module.
RxJS: Use takeUntil(destroy$) in OnDestroy to prevent memory leaks.
Components: Presentational (dumb) + Container (smart) pattern where applicable.
HTTP: Interceptor injects JWT token. ApiService wraps all requests.
```

**Applies to**: Frontend components, services, feature modules, HTTP communication.

---

### Code Quality

**Rule**: Type safety, linting, formatting.

**Compact rule**:
```
Backend: TypeScript compiler (tsc --noEmit). TypeScript strict mode enabled.
Formatting: Prettier (npm run format or npx prettier --write .).
Linter: ESLint not enforced yet (can add). Built-in TypeScript checks via ng build.
Pre-commit: No console.log, no unhandled errors, all tests pass.
```

**Applies to**: Frontend TypeScript changes, build validation.

---

### Git Workflow

**Rule**: Conventional commits, no force-pushes to main, new commits not amendments.

**Compact rule**:
```
Conventional commits: feat:, fix:, docs:, refactor:, style:, test:, chore:
No Co-Authored-By attribution (only conventional commits).
Feature branches: feature/* or feat/*
Bugfix branches: fix/* or bugfix/*
Main branch: production-ready only. PR required.
Never amend published commits. Create new commits for fixes.
```

**Applies to**: All commits, branch management, PR reviews.

---

## Skill Triggers

No custom project-level skills detected.

For specialized tasks, use:
- **sdd-explore**: Investigate a feature idea before proposing
- **sdd-spec**: Write specifications with Given/When/Then scenarios
- **sdd-design**: Architecture decisions and approach
- **sdd-apply**: Implement tasks with strict TDD enforcement
- **sdd-verify**: Validate implementation against specs

---

## User Rules (from CLAUDE.md)

- Never add "Co-Authored-By" or AI attribution to commits
- Never build after changes (frontend: `npm start` only)
- When asking a question, STOP and wait for response
- Never agree with user claims without verification
- Always propose alternatives with tradeoffs when relevant
- Verify technical claims before stating them

---

## Index

| Document | Purpose |
|----------|---------|
| CLAUDE.md | Project overview, tech stack, dev commands, architecture patterns, testing |
| .atl/skill-registry.md | This file — consolidated standards and rules |

---

## Recovery

If SDD context is lost:
```
1. mem_search(query: "sdd-init/sig-es", project: "sig-es")
2. mem_get_observation(id)  → Full project context
3. mem_search(query: "sdd/sig-es/testing-capabilities")  → Testing setup
4. mem_search(query: "sdd/sig-es/conventions")  → Development conventions
```
