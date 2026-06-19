using SIG.Domain.Enums;

namespace SIG.Application.DTOs;

// Client
public record ClientListItemDto(int Id, string Nombre, string NIF, string? Ciudad, EstadoCliente Estado, int ServiceCount);
public record ClientDetailDto(int Id, string Nombre, string NIF, EstadoCliente Estado, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);
public record ClientCreateRequest(string Nombre, string NIF, EstadoCliente? Estado, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);
public record ClientUpdateRequest(string Nombre, string NIF, EstadoCliente Estado, string? Direccion, string? Ciudad, string? Provincia, string? Pais, string? CodigoPostal, string? ContactoNombre, string? ContactoEmail, string? ContactoTelefono);

// Incidencias del cliente (PPT slide 6 + prototipo): tipo (texto libre), explicación, estado, origen/responsable,
// fecha de apertura y un histórico de cambios. Editables; alta manual (no provienen de un sistema origen).
public record ClienteIncidenciaDto(
    int Id, int ClientId, string Tipo, string Descripcion, EstadoIncidencia Estado,
    string? Origen, DateTime FechaApertura, DateTime CreatedAt, DateTime UpdatedAt,
    IReadOnlyList<IncidenciaHistorialDto> Historial);
public record IncidenciaHistorialDto(int Id, EstadoIncidencia Estado, string Nota, string? Responsable, DateTime Fecha);
// Listado global (pantalla de 1er nivel): añade el nombre del cliente para la tabla.
public record IncidenciaListItemDto(
    int Id, int ClientId, string ClientNombre, string Tipo, string Descripcion,
    EstadoIncidencia Estado, string? Origen, DateTime FechaApertura);
public record ClienteIncidenciaCreateRequest(string Tipo, string Descripcion, EstadoIncidencia? Estado, string? Origen, DateTime? FechaApertura);
public record ClienteIncidenciaUpdateRequest(string Tipo, string Descripcion, EstadoIncidencia Estado, string? Origen);
// Cambio de estado desde el panel de detalle (registra una entrada en el histórico).
public record IncidenciaCambioEstadoRequest(EstadoIncidencia Estado, string Nota, string? Responsable);

// Configuración de Presupuesto (prototipo 24/28): partidas manuales por acción/servicio (Anual/Total acción)
// con presupuesto/consumido; Restante y Avance se calculan. La pantalla añade KPIs y márgenes objetivo/real.
public record PartidaPresupuestoDto(
    int Id, int ServiceId, string Nombre, TipoPartidaPresupuesto Tipo, int? Anio,
    decimal Presupuesto, decimal Consumido, decimal Restante, decimal AvancePct, string? Descripcion);
public record PartidaPresupuestoCreateRequest(
    string Nombre, TipoPartidaPresupuesto Tipo, int? Anio, decimal Presupuesto, decimal Consumido, string? Descripcion);
public record PartidaPresupuestoUpdateRequest(
    string Nombre, TipoPartidaPresupuesto Tipo, int? Anio, decimal Presupuesto, decimal Consumido, string? Descripcion);
// Cabecera de la pantalla: totales + márgenes (objetivo manual, real calculado de los cierres) + partidas.
public record ConfigPresupuestoDto(
    int ServiceId, string ServiceNombre, string ClientNombre,
    decimal TotalPresupuesto, decimal TotalConsumido, decimal TotalRestante, decimal AvancePct,
    decimal? MargenObjetivoPct, decimal? MargenRealPct, decimal? DesviacionPp,
    int PartidasAnuales, int PartidasTotalAccion,
    IReadOnlyList<PartidaPresupuestoDto> Partidas);
// Fija el margen operativo objetivo (%) de la acción.
public record MargenObjetivoRequest(decimal? MargenObjetivoPct);

// Configuración de Factura (prototipo 25/28): categorías que agrupan conceptos de facturación por cliente.
public record CategoriaFacturaDto(int Id, int ClientId, string Nombre, IReadOnlyList<CategoriaFacturaConceptoDto> Conceptos);
public record CategoriaFacturaConceptoDto(int ConceptId, string Nombre);
// Alta/edición: nombre + lista de conceptos que suma. El servicio valida que sean del cliente y que no
// estén ya en otra categoría (devuelve 409 si lo están).
public record CategoriaFacturaCreateRequest(string Nombre, int[] ConceptIds);
public record CategoriaFacturaUpdateRequest(string Nombre, int[] ConceptIds);
// Panel derecho "conceptos disponibles del cliente": conceptos de facturación del cliente con su estado.
public record ConceptoDisponibleDto(int ConceptId, string Nombre, bool Asignado, int? CategoriaFacturaId, string? CategoriaNombre);
// KPIs de cabecera de la pantalla.
public record ConfigFacturaResumenDto(int NumCategorias, int ConceptosMapeados, int ConceptosSinAsignar);

