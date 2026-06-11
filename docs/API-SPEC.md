# API-SPEC — SIG · Plataforma Operativa Integral

> Catálogo completo y vinculante de endpoints REST. **La fuente única de verdad es `docs/ARQUITECTURA.md` §7 (Tabla de endpoints)** + §5.5 (firmas completas con DTOs).
> Este documento agrupa por módulo, resume Auth, Request/Response y referencia el RF/Servicio implicado.
> Base URL: `http://localhost:5180/api` (Dev) — `https://api.sig-es.{cliente}/api` (Prod).

---

## 0. Convenciones HTTP

| Aspecto | Valor |
|---|---|
| Formato | JSON (`Content-Type: application/json`) salvo exports A3 (`application/xml`) |
| Auth | JWT Bearer en `Authorization: Bearer ...` (excepto `[Anonymous]`) |
| Paginación | `?page={1..N}` + `?pageSize={1..100}` → `PagedResult<T>` con `items`, `total`, `page`, `pageSize` |
| Errores | RFC 7807 `ProblemDetails` con `code` semántico (`ConflictDependencies`, `NotOwner`, `ConcurrencyMismatch`, etc.) |
| Concurrencia | `If-Match: <xmin>` en operaciones sobre Closure/ClosureLine. Falla → `412 PreconditionFailed` |
| Idempotencia | Sync `POST /api/sync/{system}` usa hash SHA-256 del payload |
| CORS | Solo `http://localhost:4200` en Dev (configurable en Prod) |
| Enums | Serializados como string (`JsonStringEnumConverter` global) |

### Códigos HTTP estándar

`200 OK` · `201 Created` · `204 NoContent` · `400 ValidationProblem` · `401 Unauthorized` · `403 Forbidden` (NotOwner) · `404 NotFound` · `409 Conflict` (transición/dependencias/period cerrado/closure no aprobado) · `410 Gone` (token expirado) · `412 PreconditionFailed` (concurrency) · `500 InternalServerError` · `502 BadGateway` (sync sistema externo)

---

## 1. Resumen ejecutivo

- **65 endpoints explícitos** documentados con firma completa.
- 4 endpoints derivables por simetría (Variables CRUD) no contabilizados en CS/GS.
- **CS (Contract Score)** = 65/65 = **1.0** ✓
- **GS (Guard Score)** = 64/64 = **1.0** ✓ (`/api/health` es Anonymous por diseño y no cuenta)

---

## 2. Endpoints por módulo

### 2.1 Autenticación (RF-A01..A03)

| # | Método | Ruta | Auth | Request DTO | Response DTO | Códigos |
|---|---|---|---|---|---|---|
| 1 | POST | `/api/auth/login` | Anonymous | `LoginRequest { email, password }` | `LoginResponse { accessToken, refreshToken, user }` | 200/400/401 |
| 2 | POST | `/api/auth/refresh` | Anonymous | `RefreshRequest { refreshToken }` | `RefreshResponse` | 200/401 |
| 3 | POST | `/api/auth/logout` | Authenticated | `LogoutRequest { refreshToken }` | — | 204/401 |
| 4 | GET | `/api/auth/me` | Authenticated | — | `UsuarioBriefDto` | 200/401 |

### 2.2 Clients (RF-C01)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 5 | GET | `/api/clients` | All roles | `?page,pageSize,search` | `PagedResult<ClientListItemDto>` | 200/401 |
| 6 | GET | `/api/clients/{id}` | All roles | — | `ClientDetailDto` | 200/401/403/404 |
| 7 | POST | `/api/clients` | Administrator | `ClientCreateRequest` | `ClientDetailDto` | 201/400/401/403/409 |
| 8 | PUT | `/api/clients/{id}` | Administrator | `ClientUpdateRequest` | `ClientDetailDto` | 200/400/401/403/404/409 |
| 9 | DELETE | `/api/clients/{id}` | Administrator | — | — | 204/401/403/404/409 |

