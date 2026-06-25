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

// TravelPerk a nivel LÍNEA (hoja "report" de la descarga Excel). Es el grano que usa el cierre de costes:
// cada línea se imputa al CECO de su "Cost object" (→ cliente/servicio); las líneas sin CECO (Subscription fee)
// van al CECO interno de SIG (0423). Convive con StagingTravelPerkViaje (viaje-level, en desuso para el cierre).
public class StagingTravelPerkLinea : IStagingRow
{
    public int Id { get; set; }
    public string TripId { get; set; } = null!;
    public string Service { get; set; } = null!;      // Hotels, Flights, Premium Service, Refund for train, Subscription fee...
    public string? CostObject { get; set; }            // "Cost object" crudo (NNNN_CLIENTE); null = sin CECO de cliente
    public string Ceco { get; set; } = null!;          // CECO normalizado para imputación (0423 si la línea no trae CostObject)
    public int? ServiceId { get; set; }                // resuelto del CECO; null si el CECO no casa con la tabla maestra
    public decimal CosteSinIVA { get; set; }           // "Cost per traveler without tax"
    public DateOnly? FechaGasto { get; set; }
    public string? TravelerEmail { get; set; }
    public string? Currency { get; set; }
    // IStagingRow
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

public class StagingA3InnuvaPayroll : IStagingRow, IAuditable, ISoftDeletable
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

    // IStagingRow properties
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }

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

// A3 INNUVA NÓMINAS - Conceptos de empleados (salarios, bonificaciones, etc.)
public class StagingA3InnuvaConcepto : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string IdExterno { get; set; } = null!;
    public string CodigoEmpleado { get; set; } = null!;
    public string NombreEmpleado { get; set; } = null!;
    public int CodigoConcepto { get; set; }
    public string DescripcionConcepto { get; set; } = null!;
    public string TipoConcepto { get; set; } = null!; // "E" (Earnings/Percepciones), "D" (Deductions/Descuentos), "I" (Información)
    public decimal Importe { get; set; }
    public string? Unidad { get; set; } // "U" (Unidades), "%" (Porcentaje), etc.
    public bool EsManual { get; set; }
    public bool EsEnEspecie { get; set; }
    public DateTime FechaUltimaSincronizacion { get; set; }

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft-Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// A3 INNUVA NÓMINAS - Nóminas calculadas (resultado de PHASE 2)
public class StagingA3InnuvaNominaCalculada : IAuditable, ISoftDeletable
{
    public int Id { get; set; }
    public string IdExterno { get; set; } = null!; // "{EmpleadoCode}_{PeriodCode}"
    public string CodigoEmpleado { get; set; } = null!;
    public string NombreEmpleado { get; set; } = null!;
    public string CodigoPeriodo { get; set; } = null!;
    public DateTime FechaPeriodo { get; set; }

    // Cálculos
    public decimal TotalPercepciones { get; set; } // Suma de conceptos tipo "E"
    public decimal TotalDescuentos { get; set; }   // Suma de conceptos tipo "D"
    public decimal SalarioNeto { get; set; }       // TotalPercepciones - TotalDescuentos

    // Control
    public bool FueEnviadoAWK { get; set; }        // ¿Fue enviado a Wolters Kluwer (PHASE 3)?
    public DateTime? FechaEnvio { get; set; }
    public string? ResponseWK { get; set; }        // Respuesta de Wolters Kluwer

    // Auditoría
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft-Delete
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

// ====== PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints ======

/// <summary>
/// PHASE 1.3a: Datos de salario del empleado
/// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/salary
/// </summary>
public class StagingA3InnuvaSalary : IStagingRow
{
    public int Id { get; set; }
    public string SalaryIdExterno { get; set; } = null!;  // {EmployeeId}_{ContractId}
    public string EmpleadoIdExterno { get; set; } = null!;
    public string ContratoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;

