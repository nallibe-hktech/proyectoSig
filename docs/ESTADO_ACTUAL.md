# ESTADO ACTUAL DE LA APLICACIÓN — SIG-es

> ⚠️ **AVISO DE VIGENCIA (leer antes de usar este documento).** Este snapshot es un estado **PRE-refactor**. Refleja la jerarquía antigua `Cliente → Proyecto → Acción → Concepto` con las entidades internas `Project` y `Action`, que **ya NO existen en el código actual**.
>
> Sobre el código vigente se han aplicado, posteriores a esta fotografía:
> 1. **Refactor Project → Service (PPT):** se eliminaron `Project` y `Action`; la entidad de dominio es **`Service`** (tabla `services`) con la jerarquía **Cliente → Servicio → Concepto**. Endpoints `/api/services` (ya no `/api/projects` ni `/api/actions`). `Concept.ServiceId` y `Closure.ServiceId` (antes `ProjectId`). Relaciones `ServiceConcept`/`ServiceUser`/`ServiceCostCenter`. Migración data-preserving `20260612071833_RenameProjectActionToService`.
> 2. **Sistema de alertas de cierre:** nueva entidad `ClosureAlerta` (+ contratos A3). Migración `20260612121215_AddClosureAlertasAndA3Contratos`.
> 3. **Staging canónico:** `staging_galan_{entradas,salidas,stocks}` y `staging_mediapost_{pedidos,recepciones}`; las `staging_mdp_*` ya no existen.
>
> **Fuente de verdad vigente:** `docs/ARQUITECTURA.md`, `docs/DATA-MODEL.md`, `docs/API-SPEC.md`. Donde este documento diga "Project"/"Acción", léase "Service" según el modelo nuevo. Se conserva el histórico abajo sin modificar.

> **Fecha:** 2026-06-12
> **Propósito:** fotografía completa y sin omisiones de cómo está construida la app HOY, como base para aplicar los cambios de `CAMBIOS_PPT_10062026.md`.
> **Método:** lectura directa del código (Domain, Application, API, Infrastructure, Frontend Angular). Cada afirmación es verificable en el repo.

---

## 0. Stack y arquitectura física

| Capa | Tecnología | Notas |
|------|-----------|-------|
| Backend | .NET 10, Clean Architecture | `SIG.Domain` / `SIG.Application` / `SIG.Infrastructure` / `SIG.API` |
| ORM | EF Core + Npgsql (PostgreSQL) | snake_case naming, migrations en `SIG.Infrastructure/Migrations` |
| Auth | JWT propio + BCrypt | `RefreshToken`, roles por `UserRole` |
| Frontend | Angular standalone + Material (M3) | 23 features, signals, `sessionStorage` para token |
| BI | Power BI sobre vistas schema `bi` | `v_cierres_por_periodo`, `v_lineas_por_concepto`, `v_aprobaciones_pendientes`, `v_audit_resumen` |
| Integraciones | Solo lectura (GET) | Celero (PG remoto read-only), Bizneo, Intratime, PayHawk, SGPV, A3/Innuva, TravelPerk (HTTP); Galán (CSV), Mediapost (Excel) |

**Convenciones clave del repo (ver CLAUDE.md):** repos con `GetByIdAndUsuarioIdAsync(id, usuarioId)`, `AsNoTracking()` en reads, Global Query Filter por `IsDeleted`, `Program.cs` con `public partial class Program {}`.

---

## 1. MODELO DE DOMINIO ACTUAL (`SIG.Domain/Entities/Entities.cs`)

### Jerarquía de negocio (ESTADO ACTUAL)
```
Client (1) ──< Project (N) ──< Action (N) ──< Concept (N vía ActionConcept)
                  │                 │
                  │                 ├──< ActionUser (N usuarios)
                  │                 └──> Department (0..1)
                  ├──< ProjectCostCenter (N CECOs)
                  ├──< ProjectUser (N usuarios)
                  ├──< Closure (N, uno por Period)
                  ├──< TarifaProyecto (N)
                  └──< PresupuestoProyecto (N)
```

### Entidades y campos

