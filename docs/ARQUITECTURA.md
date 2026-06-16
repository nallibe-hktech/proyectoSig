# ARQUITECTURA — SIG · Plataforma de Cierres

Documento único de verdad técnica. Cualquier cambio aquí lo aplica el Arquitecto en una revisión. Los demás agentes (Desarrollador Backend, Frontend, Tester) consumen este documento y respetan la fidelidad nominal (CLAUDE.md regla de oro 1).

---

## 0. Stack del proyecto

| Decisión | Valor | Justificación |
|---|---|---|
| Base de datos | PostgreSQL 16.12 | Vinculante por cliente. Power BI conectará vía conector nativo PostgreSQL. |
| Proveedor EF Core | `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 | Sección "Backend .NET 10 — con PostgreSQL" de `NUGET_VERSIONS.md`. |
| Autenticación | JWT propio con BCrypt (`BCrypt.Net-Next` 4.0.3, `System.IdentityModel.Tokens.Jwt` 8.4.0) | Vinculante por cliente. Entra ID queda fuera de MVP (Fase 2). |
| Cloud target | Standalone (sin Azure App Service / Bicep). On-premise o VM Linux/Windows del cliente. | INPUT_APP no menciona cloud. |
| Versión .NET | .NET 10 (SDK 10.0.104 detectado en environment probe) | Última LTS soportada por `NUGET_VERSIONS.md`. |
| Versión Angular | Angular 21 + Angular Material 21 | CLI 21.2.2 detectado. Material 3 con `mat.theme(...)` (gotchas Material 21). |
| Tenencia | Mono-tenant (organización única SIG ES) | Vinculante. Los `Client` son clientes-empresa de SIG, no tenants. |
| Idioma | UI español, código/identificadores inglés | Vinculante. |

---

## 1. Glosario técnico de dominio

| Término | Definición |
|---|---|
| **Client** | Empresa cliente de SIG (Alpha Foods, Beta Cosmetics, Gamma Retail). NO es un tenant. |
| **Service** | Servicio contratado por un Client a SIG. Entidad central de la jerarquía **Cliente → Servicio → Concepto** (sustituye al antiguo par Project/Action, ambos eliminados). Tiene N:M Concepts (`ServiceConcept`), N:M Users (`ServiceUser`, recursos asignados / ownership), N:M CostCenters (`ServiceCostCenter`), N Closures, N TarifaServicio y N PresupuestoServicio. Conserva metadatos de interlocutor heredados del antiguo Project. (Nota de fidelidad: el campo externo `project`/`PROJECT_ID` de sistemas ajenos como Intratime/Celero NO se renombra; solo desaparece la **entidad interna** Project.) |
| **Concept** | Regla de cálculo con una fórmula. Tipo `Pago` (sale dinero al empleado) o `Factura` (entra dinero del cliente). Tiene validez temporal (`fechaDesde`/`fechaHasta`). Se vincula a uno o varios Service vía `ServiceConcept`; `ServiceId` nulo = concepto global aplicable a todos los servicios. |
| **Variable** | Mapeo entre una pregunta de Celero (`questionIdExterno`) y un valor numérico. Ej: respuesta "Sí" → 1, "No" → 0. |
| **Period** | Mes de cierre. Estados: Abierto, Cerrado, Bloqueado. |
| **Closure** | Conjunto de líneas (`ClosureLine`) calculadas para un par `Service × Period`. Estados de aprobación. |
| **ClosureAlerta** | Alerta generada sobre un Closure durante el cálculo/cierre. Tipo `Bloqueante` o `Advertencia`. Tiene `Codigo`, `Descripcion` y flag `Confirmada` (con usuario y fecha de confirmación). Las bloqueantes impiden avanzar el cierre hasta confirmarse. |
| **ClosureLine** | Línea individual: importe calculado por un Concept dentro de un Closure, opcionalmente asignada a un User (recurso de campo). |
| **Approval** | Estado actual del cierre en un paso del flujo de 5 niveles. |
| **ApprovalHistory** | Inmutable. Una fila por cada transición histórica de un Closure entre pasos del flujo. |
| **AuditLog** | Inmutable. Una fila por cada cambio de entidad / login / logout / export / recalc. Misma transacción que la operación. |
| **CalculationLog** | Inmutable. Una fila por cada ClosureLine, contiene snapshot completo de la fórmula JSON, inputs y resultado. |
| **Staging\*** | Tabla por sistema externo (Celero/Bizneo/Intratime/PayHawk) que almacena el raw payload + hash de idempotencia. |
| **Recurso** | User con rol distinto a los 7 roles operativos (gpv1..gpv4). Aparecen como `ClosureLine.UserId`. |
| **Motor de cálculo** | Componente C# (`Application/Calculation/CalculationEngine.cs`) que recibe `Concept`, `Closure` y resuelve la fórmula AST contra entidades staging y maestros. |
| **xmin** | System column de PostgreSQL usado como rowVersion para optimistic concurrency. |

---

## 2. Requisitos (trazabilidad)

### 2.1 Funcionales

| ID | Descripción | Endpoint principal | Entidad implicada |
|---|---|---|---|
| RF-A01 | Login email+password → access+refresh token | POST `/api/auth/login` | User, RefreshToken |
| RF-A02 | Logout invalida refresh tokens del usuario | POST `/api/auth/logout` | RefreshToken |
| RF-A03 | Refresh: intercambiar refresh por nuevo access | POST `/api/auth/refresh` | RefreshToken |
| RF-B01 | Dashboard KPIs período activo | GET `/api/dashboard` | Closure, Period |
| RF-B02 | Dashboard avisos automáticos | GET `/api/dashboard/avisos` | Closure, StagingX |
| RF-B03 | Dashboard "Mis servicios" filtrado por ownership | GET `/api/dashboard/mis-proyectos` | Service, ServiceUser |
| RF-C01 | CRUD Client paginado | `/api/clients` | Client |
| RF-C02 | CRUD Service (N:M CostCenter, N:M User, N:M Concept) | `/api/services` | Service, ServiceCostCenter, ServiceUser, ServiceConcept |
| RF-C03 | *(consolidado en RF-C02)* — el antiguo CRUD Action desaparece; sus relaciones (Concepts, Users) las absorbe Service | `/api/services` | Service, ServiceConcept, ServiceUser |
| RF-C04 | CRUD Concept con fórmula | `/api/concepts` | Concept |
| RF-C05 | CRUD User con NIF, multi-rol, asignaciones | `/api/users` | User, UserRole |
| RF-C06 | CRUD Role/Department/CostCenter (admin) | `/api/roles`, `/api/departments`, `/api/costcenters` | Role, Department, CostCenter |
| RF-C07 | CRUD Period con cierre/reapertura | `/api/periods`, `/api/periods/{id}/cerrar`, `/api/periods/{id}/reabrir` | Period |
| RF-D01 | Crear/recalcular Closure | POST `/api/closures`, POST `/api/closures/{id}/recalcular` | Closure, ClosureLine, CalculationLog |
| RF-D02 | Panel aprobaciones con filtros | GET `/api/approvals` | Approval, Closure |
| RF-D03 | "Pendientes" para el usuario | GET `/api/approvals/pendientes` | Approval |
| RF-D04 | Detalle aprobación con KPIs | GET `/api/closures/{id}` | Closure, ClosureLine |
| RF-D05 | Aprobar cierre | POST `/api/closures/{id}/aprobar` | Approval, ApprovalHistory |
| RF-D06 | Rechazar con motivo | POST `/api/closures/{id}/rechazar` | Approval, ApprovalHistory |
| RF-D07 | Detalle cálculo línea | GET `/api/calculations/{closureLineId}` | CalculationLog |
| RF-E01 | Sincronización sistemas externos | POST `/api/sync/{system}` | StagingX |
| RF-E02 | Export A3 Innuva | GET `/api/exports/a3-innuva/{closureId}` | Closure |
| RF-E03 | Export A3 ERP | GET `/api/exports/a3-erp/{closureId}` | Closure |
| RF-F01 | AuditLog completo en transacción | (todos los endpoints con efectos) | AuditLog |
| RF-F02 | Consulta AuditLog | GET `/api/audit` | AuditLog |
| RF-F03 | Vistas SQL en schema bi | (Power BI directo) | — |
| RF-G01 | Filtrado por ownership | (todos los repositorios) | — |
| RF-G02 | Soft delete con Global Query Filter | (entidades maestras) | — |

### 2.2 No funcionales

| ID | Descripción | Solución técnica |
|---|---|---|
| RNF-01 | Recálculo ≤1000 líneas < 30s | Motor C# in-process, carga staging en memoria una vez por Closure |
| RNF-02 | AuditLog en misma transacción | `SaveChangesInterceptor` (EF Core) |
| RNF-03 | Concurrencia optimista en Closure/ClosureLine | `xmin` PostgreSQL como rowVersion, IsConcurrencyToken |
| RNF-04 | Trazabilidad ClosureLine → CalculationLog | FK 1:1 + snapshot JSON inmutable |
| RNF-05 | API stateless | UserId siempre del JWT (`ClaimTypes.NameIdentifier`), nunca del body |
| RNF-06 | Errores con ProblemDetails y código semántico | Middleware global `ExceptionHandlingMiddleware` |
| RNF-07 | Idempotencia en sync | Hash SHA-256 del payload, único en BD |

---

## 3. Modelo de entidades

Convenciones globales aplicables:
- `Id` int PK autonumérico (sequence PostgreSQL).
- `CreatedAt`, `UpdatedAt` (timestamptz, `DateTime` UTC). Establecidos por `SaveChangesInterceptor`.
- Entidades con soft delete implementan `ISoftDeletable` (`IsDeleted: bool`, `DeletedAt: DateTime?`). Tienen `HasQueryFilter(e => !e.IsDeleted)`.
- Todas las fechas de negocio (Period.fechaInicio/Fin, Concept.fechaDesde/Hasta) usan `DateOnly` (sin tiempo ni TZ).
- Fechas con momento (timestamps de evento) usan `DateTime` UTC con `DateTime.SpecifyKind(..., DateTimeKind.Utc)` antes de persistir (gotcha `[Stack: PostgreSQL] Npgsql rechaza DateTime con Kind distinto de Utc`).
- Tabla en español de UI pero columnas en inglés (camelCase no, snake_case PostgreSQL convertido automáticamente vía EF `UseSnakeCaseNamingConvention()`).

### 3.1 Maestros

#### User (RF-C05)
```csharp
public class User : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string NIF { get; set; } = null!;          // único, regex española
    public string Nombre { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public string Email { get; set; } = null!;        // único
    public string PasswordHash { get; set; } = null!; // BCrypt 12 rounds
    public EstadoUsuario Estado { get; set; }         // Activo, Inactivo
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<ServiceUser> ServiceUsers { get; set; } = new List<ServiceUser>();
    public ICollection<ConceptUser> ConceptUsers { get; set; } = new List<ConceptUser>();
}
public enum EstadoUsuario { Activo, Inactivo }
```
Fluent API: unique index sobre `NIF` y `Email`. HasQueryFilter sobre IsDeleted. PasswordHash excluido del AuditLog.

#### Role
```csharp
public class Role : IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;       // Administrator, Direction, Fico, Backoffice, ProjectManager, Auditor, Reader
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
```
Roles fijos (no soft-delete). Seed inicial obligatorio.

#### UserRole (N:M)
```csharp
public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}
```
Clave compuesta `(UserId, RoleId)`.

#### Department (RF-C06)
```csharp
public class Department : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### CostCenter (RF-C06)
```csharp
public class CostCenter : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;       // "025888"
    public string Nombre { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ServiceCostCenter> ServiceCostCenters { get; set; } = new List<ServiceCostCenter>();
}
```
Unique index sobre `Codigo`.

#### Client (RF-C01)
```csharp
public class Client : ISoftDeletable, IAuditable
{
    public int Id { get; set; }                       // espejo clientId externo
    public string Nombre { get; set; } = null!;
    public string NIF { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    public string? Pais { get; set; }
    public string? CodigoPostal { get; set; }
    public string? ContactoNombre { get; set; }
    public string? ContactoEmail { get; set; }
    public string? ContactoTelefono { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Service> Services { get; set; } = new List<Service>();
}
```

#### Service (RF-C02)