    // Datos de salary desde API
    public decimal ImporteBruto { get; set; }
    public decimal ImporteNeto { get; set; }
    public string? Moneda { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

/// <summary>
/// PHASE 1.3b: Retenciones de impuestos (IRPF en España)
/// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/irpf
/// </summary>
public class StagingA3InnuvaIRPF : IStagingRow
{
    public int Id { get; set; }
    public string IRPFIdExterno { get; set; } = null!;  // {EmployeeId}_{ImpuestoId}
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;

    // Datos de IRPF
    public string TipoImpuesto { get; set; } = null!;  // "IRPF", "SS", etc.
    public decimal PercentajeTariacion { get; set; }
    public decimal ImporteRetencion { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

/// <summary>
/// PHASE 1.3c: Remuneraciones (percepciones complementarias)
/// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/remuneration
/// </summary>
public class StagingA3InnuvaRemuneration : IStagingRow
{
    public int Id { get; set; }
    public string RemuneracionIdExterno { get; set; } = null!;  // {EmployeeId}_{RemuId}
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;

    // Datos de remuneration
    public string TipoRemuneracion { get; set; } = null!;  // "Bono", "Comisión", "Plus", etc.
    public decimal Importe { get; set; }
    public string? Concepto { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

/// <summary>
/// PHASE 1.3d: Cuentas bancarias del empleado
/// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/bankaccounts
/// </summary>
public class StagingA3InnuvaBankAccount : IStagingRow
{
    public int Id { get; set; }
    public string CuentaIdExterno { get; set; } = null!;  // {EmployeeId}_{CuentaId}
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;

    // Datos de cuenta bancaria
    public string IBAN { get; set; } = null!;
    public string? BIC { get; set; }
    public string? NombreTitular { get; set; }
    public string? TipoCuenta { get; set; }  // "Principal", "Adicional", etc.
    public bool EsPrincipal { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }

    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

/// <summary>
/// PHASE 1.3e: Acuerdos / Colectivos (datos de negociación colectiva)
/// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/agreements
/// </summary>
public class StagingA3InnuvaAgreement : IStagingRow
{
    public int Id { get; set; }
    public string AcuerdoIdExterno { get; set; } = null!;  // {EmployeeId}_{AgreementId}
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }
    public string NIF { get; set; } = null!;

    // Datos de agreement
    public string CodigoAcuerdo { get; set; } = null!;
    public string NombreAcuerdo { get; set; } = null!;
    public string? TipoAcuerdo { get; set; }  // "Colectivo", "Empresa", "Individual", etc.
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFin { get; set; }
    public string? Descripcion { get; set; }

    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

/// <summary>
/// PHASE 1.5: Acuerdo de Contrato (contractCode, labourPeriodStartDate, annualGrossAmount, etc.)
/// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/contract-agreement
/// </summary>
public class StagingA3InnuvaContractAgreement : IStagingRow
{
    public int Id { get; set; }
    public string ContratoIdExterno { get; set; } = null!;  // {EmployeeId}_{ContractCode}
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }

    // Datos de contrato-acuerdo
    public string? CodigoContrato { get; set; }
    public string? DescripcionContrato { get; set; }
    public DateTime? FechaInicioPeriodoLaboral { get; set; }
    public DateTime? FechaFinPeriodoLaboral { get; set; }
    public int? TipoAportacionID { get; set; }
    public string? TipoAportacion { get; set; }
    public string? ModalidadAportacion { get; set; }
    public string? CodigoOcupacionCNO { get; set; }
    public decimal? MontoAñualBruto { get; set; }
    public int? TipoCobroID { get; set; }
    public string? TipoCobro { get; set; }

    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}

/// <summary>
/// PHASE 1.6: Horario de Trabajo (totalWeekHours, workDayType, etc.)
/// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/contract/timetable
/// </summary>
public class StagingA3InnuvaContractTimetable : IStagingRow
{
    public int Id { get; set; }
    public string HorarioIdExterno { get; set; } = null!;  // {EmployeeId}_timetable
    public string EmpleadoIdExterno { get; set; } = null!;
    public int? UserId { get; set; }

    // Datos de horario de trabajo
    public string? TipoDiaLaboralID { get; set; }  // "Complete", "Partial", etc.
    public decimal? TotalHorasSemanal { get; set; }
    public string? DiaLaboralCompletoInicio { get; set; }  // "Monday", "Tuesday", etc.
    public string? DiaLaboralCompletoFin { get; set; }     // "Friday", "Thursday", etc.
    public bool? TieneHorasComplementarias { get; set; }
    public string? TipoPeriodoPartial { get; set; }
    public decimal? HorasPartial { get; set; }

    // IStagingRow
    public string PayloadJson { get; set; } = null!;
    public string Hash { get; set; } = null!;
    public DateTime FechaUltimaSincronizacion { get; set; }
    public bool FlagProcesado { get; set; }
    public string? ErrorProcesamiento { get; set; }
}