| Entidad | Campos relevantes | Relaciones | Soft-delete |
|---------|-------------------|-----------|-------------|
| **User** | NIF, Nombre, Apellidos, Email, PasswordHash, Estado(EstadoUsuario), DepartmentId? | UserRoles, RefreshTokens, ProjectUsers, ActionUsers, ConceptUsers | ✔ |
| **Role** | Nombre, Descripcion? | UserRoles, Approvals | — |
| **UserRole** | (UserId, RoleId) | join | — |
| **Department** | Nombre | Users | ✔ |
| **CostCenter** | Codigo, Nombre | ProjectCostCenters | ✔ |
| **Client** | Nombre, NIF, Direccion?, Ciudad?, Provincia?, Pais?, CodigoPostal?, ContactoNombre/Email/Telefono? | Projects | ✔ |
| **Project** ⚠️ | Nombre, ClientId, Estado(EstadoProyecto), Interlocutor(Nombre/Email/Telefono)?, FechaAlta | Client, ProjectCostCenters, ProjectUsers, Actions, Closures | ✔ |
| **ProjectCostCenter** ⚠️ | (ProjectId, CostCenterId) | join | — |
| **ProjectUser** ⚠️ | (ProjectId, UserId) | join | — |
| **Action** ⚠️ | Nombre, ProjectId, ClientId, DepartmentId?, Estado(EstadoAccion) | Project, Client, Department, ActionConcepts, ActionUsers | ✔ |
| **ActionConcept** ⚠️ | (ActionId, ConceptId) | join | — |
| **ActionUser** ⚠️ | (ActionId, UserId) | join | — |
| **Concept** | Nombre, Tipo(TipoConcepto), FechaDesde, FechaHasta?, **FormulaJson**, ProjectId?(null=global), ColumnaA3? | ActionConcepts, ConceptUsers | ✔ |
| **ConceptUser** | (ConceptId, UserId) | join | — |
| **TarifaProyecto** ⚠️ | ProjectId, Nombre, Valor(€), Unidad?, FechaDesde, FechaHasta? | Project | ✔ |
| **PresupuestoProyecto** ⚠️ | ProjectId, PeriodId?, Tipo, Importe, Descripcion? | Project, Period | ✔ |
| **Variable** | Nombre, QuestionIdExterno, MapeoValoresJson | — | ✔ |
| **Period** | Nombre, FechaInicio, FechaFin, Estado(EstadoPeriodo) | Closures | — |
| **Closure** ⚠️ | **ProjectId**, PeriodId, CosteTotal, FacturacionTotal, **Margen(€)**, Estado(EstadoClosure), PasoActual(ApprovalStep), Comentarios?, RowVersion(xmin) | Project, Period, Lines, Approvals, ApprovalHistory | — |
| **ClosureLine** | ClosureId, ConceptId, UserId?, Importe, DatosEntradaJson, Tipo, TieneIncidencia, RowVersion | Closure, Concept, User, CalculationLog | — |
| **Approval** | ClosureId, RoleId, Paso(ApprovalStep), UserId?, Estado(EstadoApproval), Motivo?, FechaDecision? | Closure, Role, User | — |
| **ApprovalHistory** | ClosureId, UserId, PasoOrigen, PasoDestino, Accion, Motivo?, Timestamp | Closure, User | — |
| **AuditLog** | UserId?, EntityType, EntityId, Action(AuditAction), OldValueJson?, NewValueJson?, Timestamp, Ip? | User | — |
| **CalculationLog** | ClosureLineId(1:1), ConceptId, FormulaSnapshotJson, InputsJson, Resultado, Incidencias?, SistemaOrigen, Timestamp | ClosureLine, Concept | — |
| **RefreshToken** | UserId, TokenHash, ExpiresAt, RevokedAt?, Ip? | User | — |

⚠️ = entidad/relación afectada directamente por el cambio estructural #1 (eliminar Project / renombrar Action→Service).