> Entidad central de la jerarquía **Cliente → Servicio → Concepto**. Resultado del refactor PPT que eliminó las entidades internas `Project` y `Action`: `Service` absorbe el vínculo directo a `Client` (antes en Project) y las relaciones CECO/Usuario/Cierres/Tarifas/Presupuestos. El rename a nivel de BD lo aplica la migración `20260612071833_RenameProjectActionToService` (data-preserving). Implementado en `backend/SIG.Domain/Entities/Entities.cs`.

```csharp
public class Service : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public EstadoServicio Estado { get; set; }        // Activo, Inactivo
    // Metadatos de interlocutor heredados del antiguo Project (preservados en la migración).
    public string? InterlocutorNombre { get; set; }
    public string? InterlocutorEmail { get; set; }
    public string? InterlocutorTelefono { get; set; }
    public DateOnly FechaAlta { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ServiceConcept> ServiceConcepts { get; set; } = new List<ServiceConcept>();
    public ICollection<ServiceUser> ServiceUsers { get; set; } = new List<ServiceUser>();
    public ICollection<ServiceCostCenter> ServiceCostCenters { get; set; } = new List<ServiceCostCenter>();
    public ICollection<Closure> Closures { get; set; } = new List<Closure>();
}
public enum EstadoServicio { Activo, Inactivo }
```

#### ServiceConcept (N:M), ServiceUser (N:M) y ServiceCostCenter (N:M)
```csharp
public class ServiceConcept { public int ServiceId { get; set; } public Service Service { get; set; } = null!; public int ConceptId { get; set; } public Concept Concept { get; set; } = null!; }
public class ServiceUser { public int ServiceId { get; set; } public Service Service { get; set; } = null!; public int UserId { get; set; } public User User { get; set; } = null!; }   // ownership de servicio
public class ServiceCostCenter { public int ServiceId { get; set; } public Service Service { get; set; } = null!; public int CostCenterId { get; set; } public CostCenter CostCenter { get; set; } = null!; }
```

#### Concept (RF-C04)
```csharp
public class Concept : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public TipoConcepto Tipo { get; set; }            // Pago | Factura
    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public string FormulaJson { get; set; } = null!;  // AST JSON (ver §6)
    public int? ServiceId { get; set; }               // null = concepto global (aplica a todos los servicios)
    public Service? Service { get; set; }
    public string? ColumnaA3 { get; set; }            // columna destino del export A3: "ImporteBruto", "IRPF", "SSEmpleado", "KM", etc.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ServiceConcept> ServiceConcepts { get; set; } = new List<ServiceConcept>();
    public ICollection<ConceptUser> ConceptUsers { get; set; } = new List<ConceptUser>();
}
public enum TipoConcepto { Pago, Factura }
```

#### ConceptUser (N:M)
```csharp
public class ConceptUser { public int ConceptId { get; set; } public Concept Concept { get; set; } = null!; public int UserId { get; set; } public User User { get; set; } = null!; }
```

