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
    // Config. Presupuesto (prototipo 24/28): margen operativo objetivo de la acción (%). Manual; el real se
    // calcula de los cierres (factura − coste). Null = sin objetivo definido.
    public decimal? MargenObjetivoPct { get; set; }
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

// Configuración de Presupuesto (prototipo 24/28, PPT slide 35): partida de presupuesto de una acción/servicio.
// ENTRADA MANUAL: el importe no procede de ningún origen de datos (el propio prototipo lo indica). Cada
// partida es Anual o Total acción. "Consumido" también es manual por ahora (ilustrativo hasta validar con
// SIG el mapeo partida↔conceptos); Restante y Avance se calculan. Alimenta la desviación de Errores Facturación.
public class PartidaPresupuesto : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string Nombre { get; set; } = null!;                 // "Personal de campo", "Logística", … (texto libre)
    public TipoPartidaPresupuesto Tipo { get; set; }            // Anual | TotalAccion
    public int? Anio { get; set; }                              // ejercicio (para partidas Anuales); null en Total acción
    public decimal Presupuesto { get; set; }                    // € presupuestado (manual)
    public decimal Consumido { get; set; }                      // € consumido (manual por ahora)
    public string? Descripcion { get; set; }                    // "Salario bruto + incentivos", "Galán / Mediapost"…
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Incidencia del cliente (PPT slide 6): un cliente puede tener varias incidencias, editables y con
// histórico (el histórico de cambios lo aporta el AuditInterceptor sobre IAuditable + AuditLog).
public class ClienteIncidencia : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Tipo { get; set; } = null!;       // texto libre (sin catálogo cerrado en el PPT)
    public string Descripcion { get; set; } = null!; // explicación de la incidencia
    public EstadoIncidencia Estado { get; set; }     // Abierta | EnProceso | Resuelta
    public string? Origen { get; set; }              // origen/responsable (prototipo): "Comercial", "Contabilidad"…
    public DateTime FechaApertura { get; set; }      // fecha de apertura editable (por defecto = alta)
    public ICollection<IncidenciaHistorial> Historial { get; set; } = new List<IncidenciaHistorial>();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Histórico de la incidencia (prototipo Incidencias, panel de detalle): cada cambio de estado o nota
