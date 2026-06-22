namespace SIG.Application.DTOs;

public record CeleroVisitaDto(
    string VisitaIdExterno,
    string ResourceNif,
    string ServiceName,
    string MissionName,
    DateOnly Fecha);
public record BizneoEmpleadoDto(string EmpleadoIdExterno, string NIF, string Nombre, string? Departamento);
public record BizneoAbsenceDto(string RegistroIdExterno, int UserId, int ServiceId, DateOnly Fecha, decimal Horas);
public record IntratimeEmpleadoDto(
    string UserIdExterno,   // USER_ID (ej: "20875")
    string Nombre,          // USER_NAME
    string Email,           // USER_EMAIL
    string NIF,             // USER_NIF
    string? Affiliation,    // USER_AFFILIATION
    int Role                // USER_ROLE
);
public record IntratimeFichajeDto(string FichajeIdExterno, string UserIdExterno, DateTime Entrada, DateTime? Salida);
public record IntratimeClockingRequestDto(
    string RequestIdExterno,        // REQUEST_ID
    string UserIdExterno,           // USER_ID
    DateTime FechaRequest,          // REQUEST_DATE
    string TipoRequest,             // REQUEST_TYPE (ej: "Ajuste", "Corrección")
    string Estado,                  // REQUEST_STATUS (ej: "Pendiente", "Aprobado")
    string? Razon,                  // REQUEST_REASON
    string? HoraDesde,              // REQUESTED_TIME_FROM
    string? HoraHasta               // REQUESTED_TIME_TO
);
public record PayHawkGastoDto(string GastoIdExterno, int UserId, int ServiceId, DateOnly Fecha, decimal Importe, string Categoria);
public record SgpvVisitaDto(
    string VisitaIdExterno,
    string ResourceNif,
    string CentroId,
    string? CentroNombre,
    string? ServiceName,
    DateOnly Fecha,
    decimal? HorasDuracion);
public record A3InnuvaEmpleadoDto(string EmpleadoIdExterno, string NIF, string Nombre, string? Departamento, decimal? SueldoMensual);
public record A3InnuvaGenericoDto(string IdExterno, string Nombre, string Tipo, DateTime? FechaRegistro);
public record TravelPerkViajeDto(string ViajeIdExterno, string Solicitante, DateOnly FechaInicio, DateOnly? FechaFin, decimal Presupuesto, string Estado);
public record IntratimeExpenseDto(string ExpenseIdExterno, string? UserIdExterno, DateTime FechaExpense, decimal Cantidad, string NombreExpense, string? Descripcion, string? ProyectoNombre);
public record IntratimeClienteDto(
    string ClienteIdExterno,        // CLIENT_ID
    string Nombre,                  // CLIENT_NAME
    string? Pais,                   // CLIENT_COUNTRY
    string? Region,                 // CLIENT_REGION
    string? Ciudad,                 // CLIENT_CITY
    string? Direccion               // CLIENT_ADDRESS
);
public record IntratimeProyectoDto(
    string ProyectoIdExterno,       // PROJECT_ID
    string Nombre,                  // PROJECT_NAME
    IntratimeClienteDto Cliente,    // cliente anidado
    List<string> UsuariosIds        // USER_IDs asignados
);

// GALÁN - Logística y Almacenes
public record GalanEntradaDto(
    string CodigoArticulo,
    string CodigoDepartamento,
    string CodigoFamilia,
    string Descripcion,
    DateTime Fecha,
    int Unidades,
    string Empresa,
    string Almacen,
    string Celda
);

public record GalanSalidaDto(
    string Albaran,
    string? NumeroPedidoTercero,
    string CodigoArticulo,
    string CodigoDepartamento,
    string CodigoFamilia,
    string Descripcion,
    int Unidades,
    string? CodigoTransporte,
    string? Matricula,
    DateTime Fecha,
    string? Destinatario,
    string Almacen,
    string Celda
);

public record GalanStockDto(
    string CodigoArticulo,
    string CodigoDepartamento,
    string CodigoFamilia,
    string CodigoCelda,
    decimal StockB,
    decimal StockA,
    decimal Stock,
    string Almacen,
    string Familia,
    string SubFamilia,
    string Descripcion
);

// MEDIAPOST - Distribución
public record MediapostPedidoDto(
    string PedidoId,
    string ReferenciaPedido,
    string CodigoArticulo,
    DateTime FechaPedido,
    int Cantidad,
    string Estado,
    string? DestinatarioNombre,
    string? DireccionEntrega,
    string? CodigoPostal,
    string? Ciudad,
    string? Provincia
);

public record MediapostRecepcionDto(
    string RecepcionId,
    string ReferenciaRecepcion,
    string CodigoArticulo,
    DateTime FechaRecepcion,
    int Cantidad,
    int? CantidadDañada,
    string Estado,
    string? Almacen,
    string? Observaciones
);

// A3 INNUVA NÓMINAS - Nóminas y Gestión de Personas
public record A3InnuvaNominasCompanyDto(
    string Id,
    string Code,
    string Name,
    string TaxId,
    string? Address,
    string? City,
    string? Country,
    string? ContactEmail,
    string? ContactPhone
);

public record A3InnuvaNominasPayrollDto(
    string Id,
    string EmployeeId,
    string EmployeeName,
    string PeriodCode,
    decimal BaseSalary,
    decimal Deductions,
    decimal NetSalary,
    DateTime ProcessDate
);