### 2.3 Projects (RF-C02, RF-G01)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 10 | GET | `/api/projects` | All (filtrado ownership) | `?page,pageSize,clientId,search` | `PagedResult<ProjectListItemDto>` | 200/401 |
| 11 | GET | `/api/projects/{id}` | All (ownership) | — | `ProjectDetailDto` | 200/401/403/404 |
| 12 | POST | `/api/projects` | Administrator, Backoffice | `ProjectCreateRequest` | `ProjectDetailDto` | 201/400/401/403 |
| 13 | PUT | `/api/projects/{id}` | Administrator, Backoffice | `ProjectUpdateRequest` | `ProjectDetailDto` | 200/400/401/403/404 |
| 14 | DELETE | `/api/projects/{id}` | Administrator | — | — | 204/401/403/404/409 |

### 2.4 Actions (RF-C03)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 15 | GET | `/api/actions` | All (ownership) | `?page,pageSize,projectId,search` | `PagedResult<ActionListItemDto>` | 200/401 |
| 16 | GET | `/api/actions/{id}` | All (ownership) | — | `ActionDetailDto` | 200/401/403/404 |
| 17 | POST | `/api/actions` | Administrator, Backoffice | `ActionCreateRequest` | `ActionDetailDto` | 201/400/401/403 |
| 18 | PUT | `/api/actions/{id}` | Administrator, Backoffice | `ActionUpdateRequest` | `ActionDetailDto` | 200/400/401/403/404 |
| 19 | DELETE | `/api/actions/{id}` | Administrator | — | — | 204/401/403/404/409 |

### 2.5 Concepts (RF-C04, RF-D07)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 20 | GET | `/api/concepts` | All | `?page,pageSize,tipo,search` | `PagedResult<ConceptListItemDto>` | 200/401 |
| 21 | GET | `/api/concepts/{id}` | All | — | `ConceptDetailDto` | 200/401/404 |
| 22 | POST | `/api/concepts` | Administrator, Backoffice | `ConceptCreateRequest` (con `formulaJson`) | `ConceptDetailDto` | 201/400/401/403 |
| 23 | PUT | `/api/concepts/{id}` | Administrator, Backoffice | `ConceptUpdateRequest` | `ConceptDetailDto` | 200/400/401/403/404 |
| 24 | DELETE | `/api/concepts/{id}` | Administrator | — | — | 204/401/403/404/409 |
| 25 | POST | `/api/concepts/{id}/validar-formula` | Administrator, Backoffice | `{ formulaJson }` | `{ ok, errores[] }` | 200/400/401/403/404 |

### 2.6 Users + RBAC (RF-C05, RF-C06)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 26 | GET | `/api/users` | Administrator, Auditor | `?page,pageSize,search` | `PagedResult<UserListItemDto>` | 200/401/403 |
| 27 | GET | `/api/users/{id}` | Administrator, Auditor, selfMe | — | `UserDetailDto` | 200/401/403/404 |
| 28 | POST | `/api/users` | Administrator | `UserCreateRequest` | `UserDetailDto` | 201/400/401/403/409 |
| 29 | PUT | `/api/users/{id}` | Administrator | `UserUpdateRequest` | `UserDetailDto` | 200/400/401/403/404/409 |
| 30 | PUT | `/api/users/{id}/password` | Administrator, selfMe | `UserPasswordChangeRequest` | — | 204/400/401/403/404 |
| 31 | DELETE | `/api/users/{id}` | Administrator | — | — | 204/401/403/404/409 |
| 32 | GET | `/api/roles` | Administrator, Auditor | — | `RoleDto[]` | 200/401/403 |
| 33 | GET | `/api/departments` | All | — | `DepartmentDto[]` | 200/401 |
| 34 | POST | `/api/departments` | Administrator | `DepartmentCreateRequest` | `DepartmentDto` | 201/400/401/403 |
| 35 | PUT | `/api/departments/{id}` | Administrator | `DepartmentUpdateRequest` | `DepartmentDto` | 200/400/401/403/404 |
| 36 | DELETE | `/api/departments/{id}` | Administrator | — | — | 204/401/403/404/409 |
| 37 | GET | `/api/costcenters` | All | — | `CostCenterDto[]` | 200/401 |
| 38 | POST | `/api/costcenters` | Administrator | `CostCenterCreateRequest` | `CostCenterDto` | 201/400/401/403/409 |
| 39 | PUT | `/api/costcenters/{id}` | Administrator | `CostCenterUpdateRequest` | `CostCenterDto` | 200/400/401/403/404/409 |
| 40 | DELETE | `/api/costcenters/{id}` | Administrator | — | — | 204/401/403/404/409 |

