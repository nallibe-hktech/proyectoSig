using SIG.Domain.Enums;

namespace SIG.Application.DTOs;

public record ClosureListItemDto(int Id, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal CosteTotal, decimal FacturacionTotal, decimal Margen, EstadoClosure Estado, ApprovalStep PasoActual);
public record ClosureDetailDto(int Id, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal CosteTotal, decimal FacturacionTotal, decimal Margen, EstadoClosure Estado, ApprovalStep PasoActual, string? Comentarios, uint RowVersion, ClosureLineDto[] Lines, ApprovalDto[] Approvals, ClosureAlertaDto[] Alertas);
public record ClosureLineDto(
    int Id,
    int ConceptId,
    string ConceptNombre,
    int? UserId,
    string? UserNombre,
    decimal Importe,
    TipoConcepto Tipo,
    bool TieneIncidencia,
    uint RowVersion,
    bool EsManual = false,
    decimal? ImporteOriginal = null,
    string? MotivoManual = null,
    string? SourceDataSummary = null,
    string? InputMetadata = null);

// Ola 2 (#3a): override manual de importe de línea y alta de línea de incentivo manual.
public record ClosureLineOverrideRequest(decimal Importe, string Motivo);
public record ClosureLineIncentivoRequest(int ConceptId, TipoConcepto Tipo, decimal Importe, string Motivo, int? UserId);
public record ClosureCreateRequest(int ServiceId, int PeriodId, string? Comentarios);
public record ClosureRecalcRequest(string? Comentarios);

public record ApprovalDto(int Id, ApprovalStep Paso, int RoleId, string RoleNombre, EstadoApproval Estado, int? UserId, string? UserNombre, string? Motivo, DateTime? FechaDecision);
public record ClosureApproveRequest(string? Comentarios);
public record ClosureRejectRequest(string Motivo);

public record ApprovalFilterRequest(int? PeriodId, int? ClientId, int? CostCenterId, EstadoClosure? Estado, int? UserId, int? DepartmentId, TipoConcepto? Tipo, int? ConceptId, int Page = 1, int PageSize = 25);
public record ApprovalPanelItemDto(int ClosureId, int ServiceId, string ServiceNombre, int ClientId, string ClientNombre, int PeriodId, string PeriodNombre, EstadoClosure Estado, ApprovalStep PasoActual, string PasoActualRol, decimal Margen, DateTime UpdatedAt);
public record ApprovalHistoryDto(int Id, int ClosureId, int UserId, string UserNombre, ApprovalStep PasoOrigen, ApprovalStep PasoDestino, string Accion, string? Motivo, DateTime Timestamp);
public record BatchApproveRequest(int[] Ids);
public record BatchRejectRequest(int[] Ids);

public record ClosureAlertaDto(
    int Id,
    TipoAlerta Tipo,
    string Codigo,
    string Descripcion,
    string? Detalle,
    bool Confirmada,
    string? ConfirmadaPorNombre,
    DateTime? FechaConfirmacion
);

// ─────────────────── Ola 3b (#10): cierres separados ───────────────────
// Diseño A — raíces separadas, hijos por FK. "Margen al vuelo" (no se almacena).
public record CierreListItemDto(int Id, TipoCierre TipoCierre, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal Total, EstadoClosure Estado, ApprovalStep PasoActual);
public record CierreDetailDto(int Id, TipoCierre TipoCierre, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal Total, EstadoClosure Estado, ApprovalStep PasoActual, string? Comentarios, uint RowVersion, ClosureLineDto[] Lines, ApprovalDto[] Approvals, ClosureAlertaDto[] Alertas);
public record CierreCreateRequest(int ServiceId, int PeriodId, string? Comentarios);
public record CierreRecalcRequest(string? Comentarios);
public record CierreLineOverrideRequest(decimal Importe, string Motivo);
public record CierreLineIncentivoRequest(int ConceptId, decimal Importe, string Motivo, int? UserId);
public record CierreApproveRequest(string? Comentarios);
public record CierreRejectRequest(string Motivo);

// Panel de aprobaciones agregando AMBOS tipos de cierre (indica el tipo).
public record CierrePanelItemDto(int CierreId, TipoCierre TipoCierre, int ServiceId, string ServiceNombre, int ClientId, string ClientNombre, int PeriodId, string PeriodNombre, EstadoClosure Estado, ApprovalStep PasoActual, string PasoActualRol, decimal Total, DateTime UpdatedAt);
public record CierreHistoryDto(int Id, int CierreId, TipoCierre TipoCierre, int UserId, string UserNombre, ApprovalStep PasoOrigen, ApprovalStep PasoDestino, string Accion, string? Motivo, DateTime Timestamp);

// Dashboard: margen al vuelo emparejando costes+facturación por (ServiceId, PeriodId).
public record MargenServicioPeriodoDto(int ServiceId, string ServiceNombre, int PeriodId, decimal CosteTotal, decimal FacturacionTotal, decimal Margen);
