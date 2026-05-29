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
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
    public ICollection<ActionUser> ActionUsers { get; set; } = new List<ActionUser>();
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
    public ICollection<ProjectCostCenter> ProjectCostCenters { get; set; } = new List<ProjectCostCenter>();
}

public class Client : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
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
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}

public class Project : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public EstadoProyecto Estado { get; set; }
    public string? InterlocutorNombre { get; set; }
    public string? InterlocutorEmail { get; set; }
    public string? InterlocutorTelefono { get; set; }
    public DateOnly FechaAlta { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ProjectCostCenter> ProjectCostCenters { get; set; } = new List<ProjectCostCenter>();
    public ICollection<ProjectUser> ProjectUsers { get; set; } = new List<ProjectUser>();
    public ICollection<Action> Actions { get; set; } = new List<Action>();
    public ICollection<Closure> Closures { get; set; } = new List<Closure>();
}

public class ProjectCostCenter
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int CostCenterId { get; set; }
    public CostCenter CostCenter { get; set; } = null!;
}

public class ProjectUser
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Action : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public EstadoAccion Estado { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ActionConcept> ActionConcepts { get; set; } = new List<ActionConcept>();
    public ICollection<ActionUser> ActionUsers { get; set; } = new List<ActionUser>();
}

public class ActionConcept
{
    public int ActionId { get; set; }
    public Action Action { get; set; } = null!;
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
}

public class ActionUser
{
    public int ActionId { get; set; }
    public Action Action { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
}

public class Concept : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public string Nombre { get; set; } = null!;
    public TipoConcepto Tipo { get; set; }
    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public string FormulaJson { get; set; } = null!;
    public int? ProjectId { get; set; }   // null = global concept applies to all projects
    public Project? Project { get; set; }
    public string? ColumnaA3 { get; set; } // Maps to A3 export column: "ImporteBruto", "IRPF", "SSEmpleado", "KM", etc.
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ActionConcept> ActionConcepts { get; set; } = new List<ActionConcept>();
    public ICollection<ConceptUser> ConceptUsers { get; set; } = new List<ConceptUser>();
}

public class ConceptUser
{
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
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
    public EstadoPeriodo Estado { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Closure> Closures { get; set; } = new List<Closure>();
}

public class Closure : IAuditable
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    public decimal CosteTotal { get; set; }
    public decimal FacturacionTotal { get; set; }
    public decimal Margen { get; set; }
    public EstadoClosure Estado { get; set; }
    public ApprovalStep PasoActual { get; set; }
    public string? Comentarios { get; set; }
    public DateTime FechaCreacion { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<ClosureLine> Lines { get; set; } = new List<ClosureLine>();
    public ICollection<Approval> Approvals { get; set; } = new List<Approval>();
    public ICollection<ApprovalHistory> ApprovalHistory { get; set; } = new List<ApprovalHistory>();
}

public class ClosureLine : IAuditable
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public Closure Closure { get; set; } = null!;
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public decimal Importe { get; set; }
    public string DatosEntradaJson { get; set; } = null!;
    public TipoConcepto Tipo { get; set; }
    public bool TieneIncidencia { get; set; }
    public uint RowVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public CalculationLog? CalculationLog { get; set; }
}

public class Approval : IAuditable
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public Closure Closure { get; set; } = null!;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
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
    public int ClosureId { get; set; }
    public Closure Closure { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public ApprovalStep PasoOrigen { get; set; }
    public ApprovalStep PasoDestino { get; set; }
    public string Accion { get; set; } = null!;
    public string? Motivo { get; set; }
    public DateTime Timestamp { get; set; }
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