#### Variable
```csharp
public class Variable : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;       // "PuntoMontado"
    public string QuestionIdExterno { get; set; } = null!;  // "Q12"
    public string MapeoValoresJson { get; set; } = null!;   // [{ "respuesta": "Sí", "valor": 1 }, ...]
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

#### Period (RF-C07)
```csharp
public class Period : IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;       // "Marzo 2026"
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public EstadoPeriodo Estado { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Closure> Closures { get; set; } = new List<Closure>();
}
public enum EstadoPeriodo { Abierto, Cerrado, Bloqueado }
```
Sin soft-delete (periodos no se borran).

### 3.2 Transaccionales

#### Closure (RF-D01, RNF-03)
```csharp
public class Closure : IAuditable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    public decimal CosteTotal { get; set; }           // decimal(18,4)
    public decimal FacturacionTotal { get; set; }
    public decimal Margen { get; set; }               // = FacturacionTotal - CosteTotal
    public EstadoClosure Estado { get; set; }
    public ApprovalStep PasoActual { get; set; }      // 1..5
    public string? Comentarios { get; set; }
    public DateTime FechaCreacion { get; set; }
    public uint RowVersion { get; set; }              // xmin PostgreSQL
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ClosureLine> Lines { get; set; } = new List<ClosureLine>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
    public ICollection<ClosureAlerta> Alertas { get; set; } = new List<ClosureAlerta>();
}
public enum EstadoClosure { Borrador, EnAprobacion, Aprobado, Rechazado, Exportado }
public enum ApprovalStep { ProjectManager = 1, Backoffice = 2, Fico = 3, Direction = 4, SystemExports = 5 }
```
Unique constraint `(ServiceId, PeriodId)` — un Closure por Service×Period.
RowVersion: `b.Property(c => c.RowVersion).IsRowVersion().HasColumnName("xmin").HasColumnType("xid").ValueGeneratedOnAddOrUpdate().IsConcurrencyToken();`

#### ClosureLine (RF-D01, RNF-03)
```csharp
public class ClosureLine : IAuditable
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public Closure Closure { get; set; } = null!;
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public int? UserId { get; set; }                  // recurso de campo opcional
    public User? User { get; set; }
    public decimal Importe { get; set; }              // decimal(18,4)
    public string DatosEntradaJson { get; set; } = null!;  // inputs resueltos
    public TipoConcepto Tipo { get; set; }            // copiado de Concept para inmutabilidad histórica
    public bool TieneIncidencia { get; set; }         // EmptyDataset / DivisionByZero
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public CalculationLog? CalculationLog { get; set; }
}
```
Misma config rowVersion que Closure.

### 3.3 Aprobación

#### Approval
```csharp
public class Approval : IAuditable
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public Closure Closure { get; set; } = null!;
    public int RoleId { get; set; }                   // rol que debe aprobar este paso
    public Role Role { get; set; } = null!;
    public ApprovalStep Paso { get; set; }
    public int? UserId { get; set; }                  // usuario que aprobó/rechazó
    public User? User { get; set; }
    public EstadoApproval Estado { get; set; }
    public string? Motivo { get; set; }
    public DateTime? FechaDecision { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
public enum EstadoApproval { Pendiente, Aprobado, Rechazado }
```

#### ApprovalHistory (INMUTABLE)
```csharp
public class ApprovalHistory
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public Closure Closure { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ApprovalStep PasoOrigen { get; set; }
    public ApprovalStep PasoDestino { get; set; }
    public string Accion { get; set; } = null!;       // "Aprobar" | "Rechazar" | "Recalcular"
    public string? Motivo { get; set; }
    public DateTime Timestamp { get; set; }
}
```
Sin `UpdatedAt`. Tabla sin endpoints PUT/DELETE.

#### ClosureAlerta (sistema de alertas de cierre)
```csharp
public class ClosureAlerta : IAuditable
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public Closure Closure { get; set; } = null!;
    public TipoAlerta Tipo { get; set; }              // Bloqueante | Advertencia
    public string Codigo { get; set; } = null!;       // código semántico de la alerta
    public string Descripcion { get; set; } = null!;
    public string? Detalle { get; set; }
    public bool Confirmada { get; set; }
    public int? ConfirmadaPorUserId { get; set; }
    public User? ConfirmadaPor { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
public enum TipoAlerta { Bloqueante, Advertencia }
```
Generadas durante el cálculo/cierre (ver §3.8). Una alerta `Bloqueante` no confirmada impide avanzar el cierre; las `Advertencia` son informativas. La migración que introduce esta tabla (junto con los contratos A3) es `20260612121215_AddClosureAlertasAndA3Contratos`.

### 3.4 Auditoría

#### AuditLog (RF-F01, RNF-02)
```csharp
public class AuditLog
{
    public long Id { get; set; }                      // bigserial
    public int? UserId { get; set; }                  // null si acción sistémica (sync)
    public User? User { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;     // string para soportar PKs compuestas
    public AuditAction Action { get; set; }
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Ip { get; set; }
}
public enum AuditAction { Create, Update, Delete, Login, Logout, Export, Recalc }
```
Sin soft-delete. Inmutable: solo INSERT.

#### CalculationLog (RF-D07, RNF-04)
```csharp
public class CalculationLog
{
    public int Id { get; set; }
    public int ClosureLineId { get; set; }
    public ClosureLine ClosureLine { get; set; } = null!;
    public int ConceptId { get; set; }                // snapshot del concepto evaluado
    public Concept Concept { get; set; } = null!;
    public string FormulaSnapshotJson { get; set; } = null!;  // copia completa
    public string InputsJson { get; set; } = null!;
    public decimal Resultado { get; set; }
    public string? Incidencias { get; set; }          // JSON array de {tipo, detalle}
    public string SistemaOrigen { get; set; } = null!;  // "Celero" | "Bizneo" | "Mixto" | etc.
    public DateTime Timestamp { get; set; }
}
```
Inmutable. FK 1:1 con ClosureLine (`ClosureLineId` unique).

### 3.5 Staging (una tabla por sistema externo)

Base común vía interfaz:
```csharp
public interface IStagingRow
{
    int Id { get; set; }
    string PayloadJson { get; set; }
    string Hash { get; set; }                         // SHA-256 del PayloadJson, unique
    DateTime FechaUltimaSincronizacion { get; set; }
    bool FlagProcesado { get; set; }
    string? ErrorProcesamiento { get; set; }
}
```

#### StagingCeleroVisita
```csharp
public class StagingCeleroVisita : IStagingRow
{
    public int Id { get; set; }
    public string VisitaIdExterno { get; set; } = null!;
    public int UserId { get; set; }                   // GPV que realizó la visita
    public int ProjectId { get; set; }
    public int ActionId { get; set; }
    public DateOnly Fecha { get; set; }
    public int TipoVisita { get; set; }               // 1 estándar, 2 premium
    public int PuntoMontado { get; set; }             // 0 / 1
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}
```

#### StagingBizneoEmpleado
```csharp
public class StagingBizneoEmpleado : IStagingRow
{
    public int Id { get; set; }
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }                  // match al User local por NIF/email
    public string NIF { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Departamento { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}
```

#### StagingBizneoHora
```csharp
public class StagingBizneoHora : IStagingRow
{
    public int Id { get; set; }
    public string RegistroIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Horas { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}
```

#### StagingIntratimeFichaje
```csharp
public class StagingIntratimeFichaje : IStagingRow
{
    public int Id { get; set; }
    public string FichajeIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime Entrada { get; set; }             // UTC
    public DateTime? Salida { get; set; }             // UTC
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}
```

#### StagingPayHawkGasto
```csharp
public class StagingPayHawkGasto : IStagingRow
{
    public int Id { get; set; }
    public string GastoIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public int ProjectId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Importe { get; set; }
    public string Categoria { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}
```

### 3.6 RefreshToken (auth)
```csharp
public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;    // SHA-256 del refresh token
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Ip { get; set; }
}
```

### 3.7 Relaciones — diagrama lógico (Mermaid)

```
Client (1)──(N) Service (1)──(N) Concept   (jerarquía Cliente → Servicio → Concepto)
Service (N)──(N) Concept     via ServiceConcept
Service (N)──(N) User        via ServiceUser        (ownership de servicio)
Service (N)──(N) CostCenter  via ServiceCostCenter
Service (1)──(N) Department  (FK opcional)
Concept (N)──(N) User        via ConceptUser
Concept (N)──(1) Service     (ServiceId nullable: null = concepto global)
User (N)──(N) Role           via UserRole
User (1)──(N) Department     (FK opcional)
Closure (N)──(1) Service
Closure (N)──(1) Period
Closure (1)──(N) ClosureLine
Closure (1)──(N) ClosureAlerta
ClosureLine (N)──(1) Concept
ClosureLine (N)──(1) User    (recurso opcional)
ClosureLine (1)──(1) CalculationLog
Closure (1)──(N) Approval
Closure (1)──(N) ApprovalHistory
```

---

## 4. Flujo principal numerado (end-to-end)

1. Usuario accede a `http://localhost:4200` → Angular sirve la SPA.
2. AuthGuard detecta ausencia de JWT en sessionStorage → redirige a `/login`.
3. Usuario introduce email + password → componente `LoginComponent` llama `AuthService.login(email, password)`.
4. Frontend POST `/api/auth/login` → controlador `AuthController` valida `LoginRequest` (FluentValidation), llama `IAuthService.LoginAsync`.
5. `AuthService` (Infrastructure) verifica password con `BCrypt.Verify`, genera access token (30 min, claims: nameid, email, roles) y refresh token (7 días, hash SHA-256 persistido en `RefreshToken`).
6. AuditLog grabado en MISMA transacción (`Action=Login`, ip de `HttpContext.Connection.RemoteIpAddress`).
7. Backend responde 200 `{ accessToken, refreshToken, user: { id, nombre, roles } }`.
8. Frontend guarda tokens en sessionStorage, redirige a `/dashboard` según rol (ProjectManager → vista pendientes; Administrator → admin landing).
9. Dashboard llama GET `/api/dashboard?periodId=...` con `Authorization: Bearer <accessToken>`.
10. Backend valida JWT (`JwtBearer` middleware), inyecta `ICurrentUserService` que expone `UserId` y `Roles`. `DashboardController` resuelve KPIs filtrados por ownership (RF-G01).
11. Usuario navega a Servicios → ServicesListComponent llama `ServiceService.getAll({ page, pageSize, clientId, search })` → GET `/api/services?page=1&pageSize=25`. Backend devuelve solo servicios donde el usuario tiene `ServiceUser` (si rol ProjectManager) o todos (admin/auditor).
12. Usuario abre un Closure → GET `/api/closures/{id}` devuelve cabecera + líneas + estado de aprobación.
13. Si paso del flujo = rol del usuario, aparece botón "Aprobar" y "Rechazar".
14. Aprobar → POST `/api/closures/{id}/aprobar` con header `If-Match: <rowVersion>`. Backend:
    - Verifica que el usuario tiene el rol del paso actual.
    - Actualiza `Approval.Estado=Aprobado`, `Approval.FechaDecision=UtcNow`.
    - Inserta `ApprovalHistory` (pasoOrigen → pasoDestino).
    - Crea siguiente `Approval` con `RoleId` del paso siguiente.
    - Si pasoActual=5 (SystemExports), marca `Closure.Estado=Aprobado`.
    - Todo en una sola transacción + AuditLog (`Action=Update`).
15. Llegado a `Estado=Aprobado`, aparece botón "Exportar A3 Innuva" y "Exportar A3 ERP".
16. Click descarga → GET `/api/exports/a3-innuva/{closureId}` → backend genera XML (servicio `A3InnuvaExportService`), graba AuditLog `Action=Export`, devuelve archivo con `Content-Disposition: attachment`.
17. Administrator dispara sincronización manualmente: POST `/api/sync/celero` → `SyncService` llama `ICeleroClient` (en Dev = `CeleroFakeClient` Bogus semilla 20260101), upsertea filas en `StagingCeleroVisita` con hash de idempotencia, devuelve resumen.
18. Logout: POST `/api/auth/logout` → revoca TODOS los refresh tokens del usuario, AuditLog `Action=Logout`, frontend vacía sessionStorage y navega a `/login`.

---

## 5. Definiciones técnicas completas

### 5.1 DTOs (Application/DTOs)

DTOs en `records` con validación FluentValidation separada. Solo se muestran los más relevantes; el Desarrollador genera el resto por isomorfismo.

#### Auth
```csharp
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, UsuarioBriefDto User);
public record RefreshRequest(string RefreshToken);
public record RefreshResponse(string AccessToken, string RefreshToken);
public record LogoutRequest(string? RefreshToken);
public record UsuarioBriefDto(int Id, string Nombre, string Apellidos, string Email, string[] Roles);
```

#### Client
```csharp
public record ClientListItemDto(int Id, string Nombre, string NIF, string? Ciudad, int ProjectCount);
public record ClientDetailDto(int Id, string Nombre, string NIF, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);
public record ClientCreateRequest(string Nombre, string NIF, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);
public record ClientUpdateRequest(string Nombre, string NIF, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);
```

#### Service
DTOs reales en `backend/SIG.Application/DTOs/MaestrosDtos.cs`. Service unifica los antiguos Project + Action.
```csharp
public record ServiceListItemDto(int Id, string Nombre, int ClientId, string ClientNombre, int? DepartmentId, EstadoServicio Estado);
public record ServiceDetailDto(int Id, string Nombre, int ClientId, string ClientNombre, int? DepartmentId, EstadoServicio Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds, int[] ConceptIds);
public record ServiceCreateRequest(string Nombre, int ClientId, int? DepartmentId, EstadoServicio Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds, int[] ConceptIds);
public record ServiceUpdateRequest(string Nombre, int ClientId, int? DepartmentId, EstadoServicio Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds, int[] ConceptIds);
```

#### Concept
```csharp
public record ConceptListItemDto(int Id, string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta);
public record ConceptDetailDto(int Id, string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ServiceIds, int[] UserIds);
public record ConceptCreateRequest(string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ServiceIds, int[] UserIds);
public record ConceptUpdateRequest(string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ServiceIds, int[] UserIds);
```

#### User
```csharp
public record UserListItemDto(int Id, string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, string[] Roles);
public record UserDetailDto(int Id, string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserCreateRequest(string NIF, string Nombre, string Apellidos, string Email, string Password, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserUpdateRequest(string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserPasswordChangeRequest(string NewPassword);
```

#### Period / Closure / Approval
```csharp
public record PeriodDto(int Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin, EstadoPeriodo Estado);
public record PeriodCreateRequest(string Nombre, DateOnly FechaInicio, DateOnly FechaFin);
public record PeriodUpdateRequest(string Nombre, DateOnly FechaInicio, DateOnly FechaFin);

public record ClosureListItemDto(int Id, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal CosteTotal, decimal FacturacionTotal, decimal Margen, EstadoClosure Estado, ApprovalStep PasoActual);
public record ClosureDetailDto(int Id, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal CosteTotal, decimal FacturacionTotal, decimal Margen, EstadoClosure Estado, ApprovalStep PasoActual, string? Comentarios, uint RowVersion, ClosureLineDto[] Lines, ApprovalDto[] Approvals, ClosureAlertaDto[] Alertas);
public record ClosureLineDto(int Id, int ConceptId, string ConceptNombre, int? UserId, string? UserNombre, decimal Importe, TipoConcepto Tipo, bool TieneIncidencia, uint RowVersion);
public record ClosureAlertaDto(int Id, TipoAlerta Tipo, string Codigo, string Descripcion, string? Detalle, bool Confirmada, string? ConfirmadaPorNombre, DateTime? FechaConfirmacion);
public record ClosureCreateRequest(int ServiceId, int PeriodId, string? Comentarios);
public record ClosureRecalcRequest(string? Comentarios);
public record ApprovalDto(int Id, ApprovalStep Paso, int RoleId, string RoleNombre, EstadoApproval Estado, int? UserId, string? UserNombre, string? Motivo, DateTime? FechaDecision);
public record ClosureApproveRequest(string? Comentarios);
public record ClosureRejectRequest(string Motivo);
```

#### Approval panel
```csharp
public record ApprovalFilterRequest(int? PeriodId, int? ClientId, int? CostCenterId, EstadoClosure? Estado, int? UserId, int? DepartmentId, TipoConcepto? Tipo, int? ConceptId, int Page = 1, int PageSize = 25);
public record ApprovalPanelItemDto(int ClosureId, int ServiceId, string ServiceNombre, int ClientId, string ClientNombre, int PeriodId, string PeriodNombre, EstadoClosure Estado, ApprovalStep PasoActual, string PasoActualRol, decimal Margen, DateTime UpdatedAt);
```

#### Dashboard
```csharp
public record DashboardKpisDto(int PeriodId, string PeriodNombre, int CierresCompletados, int CierresPendientes, decimal FacturacionTotal, decimal CosteTotal, decimal Margen);
public record DashboardAvisoDto(string Tipo, string Descripcion, int? EntityId);  // Tipo: "CierrePendiente" | "PeriodoBloqueado" | "ErrorSync"
public record MiServicioDto(int ServiceId, string Nombre, int ClientId, string ClientNombre, int? ClosureId, EstadoClosure? Estado, ApprovalStep? PasoActual, decimal? CosteTotal, decimal? FacturacionTotal, decimal? Margen);
```

#### CalculationLog / Sync / Audit
```csharp
public record CalculationDetailDto(int ClosureLineId, int ConceptId, string ConceptNombre, string FormulaSnapshotJson, string InputsJson, decimal Resultado, string? Incidencias, string SistemaOrigen, DateTime Timestamp);
public record SyncResultDto(string Sistema, int FilasInsertadas, int FilasDuplicadasIgnoradas, int FilasError, DateTime FechaUltimaSincronizacion);
public record AuditLogFilterRequest(int? UserId, string? EntityType, AuditAction? Action, DateOnly? Desde, DateOnly? Hasta, int Page = 1, int PageSize = 50);
public record AuditLogDto(long Id, int? UserId, string? UserNombre, string EntityType, string EntityId, AuditAction Action, string? OldValueJson, string? NewValueJson, DateTime Timestamp, string? Ip);
```

#### Paginación
```csharp
public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
```

### 5.2 Interfaces (Application/Interfaces)

#### Servicios
```csharp
public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest req, string? ip, CancellationToken ct);
    Task<RefreshResponse> RefreshAsync(RefreshRequest req, string? ip, CancellationToken ct);
    Task LogoutAsync(int userId, string? refreshToken, CancellationToken ct);
}

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    string? Ip { get; }
    bool IsInRole(string role);
    bool IsInAnyRole(params string[] roles);
}

public interface IClientService { /* CRUD methods, ver §7 endpoints */ }
public interface IServiceService { /* CRUD + ownership (unifica los antiguos IProjectService + IActionService) */ }
public interface IConceptService { /* CRUD + validación fórmula */ }
public interface IUserService { /* CRUD + cambio password */ }
public interface IRoleService { Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct); }
public interface IDepartmentService { /* CRUD */ }
public interface ICostCenterService { /* CRUD */ }
public interface IPeriodService { /* CRUD + cerrar/reabrir */ }

public interface IClosureService
{
    Task<PagedResult<ClosureListItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> GetByIdForUserAsync(int id, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> CreateAsync(ClosureCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> RecalcAsync(int closureId, ClosureRecalcRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> ApproveAsync(int closureId, ClosureApproveRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> RejectAsync(int closureId, ClosureRejectRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
}

public interface IApprovalService
{
    Task<PagedResult<ApprovalPanelItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct);
    Task<PagedResult<ApprovalPanelItemDto>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardKpisDto> GetKpisAsync(int periodId, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<DashboardAvisoDto>> GetAvisosAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<MiServicioDto>> GetMisServiciosAsync(int? periodId, int usuarioId, CancellationToken ct);
}

public interface ICalculationService
{
    Task<CalculationDetailDto> GetByClosureLineForUserAsync(int closureLineId, int usuarioId, CancellationToken ct);
}

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogFilterRequest filter, CancellationToken ct);
}

public interface ISyncService
{
    Task<SyncResultDto> SyncAsync(string sistema, CancellationToken ct);   // valida sistema ∈ { "celero","bizneo","intratime","payhawk" }
}

public interface IExportService
{
    Task<(byte[] Content, string FileName)> ExportA3InnuvaAsync(int closureId, int usuarioId, CancellationToken ct);
    Task<(byte[] Content, string FileName)> ExportA3ErpAsync(int closureId, int usuarioId, CancellationToken ct);
}

public interface ISeedService
{
    Task RunIfEmptyAsync(CancellationToken ct);     // arranque automático
    Task RegenerateAsync(CancellationToken ct);     // endpoint dev
}
```

#### Motor de cálculo
```csharp
public interface ICalculationEngine
{
    /// <summary>Evalúa la fórmula de un Concept contra un Closure y devuelve el resultado + inputs + incidencias.</summary>
    Task<CalculationResult> EvaluateAsync(Concept concept, Closure closure, CancellationToken ct);
}

public record CalculationResult(decimal Resultado, string InputsJson, string FormulaSnapshotJson, string SistemaOrigen, IReadOnlyList<CalculationIncidencia> Incidencias);
public record CalculationIncidencia(string Tipo, string Detalle);
```

#### Clientes de integración (Application/Interfaces/Integrations)
```csharp
public interface ICeleroClient
{
    Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}
public interface IBizneoClient
{
    Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<BizneoHoraDto>> GetHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}
public interface IIntratimeClient
{
    Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}
public interface IPayHawkClient
{
    Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}
// DTOs de integración: solo campos relevantes, deserializan de JSON externo o se generan en el fake.
public record CeleroVisitaDto(string VisitaIdExterno, int UserId, int ProjectId, int ActionId, DateOnly Fecha, int TipoVisita, int PuntoMontado);
public record BizneoEmpleadoDto(string EmpleadoIdExterno, string NIF, string Nombre, string? Departamento);
public record BizneoHoraDto(string RegistroIdExterno, int UserId, int ProjectId, DateOnly Fecha, decimal Horas);
public record IntratimeFichajeDto(string FichajeIdExterno, int UserId, DateTime Entrada, DateTime? Salida);
public record PayHawkGastoDto(string GastoIdExterno, int UserId, int ProjectId, DateOnly Fecha, decimal Importe, string Categoria);
```

### 5.3 Interfaces de repositorios (Application/Interfaces/Repositories)

Regla obligatoria (CLAUDE.md): firma `GetByIdAndUsuarioIdAsync(int id, int usuarioId)`. Para listados, equivalente `ListForUserAsync(int usuarioId, ...)`. Aplica filtrado de ownership (§ Sup-H03).

```csharp
public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);                             // privilegiado (admin/sistema)
    Task<User?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);  // con filtrado ownership
    Task<bool> ExistsByEmailAsync(string email, int? excludeId, CancellationToken ct);
    Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct);
    Task<PagedResult<User>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task RevokeAllByUserAsync(int userId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClientRepository
{
    Task<Client?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<PagedResult<Client>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task AddAsync(Client client, CancellationToken ct);
    Task<bool> HasServicesAsync(int clientId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

// Unifica los antiguos IProjectRepository + IActionRepository.
public interface IServiceRepository
{
    Task<Service?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<Service?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<Service>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct);
    Task AddAsync(Service service, CancellationToken ct);
    Task<bool> IsUserAssignedAsync(int serviceId, int usuarioId, CancellationToken ct);
    Task<bool> HasClosuresAsync(int serviceId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IConceptRepository
{
    Task<Concept?> GetByIdAsync(int id, CancellationToken ct);
    Task<Concept?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<PagedResult<Concept>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, TipoConcepto? tipo, string? search, CancellationToken ct);
    Task AddAsync(Concept concept, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IPeriodRepository
{
    Task<Period?> GetByIdAsync(int id, CancellationToken ct);
    Task<Period?> GetActivoAsync(CancellationToken ct);     // último Abierto
    Task<IReadOnlyList<Period>> ListAsync(CancellationToken ct);
    Task AddAsync(Period period, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClosureRepository
{
    Task<Closure?> GetByIdAsync(int id, CancellationToken ct);                          // con líneas, para motor
    Task<Closure?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<Closure?> GetByServiceAndPeriodAsync(int serviceId, int periodId, CancellationToken ct);
    Task<PagedResult<Closure>> ListPaginatedForUserAsync(int usuarioId, ApprovalFilterRequest filter, CancellationToken ct);
    Task<PagedResult<Closure>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct);
    Task AddAsync(Closure closure, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClosureLineRepository
{
    Task<ClosureLine?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task RemoveAllByClosureAsync(int closureId, CancellationToken ct);                  // usar ExecuteDeleteAsync (gotcha EF Core 9)
    Task AddRangeAsync(IEnumerable<ClosureLine> lines, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IApprovalRepository
{
    Task<Approval?> GetCurrentByClosureAsync(int closureId, CancellationToken ct);
    Task<IReadOnlyList<Approval>> ListByClosureAsync(int closureId, CancellationToken ct);
    Task AddAsync(Approval approval, CancellationToken ct);
    Task AddHistoryAsync(ApprovalHistory history, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICalculationLogRepository
{
    Task<CalculationLog?> GetByClosureLineAndUsuarioIdAsync(int closureLineId, int usuarioId, CancellationToken ct);
    Task AddAsync(CalculationLog log, CancellationToken ct);
    Task RemoveAllByClosureAsync(int closureId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLog>> ListAsync(AuditLogFilterRequest filter, CancellationToken ct);
    Task AddAsync(AuditLog log, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IStagingRepository<TStaging> where TStaging : class, IStagingRow
{
    Task<bool> ExistsByHashAsync(string hash, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<TStaging> rows, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
```

NOTA crítica sobre AsNoTracking (gotcha [Stack: EF Core 9] AsNoTracking en getters por id usados en escritura): los métodos `GetByIdAndUsuarioIdAsync` que sirven a servicios que después modifican la entidad y llaman `SaveChangesAsync` **NO llevan AsNoTracking**. Los métodos `List*Async`, conteos y proyecciones SÍ llevan AsNoTracking.

### 5.4 Controladores (API)

Firma resumida (verbo, ruta, atributos, DTO request, DTO response, códigos HTTP). El detalle completo está en §7.

Convenciones:
- Todos los controladores `[ApiController]` con `[Route("api/[controller]")]`.
- `[Authorize]` por defecto a nivel de controller. `[AllowAnonymous]` solo en `/auth/login` y `/auth/refresh`.
- Endpoints con políticas por rol: `[Authorize(Roles = "Administrator")]`, `[Authorize(Roles = "Administrator,Direction,Fico,Backoffice,ProjectManager,Auditor,Reader")]`, etc.
- `[ProducesResponseType(typeof(T), 200)]` en cada acción.
- Errores: middleware global produce `ProblemDetails` con `extensions.code` (RNF-06).
- UserId del JWT vía `ICurrentUserService.UserId`. PROHIBIDO leerlo del body (RNF-05).

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous] [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    [ProducesResponseType(typeof(ValidationProblemDetails), 400)]
    public Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct);

    [AllowAnonymous] [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), 200)]
    [ProducesResponseType(typeof(ProblemDetails), 401)]
    public Task<ActionResult<RefreshResponse>> Refresh(RefreshRequest req, CancellationToken ct);

    [Authorize] [HttpPost("logout")]
    [ProducesResponseType(204)]
    public Task<IActionResult> Logout(LogoutRequest req, CancellationToken ct);

    [Authorize] [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioBriefDto), 200)]
    public Task<ActionResult<UsuarioBriefDto>> Me(CancellationToken ct);
}
```

(Estructura análoga para `ClientsController`, `ServicesController` (`api/services`, sustituye a los antiguos `ProjectsController` + `ActionsController`), `ConceptsController`, `UsersController`, `RolesController`, `DepartmentsController`, `CostCentersController`, `PeriodsController`, `ClosuresController`, `ApprovalsController`, `DashboardController`, `CalculationsController`, `AuditController`, `SyncController`, `ExportsController`, `DevController`. La tabla completa con verbos y DTOs en §7.)

### 5.5 Jerarquía de excepciones (Domain/Exceptions)

```csharp
public abstract class DomainException : Exception
{
    public abstract string Code { get; }              // expuesto en ProblemDetails.extensions.code
    public abstract int HttpStatusCode { get; }
    protected DomainException(string message) : base(message) { }
}

