using SIG.Domain.Common;
using SIG.Domain.Enums;

namespace SIG.Domain.Entities;

public class User : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string NIF { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Apellidos { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public EstadoUsuario Estado { get; set; }
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

public class Role : IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
}

public class UserRole
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
}

public class Department : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class CostCenter : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ServiceCostCenter> ServiceCostCenters { get; set; } = new List<ServiceCostCenter>();
}

public class Client : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string NIF { get; set; } = null!;
    public EstadoCliente Estado { get; set; }
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

// Service = antiguo Action renombrado, que absorbe el vínculo directo a Client
// y las relaciones CECO/Usuario/Cierres/Tarifas/Presupuestos que colgaban de Project.
// (PPT §1: Cliente → Servicio → Concepto; "Proyecto" desaparece.)
public class Service : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public EstadoServicio Estado { get; set; }
    // Metadatos heredados de Project (preservados en la migración; revisar en olas posteriores
    // si el interlocutor pasa a modelarse como ServiceUser con rol — PPT §2.3).
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
    // Ola 3b (#10): el antiguo Closure se ha dividido en dos raíces independientes.
    public ICollection<CierreCostes> CierresCostes { get; set; } = new List<CierreCostes>();
    public ICollection<CierreFacturacion> CierresFacturacion { get; set; } = new List<CierreFacturacion>();
}

public class ServiceConcept
{
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
}

public class ServiceUser
{
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class ServiceCostCenter
{
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int CostCenterId { get; set; }
    public CostCenter CostCenter { get; set; } = null!;
}

public class Concept : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public TipoConcepto Tipo { get; set; }
    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public string FormulaJson { get; set; } = null!;
    public int? ServiceId { get; set; }   // null = global concept applies to all services
    public Service? Service { get; set; }
    public string? ColumnaA3 { get; set; } // Maps to A3 export column: "ImporteBruto", "IRPF", "SSEmpleado", "KM", etc.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ServiceConcept> ServiceConcepts { get; set; } = new List<ServiceConcept>();
    public ICollection<ConceptUser> ConceptUsers { get; set; } = new List<ConceptUser>();
}

