namespace SIG.Application.DTOs;

public record CeleroVisitaDto(
    string VisitaIdExterno,
    string ResourceNif,
    string ServiceName,
    string MissionName,
    DateOnly Fecha,
    // Campos de origen Celero ingestados para alimentar la segmentación del motor por PayloadJson
    // (ver docs/RETOMA_INPOST_FACTURACION.md §4.3). Se serializan completos al PayloadJson y el
    // RowAdapter los expone: "Estado" se mapea al flag de excepción tipado; el resto queda filtrable
    // por nombre vía el diccionario Extra. Opcionales para no romper construcciones existentes.
    int? DuracionMinutos = null,         // realDuration de Celero (unidad pendiente de confirmar, §4.2)
    string? Estado = null,               // visitStatus: done | failed | cancelled ...
    string? Provincia = null,            // addressState del centro/POA
    string? Ciudad = null,               // addressCity de la visita (enriquecimiento CAMBIO 4)
    string? CancellationReason = null,   // cancellationReason cuando la visita no se realiza
    string? Muebles = null,              // nombres de artículos extraídos de feedback→article (concatenados con |)
    string? TipoMueble = null);          // categorías de artículos extraídos de feedback→article (concatenadas con |)
public record BizneoEmpleadoDto(string EmpleadoIdExterno, string Email, string Nombre, string? Departamento);
public record BizneoAbsenceDto(string RegistroIdExterno, int UserId, int ServiceId, DateOnly Fecha, DateOnly? FechaFin, decimal Horas, string? Estado);
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
public record A3InnuvaEmpleadoDto(
    string IdExterno,
    string NIF,
    string Nombre,
    string? Departamento,
    decimal? SueldoMensual,
    DateTime FechaUltimaSincronizacion
);
public record A3InnuvaGenericoDto(string IdExterno, string Nombre, string Tipo, DateTime? FechaRegistro);
public record TravelPerkViajeDto(string ViajeIdExterno, string Solicitante, DateOnly FechaInicio, DateOnly? FechaFin, decimal Presupuesto, string Estado);
// TravelPerk se integra por descarga Excel (hoja "report"), a nivel LÍNEA — no a nivel viaje.
// CostObject = "Cost object" = proyecto/CECO al que se imputa el coste; CosteSinIVA = "Cost per traveler without tax".
public record TravelPerkLineaDto(
    string TripId,
    string Service,
    string? CostObject,
    decimal CosteSinIVA,
    string? TravelerEmail,
    string? Currency,
    DateOnly? FechaGasto);
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

public record EmployeeDto(
    string EmployeeId,
    string EmployeeCode,
    string CompleteName,
    string IdentifierNumber,
    string? WorkplaceCode,
    DateTime? EnrolmentDate
);

public record ConceptoDto(
    int? ConceptCode,
    string Description,
    decimal Amount,
    string ConceptType,
    bool InKind,
    bool Manual,
    string ConceptCollectionTypeDesc,
    string? CodigoEmpleado = null,
    string? NombreEmpleado = null
);

public record A3InnuvaNominaCalculadaDto(
    string IdExterno,
    string CodigoEmpleado,
    string NombreEmpleado,
    string CodigoPeriodo,
    decimal TotalPercepciones,
    decimal TotalDescuentos,
    decimal SalarioNeto,
    bool FueEnviadoAWK,
    DateTime? FechaEnvio,
    string? ResponseWK
);

public record A3InnuvaConceptoDto(
    string IdExterno,
    string CodigoEmpleado,
    string NombreEmpleado,
    int CodigoConcepto,
    string DescripcionConcepto,
    string TipoConcepto,
    decimal Importe,
    string? Unidad,
    bool EsManual,
    bool EsEnEspecie,
    DateTime FechaUltimaSincronizacion
);

// ====== PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints ======

public record SalaryDto(
    string IdExterno,
    string EmployeeCode,
    string NIF,
    decimal GrossSalary,
    decimal NetSalary,
    string? Currency,
    DateTime StartDate,
    DateTime? EndDate
);

public record IRPFDto(
    string IdExterno,
    string EmployeeCode,
    string NIF,
    string TaxType,
    decimal TaxRate,
    decimal RetentionAmount,
    DateTime StartDate,
    DateTime? EndDate
);

public record RemunerationDto(
    string IdExterno,
    string EmployeeCode,
    string NIF,
    string RemunerationType,
    decimal Amount,
    string? Concept,
    DateTime StartDate,
    DateTime? EndDate
);

public record BankAccountDto(
    string IdExterno,
    string EmployeeCode,
    string NIF,
    string IBAN,
    string? BIC,
    string? AccountHolderName,
    string? AccountType,
    bool IsPrimary,
    DateTime StartDate,
    DateTime? EndDate
);

public record AgreementDto(
    string IdExterno,
    string EmployeeCode,
    string NIF,
    string AgreementCode,
    string AgreementName,
    string? AgreementType,
    DateTime StartDate,
    DateTime? EndDate,
    string? Description
);

// CONTRACT AGREEMENT - Datos del contrato (contractCode, labourPeriodStartDate, etc.)
public record ContractAgreementDto(
    string IdExterno,
    string EmployeeCode,
    string? ContractCode,
    string? ContractDescription,
    DateTime? LabourPeriodStartDate,
    DateTime? LabourPeriodEndDate,
    int? ContributionTypeID,
    string? ContributionType,
    string? ContributionModalityType,
    string? CnoOccupationID,
    decimal? AnnualGrossAmount,
    int? CollectionTypeID,
    string? CollectionType
);

// CONTRACT TIMETABLE - Datos de horario de trabajo
public record ContractTimetableDto(
    string IdExterno,
    string EmployeeCode,
    string? WorkDayTypeID,
    decimal? TotalWeekHours,
    string? CompleteWorkDayStartID,
    string? CompleteWorkDayEndID,
    bool? IndComplementaryHours,
    string? PartialPeriodTypeID,
    decimal? PartialHours
);

// CONTRACT CLAUSES - Cláusulas del contrato (opcional, puede estar vacío)
public record ContractClauseDto(
    string IdExterno,
    string EmployeeCode,
    string? ClauseCode,
    string? ClauseDescription,
    DateTime? StartDate,
    DateTime? EndDate
);

// A3 INNUVA ERP - Wolters Kluwer OINV API (Facturación)
public record A3ERPFacturaDto(
    string IdExterno,
    string CodigoFactura,
    string CodigoCliente,
    string NombreCliente,
    DateTime FechaFactura,
    decimal ImporteBase,
    decimal ImporteIVA,
    decimal ImporteTotal,
    string Estado,
    DateTime? FechaVencimiento
);

public record A3ERPClienteDto(
    string IdExterno,
    string CodigoCliente,
    string NombreCliente,
    string? NIF,
    string? Email,
    string? Telefono,
    string? Direccion,
    string? Ciudad,
    string? CodigoPostal
);

public record A3ERPLineaFacturaDto(
    string IdExterno,
    string FacturaId,
    string CodigoProducto,
    string DescripcionProducto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal ImporteLinea,
    string? ConceptoFacturacion
);