public sealed class EntityNotFoundException : DomainException
{
    public override string Code => "entity_not_found";
    public override int HttpStatusCode => 404;
    public EntityNotFoundException(string entity, object id) : base($"{entity} con id {id} no encontrado.") { }
}

public sealed class NotOwnerException : DomainException
{
    public override string Code => "not_owner";
    public override int HttpStatusCode => 403;
    public NotOwnerException() : base("El usuario no tiene acceso a esta entidad.") { }
}

public sealed class ConcurrencyConflictException : DomainException
{
    public override string Code => "concurrency_conflict";
    public override int HttpStatusCode => 412;
    public ConcurrencyConflictException() : base("La entidad fue modificada por otra operación. Recarga y reintenta.") { }
}

public sealed class PeriodClosedException : DomainException
{
    public override string Code => "period_closed";
    public override int HttpStatusCode => 409;
    public PeriodClosedException(string periodName) : base($"El período {periodName} está cerrado o bloqueado.") { }
}

public sealed class InvalidApprovalTransitionException : DomainException
{
    public override string Code => "invalid_approval_transition";
    public override int HttpStatusCode => 409;
    public InvalidApprovalTransitionException(string detalle) : base(detalle) { }
}

public sealed class FormulaInvalidException : DomainException
{
    public override string Code => "formula_invalid";
    public override int HttpStatusCode => 400;
    public FormulaInvalidException(string detalle) : base(detalle) { }
}

public sealed class DuplicateException : DomainException
{
    public override string Code => "duplicate";
    public override int HttpStatusCode => 409;
    public DuplicateException(string detalle) : base(detalle) { }
}

public sealed class DependenciesExistException : DomainException
{
    public override string Code => "dependencies_exist";
    public override int HttpStatusCode => 409;
    public DependenciesExistException(int count) : base($"No se puede eliminar: existen {count} dependencias.") { }
}

public sealed class ClosureNotApprovedException : DomainException
{
    public override string Code => "closure_not_approved";
    public override int HttpStatusCode => 409;
    public ClosureNotApprovedException() : base("Solo se pueden exportar cierres en estado Aprobado.") { }
}

public sealed class IntegrationException : DomainException
{
    public override string Code => "integration_error";
    public override int HttpStatusCode => 502;
    public IntegrationException(string sistema, string detalle) : base($"Error en sistema externo {sistema}: {detalle}") { }
}
```

Middleware `ExceptionHandlingMiddleware`:
```csharp
public class ExceptionHandlingMiddleware
{
    public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
    {
        try { await next(ctx); }
        catch (DomainException dex)
        {
            ctx.Response.StatusCode = dex.HttpStatusCode;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = dex.HttpStatusCode,
                Title = dex.Message,
                Extensions = { ["code"] = dex.Code }
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            // Mapeo automático del concurrency conflict de EF
            ctx.Response.StatusCode = 412;
            await ctx.Response.WriteAsJsonAsync(new ProblemDetails { Status = 412, Title = "Concurrency conflict", Extensions = { ["code"] = "concurrency_conflict" } });
        }
        // (...) catch genérico → 500 con code="internal_error"
    }
}
```

### 5.6 Modelos y servicios Angular (TypeScript)

Interfaces TypeScript espejo de los DTOs C# (sección 5.1). El Frontend los genera a partir de los records C# por isomorfismo; aquí se documentan los más relevantes para garantizar cobertura.

#### Auth
```typescript
export interface LoginRequest { email: string; password: string; }
export interface LoginResponse { accessToken: string; refreshToken: string; user: UsuarioBrief; }
export interface UsuarioBrief { id: number; nombre: string; apellidos: string; email: string; roles: string[]; }
```

#### Dashboard
```typescript
export interface DashboardKpis { periodId: number; periodNombre: string; cierresCompletados: number; cierresPendientes: number; facturacionTotal: number; costeTotal: number; margen: number; }
export interface DashboardAviso { tipo: 'CierrePendiente' | 'PeriodoBloqueado' | 'ErrorSync'; descripcion: string; entityId?: number; }
export interface MiServicio { serviceId: number; nombre: string; clientId: number; clientNombre: string; closureId?: number; estado?: EstadoClosure; pasoActual?: ApprovalStep; costeTotal?: number; facturacionTotal?: number; margen?: number; }
```

#### Client
```typescript
export interface ClientListItem { id: number; nombre: string; nif: string; ciudad?: string; projectCount: number; }
export interface ClientDetail { id: number; nombre: string; nif: string; direccion?: string; ciudad?: string; provincia?: string; pais?: string; codigoPostal?: string; contactoNombre?: string; contactoEmail?: string; contactoTelefono?: string; }
export interface ClientCreate { nombre: string; nif: string; direccion?: string; ciudad?: string; provincia?: string; pais?: string; codigoPostal?: string; contactoNombre?: string; contactoEmail?: string; contactoTelefono?: string; }
```

#### Service / Concept
```typescript
export interface ServiceListItem { id: number; nombre: string; clientId: number; clientNombre: string; departmentId?: number; estado: EstadoServicio; }
export interface ServiceDetail { id: number; nombre: string; clientId: number; clientNombre: string; departmentId?: number; estado: EstadoServicio; interlocutorNombre?: string; interlocutorEmail?: string; interlocutorTelefono?: string; fechaAlta: string; costCenterIds: number[]; userIds: number[]; conceptIds: number[]; }
export interface ConceptDetail { id: number; nombre: string; tipo: TipoConcepto; fechaDesde: string; fechaHasta?: string; formulaJson: string; serviceIds: number[]; userIds: number[]; }
```

#### Period / Closure / Approval
```typescript
export interface PeriodDto { id: number; nombre: string; fechaInicio: string; fechaFin: string; estado: EstadoPeriodo; }
export interface ClosureListItem { id: number; serviceId: number; serviceNombre: string; periodId: number; periodNombre: string; costeTotal: number; facturacionTotal: number; margen: number; estado: EstadoClosure; pasoActual: ApprovalStep; }
export interface ClosureDetail { id: number; serviceId: number; serviceNombre: string; periodId: number; periodNombre: string; costeTotal: number; facturacionTotal: number; margen: number; estado: EstadoClosure; pasoActual: ApprovalStep; comentarios?: string; rowVersion: number; lines: ClosureLine[]; approvals: ApprovalDto[]; alertas: ClosureAlerta[]; }
export interface ClosureAlerta { id: number; tipo: TipoAlerta; codigo: string; descripcion: string; detalle?: string; confirmada: boolean; confirmadaPorNombre?: string; fechaConfirmacion?: string; }
export interface ClosureLine { id: number; conceptId: number; conceptNombre: string; userId?: number; userNombre?: string; importe: number; tipo: TipoConcepto; tieneIncidencia: boolean; rowVersion: number; }
export interface ApprovalPanelItem { closureId: number; serviceId: number; serviceNombre: string; clientId: number; clientNombre: string; periodId: number; periodNombre: string; estado: EstadoClosure; pasoActual: ApprovalStep; pasoActualRol: string; margen: number; updatedAt: string; }
```

#### Enums (string)
```typescript
export type EstadoPeriodo = 'Abierto' | 'Cerrado' | 'Bloqueado';
export type EstadoClosure = 'Borrador' | 'Calculado' | 'EnRevision' | 'AprobadoFico' | 'AprobadoDir' | 'Cerrado';
export type ApprovalStep = 'Calculo' | 'Gestion' | 'Backoffice' | 'Fico' | 'Direccion';
export type EstadoServicio = 'Activo' | 'Inactivo';
export type TipoConcepto = 'Pago' | 'Factura';
export type TipoAlerta = 'Bloqueante' | 'Advertencia';
export type AuditAction = 'Crear' | 'Actualizar' | 'Eliminar' | 'Aprobar' | 'Rechazar';
```

#### Servicios Angular (inyectables)
```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  login(req: LoginRequest): Observable<LoginResponse>;
  refresh(req: RefreshRequest): Observable<RefreshResponse>;
  logout(): Observable<void>;
  me(): Observable<UsuarioBrief>;
}