public class ConceptUser
{
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class TarifaServicio : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string Nombre { get; set; } = null!;    // "Visita estándar", "Hora extra", "KM", etc.
    public decimal Valor { get; set; }              // price in EUR
    public string? Unidad { get; set; }             // "visita" | "hora" | "km" | "dia" (informational)
    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PresupuestoServicio : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int? PeriodId { get; set; }              // null = applies to all periods
    public Period? Period { get; set; }
    public TipoConcepto Tipo { get; set; }          // Pago | Factura
    public decimal Importe { get; set; }
    public string? Descripcion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Variable : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public string QuestionIdExterno { get; set; } = null!;
    public string MapeoValoresJson { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class Period : IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int DiaPago { get; set; }                  // día del mes de pago: 30, 15 o 9
    public EstadoPeriodo Estado { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<CierreCostes> CierresCostes { get; set; } = new List<CierreCostes>();
    public ICollection<CierreFacturacion> CierresFacturacion { get; set; } = new List<CierreFacturacion>();
}

// Ola 3b (#10): contrato común de ambas raíces de cierre para reutilizar la lógica de servicio
// (Diseño A — raíces separadas, hijos por FK; "Margen al vuelo": el margen NO se almacena).
public interface ICierre : IAuditable
{
    int Id { get; set; }
    int ServiceId { get; set; }
    int PeriodId { get; set; }
    decimal Total { get; set; }            // Costes: antiguo CosteTotal; Facturacion: antiguo FacturacionTotal
    EstadoClosure Estado { get; set; }
    ApprovalStep PasoActual { get; set; }
    string? Comentarios { get; set; }
    DateTime FechaCreacion { get; set; }
    uint RowVersion { get; set; }
    TipoCierre TipoCierre { get; }
    Service Service { get; set; }
    Period Period { get; set; }
    ICollection<ClosureLine> Lines { get; set; }
    ICollection<Approval> Approvals { get; set; }
    ICollection<ApprovalHistory> ApprovalHistory { get; set; }
    ICollection<ClosureAlerta> Alertas { get; set; }
}

// Cierre MENSUAL de costes. Total = suma de líneas Pago.
public class CierreCostes : ICierre, IAuditable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    public decimal Total { get; set; }
    public EstadoClosure Estado { get; set; }
    public ApprovalStep PasoActual { get; set; }
    public string? Comentarios { get; set; }
    public DateTime FechaCreacion { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public TipoCierre TipoCierre => TipoCierre.Costes;
    public ICollection<ClosureLine> Lines { get; set; } = new List<ClosureLine>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
    public ICollection<ClosureAlerta> Alertas { get; set; } = new List<ClosureAlerta>();
}

// Cierre de FACTURACIÓN. Puede quedar pendiente varios meses; no es mensual. Total = suma de líneas Factura.
public class CierreFacturacion : ICierre, IAuditable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    public decimal Total { get; set; }
    public EstadoClosure Estado { get; set; }
    public ApprovalStep PasoActual { get; set; }
    public string? Comentarios { get; set; }
    public DateTime FechaCreacion { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public TipoCierre TipoCierre => TipoCierre.Facturacion;
    public ICollection<ClosureLine> Lines { get; set; } = new List<ClosureLine>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
    public ICollection<ClosureAlerta> Alertas { get; set; } = new List<ClosureAlerta>();
}

// Hijo compartido: exactamente UNA de las dos FK (CierreCostesId / CierreFacturacionId) está poblada.
public class ClosureLine : IAuditable
{
    public int Id { get; set; }
    public int? CierreCostesId { get; set; }
    public CierreCostes? CierreCostes { get; set; }
    public int? CierreFacturacionId { get; set; }
    public CierreFacturacion? CierreFacturacion { get; set; }
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public decimal Importe { get; set; }
    public string DatosEntradaJson { get; set; } = null!;
    public TipoConcepto Tipo { get; set; }
    public bool TieneIncidencia { get; set; }
    // Ola 2 (#3a): override manual de importe e incentivos manuales.
    public bool EsManual { get; set; }                // línea introducida o ajustada a mano (no recalculable por fórmula)
    public decimal? ImporteOriginal { get; set; }     // importe calculado original, si hubo override
    public string? MotivoManual { get; set; }         // auditoría del ajuste/incentivo manual
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public CalculationLog? CalculationLog { get; set; }
}

public class Approval : IAuditable
{
    public int Id { get; set; }
    // Ola 3b (#10): exactamente UNA de las dos FK está poblada (costes o facturación).
    public int? CierreCostesId { get; set; }
    public CierreCostes? CierreCostes { get; set; }
    public int? CierreFacturacionId { get; set; }
    public CierreFacturacion? CierreFacturacion { get; set; }
    // Ola 3a (#1): el paso Grupo no corresponde a un rol único (rol global + asignación al servicio),
    // por lo que RoleId es nullable. La fuente de verdad de qué exige cada paso es Approval.Paso.
    public int? RoleId { get; set; }
    public Role? Role { get; set; }
    public ApprovalStep Paso { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public EstadoApproval Estado { get; set; }
    public string? Motivo { get; set; }
    public DateTime? FechaDecision { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ApprovalHistory
{
    public int Id { get; set; }
    // Ola 3b (#10): exactamente UNA de las dos FK está poblada (costes o facturación).
    public int? CierreCostesId { get; set; }
    public CierreCostes? CierreCostes { get; set; }
    public int? CierreFacturacionId { get; set; }
    public CierreFacturacion? CierreFacturacion { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ApprovalStep PasoOrigen { get; set; }
    public ApprovalStep PasoDestino { get; set; }
    public string Accion { get; set; } = null!;
    public string? Motivo { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ClosureAlerta : IAuditable
{
    public int Id { get; set; }
    // Ola 3b (#10): exactamente UNA de las dos FK está poblada (costes o facturación).
    public int? CierreCostesId { get; set; }
    public CierreCostes? CierreCostes { get; set; }
    public int? CierreFacturacionId { get; set; }
    public CierreFacturacion? CierreFacturacion { get; set; }
    public TipoAlerta Tipo { get; set; }
    public string Codigo { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public string? Detalle { get; set; }
    public bool Confirmada { get; set; }
    public int? ConfirmadaPorUserId { get; set; }
    public User? ConfirmadaPor { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AuditLog
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public AuditAction Action { get; set; }
    public string? OldValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Ip { get; set; }
}

public class CalculationLog
{
    public int Id { get; set; }
    public int ClosureLineId { get; set; }
    public ClosureLine ClosureLine { get; set; } = null!;
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public string FormulaSnapshotJson { get; set; } = null!;
    public string InputsJson { get; set; } = null!;
    public decimal Resultado { get; set; }
    public string? Incidencias { get; set; }
    public string SistemaOrigen { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}

public class RefreshToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Ip { get; set; }
}