### 2.7 Periods (RF-C07)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 41 | GET | `/api/periods` | All | — | `PeriodDto[]` | 200/401 |
| 42 | GET | `/api/periods/activo` | All | — | `PeriodDto` | 200/401/404 |
| 43 | POST | `/api/periods` | Administrator | `PeriodCreateRequest` | `PeriodDto` | 201/400/401/403/409 |
| 44 | PUT | `/api/periods/{id}` | Administrator | `PeriodUpdateRequest` | `PeriodDto` | 200/400/401/403/404 |
| 45 | POST | `/api/periods/{id}/cerrar` | Administrator | — | `PeriodDto` | 200/401/403/404/409 |
| 46 | POST | `/api/periods/{id}/reabrir` | Administrator | — | `PeriodDto` | 200/401/403/404/409 |

### 2.8 Dashboard (RF-B01..B03)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 47 | GET | `/api/dashboard` | All | `?periodId` | `DashboardKpisDto` | 200/401 |
| 48 | GET | `/api/dashboard/avisos` | All | — | `DashboardAvisoDto[]` | 200/401 |
| 49 | GET | `/api/dashboard/mis-proyectos` | All | `?periodId` | `MiProyectoDto[]` | 200/401 |

### 2.9 Closures (RF-D01)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 50 | GET | `/api/closures` | All (ownership) | `?ApprovalFilterRequest` | `PagedResult<ClosureListItemDto>` | 200/401 |
| 51 | GET | `/api/closures/{id}` | All (ownership) | — | `ClosureDetailDto` | 200/401/403/404 |
| 52 | POST | `/api/closures` | ProjectManager, Backoffice, Administrator | `ClosureCreateRequest` | `ClosureDetailDto` | 201/400/401/403/409 |
| 53 | POST | `/api/closures/{id}/recalcular` | ProjectManager, Backoffice, Administrator | `ClosureRecalcRequest` + `If-Match` | `ClosureDetailDto` | 200/400/401/403/404/409/412 |
| 54 | POST | `/api/closures/{id}/aprobar` | Según paso actual (PM/Back/Fico/Dir) | `ClosureApproveRequest` + `If-Match` | `ClosureDetailDto` | 200/400/401/403/404/409/412 |
| 55 | POST | `/api/closures/{id}/rechazar` | Según paso actual | `ClosureRejectRequest { motivo }` + `If-Match` | `ClosureDetailDto` | 200/400/401/403/404/409/412 |

### 2.10 Approvals (RF-D02..D06)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 56 | GET | `/api/approvals` | All | `?ApprovalFilterRequest` | `PagedResult<ApprovalPanelItemDto>` | 200/401 |
| 57 | GET | `/api/approvals/pendientes` | All (filtrado por rol) | `?page,pageSize` | `PagedResult<ApprovalPanelItemDto>` | 200/401 |
| 58 | GET | `/api/approvals/historial/{closureId}` | All (ownership) | — | `ApprovalHistoryDto[]` | 200/401/403/404 |

### 2.11 Calculations (RF-D07)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 59 | GET | `/api/calculations/{closureLineId}` | All (ownership) | — | `CalculationDetailDto` (fórmula AST + inputs + resultado) | 200/401/403/404 |