### Enums (`SIG.Domain/Enums/Enums.cs`)
```csharp
EstadoUsuario  { Activo, Inactivo }
EstadoProyecto { Activo, Pausado, Cerrado }          // ⚠️ a eliminar
EstadoAccion   { Activa, Inactiva }                   // ⚠️ → EstadoServicio
TipoConcepto   { Pago, Factura }
EstadoPeriodo  { Abierto, Cerrado, Bloqueado }        // ⚠️ insuficiente (ver §2.5 PPT)
EstadoClosure  { Borrador, EnAprobacion, Aprobado, Rechazado, Exportado }
ApprovalStep   { ProjectManager=1, Backoffice=2, Fico=3, Direction=4, SystemExports=5 }  // ⚠️ rediseñar
EstadoApproval { Pendiente, Aprobado, Rechazado }
AuditAction    { Create, Update, Delete, Login, Logout, Export, Recalc }
```

---

## 2. PERSISTENCIA (`SIG.Infrastructure`)

### DbSets (`Persistence/AppDbContext.cs`)
26 DbSets de negocio + ~15 staging. Afectados por #1:
`Project`, `ProjectCostCenter`, `ProjectUser`, `Action`, `ActionConcept`, `ActionUser`, `TarifaProyecto`, `PresupuestoProyecto` (+ FKs en `Concept`, `Closure`).

### Tablas físicas (snake_case) y FKs (`AppDbContextModelSnapshot.cs`)
| Tabla | FKs salientes | OnDelete |
|-------|---------------|----------|
| `projects` | client_id→clients | Restrict |
| `project_cost_centers` | project_id, cost_center_id | — |
| `project_users` | project_id, user_id | — |
| `actions` | project_id→projects, client_id→clients, department_id→departments | Restrict / Restrict / SetNull |
| `action_concepts` | action_id, concept_id | — |
| `action_users` | action_id, user_id | — |
| `concepts` | project_id→projects | SetNull |
| `closures` | project_id→projects, period_id→periods | Restrict; índice ÚNICO (project_id, period_id) |
| `closure_lines` | closure_id, concept_id, user_id | Cascade / Restrict / SetNull |
| `approvals` | closure_id, role_id, user_id | Cascade / Restrict / SetNull |
| `tarifas_proyecto` | project_id→projects | Cascade |
| `presupuestos_proyecto` | project_id→projects, period_id→periods | Cascade / SetNull |
| `celero_service_mappings` | project_id→projects | Restrict (índice único celero_service_name) |
| `celero_mission_mappings` | action_id→actions | Restrict (índice único celero_mission_name) |
| `calculation_logs` | closure_line_id(1:1), concept_id | Cascade / Restrict |

Global Query Filter `IsDeleted` en: User, Department, CostCenter, Client, Project, Action, Concept, TarifaProyecto, PresupuestoProyecto, Variable.

### Seed (`Seed/DataSeeder.cs`) — datos de ejemplo (dev)
- 12 Roles: Administrator, Direction, Fico, Backoffice, ProjectManager, Auditor, Reader, RRHH, Facilitador, Interlocutor, Gestor, Auxiliar
- 4 Departments, 4 CostCenters (025888, 035501, 035502, 041200)
- 13 Users (admin/direccion/fico/2×backoffice/4×PM/auditor/reader/4×GPV)
- 3 Clients, **8 Projects**, **~16-24 Actions**, 4 Variables, 8 Concepts
- 5 Periods (Nov 2025 – Mar 2026), **~40 Closures** (8 proyectos × 5 periodos)
- Staging de muestra (Celero, Bizneo, Intratime, PayHawk)

### Repositorios (`Repositories/Repositories.cs`)
19 repos. `OwnershipHelper.PrivilegedRoles` = {Administrator, Direction, Fico, Backoffice, Auditor, Reader}. ProjectManager ve solo lo asignado.
Afectados por #1: `ProjectRepository`, `ActionRepository` (+ métodos `HasActionsOrClosuresAsync`, `HasClosuresAsync`).

### Integraciones (`Integrations/`) — TODAS SOLO LECTURA
Celero (PostgreSQL remoto con `default_transaction_read_only=on`), Bizneo/Intratime/PayHawk/SGPV/A3-Innuva/TravelPerk (HTTP), Galán (CSV), Mediapost (Excel). Modo Fake conmutty por config `Integrations:UseFake`. Paths de fichero en `Integrations:Galan/Mediapost:BasePath`.

---

## 3. API REST (`SIG.API/Controllers`)

