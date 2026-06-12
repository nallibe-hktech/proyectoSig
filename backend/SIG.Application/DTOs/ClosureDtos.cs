using SIG.Domain.Enums;

namespace SIG.Application.DTOs;

public record ClosureListItemDto(int Id, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal CosteTotal, decimal FacturacionTotal, decimal Margen, EstadoClosure Estado, ApprovalStep PasoActual);
public record ClosureDetailDto(int Id, int ServiceId, string ServiceNombre, int PeriodId, string PeriodNombre, decimal CosteTotal, decimal FacturacionTotal, decimal Margen, EstadoClosure Estado, ApprovalStep PasoActual, string? Comentarios, uint RowVersion, ClosureLineDto[] Lines, ApprovalDto[] Approvals);
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
    string? SourceDataSummary = null,
    string? InputMetadata = null);
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
