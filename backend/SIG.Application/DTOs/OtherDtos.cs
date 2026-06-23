using SIG.Domain.Enums;

namespace SIG.Application.DTOs;

// Contratos A3 Innuva (Ola 2 #2 — contratos de un día)
public record ContratoUnDiaDto(
    int Id,
    string ContratoIdExterno,
    string NIF,
    DateTime FechaInicio,
    DateTime FechaFin,
    decimal ImporteBruto,
    int? UserId,
    string? UserNombre,
    bool IgnoradoEnCierre,
    string? MotivoIgnorar);
public record ContratoIgnorarRequest(bool Ignorar, string? Motivo);

// Dashboard
public record KpiClienteDto(int ClientId, string Nombre, decimal Facturacion, decimal Coste, decimal Margen, decimal PctTotal);
public record EvolucionPeriodoDto(string PeriodNombre, decimal Facturacion, decimal Coste, decimal Margen);

public record DashboardKpisDto(
    int PeriodId, string PeriodNombre,
    int CierresCompletados, int CierresPendientes,
    // PPT slide 3: distinguir cierre de costes vs cierre de facturación.
    int CierresCostesCompletados, int CierresCostesPendientes,
    int CierresFacturacionCompletados, int CierresFacturacionPendientes,
    decimal FacturacionTotal, decimal CosteTotal, decimal Margen, decimal MargenPct,
    IReadOnlyList<KpiClienteDto> DesglosePorCliente,
    IReadOnlyList<EvolucionPeriodoDto> Evolucion);

public record DashboardAvisoDto(string Tipo, string Descripcion, int? EntityId);

// Informes (PPT slide 23): reporting NATIVO en la app (no Power BI).
// Resultado: drill-down dpto→cliente→servicio con facturación/coste/margen (real, del año).
public record ReporteResultadoFilaDto(
    int? DepartmentId, string? DepartmentNombre,
    int ClientId, string ClientNombre,
    int ServiceId, string ServiceNombre,
    decimal Facturacion, decimal Coste, decimal Margen);
public record ReporteResultadoDto(int Anio, IReadOnlyList<ReporteResultadoFilaDto> Filas);

// Previsión (Forecast) vs Real (facturación/margen del cierre), por dpto/cliente/mes.
public record PrevisionRealCeldaDto(int Mes,
    decimal VentasPrevistas, decimal VentasReales,
    decimal MargenPrevisto, decimal MargenReal);
public record PrevisionRealFilaDto(
    int? DepartmentId, string? DepartmentNombre, int ClientId, string ClientNombre,
    IReadOnlyList<PrevisionRealCeldaDto> Meses,
    decimal TotalVentasPrevistas, decimal TotalVentasReales,
    decimal TotalMargenPrevisto, decimal TotalMargenReal);
public record PrevisionRealDto(int Anio, IReadOnlyList<PrevisionRealFilaDto> Filas);

public record MiServicioDto(
    int ServiceId, string Nombre, int ClientId, string ClientNombre,
    int? ClosureId, EstadoClosure? Estado, ApprovalStep? PasoActual,
    decimal? CosteTotal, decimal? FacturacionTotal, decimal? Margen);

// A3 ERP (Contabilidad) — estado del hub de traspaso de facturas a A3 ERP.
// Connected/Modo se derivan de la presencia de configuración (Integrations:A3Erp).
// El traspaso genera un fichero descargable; nunca escribe en sistemas del cliente.
public record A3ErpStatusDto(bool Connected, string Modo, string Mensaje);

// Calculation
public record CalculationDetailDto(int ClosureLineId, int ConceptId, string ConceptNombre, string FormulaSnapshotJson, string InputsJson, decimal Resultado, string? Incidencias, string SistemaOrigen, DateTime Timestamp);

// Sync / Audit
public record SyncResultDto(
    string Sistema,
    bool Exito,
    int RegistrosInsertados,
    int RegistrosActualizados,
    int RegistrosError,
    string? MensajeError = null,
    DateTime? FechaUltimaSincronizacion = null);

public record ProcessingResultDto(
    DateTime Timestamp,
    Dictionary<string, (int Processed, int Errors)> Systems,
    int TotalProcessed = 0,
    int TotalErrors = 0,
    string? Error = null);

// File Sync Result — when uploading Excel files for Galán, Mediapost, etc.
public record FileSyncResultDto(
    string TipoArchivo,                    // "Entradas", "Salidas", "Stock", "Almacenaje", "Pedidos", "Recepciones"
    bool Exito,
    int RegistrosInsertados,
    int RegistrosActualizados,
    int RegistrosDuplicados,
    int RegistrosError,
    string? MensajeError = null,
    DateTime? FechaSincronizacion = null,
    IReadOnlyList<string>? DetallesErrores = null);

public record AuditLogFilterRequest(int? UserId, string? EntityType, AuditAction? Action, DateOnly? Desde, DateOnly? Hasta, int Page = 1, int PageSize = 50);
public record AuditLogDto(long Id, int? UserId, string? UserNombre, string EntityType, string EntityId, AuditAction Action, string? OldValueJson, string? NewValueJson, DateTime Timestamp, string? Ip);

// SGPV Productos
public record SgpvProductoDto(
    string IdProducto,
    string IdCliente,
    string Cliente,
    string Categoria,
    string Subcategoria,
    string CodigoReferencia,
    string Referencia,
    string EAN,
    string Marca,
    string PVPRecomendado,
    string Competencia,
    bool Activo);

// Intratime Discrepancies
public record DiscrepanciaIntratimeDto(
    int UserId,
    decimal HorasIntratime,
    int VisitasCelero,
    decimal HorasSgpv,
    decimal HorasEsperadas,
    decimal Diferencia,
    bool TieneDiscrepancia);