| Controller | Ruta base | Endpoints | Auth destacada |
|-----------|-----------|-----------|----------------|
| AuthController | `api/auth` | login, refresh, logout, me | AllowAnonymous en login/refresh |
| ClientsController | `api/clients` | CRUD | Create/Update/Delete: Administrator |
| **ProjectsController** ⚠️ | `api/projects` | CRUD | Create/Update: Administrator,Backoffice; Delete: Administrator |
| **ActionsController** ⚠️ | `api/actions` | CRUD (params: projectId?, search?) | idem |
| ConceptsController | `api/concepts` | CRUD + `POST {id}/validar-formula` | Create/Update: Administrator,Backoffice |
| VariablesController | `api/variables` | CRUD | — |
| PeriodsController | `api/periods` | CRUD + activo + `cerrar` + `reabrir` | Administrator |
| ClosuresController | `api/closures` | list, get, create, `recalcular`, `aprobar`, `rechazar` (If-Match) | flujo por rol |
| ApprovalsController | `api/approvals` | list, pendientes, historial/{id}, batch/aprobar, batch/rechazar | — |
| UsersController | `api/users` | CRUD + password | Administrator,Auditor (read); Administrator (write) |
| RolesController | `api/roles` | list | Administrator,Auditor |
| DepartmentsController | `api/departments` | CRUD | — |
| CostCentersController | `api/costcenters` | CRUD | — |
| AuditController | `api/audit` | list (filtros) | Administrator,Auditor |
| **TarifasController** ⚠️ | `api/projects/{projectId}/tarifas` | CRUD anidado | Administrator,Backoffice |
| **PresupuestosController** ⚠️ | `api/projects/{projectId}/presupuestos` | CRUD anidado | Administrator,Backoffice |
| CalculationsController | `api/calculations` | get by closureLineId | — |
| SyncController | `api/sync` | `POST {system}`, `process`, celero/stats, intratime/discrepancias | Administrator |
| ExportsController | `api/exports` | a3-innuva/{closureId}, a3-erp/{closureId} | Administrator,Fico,Direction |
| CeleroVisitasController | `api/celero-visitas` | list, get, update (mapeo proyecto/accion) | — |
| GalanController | `api/galan` | entradas, salidas, stock, dashboard, upload | upload: Administrator |
| MediapostController | `api/mediapost` | pedidos, recepciones, dashboard, upload | upload: Administrator |
| DevController | `api/dev` | `regenerar-seed` | solo Dev/Test |

---

## 4. APPLICATION — Servicios y lógica núcleo

### Motor de cálculo (`Calculation/`)
`ICalculationEngine.EvaluateAsync(concept, closure, recursoId?)`:
1. Parsea `concept.FormulaJson` (`IFormulaParser`).
2. Carga contexto (`ICalculationDataLoader`): visitas Celero, gastos PayHawk, horas Bizneo, fichajes Intratime, tarifas, etc.
3. Evalúa AST recursivo de `FormulaNode`:
   - `NumberNode` (literal), `VariableNode` (resuelve por `IVariableResolver`), `AggregateNode` (Sum/Count/Min/Max sobre filas de un `SourceNode`), `BinaryOpNode` (Add/Sub/Mul/Div/Pct).
   - `SourceNode.Entity` ∈ {GastosPayHawk, VisitasCelero, HorasBizneo, HorasIntratime, TarifasProyecto, VisitasSgpv}; `FilterSpec` con Op Eq/Neq/Gt/Gte/Lt/Lte/In.
4. Registra incidencias (EmptyDataset, DivisionByZero…).
5. Devuelve `CalculationResult` (Resultado, InputsJson, FormulaSnapshot, SistemaOrigen, Incidencias).

> **Relevante para PPT §2.4:** NO existe hoy el concepto de "pre-filtro/pretratamiento configurable por cliente" (descontar primeros X km, máx €/día), ni "diccionario de equivalencias Innuva" en `Concept`, ni separación física Pago/Facturación (solo `TipoConcepto`). El multiplicador/%/agrupación de conceptos de facturación tampoco existe.