@Injectable({ providedIn: 'root' })
export class ClientsService {
  getAll(query?: { page?: number; pageSize?: number; search?: string }): Observable<PagedResult<ClientListItem>>;
  getById(id: number): Observable<ClientDetail>;
  create(req: ClientCreate): Observable<ClientDetail>;
  update(id: number, req: ClientCreate): Observable<ClientDetail>;
  delete(id: number): Observable<void>;
}

export interface PagedResult<T> { items: T[]; total: number; page: number; pageSize: number; }
```

El resto de servicios (ServiceService — `core/api/services.service.ts`, sustituye a los antiguos ProjectsService + ActionsService —, ConceptsService, UsersService, PeriodsService, ClosuresService, ApprovalsService, DashboardService, AuditService, SyncService, ExportsService) siguen el mismo patrón con métodos `getAll/getById/create/update/delete` según corresponda. El Frontend los implementa con `HttpClient` tipado + interceptor JWT.

---

## 6. Motor de cálculo (sección dedicada — crítico)

### 6.1 Primitivas y operaciones

Soportadas en el AST JSON (ver §SUP-D01):

| Tipo nodo | Atributos | Semántica |
|---|---|---|
| `Number` | `value: number` | Constante decimal. |
| `Variable` | `variableId: int` | Resuelve `Variable.MapeoValoresJson` contra la respuesta de Celero con `questionIdExterno`. |
| `Source` | `entity`, `field?`, `filters[]` | Apunta a una fuente: `GastosPayHawk`, `VisitasCelero`, `HorasBizneo`, `HorasIntratime`. `field` opcional (`importe`, `horas`). |
| `Aggregate` | `op` (`Sum`/`Count`/`Min`/`Max`), `source: Source`, `field?` | Agregación sobre las filas resultantes. |
| `BinaryOp` | `op` (`Add`/`Sub`/`Mul`/`Div`/`Pct`), `left`, `right` | Operación binaria. `Pct` calcula `left * (1 + right/100)`. |

### 6.2 Filtros disponibles

Cada `Source.filters[]` es una lista de predicados aplicados con AND:

```json
{ "field": "<nombreCampo>", "op": "Eq|Neq|Gt|Gte|Lt|Lte|In", "value": <constante> }
```

Filtros válidos por entidad origen:
- `GastosPayHawk`: `Fecha`, `UserId`, `ProjectId`, `Importe`, `Categoria`.
- `VisitasCelero`: `Fecha`, `UserId`, `ProjectId`, `ActionId`, `TipoVisita`, `PuntoMontado`.
- `HorasBizneo`: `Fecha`, `UserId`, `ProjectId`, `Horas`.
- `HorasIntratime`: `Entrada`, `UserId`.

Filtros implícitos siempre aplicados por el motor (NO escritos en el JSON):
- Período del Closure: `Fecha >= Closure.Period.FechaInicio AND Fecha <= Closure.Period.FechaFin`.
- Servicio del Closure: el motor filtra las filas staging por el servicio del cierre (`Closure.ServiceId`) usando el campo externo `project`/`ProjectId` que esos sistemas ajenos exponen. (El campo externo NO se renombra; solo la entidad interna Project desapareció.)
- Si la línea es por recurso (`ClosureLine.UserId` se está calculando): `UserId == ClosureLine.UserId`.

### 6.3 Ejemplos JSON

**Suma de gastos directos** (`Suma(GastosPayHawk.importe)`):
```json
{
  "type": "Aggregate",
  "op": "Sum",
  "field": "Importe",
  "source": { "type": "Source", "entity": "GastosPayHawk", "filters": [] }
}
```

**Bonus por visita estándar** (`Cuenta(VisitasCelero TipoVisita=1) × 5`):
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Count",
    "source": {
      "type": "Source",
      "entity": "VisitasCelero",
      "filters": [ { "field": "TipoVisita", "op": "Eq", "value": 1 } ]
    }
  },
  "right": { "type": "Number", "value": 5 }
}
```

**Pago por horas con variable TarifaHora** (`Suma(HorasBizneo.horas) × Variable.TarifaHora`):
```json
{
  "type": "BinaryOp", "op": "Mul",
  "left": { "type": "Aggregate", "op": "Sum", "field": "Horas", "source": { "type": "Source", "entity": "HorasBizneo", "filters": [] } },
  "right": { "type": "Variable", "variableId": 4 }
}
```

**Refacturación gastos +15%** (`Suma(GastosPayHawk.importe) × 1.15` o equivalente `+ 15%`):
```json
{ "type": "BinaryOp", "op": "Pct",
  "left": { "type": "Aggregate", "op": "Sum", "field": "Importe", "source": { "type": "Source", "entity": "GastosPayHawk", "filters": [] } },
  "right": { "type": "Number", "value": 15 } }
```

### 6.4 Evaluación en C# (algoritmo)

`Application/Calculation/CalculationEngine.cs`:

```csharp
public class CalculationEngine : ICalculationEngine
{
    private readonly IFormulaParser _parser;          // System.Text.Json
    private readonly ICalculationDataLoader _loader;  // carga datasets por Closure (cache in-memory)
    private readonly IVariableResolver _varResolver;

    public async Task<CalculationResult> EvaluateAsync(Concept concept, Closure closure, CancellationToken ct)
    {
        var ast = _parser.Parse(concept.FormulaJson);                          // FormulaInvalidException si parse falla
        var ctx = await _loader.LoadAsync(closure, ct);                        // VisitasCelero, GastosPayHawk, HorasBizneo, HorasIntratime filtrados por período+proyecto
        var incidencias = new List<CalculationIncidencia>();
        var resultado = EvaluateNode(ast, ctx, closure, concept, incidencias); // recursivo
        var inputsJson = JsonSerializer.Serialize(ctx.UsedInputs);
        var sistemaOrigen = ctx.DetectSistemaOrigen();                         // "Celero" si solo se usó VisitasCelero, etc.
        return new CalculationResult(Math.Round(resultado, 2), inputsJson, concept.FormulaJson, sistemaOrigen, incidencias);
    }

    private decimal EvaluateNode(FormulaNode node, CalculationContext ctx, Closure closure, Concept concept, List<CalculationIncidencia> inc) =>
        node switch
        {
            NumberNode n => (decimal)n.Value,
            VariableNode v => _varResolver.Resolve(v.VariableId, ctx),
            AggregateNode a => Aggregate(a, ctx, closure, inc),
            BinaryOpNode b => ApplyBinary(b.Op, EvaluateNode(b.Left, ctx, closure, concept, inc), EvaluateNode(b.Right, ctx, closure, concept, inc), inc),
            _ => throw new FormulaInvalidException($"Tipo de nodo desconocido: {node.GetType().Name}")
        };

    private decimal Aggregate(AggregateNode a, CalculationContext ctx, Closure closure, List<CalculationIncidencia> inc)
    {
        var rows = ctx.FilteredRows(a.Source, closure);                        // implícitos + explícitos
        if (rows.Count == 0)
        {
            inc.Add(new CalculationIncidencia("EmptyDataset", $"Sin datos para {a.Source.Entity} en el período."));
            return 0m;
        }
        return a.Op switch
        {
            "Sum" => a.Field is null ? rows.Count : rows.Sum(r => r.GetDecimal(a.Field)),
            "Count" => rows.Count,
            "Min" => rows.Min(r => r.GetDecimal(a.Field!)),
            "Max" => rows.Max(r => r.GetDecimal(a.Field!)),
            _ => throw new FormulaInvalidException($"Operación de agregación desconocida: {a.Op}")
        };
    }

    private decimal ApplyBinary(string op, decimal l, decimal r, List<CalculationIncidencia> inc) => op switch
    {
        "Add" => l + r,
        "Sub" => l - r,
        "Mul" => l * r,
        "Div" => r == 0 ? Incidente(inc, "DivisionByZero", "División por cero") : l / r,
        "Pct" => l * (1 + r / 100),
        _ => throw new FormulaInvalidException($"Operación binaria desconocida: {op}")
    };

    private static decimal Incidente(List<CalculationIncidencia> inc, string tipo, string detalle) { inc.Add(new CalculationIncidencia(tipo, detalle)); return 0; }
}
```

### 6.5 Snapshot de fórmula y reproducibilidad

Una vez evaluado, el motor persiste:

```csharp
var line = new ClosureLine {
    ClosureId = closure.Id, ConceptId = concept.Id, UserId = recursoId,
    Importe = result.Resultado, DatosEntradaJson = result.InputsJson,
    Tipo = concept.Tipo, TieneIncidencia = result.Incidencias.Any()
};
await _lineRepo.AddAsync(line, ct);
await _lineRepo.SaveChangesAsync(ct);                                 // genera Id

await _calcLogRepo.AddAsync(new CalculationLog {
    ClosureLineId = line.Id, ConceptId = concept.Id,
    FormulaSnapshotJson = result.FormulaSnapshotJson,                 // ¡copia completa, no FK!
    InputsJson = result.InputsJson,
    Resultado = result.Resultado,
    Incidencias = result.Incidencias.Any() ? JsonSerializer.Serialize(result.Incidencias) : null,
    SistemaOrigen = result.SistemaOrigen,
    Timestamp = DateTime.UtcNow
}, ct);
```

Editar `Concept.FormulaJson` después NO afecta a los `CalculationLog` antiguos: el snapshot persiste la fórmula tal como se evaluó (cumple decisión vinculante INPUT_APP §6).

### 6.6 Rendimiento (RNF-01)

- El `ICalculationDataLoader` carga los datasets (`StagingPayHawkGasto`, `StagingCeleroVisita`, etc.) una sola vez por `Closure` y los mantiene en memoria durante toda la evaluación.
- Las filas se filtran en memoria con LINQ-to-Objects (no se traduce a SQL por línea).
- Para 1000 líneas, complejidad O(L × D) donde L=líneas y D=tamaño dataset filtrado (máx ~600 visitas). Estimado <5 segundos en hardware desarrollo (cumple RNF-01 con margen).

---

## 7. Tabla de endpoints

