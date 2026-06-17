# DATA-MODEL — SIG · Plataforma Operativa Integral

> Vista consolidada del modelo de datos. **La fuente única de verdad es `docs/ARQUITECTURA.md` §3 (Modelo de entidades)** y §5 (Definiciones técnicas).
> Este documento ofrece el resumen tabular y el esquema relacional para Backend, QA, Designer y Power BI.
> Stack: PostgreSQL 16.12 + EF Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4) + `UseSnakeCaseNamingConvention()`.

---

## 0. Convenciones globales

| Convención | Valor |
|---|---|
| Id de entidad | `int` PK autonumérico (sequence PostgreSQL) |
| Auditoría temporal | `CreatedAt`, `UpdatedAt` (`timestamptz`, UTC, `SaveChangesInterceptor`) |
| Soft delete | Interface `ISoftDeletable` + `HasQueryFilter(e => !e.IsDeleted)` |
| Fechas de negocio | `DateOnly` (Period.fechaInicio/Fin, Concept.fechaDesde/Hasta) |
| Timestamps de evento | `DateTime` con `Kind=Utc` obligatorio (gotcha Npgsql) |
| Concurrencia | `xmin` PostgreSQL como rowVersion en `Closure`/`ClosureLine` |
| Idioma | tablas/columnas en inglés snake_case; UI en español |
| Auditoría inmutable | `AuditLog`, `ApprovalHistory`, `CalculationLog` |

---

## 1. Tabla maestra de entidades

| # | Entidad | Tabla SQL | Tipo | RF | Soft delete | Auditable | Trazabilidad |
|---|---|---|---|---|:---:|:---:|---|
| 1 | `User` | `users` | Maestro | RF-A01, RF-C05 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 2 | `Role` | `roles` | Catálogo seed | RF-C06 | ✗ | ✗ | ARQUITECTURA §3.1 |
| 3 | `UserRole` | `user_roles` | N:M | RF-C05 | ✗ | ✗ | ARQUITECTURA §3.1 |
| 4 | `Department` | `departments` | Maestro | RF-C06 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 5 | `Client` | `clients` | Maestro | RF-C01 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 6 | `Service` | `services` | Maestro | RF-C02 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 7 | `CostCenter` | `cost_centers` | Maestro | RF-C06 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 8 | `ServiceCostCenter` | `service_cost_centers` | N:M | RF-C02 | ✗ | ✗ | ARQUITECTURA §3.1 |
| 9 | `ServiceUser` | `service_users` | N:M | RF-C02, RF-G01 | ✗ | ✗ | ARQUITECTURA §3.1 |
| 10 | `Concept` | `concepts` | Maestro | RF-C04 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 11 | `ServiceConcept` | `service_concepts` | N:M | RF-C03 | ✗ | ✗ | ARQUITECTURA §3.1 |
| 12 | `Variable` | `variables` | Maestro | RF-C04 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 13 | `TarifaServicio` | `tarifa_servicios` | Maestro | RF-C02 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 14 | `PresupuestoServicio` | `presupuesto_servicios` | Maestro | RF-C02 | ✓ | ✓ | ARQUITECTURA §3.1 |
| 15 | `Period` | `periods` | Maestro | RF-C07 | ✗ | ✓ | ARQUITECTURA §3.2 |
| 16 | `Closure` | `closures` | Transaccional | RF-D01..D06 | ✗ | ✓ + xmin | ARQUITECTURA §3.2 |
| 17 | `ClosureLine` | `closure_lines` | Transaccional | RF-D01, RF-D07 | ✗ | ✓ + xmin | ARQUITECTURA §3.2 |
| 18 | `ClosureAlerta` | `closure_alertas` | Transaccional | RF-D01..D06 | ✗ | ✓ | ARQUITECTURA §3.2 |
| 19 | `Approval` | `approvals` | Transaccional | RF-D02..D06 | ✗ | ✓ | ARQUITECTURA §3.2 |
| 20 | `ApprovalHistory` | `approval_history` | Inmutable | RF-D05, RF-D06 | ✗ | append-only | ARQUITECTURA §3.2 |
| 21 | `CalculationLog` | `calculation_logs` | Inmutable | RF-D01, RF-D07 | ✗ | append-only | ARQUITECTURA §3.2 |
| 22 | `RefreshToken` | `refresh_tokens` | Token | RF-A02, RF-A03 | ✗ | ✗ | ARQUITECTURA §3.1 |
| 23 | `AuditLog` | `audit_logs` | Inmutable | RF-F01, RF-F02 | ✗ | append-only | ARQUITECTURA §3.3 |
| 24 | `StagingCelero` | `staging_celero_visitas` | Staging | RF-E01 | ✗ | hash idempotencia | ARQUITECTURA §3.3 |
| 25 | `StagingBizneo` | `staging_bizneo_horas` | Staging | RF-E01 | ✗ | hash idempotencia | ARQUITECTURA §3.3 |
| 26 | `StagingIntratime` | `staging_intratime_fichajes` | Staging | RF-E01 | ✗ | hash idempotencia | ARQUITECTURA §3.3 |
| 27 | `StagingPayHawk` | `staging_payhawk_gastos` | Staging | RF-E01 | ✗ | hash idempotencia | ARQUITECTURA §3.3 |