### Closure / Aprobaciones (`ClosureService.cs`, núcleo)
- `CreateAsync`: crea Closure + `ComputeLinesAsync` (identifica conceptos por rango de fecha, evalúa cada uno, separa por Tipo: Pago=coste, Factura=facturación, calcula `Margen = Facturacion − Coste` en €) + primera Approval (paso ProjectManager).
- `RecalcAsync`: recalcula líneas solo en Borrador/Rechazado.
- `ApproveAsync`: avanza `ApprovalStep` ProjectManager→Backoffice→Fico→Direction→SystemExports (auto-Aprobado). Registra `ApprovalHistory`.
- `RejectAsync`: retrocede un paso, marca Rechazado.
- Concurrencia optimista por `RowVersion` (If-Match).
- `StepToRole(ApprovalStep)` mapea paso→rol.

> **Relevante para PPT §2.8:** el flujo decidido en PPT es `RevisiónOperaciones → AprobaciónFICO → (Dirección opc.) → CierreFICO`, y el paso de cálculo NO debe mostrarse. El enum/flujo actual (`ProjectManager→Backoffice→Fico→Direction→SystemExports`) debe realinearse.

### Dashboard (`DashboardCalcSyncAudit.cs` → `DashboardService`)
- `GetKpisAsync(periodId?)`: cuenta cierres completados/pendientes; suma facturación/coste/margen; top-6 clientes por facturación; evolución últimos 6 periodos cerrados.
- `GetAvisosAsync`: avisos de cierres pendientes/rechazados, incidencias de cálculo, periodos bloqueados, periodos por vencer (≤7 días). `DashboardAvisoDto { Tipo, Descripcion, EntityId? }`.
- `GetMisProyectosAsync(periodId?)`: proyectos asignados al usuario + estado de cierre del periodo.

> **Relevante para PPT §2.1:** el aviso ya trae `EntityId?` (base para deep-link). Hoy NO se expone "coste real de lo facturado" junto a margen como tarjeta, ni "cierre de facturación" separado de "cierre de costes", ni unidades unificadas en K. Existen los gráficos "Margen vs Objetivo" y "Objetivos del Período" que el PPT pide fusionar/eliminar.

### Otros servicios
ProjectService, ActionService, ConceptService (valida fórmula), PeriodService (Abierto↔Cerrado, una sola activa), ApprovalService (agrega + delega en ClosureService), ClientService, UserService (hash password, roles), Tarifa/PresupuestoProyectoService, Variable/Role/Department/CostCenterService, SyncService, AuditService, CalculationService.

---

## 5. FRONTEND ANGULAR (`frontend/src/app`)

### Rutas y menú (`app.routes.ts`, `layout/shell`)
**Menú operativo:** Dashboard, Clients, **Projects** ⚠️, **Actions** ⚠️, Concepts, Variables, Periods, Approvals, Closures, Reports.
**Menú administración** (Administrator/Auditor/Fico): Cost Centers, Departments, Roles, Users, Audit Log, Sync, Celero Visitas, Galán, Mediapost, Bizneo, Intratime, PayHawk.
**AppBar:** selector de Período global (`<mat-select>` con `PeriodService.activeId`).

