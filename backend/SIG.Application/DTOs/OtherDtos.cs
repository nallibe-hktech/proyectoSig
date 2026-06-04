using SIG.Domain.Enums;

namespace SIG.Application.DTOs;

// Dashboard
public record DashboardKpisDto(int PeriodId, string PeriodNombre, int CierresCompletados, int CierresPendientes, decimal FacturacionTotal, decimal CosteTotal, decimal Margen);
public record DashboardAvisoDto(string Tipo, string Descripcion, int? EntityId);
public record MiProyectoDto(int ProjectId, string Nombre, int ClientId, string ClientNombre, int? ClosureId, EstadoClosure? Estado, ApprovalStep? PasoActual);

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
public record AuditLogFilterRequest(int? UserId, string? EntityType, AuditAction? Action, DateOnly? Desde, DateOnly? Hasta, int Page = 1, int PageSize = 50);
public record AuditLogDto(long Id, int? UserId, string? UserNombre, string EntityType, string EntityId, AuditAction Action, string? OldValueJson, string? NewValueJson, DateTime Timestamp, string? Ip);