// Service (antes Action; absorbe el vínculo directo a Client y los metadatos de Project)
public record ServiceListItemDto(int Id, string Nombre, int ClientId, string ClientNombre, int? DepartmentId, EstadoServicio Estado);
public record ServiceDetailDto(int Id, string Nombre, int ClientId, string ClientNombre, int? DepartmentId, EstadoServicio Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds, int[] ConceptIds);
public record ServiceCreateRequest(string Nombre, int ClientId, int? DepartmentId, EstadoServicio Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds, int[] ConceptIds);
public record ServiceUpdateRequest(string Nombre, int ClientId, int? DepartmentId, EstadoServicio Estado, string? InterlocutorNombre, string? InterlocutorEmail, string? InterlocutorTelefono, DateOnly FechaAlta, int[] CostCenterIds, int[] UserIds, int[] ConceptIds);

// Concept
public record ConceptListItemDto(int Id, string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta);
public record ConceptDetailDto(int Id, string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ServiceIds, int[] UserIds);
public record ConceptCreateRequest(string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ServiceIds, int[] UserIds);
public record ConceptUpdateRequest(string Nombre, TipoConcepto Tipo, DateOnly FechaDesde, DateOnly? FechaHasta, string FormulaJson, int[] ServiceIds, int[] UserIds);
public record ValidarFormulaRequest(string FormulaJson);
public record ValidarFormulaResponse(bool Ok, string[] Errores);

// Variable
public record VariableDto(int Id, string Nombre, string QuestionIdExterno, string MapeoValoresJson);
public record VariableCreateRequest(string Nombre, string QuestionIdExterno, string MapeoValoresJson);
public record VariableUpdateRequest(string Nombre, string QuestionIdExterno, string MapeoValoresJson);

// User
public record UserListItemDto(int Id, string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, string[] Roles);
public record UserDetailDto(int Id, string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserCreateRequest(string NIF, string Nombre, string Apellidos, string Email, string Password, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserUpdateRequest(string NIF, string Nombre, string Apellidos, string Email, EstadoUsuario Estado, int? DepartmentId, int[] RoleIds);
public record UserPasswordChangeRequest(string NewPassword);

// Role / Department / CostCenter
public record RoleDto(int Id, string Nombre, string? Descripcion);
public record DepartmentDto(int Id, string Nombre);
public record DepartmentCreateRequest(string Nombre);
public record DepartmentUpdateRequest(string Nombre);
public record CostCenterDto(int Id, string Codigo, string Nombre);
public record CostCenterCreateRequest(string Codigo, string Nombre);
public record CostCenterUpdateRequest(string Codigo, string Nombre);

// Period
public record PeriodDto(int Id, string Nombre, DateOnly FechaInicio, DateOnly FechaFin, int DiaPago, EstadoPeriodo Estado);
public record PeriodCreateRequest(string Nombre, DateOnly FechaInicio, DateOnly FechaFin, int DiaPago);
public record PeriodUpdateRequest(string Nombre, DateOnly FechaInicio, DateOnly FechaFin, int DiaPago);

// TarifaServicio
public record TarifaServicioDto(int Id, int ServiceId, string Nombre, decimal Valor, string? Unidad, DateOnly FechaDesde, DateOnly? FechaHasta);
public record TarifaServicioCreateRequest(string Nombre, decimal Valor, string? Unidad, DateOnly FechaDesde, DateOnly? FechaHasta);
public record TarifaServicioUpdateRequest(string Nombre, decimal Valor, string? Unidad, DateOnly FechaDesde, DateOnly? FechaHasta);

// PresupuestoServicio
public record PresupuestoServicioDto(int Id, int ServiceId, int? PeriodId, TipoConcepto Tipo, decimal Importe, string? Descripcion);
public record PresupuestoServicioCreateRequest(int? PeriodId, TipoConcepto Tipo, decimal Importe, string? Descripcion);
public record PresupuestoServicioUpdateRequest(int? PeriodId, TipoConcepto Tipo, decimal Importe, string? Descripcion);

// Forecast (PPT slide 36): previsión mensual de ventas/margen/GPP por servicio.
public record ForecastDto(int Id, int ServiceId, int Anio, int Mes, decimal VentasPrevistas, decimal? MargenPrevisto, int? PersonasCampo);
// Upsert por mes: si existe (servicio, año, mes) actualiza; si no, crea.
public record ForecastUpsertRequest(int Anio, int Mes, decimal VentasPrevistas, decimal? MargenPrevisto, int? PersonasCampo);

// Resumen tipo pivote (slide 36): filas por dpto+cliente, columnas por mes, totales.
public record ForecastResumenCeldaDto(int Mes, decimal Ventas, decimal Margen, int Personas);
public record ForecastResumenFilaDto(
    int? DepartmentId, string? DepartmentNombre, int ClientId, string ClientNombre,
    IReadOnlyList<ForecastResumenCeldaDto> Meses,
    decimal TotalVentas, decimal TotalMargen, int TotalPersonas);
public record ForecastResumenDto(int Anio, IReadOnlyList<ForecastResumenFilaDto> Filas);