### Features (23 carpetas en `features/`)
- **dashboard** (1 componente grande, ~833 líneas): 4 KPI cards (Facturación total, Margen promedio, Cierres completados, Pend. aprobación), gráfico área "Evolución de Facturación", donut "Facturación por Cliente", tabla **"Mis Proyectos"** (cols: Proyecto, Cliente, Coste Bruto, Facturación, Margen, Estado, Acción), gauge **"Margen vs Objetivo"**, panel **"Objetivos del Período"**, panel "Alertas".
- **projects** ⚠️ (4 comp + subcarpetas tarifas/presupuestos): list, form, detail. Menú "Projects".
- **actions** ⚠️ (3 comp): list (cols ID/ACCION/PROYECTO/ESTADO/CECO/ACCIONES), form (campo "Proyecto*"), detail. Genera "SRV-{id}".
- **concepts** (4 comp + formula-editor): list (NOMBRE/TIPO/DESDE/HASTA), form, detail, **formula-editor** (canónico). Filtro Tipo Pago/Factura. *(Nota memoria: el editor canónico es `concepts/formula-editor`; el canvas en `calculations/components` es código muerto.)*
- **periods** (2 comp): list (Estado badge Abierto/Cerrado/Bloqueado, botones cerrar/reabrir), form. *(Hoy NO tiene campos "ámbito"/"responsable" en el modelo, pero el PPT §2.5 los menciona para quitar — verificar en plantilla.)*
- **approvals** (2 comp): **panel con datos MOCK, no integrado al backend**. Filtros Periodo/Cliente/Proyecto/Estado; tabla pendientes; detalle de cierre; flujo de 5 pasos (Cálculo→Revisión→FICO→Dirección→Cierre).
- **closures** (3 comp): list (cols PROYECTO/PERIODO/COSTE/FACTURACIÓN/MARGEN/ESTADO), form, detail. Texto "Flujo de aprobación (5 pasos): 1 PM → 2 Backoffice → 3 Fico → 4 Direction → 5 Exportado".
- **clients** (4 comp): list (Nombre/NIF/Ciudad), form (datos+contacto+dirección), detail. `ClientListItemDto.projectCount`.
- **departments** (2 comp): list (Nombre), form. *(PPT §2.15: quitar "usuarios del departamento".)*
- **cost-centers** (2 comp): list (Código/Nombre), form.
- **users** (3 comp): list + filtros (departamento/rol/estado), form, detail.
- **roles** (1 comp, read-only): tabla con permisos por columna (Pagos/Facturaciones/Usuarios/Roles), scope Global/Proyecto. **Datos internos (interfaz `RolDef`), no del backend.**
- **audit** (1 comp): tabla (Fecha/Usuario/Tipo Acción/Cliente/Proyecto/Recurso/Entidad) + filtros. *(PPT §2.16: añadir columna "detalle" before/after.)*
- **calculations** (1 comp detail): muestra fórmula snapshot + inputs + resultado de un `closureLineId`.
- **variables** (2 comp): list (Nombre/QuestionId/Mapeos), form.
- **reports** (1 comp): tarjetas que apuntan a vistas Power BI.
- **sync** (1 comp): tarjetas por sistema + "Procesar Registros".
- **celero-visitas** (2 comp): tabla de visitas + mapeo a usuario/proyecto/acción; celero-mapeos.
- **galan** (4 comp): dashboard + entradas/salidas/stock.
- **mediapost** (3 comp): dashboard + pedidos/recepciones.
- **bizneo / intratime / payhawk** (1 dashboard cada uno).

### Modelos TS (`models/dtos.ts`, `models/enums.ts`)
Enums espejo de backend (incluye `EstadoProyecto`, `EstadoAccion`, `ApprovalStep` con los 5 pasos). Helpers `badgeClassFromClosure` / `badgeLabelFromClosure`. ~70 interfaces DTO.

### Servicios HTTP (`core/api`)
ActionService→`/actions`, ProjectService→`/projects`, ConceptService→`/concepts`, PeriodService→`/periods`, ClosureService→`/closures`, ApprovalService→`/approvals`, ClientService, UserService, CatalogService→`/catalogs` (departments/costcenters/roles), VariableService, SyncService, CalculationService, DashboardService, AuditService, MiscService.

### Componentes shared
breadcrumbs, confirm-dialog, dashboard-design (logo SVG), empty-state, login-design, page-skeleton, pie-chart, state-badge.

---

## 6. MAPA DE IMPACTO DEL CAMBIO #1 (eliminar Project / Action→Service)

### Backend — ficheros a tocar
- **Domain:** `Entities.cs` (eliminar Project/ProjectCostCenter/ProjectUser; renombrar Action→Service, ActionConcept→ServiceConcept, ActionUser→ServiceUser; reasociar Closure/Tarifa/Presupuesto/Concept a Service; mover ProjectCostCenter→ServiceCostCenter, ProjectUser ya existe como ActionUser→ServiceUser). `Enums.cs` (eliminar EstadoProyecto; EstadoAccion→EstadoServicio).
- **Infrastructure:** `AppDbContext.cs` (DbSets), `Configurations.cs` (8+ configs, ToTable, FKs, query filters), `Repositories.cs` (ProjectRepository fuera; ActionRepository→ServiceRepository), `DependencyInjection.cs` (registros), `DataSeeder.cs` (sin Projects; Actions→Services), **+ migración EF Core** (drop projects/joins; rename actions→services y joins; redirigir FKs de closures/tarifas/presupuestos/concepts/celero_*).
- **Application:** DTOs (Project* fuera; Action*→Service*), Services (ProjectService fuera; ActionService→ServiceService; ajustar Closure/Tarifa/Presupuesto/Dashboard/Concept que referencian projectId), Interfaces, Validators.
- **API:** eliminar `ProjectsController`; `ActionsController`→`ServicesController` (`api/servicios` o `api/services`); reanclar Tarifas/Presupuestos a `api/services/{serviceId}/...`; ajustar CeleroVisitas (mapeo a service).

