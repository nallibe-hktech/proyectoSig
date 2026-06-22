using SIG.Domain.Common;

namespace SIG.Domain.Entities.Staging;

public class StagingCeleroVisita : IStagingRow
{
    public int Id { get; set; }

    // Datos crudos de Celero
    public string VisitaIdExterno { get; set; } = null!;
    public string ResourceNif { get; set; } = "";
    public string ServiceName { get; set; } = "";
    public string MissionName { get; set; } = "";
    public DateOnly Fecha { get; set; }

    // IDs resueltos (nullable si no hay coincidencia)
    public int? UserId { get; set; }
    public int? ServiceId { get; set; }   // antes ActionId+ProjectId: la visita resuelve a un Servicio

    // Mapeos y anotaciones locales (enriquecimiento de datos)
    public string? Notas { get; set; }
    public int? MapeadoPor { get; set; }
    public DateTime? FechaMapeo { get; set; }
    public string? EstadoMapeo { get; set; }

    // Auditoría y control
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingBizneoEmpleado : IStagingRow
{
    public int Id { get; set; }
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Departamento { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingBizneoAbsence : IStagingRow
{
    public int Id { get; set; }
    public string RegistroIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public int ServiceId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Horas { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingIntratimeFichaje : IStagingRow
{
    public int Id { get; set; }
    public string FichajeIdExterno { get; set; } = null!;
    public string UserIdExterno { get; set; } = null!;  // ID externo de Intratime (ej: "20875")
    public int? UserId { get; set; }                    // ID interno SIG-ES resuelto por NIF
    public DateTime Entrada { get; set; }
    public DateTime? Salida { get; set; }
    public decimal? HorasCalculadas { get; set; }       // (Salida - Entrada) en horas
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingPayHawkGasto : IStagingRow
{
    public int Id { get; set; }
    public string GastoIdExterno { get; set; } = null!;
    public int UserId { get; set; }
    public int? ServiceId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Importe { get; set; }
    public string Categoria { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

// Mapeos Celero → SIG-es
public class CeleroResourceMapping : IAuditable
{
    public int Id { get; set; }
    public string CeleroNif { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CeleroServiceMapping : IAuditable
{
    public int Id { get; set; }
    public string CeleroServiceName { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CeleroMissionMapping : IAuditable
{
    public int Id { get; set; }
    public string CeleroMissionName { get; set; } = null!;
    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public string? Descripcion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StagingSgpvVisita : IStagingRow
{
    public int Id { get; set; }

    // Datos crudos de SGPV
    public string VisitaIdExterno { get; set; } = null!;
    public string ResourceNif { get; set; } = "";
    public string CentroId { get; set; } = null!;
    public string? CentroNombre { get; set; }
    public string? ServiceName { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal? HorasDuracion { get; set; }

    // IDs resueltos
    public int? UserId { get; set; }
    public int? ServiceId { get; set; }

    // Auditoría y control
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingA3InnuvaEmpleado : IStagingRow
{
    public int Id { get; set; }
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string? Departamento { get; set; }
    public decimal? SueldoMensual { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingTravelPerkViaje : IStagingRow
{
    public int Id { get; set; }
    public string ViajeIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string Solicitante { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public decimal Presupuesto { get; set; }
    public string Estado { get; set; } = null!;
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingSgpvProducto : IStagingRow
{
    public int Id { get; set; }
    public string IdProducto { get; set; } = null!;
    public string IdCliente { get; set; } = null!;
    public string Cliente { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public string Subcategoria { get; set; } = null!;
    public string CodigoReferencia { get; set; } = null!;
    public string Referencia { get; set; } = null!;
    public string EAN { get; set; } = null!;
    public string Marca { get; set; } = null!;
    public string? PVPRecomendado { get; set; }
    public string? Competencia { get; set; }
    public bool Activo { get; set; }
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingIntratimeEmpleado : IStagingRow
{
    public int Id { get; set; }
    public string UserIdExterno { get; set; } = null!;  // USER_ID externo de Intratime
    public int? UserId { get; set; }                    // Id interno SIG-ES resuelto por NIF
    public string Nombre { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string NIF { get; set; } = null!;
    public string? Affiliation { get; set; }
    public int Role { get; set; }
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingIntratimeClockingRequest : IStagingRow
{
    public int Id { get; set; }
    public string RequestIdExterno { get; set; } = null!;  // REQUEST_ID
    public string UserIdExterno { get; set; } = null!;     // USER_ID
    public int? UserId { get; set; }                       // Id interno SIG-ES
    public DateTime FechaRequest { get; set; }             // REQUEST_DATE
    public string TipoRequest { get; set; } = null!;       // REQUEST_TYPE: "Ajuste", "Corrección"
    public string Estado { get; set; } = null!;            // REQUEST_STATUS: "Pendiente", "Aprobado", "Rechazado"
    public string? Razon { get; set; }                     // REQUEST_REASON
    public string? HoraDesde { get; set; }                 // REQUESTED_TIME_FROM
    public string? HoraHasta { get; set; }                 // REQUESTED_TIME_TO
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingIntratimeExpense : IStagingRow
{
    public int Id { get; set; }
    public string ExpenseIdExterno { get; set; } = null!;  // INOUT_EXPENSE_ID
    public string? UserIdExterno { get; set; }             // INOUT_USER_ID (puede no existir)
    public int? UserId { get; set; }                       // Id interno SIG-ES resuelto por NIF
    public DateTime FechaExpense { get; set; }             // INOUT_DATE
    public decimal Cantidad { get; set; }                  // INOUT_AMOUNT (en centavos, dividir por 100)
    public string NombreExpense { get; set; } = null!;     // INOUT_EXPENSE_NAME (ej: "Comidas")
    public string? Descripcion { get; set; }               // INOUT_COMMENTS
    public string? ProyectoNombre { get; set; }            // INOUT_PROJECT_NAME (fallback si no hay ServiceId)
    public int? ServiceId { get; set; }                    // Resuelto del nombre del servicio
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

// GALÁN - Logística y Almacenes
public class StagingGalanEntrada : IStagingRow
{
    public int Id { get; set; }
    public string CodigoArticulo { get; set; } = null!;
    public string CodigoDepartamento { get; set; } = null!;
    public string CodigoFamilia { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public DateTime Fecha { get; set; }
    public int Unidades { get; set; }
    public string Empresa { get; set; } = null!;
    public string Almacen { get; set; } = null!;
    public string Celda { get; set; } = null!;
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingGalanSalida : IStagingRow
{
    public int Id { get; set; }
    public string Albaran { get; set; } = null!;
    public string? NumeroPedidoTercero { get; set; }
    public string CodigoArticulo { get; set; } = null!;
    public string CodigoDepartamento { get; set; } = null!;
    public string CodigoFamilia { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public int Unidades { get; set; }
    public string? CodigoTransporte { get; set; }
    public string? Matricula { get; set; }
    public DateTime Fecha { get; set; }
    public string? Destinatario { get; set; }
    public string Almacen { get; set; } = null!;
    public string Celda { get; set; } = null!;
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingGalanStock : IStagingRow
{
    public int Id { get; set; }
    public string CodigoArticulo { get; set; } = null!;
    public string CodigoDepartamento { get; set; } = null!;
    public string CodigoFamilia { get; set; } = null!;
    public string CodigoCelda { get; set; } = null!;
    public decimal StockB { get; set; }
    public decimal StockA { get; set; }
    public decimal Stock { get; set; }
    public string Almacen { get; set; } = null!;
    public string Familia { get; set; } = null!;
    public string SubFamilia { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

// MEDIAPOST - Distribución
public class StagingMediapostPedido : IStagingRow
{
    public int Id { get; set; }
    public string PedidoId { get; set; } = null!;
    public string ReferenciaPedido { get; set; } = null!;
    public string CodigoArticulo { get; set; } = null!;
    public DateTime FechaPedido { get; set; }
    public int Cantidad { get; set; }
    public string Estado { get; set; } = null!;
    public string? DestinatarioNombre { get; set; }
    public string? DireccionEntrega { get; set; }
    public string? CodigoPostal { get; set; }
    public string? Ciudad { get; set; }
    public string? Provincia { get; set; }
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

public class StagingMediapostRecepcion : IStagingRow
{
    public int Id { get; set; }
    public string RecepcionId { get; set; } = null!;
    public string ReferenciaRecepcion { get; set; } = null!;
    public string CodigoArticulo { get; set; } = null!;
    public DateTime FechaRecepcion { get; set; }
    public int Cantidad { get; set; }
    public int? CantidadDañada { get; set; }
    public string Estado { get; set; } = null!;
    public string? Almacen { get; set; }
    public string? Observaciones { get; set; }
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

// A3 INNUVA - Contratos de empleados (ERP)
public class StagingA3InnuvaContrato : IStagingRow
{
    public int Id { get; set; }
    public string ContratoIdExterno { get; set; } = null!;
    public string NIF { get; set; } = null!;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal ImporteBruto { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    // Ola 2 (#2): contratos de un día (FechaInicio == FechaFin) señalados; exclusión manual
    public bool IgnoradoEnCierre { get; set; }
    public string? MotivoIgnorar { get; set; }
    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

// A3 INNUVA NÓMINAS - Empresas y Nóminas (OAuth Wolters Kluwer)
public class StagingA3InnuvaCompany : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string IdExterno { get; set; } = null!;
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Nif { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Pais { get; set; }
    public string? EmailContacto { get; set; }
    public string? TelefonoContacto { get; set; }
    public DateTime FechaUltimaActualizacion { get; set; }

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft-Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class StagingA3InnuvaPayroll : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string IdExterno { get; set; } = null!;
    public string IdEmpleado { get; set; } = null!;
    public string NombreEmpleado { get; set; } = null!;
    public string CodigoPeriodo { get; set; } = null!;
    public decimal SalarioBase { get; set; }
    public decimal Deducciones { get; set; }
    public decimal SalarioNeto { get; set; }
    public DateTime FechaProcesamiento { get; set; }

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft-Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// TEST TABLES - A3 INNUVA NÓMINAS (para pruebas sin riesgo)
public class StagingA3InnuvaCompanyTest : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string IdExterno { get; set; } = null!;
    public string Codigo { get; set; } = null!;
    public string Nombre { get; set; } = null!;
    public string Nif { get; set; } = null!;
    public string? Direccion { get; set; }
    public string? Ciudad { get; set; }
    public string? Pais { get; set; }
    public string? EmailContacto { get; set; }
    public string? TelefonoContacto { get; set; }
    public DateTime FechaUltimaActualizacion { get; set; }

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft-Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class StagingA3InnuvaPayrollTest : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string IdExterno { get; set; } = null!;
    public string IdEmpleado { get; set; } = null!;
    public string NombreEmpleado { get; set; } = null!;
    public string CodigoPeriodo { get; set; } = null!;
    public decimal SalarioBase { get; set; }
    public decimal Deducciones { get; set; }
    public decimal SalarioNeto { get; set; }
    public DateTime FechaProcesamiento { get; set; }

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft-Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