Códigos HTTP estándar: 200 OK / 201 Created / 204 NoContent / 400 ValidationProblem / 401 Unauthorized / 403 Forbidden (NotOwner) / 404 NotFound / 409 Conflict (transición/dependencias/period closed/closure not approved) / 412 PreconditionFailed (concurrency) / 500 InternalServerError.

| # | Método | Ruta | Auth (Roles) | DTO Request | DTO Response | Validaciones clave | Códigos |
|---|---|---|---|---|---|---|---|
| 1 | POST | /api/auth/login | Anonymous | LoginRequest | LoginResponse | Email obligatorio formato email; Password obligatorio min 8 | 200/400/401 |
| 2 | POST | /api/auth/refresh | Anonymous | RefreshRequest | RefreshResponse | RefreshToken obligatorio | 200/401 |
| 3 | POST | /api/auth/logout | Authenticated | LogoutRequest | — | — | 204/401 |
| 4 | GET | /api/auth/me | Authenticated | — | UsuarioBriefDto | — | 200/401 |
| 5 | GET | /api/clients | Administrator,Direction,Fico,Backoffice,ProjectManager,Auditor,Reader | query: page,pageSize,search | PagedResult<ClientListItemDto> | page>=1, pageSize 1-100 | 200/401 |
| 6 | GET | /api/clients/{id} | Idem | — | ClientDetailDto | id>0 | 200/401/403/404 |
| 7 | POST | /api/clients | Administrator | ClientCreateRequest | ClientDetailDto | Nombre 2-200; NIF regex española; Email formato | 201/400/401/403/409 |
| 8 | PUT | /api/clients/{id} | Administrator | ClientUpdateRequest | ClientDetailDto | Idem POST | 200/400/401/403/404/409 |
| 9 | DELETE | /api/clients/{id} | Administrator | — | — | sin Services activos | 204/401/403/404/409 |
| 10 | GET | /api/services | All roles | query: page,pageSize,clientId,search | PagedResult<ServiceListItemDto> | filtrado ownership | 200/401 |
| 11 | GET | /api/services/{id} | All roles | — | ServiceDetailDto | ownership | 200/401/403/404 |
| 12 | POST | /api/services | Administrator,Backoffice | ServiceCreateRequest | ServiceDetailDto | Nombre 2-200; ClientId existe; FechaAlta válida; CostCenterIds/UserIds/ConceptIds existen | 201/400/401/403 |
| 13 | PUT | /api/services/{id} | Administrator,Backoffice | ServiceUpdateRequest | ServiceDetailDto | Idem | 200/400/401/403/404 |
| 14 | DELETE | /api/services/{id} | Administrator | — | — | sin Closures asociados | 204/401/403/404/409 |
| 15-19 | — | *(antiguos `/api/actions` eliminados)* | — | — | — | el CRUD de Action se consolidó en `/api/services` (RF-C02); Tarifas/Presupuestos cuelgan de `api/services/{serviceId}/...` | — |
| 20 | GET | /api/concepts | All roles | query: page,pageSize,tipo,search | PagedResult<ConceptListItemDto> | — | 200/401 |
| 21 | GET | /api/concepts/{id} | All roles | — | ConceptDetailDto | — | 200/401/404 |
| 22 | POST | /api/concepts | Administrator,Backoffice | ConceptCreateRequest | ConceptDetailDto | Nombre; Tipo válido; FechaDesde<=FechaHasta; FormulaJson parseable | 201/400/401/403 |
| 23 | PUT | /api/concepts/{id} | Administrator,Backoffice | ConceptUpdateRequest | ConceptDetailDto | Idem; FormulaJson valida AST | 200/400/401/403/404 |
| 24 | DELETE | /api/concepts/{id} | Administrator | — | — | sin Services que lo referencien | 204/401/403/404/409 |
| 25 | POST | /api/concepts/{id}/validar-formula | Administrator,Backoffice | { formulaJson: string } | { ok: bool, errores: string[] } | parsea sin ejecutar | 200/400/401/403/404 |
| 26 | GET | /api/users | Administrator,Auditor | query: page,pageSize,search | PagedResult<UserListItemDto> | — | 200/401/403 |
| 27 | GET | /api/users/{id} | Administrator,Auditor + selfMe | — | UserDetailDto | — | 200/401/403/404 |
| 28 | POST | /api/users | Administrator | UserCreateRequest | UserDetailDto | NIF español; Email formato; Password>=8; Roles existen | 201/400/401/403/409 |
| 29 | PUT | /api/users/{id} | Administrator | UserUpdateRequest | UserDetailDto | Idem sin password | 200/400/401/403/404/409 |
| 30 | PUT | /api/users/{id}/password | Administrator + selfMe | UserPasswordChangeRequest | — | NewPassword>=8 | 204/400/401/403/404 |
| 31 | DELETE | /api/users/{id} | Administrator | — | — | sin Closures como recurso | 204/401/403/404/409 |
| 32 | GET | /api/roles | Administrator,Auditor | — | RoleDto[] | — | 200/401/403 |
| 33 | GET | /api/departments | All roles | — | DepartmentDto[] | — | 200/401 |
| 34 | POST | /api/departments | Administrator | DepartmentCreateRequest | DepartmentDto | Nombre 2-100 | 201/400/401/403 |
| 35 | PUT | /api/departments/{id} | Administrator | DepartmentUpdateRequest | DepartmentDto | Nombre | 200/400/401/403/404 |
| 36 | DELETE | /api/departments/{id} | Administrator | — | — | sin Users ni Services | 204/401/403/404/409 |
| 37 | GET | /api/costcenters | All roles | — | CostCenterDto[] | — | 200/401 |
| 38 | POST | /api/costcenters | Administrator | CostCenterCreateRequest | CostCenterDto | Codigo regex `^\d{6}$`; Nombre | 201/400/401/403/409 |
| 39 | PUT | /api/costcenters/{id} | Administrator | CostCenterUpdateRequest | CostCenterDto | Idem | 200/400/401/403/404/409 |
| 40 | DELETE | /api/costcenters/{id} | Administrator | — | — | sin Services | 204/401/403/404/409 |
| 41 | GET | /api/periods | All roles | — | PeriodDto[] | — | 200/401 |
| 42 | GET | /api/periods/activo | All roles | — | PeriodDto | — | 200/401/404 |
| 43 | POST | /api/periods | Administrator | PeriodCreateRequest | PeriodDto | Nombre único; FechaInicio<=FechaFin | 201/400/401/403/409 |
| 44 | PUT | /api/periods/{id} | Administrator | PeriodUpdateRequest | PeriodDto | Idem | 200/400/401/403/404 |
| 45 | POST | /api/periods/{id}/cerrar | Administrator | — | PeriodDto | Estado actual Abierto; sin Approvals pendientes | 200/401/403/404/409 |
| 46 | POST | /api/periods/{id}/reabrir | Administrator | — | PeriodDto | Estado actual Cerrado | 200/401/403/404/409 |
| 47 | GET | /api/dashboard | All roles | query: periodId? | DashboardKpisDto | — | 200/401 |
| 48 | GET | /api/dashboard/avisos | All roles | — | DashboardAvisoDto[] | — | 200/401 |
| 49 | GET | /api/dashboard/mis-proyectos | All roles | query: periodId? | MiServicioDto[] | — | 200/401 |
| 50 | GET | /api/closures | All roles | query: ApprovalFilterRequest | PagedResult<ClosureListItemDto> | ownership | 200/401 |
| 51 | GET | /api/closures/{id} | All roles | — | ClosureDetailDto | ownership | 200/401/403/404 |
| 52 | POST | /api/closures | ProjectManager,Backoffice,Administrator | ClosureCreateRequest | ClosureDetailDto | Period.Estado=Abierto; Service visible al user; no existe ya Closure(Service,Period) | 201/400/401/403/409 |
| 53 | POST | /api/closures/{id}/recalcular | ProjectManager,Backoffice,Administrator | ClosureRecalcRequest, If-Match | ClosureDetailDto | Period.Estado=Abierto; estado=Borrador o Rechazado | 200/400/401/403/404/409/412 |
| 54 | POST | /api/closures/{id}/aprobar | ProjectManager,Backoffice,Fico,Direction (según paso actual) | ClosureApproveRequest, If-Match | ClosureDetailDto | rol del usuario coincide con paso actual | 200/400/401/403/404/409/412 |
| 55 | POST | /api/closures/{id}/rechazar | Backoffice,Fico,Direction (según paso actual) | ClosureRejectRequest, If-Match | ClosureDetailDto | Motivo obligatorio; transición válida | 200/400/401/403/404/409/412 |
| 56 | GET | /api/approvals | All roles | query: ApprovalFilterRequest | PagedResult<ApprovalPanelItemDto> | — | 200/401 |
| 57 | GET | /api/approvals/pendientes | All roles | query: page,pageSize | PagedResult<ApprovalPanelItemDto> | rol del usuario | 200/401 |
| 58 | GET | /api/approvals/historial/{closureId} | All roles | — | ApprovalHistoryDto[] | ownership | 200/401/403/404 |
| 59 | GET | /api/calculations/{closureLineId} | All roles | — | CalculationDetailDto | ownership | 200/401/403/404 |
| 60 | GET | /api/audit | Administrator,Auditor | query: AuditLogFilterRequest | PagedResult<AuditLogDto> | — | 200/401/403 |
| 61 | POST | /api/sync/{system} | Administrator | — (system ∈ celero/bizneo/intratime/payhawk) | SyncResultDto | system válido; idempotencia hash | 200/400/401/403/502 |
| 62 | GET | /api/exports/a3-innuva/{closureId} | Administrator,Fico,Direction | — | FileContentResult application/xml | Closure.Estado=Aprobado | 200/401/403/404/409 |
| 63 | GET | /api/exports/a3-erp/{closureId} | Administrator,Fico,Direction | — | FileContentResult application/xml | Closure.Estado=Aprobado | 200/401/403/404/409 |
| 64 | POST | /api/dev/regenerar-seed | Administrator + IHostEnvironment Dev/Test/E2E + Features:AllowSeedRegeneration=true | — | { ok: bool, mensaje: string } | guards de entorno | 200/401/403/404 |
| 65 | GET | /api/health | Anonymous | — | { status: "ok", version } | — | 200 |

Subtotal: **65 endpoints** documentados con firma completa (verbo + ruta + auth + DTOs + validaciones + códigos).

Adicionales implícitos por simetría CRUD que el Desarrollador implementará si los necesita (no contabilizados en CS/GS porque son derivados):
- GET `/api/variables`, POST, PUT, DELETE — análogos a Concepts (sin fórmula).
- Total real con variables: **69 endpoints**. Para métricas CS/GS se contabilizan los 65 explícitos.

---

## 8. Análisis arquitectónico

### 8.1 Estilos considerados

**Opción A — Clean Architecture monolítica (Domain / Application / Infrastructure / API)**
- Pros: separación estricta, testable, sin acoplamiento a EF en Application, cumple regla CLAUDE.md. Encaja con el equipo .NET y con la matriz de NUGET_VERSIONS.md. Despliegue simple (un binario + Angular static).
- Contras: cuatro proyectos para un MVP puede sentirse over-engineered; sin embargo, la regla del sistema multiagente lo exige.

**Opción B — Vertical Slice Architecture (feature folders + MediatR)**
- Pros: organiza por feature (CrearClosure, AprobarClosure, …), reduce ceremonial. Excelente con CQRS.
- Contras: viola la regla CLAUDE.md "separación estricta Domain/Application/Infrastructure/API". Añade dependencia MediatR sin clara ventaja en este alcance (poco share entre features). Penaliza la legibilidad para un equipo no familiarizado.

**Opción C — Microservicios (auth / cierres / motor / sync / exports)**
- Pros: aislamiento, escala independiente.
- Contras: prematuro para MVP mono-tenant con un solo cliente. Multiplica la complejidad de despliegue, observabilidad, transacciones distribuidas. La transaccionalidad de `AuditLog` (RNF-02) y `Closure+Approval+ApprovalHistory` se vuelve enormemente más compleja entre servicios. Descartado.

### 8.2 Selección

**Opción A — Clean Architecture monolítica**, alineada con CLAUDE.md y con el resto del sistema multiagente. Cuatro proyectos: `SIG.Domain`, `SIG.Application`, `SIG.Infrastructure`, `SIG.API`. Tests en proyecto separado `SIG.Tests` referenciando los anteriores.

---

## 9. Decisiones técnicas clave