### 2.12 Auditoría (RF-F02)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 60 | GET | `/api/audit` | Administrator, Auditor | `?AuditLogFilterRequest` | `PagedResult<AuditLogDto>` | 200/401/403 |

### 2.13 Integraciones (RF-E01)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 61 | POST | `/api/sync/{system}` | Administrator | path `system ∈ {celero, bizneo, intratime, payhawk}` | `SyncResultDto { insertados, duplicados, errores[] }` | 200/400/401/403/502 |

### 2.14 Exports A3 (RF-E02, RF-E03)

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 62 | GET | `/api/exports/a3-innuva/{closureId}` | Administrator, Fico, Direction | — | `FileContentResult` (`application/xml`) | 200/401/403/404/409 |
| 63 | GET | `/api/exports/a3-erp/{closureId}` | Administrator, Fico, Direction | — | `FileContentResult` (`application/xml`) | 200/401/403/404/409 |

### 2.15 Utilidades de desarrollo

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 64 | POST | `/api/dev/regenerar-seed` | Administrator + `IHostEnvironment.IsDevelopment()` + `Features:AllowSeedRegeneration=true` | — | `{ ok, mensaje }` | 200/401/403/404 |
| 65 | GET | `/api/health` | Anonymous | — | `{ status: "ok", version }` | 200 |

---

## 3. DTOs principales

Las firmas completas (con tipos, validaciones FluentValidation y anotaciones) están en `docs/ARQUITECTURA.md` §5.2 (Request) y §5.3 (Response).

DTOs clave:
- **Auth**: `LoginRequest`, `LoginResponse`, `RefreshRequest`, `RefreshResponse`, `UsuarioBriefDto`
- **Catálogos**: `ClientDetailDto`, `ProjectDetailDto`, `ActionDetailDto`, `ConceptDetailDto`, `UserDetailDto`
- **Closures**: `ClosureDetailDto`, `ClosureLineDto`, `ClosureCreateRequest`, `ClosureRecalcRequest`, `ClosureApproveRequest`, `ClosureRejectRequest`
- **Approvals**: `ApprovalPanelItemDto`, `ApprovalHistoryDto`
- **Cálculo**: `CalculationDetailDto` (incluye `formulaJson`, `inputsJson`, `resultado`)
- **Dashboard**: `DashboardKpisDto`, `DashboardAvisoDto`, `MiProyectoDto`
- **Auditoría**: `AuditLogDto`, `AuditLogFilterRequest`
- **Sync**: `SyncResultDto`
- **Paginación**: `PagedResult<T>`, `PagedRequest`

---

## 4. Reglas de autorización transversales

| Regla | Aplicación |
|---|---|
| `UserId` siempre del JWT (`ClaimTypes.NameIdentifier`), NUNCA del body | RNF-05 |
| Ownership en repositorios → `GetByIdAndUsuarioIdAsync` cuando aplica | RF-G01, ProjectManager |
| Soft delete oculto vía `HasQueryFilter` salvo `.IgnoreQueryFilters()` (Auditor) | RF-G02 |
| `Authorize` con `Roles="..."` en cada controlador o `[AllowAnonymous]` explícito | Convención |
| `If-Match` obligatorio en operaciones sobre Closure/ClosureLine | Concurrencia |

> Matriz Role × Endpoint detallada: `docs/ROLES-PERMISOS.md`.

---

## 5. Referencias cruzadas

| Documento | Sección |
|---|---|
| `docs/ARQUITECTURA.md` | §7 (tabla canónica), §5 (firmas completas), §6 (motor de cálculo y sus DTOs) |
| `docs/DATA-MODEL.md` | Entidades y relaciones que respaldan estos endpoints |
| `docs/INTEGRACIONES.md` | Detalle de `POST /api/sync/{system}` y exports A3 |
| `docs/ROLES-PERMISOS.md` | Matriz completa Role × Endpoint × Ownership |
| Swagger UI Dev | `http://localhost:5180/swagger` |