### Frontend — ficheros a tocar
- Eliminar `features/projects`, ruta `/projects`, entrada de menú "Projects".
- `features/actions`→`features/services`, rutas `/services`, labels "Servicio", `data-testid`.
- `models/enums.ts` (EstadoProyecto fuera, EstadoAccion→EstadoServicio), `dtos.ts` (Project*/Action*).
- `core/api` (ProjectService fuera, ActionService→ServiceService).
- Dashboard "Mis Proyectos"→"Mis Servicios"; columnas; tabla Closures col "Proyecto"→"Servicio"; Approvals filtros; Audit columna "Proyecto"→"Servicio"; Celero-visitas mapeo.

### Decisiones de modelado que dependen del cambio #1 (a confirmar)
1. **Tarifa/Presupuesto**: hoy cuelgan de Project → ¿pasan a colgar de **Service**? (el PPT lo sugiere; "TarifaServicio"/"PresupuestoServicio").
2. **Closure**: hoy `project_id` con índice único (project_id, period_id) → pasa a `service_id` + único (service_id, period_id).
3. **Concept.ProjectId** (global vs por-proyecto) → `Concept.ServiceId`.
4. **CECO/Usuario**: el PPT dice que las relaciones CECO/Usuario migran a Service. Action ya tiene `ActionUser`; falta `ServiceCostCenter` (hoy es `ProjectCostCenter`).
5. **Estrategia de datos de la migración**: ¿reconstruir BD dev desde seed (simple, es entorno dev con datos de ejemplo) o migración con mapeo de datos existentes?

---

## 7. RESUMEN DE BRECHAS (lo que el PPT pide y HOY no existe)

| PPT § | Brecha respecto al código actual |
|-------|----------------------------------|
| §1 | Modelo aún `Cliente→Proyecto→Acción→Concepto`. |
| §2.1 | Sin tarjeta "coste real facturado"; sin separar cierre costes/facturación; sin unidades K; gráficos "Margen vs Objetivo" y "Objetivos del Período" aún separados; alertas no clicables (aunque `EntityId?` ya existe). |
| §2.2 | Cliente sin metadatos no-Celero (rol/teléfonos/emails ya parciales), sin resumen facturación agregado, sin entidad **Incidencias de cliente**, estado cliente no limitado a activo/inactivo. |
| §2.3 | Servicios sin columnas departamento/facturación/margen; edición no limitada a estado. |
| §2.4 | Conceptos no separados físicamente Pago/Facturación; sin pre-filtros configurables; sin diccionario Innuva; sin multiplicador/%/agrupación facturación. |
| §2.5 | `EstadoPeriodo` insuficiente; sin separar cierre costes/facturación; sin "fecha de pago"; cierre global (no por servicio); sin validación previa de gastos. |
| §2.6 | Aprobaciones: panel MOCK; sin vista matriz; sin drill-down día/empleado/concepto; sin campos de ajuste manual por línea (pago/facturación adicional + motivo). |
| §2.7-2.8 | Conceptos sin orden fijo/categorización; flujo de aprobación no alineado (paso cálculo visible, pasos no = Operaciones/FICO/Dirección/Cierre); sin desdoble Total salario/gastos; sin granularidad por contrato/llamamiento (Innuva). |
| §2.11 | Informes sin forecast/previsión; sin informe traspaso CECOs. |
| §2.12-2.13 | Roles tabla en frontend mock (no backend); sin permiso "auditorías"; sin múltiples asignaciones usuario. |
| §2.16 | Auditoría sin columna detalle before/after en UI. |

---

*Documento base. La ejecución se planifica sobre `CAMBIOS_PPT_10062026.md` §5 (plan priorizado). El cambio #1 es prerequisito del resto.*