| Decisión | Valor | Justificación / Gotcha aplicado |
|---|---|---|
| Stack DB | PostgreSQL 16 + `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4 | Vinculante cliente. NUGET_VERSIONS.md sección PostgreSQL. |
| Stack auth | JWT propio + BCrypt (BCrypt.Net-Next 4.0.3, System.IdentityModel.Tokens.Jwt 8.4.0, JwtBearer 9.0.4) | Vinculante cliente. |
| EF Core | 9.0.4 pinned EXPLÍCITAMENTE en API y Tests | Gotcha JwtBearer trae EF Core 9.0.1 transitivo. Pinear Microsoft.EntityFrameworkCore Y Microsoft.EntityFrameworkCore.Relational en Tests. |
| EF Design | 9.0.4 en API (startup-project) E Infrastructure (migrations) | Gotcha dotnet ef migrations requiere Design en startup-project. |
| Migrations | En Infrastructure, `MigrationsAssembly("SIG.Infrastructure")` en `UseNpgsql` | Gotcha Database.Migrate() no aplica si startup ≠ migrations. |
| Database.Migrate() | Llamada en Program.cs con `using Microsoft.EntityFrameworkCore;` explícito | Gotcha CS1061 si falta el using. |
| Naming convention | `UseSnakeCaseNamingConvention()` (paquete EFCore.NamingConventions 9.0.0) | Convención PostgreSQL. Nombres lower_snake_case en BD; PascalCase en C#. |
| RowVersion | `xmin` PostgreSQL como concurrency token en Closure y ClosureLine | Patrón Npgsql estándar. |
| Fechas | `DateOnly` para fechas puras; `DateTime.UtcNow` con `Kind=Utc` para timestamps | Gotcha Npgsql rechaza DateTime con Kind distinto de Utc. NUNCA `DateTime.Now` ni `new DateTime(...)` sin SpecifyKind. |
| AsNoTracking | Solo en listados y proyecciones. NUNCA en `GetByIdAndUsuarioIdAsync` usado para escribir | Gotcha AsNoTracking en getters de escritura → SaveChangesAsync no-op. |
| Borrado de líneas | `ExecuteDeleteAsync()` en lugar de `RemoveRange` | Gotcha RemoveRange sobre entidades AsNoTracking. |
| LINQ Contains de enums | Disyunción explícita `||` en `Where` para conjuntos cerrados de enums | Gotcha EF Core 9 + .NET 10 array.Contains de enums. |
| AuditLog | `SaveChangesInterceptor` ejecuta antes del SaveChanges → garantía transaccional (RNF-02) | Misma transacción que la operación auditada. |
| Soft delete | Global Query Filter en entidades con `IsDeleted`; opcional ignorar con `IgnoreQueryFilters()` | Cumple RF-G02. |
| OpenAPI | Swashbuckle.AspNetCore (Swagger). ELIMINAR `Microsoft.AspNetCore.OpenApi` del csproj API | Gotcha dotnet new webapi incluye OpenApi 10.0.x con EF Core 9.0.1 transitivo. |
| FluentValidation | DependencyInjectionExtensions + filtro global `ValidationFilter` (IAsyncActionFilter) registrado en `AddControllers(o => o.Filters.Add<ValidationFilter>())` | Gotcha FluentValidation registrado pero no conectado al pipeline. AddValidatorsFromAssemblyContaining<AlgunValidatorDeApplication>(). |
| ProblemDetails | Middleware global `ExceptionHandlingMiddleware` con `extensions["code"]` semántico | RNF-06. |
| JsonStringEnumConverter | Configurado en `builder.Services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` | Regla del sistema. Frontend debe usar enums TS como strings alineados con los nombres C# (gotcha enums numéricos TS). |
| JWT claim userId | Leer con `User.FindFirst(ClaimTypes.NameIdentifier)?.Value`, NO `"sub"` | Gotcha User.FindFirst("sub") devuelve null. |
| Refresh token | SHA-256 hash persistido. Plain token solo en respuesta. | Seguridad estándar. |
| Puerto backend | 5180 HTTP / 5181 HTTPS, fijado en `launchSettings.json` | Gotcha puerto no predecible. |
| Program.cs | Termina con `public partial class Program { }` | Regla CLAUDE.md (necesario para WebApplicationFactory en tests). |
| BackgroundService | NO se usa en MVP (no hay jobs schedulados). Si Fase 2 lo requiere → paquete `Microsoft.Extensions.Hosting.Abstractions` 9.0.4 en Infrastructure. | Gotcha BackgroundService no compila sin paquete. |
| .gitignore | NO excluir `appsettings.Development.json`, `appsettings.Testing.json`, `appsettings.E2E.json`. SÍ excluir `appsettings.Production.json` y `appsettings.Local.json`. | Gotcha .NET 10 appsettings en .gitignore por error. |
| Theming Angular | Material 21 `mat.theme(...)` con `mat.$azure-palette` + overrides CSS `--mat-sys-*` con paleta SIG navy | Gotcha Material 21 sin estilos por brand-family. Theme-first antes de features. |
| Animations | `provideAnimationsAsync()` (API Async, NO la legacy `provideAnimations()`) | Gotcha menús sin respuesta. |
| Standalone components | Imports explícitos de pipes (`DatePipe`, `SlicePipe`) en cada componente que los use | Gotcha NG8004. |
| Token storage frontend | `sessionStorage`, NUNCA `localStorage` | Regla CLAUDE.md. |
| Logout frontend | Navega siempre a `/login` tras revoke | Regla CLAUDE.md. |
| Tests E2E | Playwright + Chromium. `npx playwright install chromium` con timeout 300s | Gotcha Playwright + Windows. |
| Limpieza procesos | Tras smoke/E2E: `taskkill /IM SIG.API.exe /F` en Windows | Gotcha procesos huérfanos bloquean rebuild. |
| Credenciales seed | admin@sig.local / Demo#2026! (y demás del INPUT_APP §12) | Documentadas en §11. |

### 9.1 Sistema externo simulado (integraciones fake)

- Interfaz: `Application/Interfaces/Integrations/I{Sistema}Client.cs`.
- HTTP real (no usado en MVP): `Infrastructure/Integrations/Http/{Sistema}Client.cs` con `HttpClient` tipado registrado en DI con `AddHttpClient<ICeleroClient, CeleroClient>()`.
- Fake: `Infrastructure/Integrations/Fake/{Sistema}FakeClient.cs` con `Bogus.Faker<T>` + `Randomizer.Seed = new Random(20260101)` en el constructor estático.
- Registro DI en `DependencyInjection.cs` de Infrastructure:
  ```csharp
  if (config.GetValue<bool>("Integrations:UseFake"))
  {
      services.AddSingleton<ICeleroClient, CeleroFakeClient>();
      services.AddSingleton<IBizneoClient, BizneoFakeClient>();
      services.AddSingleton<IIntratimeClient, IntratimeFakeClient>();
      services.AddSingleton<IPayHawkClient, PayHawkFakeClient>();
  }
  else
  {
      services.AddHttpClient<ICeleroClient, CeleroClient>(/* baseUri config */);
      // idem para los demás
  }
  ```

### 9.2 Schema bi (Power BI)

Migración con SQL crudo crea schema `bi` y vistas:
- `bi.v_cierres_por_periodo` (ServiceId, Nombre, PeriodId, PeriodoNombre, CosteTotal, FacturacionTotal, Margen, Estado).
- `bi.v_lineas_por_concepto` (ClosureLineId, ConceptId, ConceptoNombre, Tipo, Importe, UserId, RecursoNombre, PeriodoId).
- `bi.v_aprobaciones_pendientes` (ClosureId, ServiceId, ServiceNombre, PasoActual, RolPendiente, DiasPendiente).
- `bi.v_audit_resumen` (UserId, Email, Acciones30dias, ÚltimaActividad).

Power BI se conecta directamente con su conector PostgreSQL nativo a `sig_plataforma_dev|prod`, schema `bi`. RF-F03.

---

## 10. Instrucciones de arranque

### 10.1 Pre-requisitos detectados (ver `docs/ENVIRONMENT.md`)

- .NET SDK 10.0.104 (instalado).
- Node 24.14.0 y npm 11.9.0 (instalados).
- Angular CLI 21.2.2 (instalado).
- PostgreSQL 16.12 corriendo en localhost:5432 con usuario `postgres` y password **`admin`** (detectada en environment probe — ver `docs/ENVIRONMENT.md`).

### 10.2 Backend

```bash
# Desde C:\dev\sig-plataforma
cd backend
dotnet restore
dotnet ef database update --project SIG.Infrastructure --startup-project SIG.API   # primera vez
dotnet run --project SIG.API
# API disponible en http://localhost:5180 y https://localhost:5181 (puertos pinned)
# Swagger UI en http://localhost:5180/swagger
```

El primer arranque en `Development`:
- Aplica migraciones automáticamente (`Database.Migrate()`).
- Ejecuta `SeedService.RunIfEmptyAsync()` con los volúmenes del INPUT_APP §12 (3 Clients, 8 Projects, 15 Users, 8 Concepts, 5 Periods, ~50 Closures, ~600 ClosureLines, etc.).
- Carga staging con datos sintéticos vía `Bogus(seed=20260101)`.

### 10.3 Frontend

```bash
cd frontend
npm install
ng serve
# SPA en http://localhost:4200, llamadas a http://localhost:5180/api
```

### 10.4 Credenciales seed (Development)

Password única para todos los usuarios demo: **`Demo#2026!`**

| Email | Nombre | Roles | Departamento | Notas |
|---|---|---|---|---|
| admin@sig.local | Admin SIG | Administrator | — | Acceso total |
| direccion@sig.local | Carmen Ruiz | Direction | Dirección | Paso 4 del flujo |
| fico@sig.local | Javier López | Fico | Finanzas | Paso 3 |
| backoffice1@sig.local | Laura Sánchez | Backoffice | Backoffice | Paso 2 |
| backoffice2@sig.local | Pedro Martín | Backoffice | Backoffice | Paso 2 |
| pm.alpha@sig.local | María García | ProjectManager | Operaciones | Owner Alpha |
| pm.beta@sig.local | David Pérez | ProjectManager | Operaciones | Owner Beta |
| pm.gamma@sig.local | Sara Gómez | ProjectManager | Operaciones | Owner Gamma |
| pm.multi@sig.local | Alex Torres | ProjectManager | Operaciones | Ownership cruzado |
| auditor@sig.local | Inés Romero | Auditor | Finanzas | Solo lectura + AuditLog |
| reader@sig.local | Luis Vega | Reader | Operaciones | Solo lectura |
| gpv1..gpv4@sig.local | (varios) | (sin rol aprobación) | Operaciones | Recursos en ClosureLine.UserId |

### 10.5 Re-seed manual

Solo en Development/Testing/E2E con `Features:AllowSeedRegeneration=true`:

```bash
curl -X POST http://localhost:5180/api/dev/regenerar-seed \
  -H "Authorization: Bearer <jwt-de-admin>"
```

En Production el endpoint devuelve 404 (no se registra).

---

## 11. Suposiciones críticas

Ver `docs/SUPOSICIONES_CRITICAS.md`. Resumen:

- Puertos backend 5180/5181, frontend 4200 (SUP-A01/A02).
- Connection strings con `Password=admin` (SUP-B01, environment probe).
- JWT access 30 min, refresh 7 días (SUP-C01).
- AST JSON de fórmula con tipos `Number`, `Variable`, `Source`, `Aggregate`, `BinaryOp` (SUP-D01).
- A3 Innuva / A3 ERP estructura razonable XML — formato exacto TODO contractual documentado en `docs/EXPORTS.md` (SUP-E01..05).
- xmin como rowVersion en Closure y ClosureLine (SUP-F01).
- AuditLog vía SaveChangesInterceptor (SUP-G01).
- Ownership: Administrator/Direction/Fico/Backoffice/Auditor/Reader ven todo; ProjectManager solo lo asignado (SUP-H01).
- Endpoint `/api/dev/regenerar-seed` con doble guard (Environment + Feature flag) (SUP-I01).
- Integraciones fake DI condicionado por `Integrations:UseFake=true` (SUP-J01..03).
- Approval secuencial hardcoded en C# (SUP-K01).
- Material 21 paleta `mat.$azure-palette` + overrides CSS para SIG navy (SUP-L01).
- Soft delete solo en maestros (SUP-M04).
- NIF regex española con letra de control (SUP-M05).

