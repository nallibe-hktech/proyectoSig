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

- Catálogo de endpoints actualizado tras el refactor **Project→Service** (módulos `Projects` y `Actions` eliminados, unificados en `Services` con subrutas `tarifas` y `presupuestos`) y la incorporación de **closure alerts**.
- Numeración: los IDs de fila se mantienen estables salvo en bloques refactorizados, donde se usan sufijos (`19a..19e`, `55a..55c`) para no renumerar el resto del documento.
- Reglas transversales (ownership, `If-Match`, enums como string) sin cambios.

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

### 2.3 Services (RF-C02, RF-C03, RF-G01)

> Unifica los antiguos módulos Projects y Actions, ambos eliminados. La entidad operativa única es **Service**. Subrutas anidadas para Tarifas y Presupuestos del servicio.

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 10 | GET | `/api/services` | All (filtrado ownership) | `?page,pageSize,clientId,search` | `PagedResult<ServiceListItemDto>` | 200/401 |
| 11 | GET | `/api/services/{id}` | All (ownership) | — | `ServiceDetailDto` | 200/401/403/404 |
| 12 | POST | `/api/services` | Administrator, Backoffice | `ServiceCreateRequest` | `ServiceDetailDto` | 201/400/401/403 |
| 13 | PUT | `/api/services/{id}` | Administrator, Backoffice | `ServiceUpdateRequest` | `ServiceDetailDto` | 200/400/401/403/404 |
| 14 | DELETE | `/api/services/{id}` | Administrator | — | — | 204/401/403/404/409 |

#### 2.3.1 Tarifas del servicio (`TarifasController`)

> Ruta anidada `/api/services/{serviceId}/tarifas`. GET es Authenticated; mutaciones requieren Administrator o Backoffice.

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 15 | GET | `/api/services/{serviceId}/tarifas` | Authenticated | — | `TarifaServicioDto[]` | 200/401/403/404 |
| 16 | GET | `/api/services/{serviceId}/tarifas/{id}` | Authenticated | — | `TarifaServicioDto` | 200/401/403/404 |
| 17 | POST | `/api/services/{serviceId}/tarifas` | Administrator, Backoffice | `TarifaServicioCreateRequest` | `TarifaServicioDto` | 201/400/401/403/404 |
| 18 | PUT | `/api/services/{serviceId}/tarifas/{id}` | Administrator, Backoffice | `TarifaServicioUpdateRequest` | `TarifaServicioDto` | 200/400/401/403/404 |
| 19 | DELETE | `/api/services/{serviceId}/tarifas/{id}` | Administrator, Backoffice | — | — | 204/401/403/404 |

#### 2.3.2 Presupuestos del servicio (`PresupuestosController`)

> Ruta anidada `/api/services/{serviceId}/presupuestos`. GET es Authenticated; mutaciones requieren Administrator o Backoffice.

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 19a | GET | `/api/services/{serviceId}/presupuestos` | Authenticated | — | `PresupuestoServicioDto[]` | 200/401/403/404 |
| 19b | GET | `/api/services/{serviceId}/presupuestos/{id}` | Authenticated | — | `PresupuestoServicioDto` | 200/401/403/404 |
| 19c | POST | `/api/services/{serviceId}/presupuestos` | Administrator, Backoffice | `PresupuestoServicioCreateRequest` | `PresupuestoServicioDto` | 201/400/401/403/404 |
| 19d | PUT | `/api/services/{serviceId}/presupuestos/{id}` | Administrator, Backoffice | `PresupuestoServicioUpdateRequest` | `PresupuestoServicioDto` | 200/400/401/403/404 |
| 19e | DELETE | `/api/services/{serviceId}/presupuestos/{id}` | Administrator, Backoffice | — | — | 204/401/403/404 |

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