// queda registrado con fecha y responsable (entrada manual de comercial/contabilidad).
public class IncidenciaHistorial : IAuditable
{
    public int Id { get; set; }
    public int IncidenciaId { get; set; }
    public ClienteIncidencia Incidencia { get; set; } = null!;
    public EstadoIncidencia Estado { get; set; }     // estado resultante tras el evento
    public string Nota { get; set; } = null!;        // descripción del evento ("Reclamación enviada al cliente")
    public string? Responsable { get; set; }         // "María (Contabilidad)", "Comercial"
    public DateTime Fecha { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Configuración de Factura (prototipo pantalla 25/28, PPT slide 37-38): una categoría de factura agrupa
// (suma) uno o varios conceptos de facturación del cliente para mostrarlos como UNA sola línea en su
// factura (ej.: "Gastos de personal" = Gastos Payhawk + Dietas). Se definen POR CLIENTE (cada cliente
// factura distinto). El mapeo concreto categoría↔conceptos lo valida SIG; el seed es ilustrativo.
public class CategoriaFactura : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string Nombre { get; set; } = null!;       // texto libre ("Gastos de personal")
    public ICollection<CategoriaFacturaConcepto> Conceptos { get; set; } = new List<CategoriaFacturaConcepto>();
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Join categoría↔concepto. Un concepto del cliente pertenece como mucho a UNA categoría (validado en el
// servicio); el panel "conceptos disponibles" del prototipo muestra los que aún están sin asignar.
public class CategoriaFacturaConcepto
{
    public int CategoriaFacturaId { get; set; }
    public CategoriaFactura CategoriaFactura { get; set; } = null!;
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
}

// Forecast (PPT slide 36): previsión mensual de ventas / margen / nº personas (GPP) por servicio.
// Granularidad servicio+mes porque el resumen del PPT pide filas por dpto y filtro por servicio,
// y el departamento vive en el Servicio (no en el Cliente). Un registro por (ServiceId, Anio, Mes).
public class Forecast : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public int Anio { get; set; }
    public int Mes { get; set; }                     // 1..12
    public decimal VentasPrevistas { get; set; }     // €
    public decimal? MargenPrevisto { get; set; }     // € (slide 23: previsión de ventas y margen bruto)
    public int? PersonasCampo { get; set; }          // GPP: nº de personas de campo previstas (slide 36)
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

// FASE 2: Tarifas por Concepto (granularidad más fina que TarifaServicio)
// Permite configurar tariffs específicamente para cada concepto, con validez por período de tiempo.
public class TarifaConcepto : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public int? ClientId { get; set; }      // null = tarifa global para el concepto; otherwise = tarifa por cliente
    public Client? Client { get; set; }
    public int? ServiceId { get; set; }     // null = aplica a todos los servicios del cliente; otherwise = específica del servicio
    public Service? Service { get; set; }
    public decimal Valor { get; set; }      // Tariff value in EUR
    public string? Unidad { get; set; }     // "visita" | "hora" | "km" | "dia" | "mes" etc. (informational)
    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// FASE 2: Presupuestos por Concepto (granularidad más fina que PresupuestoServicio)
// Permite configurar budget items específicamente para cada concepto, por período.
public class PresupuestoConcepto : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public int? ClientId { get; set; }      // null = presupuesto global; otherwise = presupuesto por cliente
    public Client? Client { get; set; }
    public int? ServiceId { get; set; }     // null = aplica a todos los servicios del cliente
    public Service? Service { get; set; }
    public int? PeriodId { get; set; }      // null = aplica a todos los períodos
    public Period? Period { get; set; }
    public TipoConcepto Tipo { get; set; }  // Pago | Factura
    public decimal Importe { get; set; }    // Budgeted amount in EUR
    public string? Descripcion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// FASE 1: Plantilla de Configuración de Concepto por Cliente
// Cada cliente puede personalizar conceptos: fórmula, tarifas, excepciones, reglas específicas.
// Si FormulaJsonOverride es null, se usa Concept.FormulaJson global.
public class PlantillaClienteConcepto : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public string? FormulaJsonOverride { get; set; }   // null = usa Concept.FormulaJson (global)
    public string? ConfiguracionJson { get; set; }     // JSON con tarifas, excepciones, reglas personalizadas
    public bool Activo { get; set; } = true;           // si el concepto está activo para este cliente
    public DateOnly FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
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

// Notificación in-app (circuito de devolución de cierre): cuando un cierre se rechaza, se avisa al
// usuario que lo había aprobado en el paso Grupo. Se muestra como campana en la barra superior.
public class Notification : IAuditable
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }          // destinatario de la notificación
    public User? Usuario { get; set; }
    public string Titulo { get; set; } = null!;
    public string Mensaje { get; set; } = null!;
    public string Tipo { get; set; } = null!;    // p.ej. "CierreDevuelto"
    public int? CierreId { get; set; }           // cierre relacionado (para navegar al detalle)
    public TipoCierre? TipoCierre { get; set; }  // discrimina costes/facturación del cierre
    public bool Leida { get; set; }
    public DateTime? LeidaAt { get; set; }
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

/// <summary>
/// Stores OAuth tokens for Wolters Kluwer A3 INNUVA Nóminas integration.
/// Used for Authorization Code Flow token management.
/// </summary>
public class A3InnuvaOAuthToken
{
    public int Id { get; set; }

    /// <summary>Authorization code from OAuth provider (temporary, exchanged for tokens).</summary>
    public string? AuthorizationCode { get; set; }

    /// <summary>Access token for API calls.</summary>
    public string? AccessToken { get; set; }

    /// <summary>Refresh token for obtaining new access tokens (sliding window).</summary>
    public string? RefreshToken { get; set; }

    /// <summary>Token type (typically "Bearer").</summary>
    public string? TokenType { get; set; }

    /// <summary>Access token expiration time (UTC).</summary>
    public DateTime? AccessTokenExpiresAt { get; set; }

    /// <summary>Refresh token expiration time (UTC).</summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    /// <summary>When the token was last synchronized from Wolters Kluwer.</summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Whether the token is still valid (not revoked or expired).</summary>
    public bool IsValid { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Payment model configuration: supports 3 payment models (FIXED, PER_VISIT, PER_SERVICE)
public class PaymentModel : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public string ModelType { get; set; } = null!;  // FIXED | PER_VISIT | PER_SERVICE
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveUntil { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<PaymentRatesConfiguration> RatesConfigurations { get; set; } = new List<PaymentRatesConfiguration>();
    public ICollection<EmployeePaymentModelMapping> EmployeeMappings { get; set; } = new List<EmployeePaymentModelMapping>();
}

// Rate configuration per client/concept/month/year
public class PaymentRatesConfiguration : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public int? ConceptId { get; set; }
    public Concept? Concept { get; set; }
    public int Year { get; set; }
    public int? Month { get; set; }  // null = applies to all months of the year
    public decimal BaseRate { get; set; }
    public string RateType { get; set; } = null!;  // FIXED | PERCENTAGE | FORMULA
    public string? RateFormula { get; set; }        // for FORMULA type: "=salario_bruto_proyecto * 0.17"
    public decimal? MinValue { get; set; }          // validation: minimum value
    public decimal? MaxValue { get; set; }          // validation: maximum value
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Per-employee payment model assignment (for edge cases where employee switches models mid-month)
public class EmployeePaymentModelMapping : IAuditable
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }  // Foreign key to external employee system (INNUVA, etc.)
    public string EmployeeCode { get; set; } = null!;
    public int PaymentModelId { get; set; }
    public PaymentModel PaymentModel { get; set; } = null!;
    public int ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveUntil { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Concept applicability rules per payment model
public class ConceptValidationRule : ISoftDeletable, IAuditable
{
    public int Id { get; set; }
    public int ConceptId { get; set; }
    public Concept Concept { get; set; } = null!;
    public string PaymentModelType { get; set; } = null!;  // FIXED | PER_VISIT | PER_SERVICE
    public bool IsApplicable { get; set; }          // whether concept applies to this payment model
    public bool IsMandatory { get; set; }           // whether concept is required for this model
    public string? CalculationMethod { get; set; }  // calculation strategy: PERCENTAGE_OF_SALARY, FIXED_AMOUNT, FORMULA, etc.
    public string? AggregationLevel { get; set; }   // aggregation level: EMPLOYEE | PROJECT | VISIT | SERVICE
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