---

## 12. Estructura de proyectos (backend)

```
C:\dev\sig-plataforma\
├── backend\
│   ├── SIG.slnx                         (solution .NET 10)
│   ├── Directory.Packages.props         (Central Package Management)
│   ├── SIG.Domain\
│   │   ├── Entities\                    (User, Client, Service, Concept, Variable, Period, Role, UserRole, Department, CostCenter, ServiceConcept, ServiceUser, ServiceCostCenter, ConceptUser, TarifaServicio, PresupuestoServicio, Closure, ClosureLine, Approval, ApprovalHistory, ClosureAlerta, AuditLog, CalculationLog, RefreshToken)
│   │   ├── Entities\Staging\            (canónico: staging_galan_{entradas,salidas,stocks}, staging_mediapost_{pedidos,recepciones}; más StagingCeleroVisita, StagingBizneoEmpleado, StagingBizneoHora, StagingIntratimeFichaje, StagingPayHawkGasto)
│   │   ├── Enums\                       (EstadoUsuario, EstadoServicio, TipoConcepto, EstadoPeriodo, EstadoClosure, ApprovalStep, EstadoApproval, TipoAlerta, AuditAction)
│   │   ├── Exceptions\                  (DomainException + derivadas)
│   │   └── Common\                      (IAuditable, ISoftDeletable, IStagingRow)
│   ├── SIG.Application\
│   │   ├── DTOs\                        (LoginRequest, ClosureDetailDto, …)
│   │   ├── Interfaces\
│   │   │   ├── Services\                (IAuthService, IClosureService, …)
│   │   │   ├── Repositories\            (IUserRepository, IClosureRepository, …)
│   │   │   └── Integrations\            (ICeleroClient, IBizneoClient, …)
│   │   ├── Services\                    (ServiceService, ClientService, ClosureService, ApprovalService, DashboardService, CalculationService, AuditService, SyncService, ExportService, SeedService — NO AuthService, ese va a Infrastructure)
│   │   ├── Calculation\                 (CalculationEngine, FormulaParser, CalculationContext, CalculationDataLoader, VariableResolver, FormulaNode AST, NumberNode, VariableNode, SourceNode, AggregateNode, BinaryOpNode)
│   │   ├── Validators\                  (LoginRequestValidator, ClientCreateRequestValidator, …)
│   │   └── Common\                      (PagedResult, CurrentUserService interface — impl en Infra)
│   ├── SIG.Infrastructure\
│   │   ├── Persistence\
│   │   │   ├── AppDbContext.cs
│   │   │   ├── Configurations\          (UserConfiguration, ClosureConfiguration, … — IEntityTypeConfiguration)
│   │   │   ├── Interceptors\            (AuditInterceptor : SaveChangesInterceptor, TimestampsInterceptor)
│   │   │   └── Migrations\              (generadas por dotnet ef)
│   │   ├── Repositories\                (UserRepository, ClosureRepository, … — implementan interfaces de Application)
│   │   ├── Services\                    (AuthService — JWT/BCrypt, CurrentUserService, A3InnuvaExportService, A3ErpExportService, SeederImpl)
│   │   ├── Integrations\
│   │   │   ├── Http\                    (CeleroClient, BizneoClient, IntratimeClient, PayHawkClient — HttpClient tipado, no usados en MVP)
│   │   │   └── Fake\                    (CeleroFakeClient, BizneoFakeClient, IntratimeFakeClient, PayHawkFakeClient — Bogus seed 20260101)
│   │   ├── Seed\                        (DataSeeder con ejecución idempotente)
│   │   └── DependencyInjection.cs       (AddInfrastructure(IServiceCollection, IConfiguration))
│   ├── SIG.API\
│   │   ├── Controllers\                 (AuthController, ClientsController, ServicesController, ConceptsController, UsersController, RolesController, DepartmentsController, CostCentersController, PeriodsController, ClosuresController, ApprovalsController, DashboardController, CalculationsController, AuditController, SyncController, ExportsController, GalanController, MediapostController, DevController, HealthController)
│   │   ├── Filters\                     (ValidationFilter : IAsyncActionFilter)
│   │   ├── Middleware\                  (ExceptionHandlingMiddleware)
│   │   ├── Properties\launchSettings.json (puertos 5180/5181 pinned)
│   │   ├── appsettings.json             (defaults)
│   │   ├── appsettings.Development.json (ConnectionStrings, JwtSettings, Integrations:UseFake=true, Features:AllowSeedRegeneration=true, Features:ShowDemoCredentials=true)
│   │   ├── appsettings.Testing.json     (DB sig_plataforma_test)
│   │   ├── appsettings.E2E.json         (DB sig_plataforma_e2e)
│   │   ├── appsettings.Production.json  (placeholders — gitignored)
│   │   └── Program.cs                   (AddControllers + AddJsonOptions(JsonStringEnumConverter), AddAuthentication(JwtBearer), AddSwaggerGen, AddInfrastructure, app.UseMiddleware<ExceptionHandlingMiddleware>, app.MapControllers, db.Database.Migrate(), await SeedService.RunIfEmptyAsync(); public partial class Program { })
│   └── SIG.Tests\
│       ├── Unit\                        (FormulaParserTests, CalculationEngineTests, ApprovalServiceTests, …)
│       ├── Integration\                 (CustomWebApplicationFactory, AuthControllerTests, ClosuresControllerTests, … con mocks de I{Sistema}Client vía Substitute)
│       └── SIG.Tests.csproj             (pinea Microsoft.EntityFrameworkCore Y Relational a 9.0.4)
└── frontend\
    ├── angular.json
    ├── package.json
    ├── src\
    │   ├── app\
    │   │   ├── app.config.ts            (provideRouter, provideAnimationsAsync, provideHttpClient con interceptor JWT)
    │   │   ├── app.routes.ts            (redirect a /home PRIMERO, layouts después — gotcha orden de rutas)
    │   │   ├── core\
    │   │   │   ├── auth\                (AuthService, JWTInterceptor, AuthGuard, RoleGuard)
    │   │   │   └── api\                 (ApiBase, ClientService, ServiceService — services.service.ts, ClosuresService, ApprovalsService, DashboardService, …)
    │   │   ├── shared\                  (Material modules, layout AppBar+SideNav, breadcrumbs, table-paged, period-selector, demo-credentials-banner)
    │   │   ├── features\
    │   │   │   ├── login\
    │   │   │   ├── dashboard\
    │   │   │   ├── clients\
    │   │   │   ├── services\            (services-list, service-form, service-detail + tarifas\ + presupuestos\)
    │   │   │   ├── concepts\            (incluye editor visual de fórmula — Designer)
    │   │   │   ├── variables\
    │   │   │   ├── periods\
    │   │   │   ├── closures\
    │   │   │   ├── approvals\
    │   │   │   ├── reports\
    │   │   │   ├── admin\
    │   │   │   │   ├── users\
    │   │   │   │   ├── roles\
    │   │   │   │   ├── departments\
    │   │   │   │   └── cost-centers\
    │   │   │   └── audit\
    │   │   └── models\                  (enums Rol, EstadoClosure, TipoConcepto, … como string enums)
    │   ├── environments\
    │   │   ├── environment.ts           (apiUrl: http://localhost:5180/api)
    │   │   └── environment.prod.ts
    │   ├── styles.scss                  (@use '@angular/material' as mat; mat.theme(...) con $azure-palette + overrides --mat-sys-* SIG navy)
    │   └── index.html                   (Inter + Roboto Mono Google Fonts)
    └── playwright.config.ts             (chromium, baseURL: http://localhost:4200)
```

---

## 13. Métricas CS y GS

### CS — Contract Score
Endpoints con firma completa (verbo + ruta + auth + DTO request + DTO response + validaciones + códigos HTTP) / total endpoints en la tabla §7.

- Endpoints documentados con firma completa: **65 / 65**.
- **CS = 1.00**.

### GS — Guard Score
Endpoints con autorización requerida correctamente marcada / endpoints que requieren auth.

- Total endpoints en §7: 65.
- Endpoints anónimos legítimos: 3 (login, refresh, health).
- Endpoints que requieren auth: 62.
- Endpoints con auth correctamente marcada (Authorize / Roles): 62.
- **GS = 62 / 62 = 1.00**.

Ambas métricas cumplen el umbral (CS ≥ 1.0, GS > 0.8).

---

## 14. Índice de trazabilidad

| RF | Entidad(es) | Service (Application) | Repository | Controller | UI Component |
|---|---|---|---|---|---|
| RF-A01 | User, RefreshToken | IAuthService | IUserRepository | AuthController | login |
| RF-A02 | RefreshToken | IAuthService | IRefreshTokenRepository | AuthController | login |
| RF-A03 | RefreshToken | IAuthService | IRefreshTokenRepository | AuthController | — (interceptor) |
| RF-B01 | Closure, Period | IDashboardService | IClosureRepository, IPeriodRepository | DashboardController | dashboard |
| RF-B02 | Closure, StagingX | IDashboardService | IClosureRepository | DashboardController | dashboard |
| RF-B03 | Service, ServiceUser | IDashboardService | IServiceRepository | DashboardController | dashboard |
| RF-C01 | Client | IClientService | IClientRepository | ClientsController | clients |
| RF-C02 | Service, ServiceCostCenter, ServiceUser, ServiceConcept | IServiceService | IServiceRepository | ServicesController | services |
| RF-C03 | *(consolidado en RF-C02 — Action eliminado)* | IServiceService | IServiceRepository | ServicesController | services |
| RF-C04 | Concept | IConceptService | IConceptRepository | ConceptsController | conceptos |
| RF-C05 | User, UserRole | IUserService | IUserRepository | UsersController | admin/usuarios |
| RF-C06 | Role, Department, CostCenter | IRoleService, IDepartmentService, ICostCenterService | IRoleRepository, IDepartmentRepository, ICostCenterRepository | RolesController, DepartmentsController, CostCentersController | admin/roles, admin/departments, admin/cost-centers |
| RF-C07 | Period | IPeriodService | IPeriodRepository | PeriodsController | periodos |
| RF-D01 | Closure, ClosureLine, CalculationLog | IClosureService, ICalculationService | IClosureRepository, ICalculationLogRepository | ClosuresController | closures |
| RF-D02 | Approval, Closure | IApprovalService | IApprovalRepository, IClosureRepository | ApprovalsController | aprobaciones |
| RF-D03 | Approval | IApprovalService | IApprovalRepository | ApprovalsController | aprobaciones |
| RF-D04 | Closure, ClosureLine | IClosureService | IClosureRepository | ClosuresController | aprobaciones (detalle) |
| RF-D05 | Approval, ApprovalHistory | IApprovalService | IApprovalRepository | ApprovalsController | aprobaciones |
| RF-D06 | Approval, ApprovalHistory | IApprovalService | IApprovalRepository | ApprovalsController | aprobaciones |
| RF-D07 | CalculationLog | ICalculationService | ICalculationLogRepository | CalculationsController | conceptos (detalle cálculo) |
| RF-E01 | StagingX | ISyncService | IStagingXRepository | SyncController | — (trigger manual admin) |
| RF-E02 | Closure | IExportService | IClosureRepository | ExportsController | contabilidad |
| RF-E03 | Closure | IExportService | IClosureRepository | ExportsController | contabilidad |
| RF-F01 | AuditLog | (SaveChangesInterceptor) | — | — | — (transaccional) |
| RF-F02 | AuditLog | IAuditService | IAuditLogRepository | AuditController | auditoría |
| RF-F03 | — (vistas SQL) | — | — | — | Power BI |
| RF-G01 | — (cross-cutting) | CurrentUserService | (todos los repositorios) | — | — (middleware) |
| RF-G02 | ISoftDeletable | — | HasQueryFilter EF | — | — (infraestructura) |

**Documentos relacionados:**
- `docs/SUPOSICIONES_CRITICAS.md` — decisiones autónomas del Arquitecto.
- `docs/ENVIRONMENT.md` — versiones de tooling + password real de PostgreSQL local (`admin`).
- `docs/PROGRESO.md` — estado del pipeline.
- `docs/EXPORTS.md` — estructura XML A3 Innuva y A3 ERP (a generar por el Desarrollador en su fase; placeholder).
- `docs/BLOQUEANTES.md` — sin entradas; no hay bloqueantes en Fase 1.