> **Detalle completo (propiedades, tipos C#, configuración Fluent API, índices, FKs):** `docs/ARQUITECTURA.md` §3.
>
> **Notas del modelo actual (refactor Project→Service):**
> - La jerarquía es `Client → Service → Concept`. No existen las entidades `Project` ni `Action`; la entidad de dominio es `Service` (tabla `services`).
> - `Concept` tiene FK opcional `service_id` (`null` = concepto global, aplica a todos los servicios) y columna opcional `columna_a3` (mapeo a A3).
> - `ClosureAlerta` (`closure_alertas`): alertas generadas en el cálculo del cierre — `Tipo` (enum `TipoAlerta`: `Bloqueante`, `Advertencia`), `Codigo`, `Descripcion`, `Detalle?`, `Confirmada`, `ConfirmadaPorUserId?`, `FechaConfirmacion?`.
> - `TarifaServicio` (`tarifa_servicios`): tarifas por servicio — `Valor` (decimal EUR), `Unidad?` (visita|hora|km|dia, informativo), `FechaDesde`/`FechaHasta?` (`DateOnly`).
> - `PresupuestoServicio` (`presupuesto_servicios`): presupuesto por servicio — `PeriodId?` (`null` = todos los periodos), `Tipo` (enum `TipoConcepto`: `Pago`|`Factura`), `Importe` (decimal), `Descripcion?`.

---

## 2. Diagrama relacional (alto nivel)

```
                          ┌──────────┐
                          │ Client   │
                          └─────┬────┘
                                │ 1
                                │
                          ┌─────▼────┐
                          │ Service  │──N:M── CostCenter (ServiceCostCenter)
                          │          │──N:M── User       (ServiceUser, ownership)
                          │          │──N:M── Concept    (ServiceConcept)
                          └─────┬────┘
                                │ 1
                                │  (Concept.service_id opcional: null = global)
                                │
   Period ◄──────── Closure ────┤
     1                1│  N:1   │ N
                       │N       │
                       │        ├──1:N── ClosureAlerta (Bloqueante/Advertencia)
                       │        │
               ┌───────▼────────▼──────┐
               │   ClosureLine         │──1:1── CalculationLog
               │   (xmin concurrency)  │
               └───────────────────────┘
                       │ N
                       │
                  Concept (Pago/Factura)
                       │
                  Variable (Celero mapping)


   Closure ── 1:1 ── Approval ── N:1 ── User (approvedBy)
                        │
                        │ N
                  ApprovalHistory  (inmutable, append-only)


   User ──N:M── Role (UserRole)
   User ──N:1── Department


   AuditLog (cross-cutting, todas las operaciones)
   StagingX (raw payloads por sistema externo, hash SHA-256 idempotencia)
   RefreshToken (1:N por User, SHA-256 hash)
```

---

## 3. Catálogos seed obligatorios

### 3.1 `roles`

| Id | Nombre | Descripción operativa |
|---|---|---|
| 1 | `Administrator` | Acceso total, configuración, usuarios |
| 2 | `Direction` | Aprobación final, KPIs globales |
| 3 | `Fico` | Aprobación financiera |
| 4 | `Backoffice` | Validación de cálculos, devolución a gestor |
| 5 | `ProjectManager` | Inicia cierres de servicios asignados (ownership) |
| 6 | `Auditor` | Solo lectura de AuditLog y CalculationLog |
| 7 | `Reader` | Solo lectura operativa |

### 3.2 `periods` (seed inicial Dev/E2E)

`Enero 2026`, `Febrero 2026`, `Marzo 2026` (Estado=`Abierto` el activo, cerrados el resto).

### 3.3 Datos de prueba

Detallados en `context_SIG_es.md` §15 — clientes: Amex, Granini, Apple, Coty, Dyson, Future Cosmetics. Servicios: "Amex Shop Small", "Granini GPVs", etc.

---

## 4. Índices recomendados

| Tabla | Columna(s) | Tipo | Razón |
|---|---|---|---|
| `users` | `email` | UNIQUE | Login y registro |
| `users` | `nif` | UNIQUE | Identificación fiscal |
| `clients` | `nif` | UNIQUE | Identificación fiscal |
| `cost_centers` | `codigo` | UNIQUE | Catálogo |
| `concepts` | `tipo, is_deleted` | BTREE | Filtros UI |
| `closures` | `service_id, period_id` | UNIQUE (compuesto) | 1 closure por servicio/periodo |
| `closure_alertas` | `closure_id` | BTREE | Carga de alertas del cierre |
| `closure_lines` | `closure_id` | BTREE | Carga de detalle |
| `approval_history` | `closure_id, created_at` | BTREE | Histórico ordenado |
| `audit_logs` | `entity_name, entity_id, created_at` | BTREE | Consulta auditoría |
| `staging_celero_visitas` | `payload_hash` | UNIQUE | Idempotencia SHA-256 |
| `staging_bizneo_horas` | `payload_hash` | UNIQUE | Idempotencia SHA-256 |
| `staging_intratime_fichajes` | `payload_hash` | UNIQUE | Idempotencia SHA-256 |
| `staging_payhawk_gastos` | `payload_hash` | UNIQUE | Idempotencia SHA-256 |
| `refresh_tokens` | `token_hash` | UNIQUE | Login refresh |

---

## 5. Vistas analíticas para Power BI (schema `bi`)

| Vista | Origen | Uso |
|---|---|---|
| `bi.vw_closures_summary` | Closure + Service + Client + Period | Cuadros KPI por periodo |
| `bi.vw_closure_lines_detail` | ClosureLine + Concept + User | Margen por concepto/empleado |
| `bi.vw_approvals_timeline` | ApprovalHistory + User | Tiempo medio aprobación |
| `bi.vw_audit_trail` | AuditLog + User | Compliance y trazabilidad |
| `bi.vw_costcenter_distribution` | ServiceCostCenter + Closure | Distribución coste por CECO |

> Detalles SQL en `docs/EXPORTS.md` y `docs/ARQUITECTURA.md` §5 (RF-F03). Power BI conecta vía conector nativo PostgreSQL (read-only role `bi_reader`).

---

## 6. Reglas de integridad clave

1. **Closure único por (Service, Period)** — UNIQUE compuesto + validación de servicio antes del INSERT.
2. **ClosureLine.Importe = CalculationLog.Resultado** — el motor de cálculo escribe ambos en la misma transacción.
3. **Approval.Estado** sólo transiciona vía `Aprobar` o `Rechazar` (máquina de estados en `ApprovalService`); historial obligatorio en `ApprovalHistory`.
4. **No DELETE físico** en entidades maestras: soft delete via `IsDeleted` + filtro global.
5. **AuditLog se inserta en la misma transacción** que la operación (`SaveChangesInterceptor`), con `before`/`after` JSON.
6. **Soft-deleted records nunca aparecen en queries normales** salvo `.IgnoreQueryFilters()` (uso reservado a Auditor).

---

## 7. Referencias cruzadas

| Documento | Sección |
|---|---|
| `docs/ARQUITECTURA.md` | §3 (entidades), §5 (Fluent API + DTOs), §14 (trazabilidad) |
| `docs/API-SPEC.md` | Endpoints CRUD por entidad |
| `docs/INTEGRACIONES.md` | Tablas staging y mapeo de campos externos |
| `docs/ROLES-PERMISOS.md` | Matriz Role × Endpoint × Ownership |
| `docs/SUPOSICIONES_CRITICAS.md` | SUP-F01 (xmin), SUP-M05 (UTC), SUP-M01 (soft delete scope) |