> `ClosureCreateRequest` referencia el servicio vía `ServiceId` (antes `ProjectId`).

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 50 | GET | `/api/closures` | All (ownership) | `?ApprovalFilterRequest` | `PagedResult<ClosureListItemDto>` | 200/401 |
| 51 | GET | `/api/closures/{id}` | All (ownership) | — | `ClosureDetailDto` | 200/401/403/404 |
| 52 | POST | `/api/closures` | ProjectManager, Backoffice, Administrator | `ClosureCreateRequest { serviceId, periodId, comentarios? }` | `ClosureDetailDto` | 201/400/401/403/409 |
| 53 | POST | `/api/closures/{id}/recalcular` | ProjectManager, Backoffice, Administrator | `ClosureRecalcRequest` + `If-Match` | `ClosureDetailDto` | 200/400/401/403/404/409/412 |
| 54 | POST | `/api/closures/{id}/aprobar` | Según paso actual (PM/Back/Fico/Dir) | `ClosureApproveRequest` + `If-Match` | `ClosureDetailDto` | 200/400/401/403/404/409/412 |
| 55 | POST | `/api/closures/{id}/rechazar` | Según paso actual | `ClosureRejectRequest { motivo }` + `If-Match` | `ClosureDetailDto` | 200/400/401/403/404/409/412 |

#### 2.9.1 Alertas de cierre (closure alerts)

> Alertas calculadas sobre cada Closure. La confirmación devuelve el `ClosureDetailDto` actualizado. Todos validan acceso (ownership) al closure.

| # | Método | Ruta | Auth | Request | Response | Códigos |
|---|---|---|---|---|---|---|
| 55a | GET | `/api/closures/{id}/alertas` | All (ownership) | — | `ClosureAlertaDto[]` | 200/401/403/404 |
| 55b | POST | `/api/closures/{id}/alertas/{alertaId}/confirmar` | ProjectManager, Backoffice, Fico, Direction, Administrator | — | `ClosureDetailDto` | 200/401/403/404/409 |
| 55c | GET | `/api/closures/todas-alertas` | All (ownership) | — | `ClosureAlertaResumida[]` | 200/401 |

> `ClosureAlertaResumida` (resumen de alertas de todos los cierres visibles): `{ id, tipo (string), codigo, descripcion, confirmada, closureId, serviceId, closureNombre }`.

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
- **Catálogos**: `ClientDetailDto`, `ServiceDetailDto`, `ServiceListItemDto`, `ConceptDetailDto`, `UserDetailDto`
- **Service (campos)**: `ServiceListItemDto { id, nombre, clientId, clientNombre, departmentId?, estado }`; `ServiceDetailDto { id, nombre, clientId, clientNombre, departmentId?, estado, interlocutorNombre?, interlocutorEmail?, interlocutorTelefono?, fechaAlta, costCenterIds[], userIds[], conceptIds[] }`; `ServiceCreateRequest`/`ServiceUpdateRequest` con los mismos campos editables (`nombre, clientId, departmentId?, estado, interlocutor*, fechaAlta, costCenterIds[], userIds[], conceptIds[]`).
- **Tarifas/Presupuestos**: `TarifaServicioDto { id, serviceId, nombre, valor, unidad?, fechaDesde, fechaHasta? }` (+ Create/Update); `PresupuestoServicioDto { id, serviceId, periodId?, tipo, importe, descripcion? }` (+ Create/Update).
- **Concept**: `ConceptDetailDto` y `ConceptCreateRequest`/`ConceptUpdateRequest` usan `serviceIds[]` (antes `actionIds[]`) y `userIds[]`.
- **Closures**: `ClosureListItemDto { id, serviceId, serviceNombre, periodId, periodNombre, costeTotal, facturacionTotal, margen, estado, pasoActual }`; `ClosureDetailDto { ..., serviceId, serviceNombre, ..., lines[], approvals[], alertas[] }`; `ClosureCreateRequest { serviceId, periodId, comentarios? }`; `ClosureRecalcRequest`, `ClosureApproveRequest`, `ClosureRejectRequest`.
- **Closure alerts**: `ClosureAlertaDto { id, tipo, codigo, descripcion, detalle?, confirmada, confirmadaPorNombre?, fechaConfirmacion? }`; `ClosureAlertaResumida { id, tipo, codigo, descripcion, confirmada, closureId, serviceId, closureNombre }`.
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
