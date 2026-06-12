namespace SIG.Application.DTOs;

// DTO para listar visitas
public record CeleroVisitaListDto(
    int Id,
    string VisitaIdExterno,
    string ResourceNif,
    string ServiceName,
    string MissionName,
    DateOnly Fecha,
    int? UserId,
    int? ServiceId,
    string? Notas,
    string? EstadoMapeo
);

// DTO para detalle de una visita
public record CeleroVisitaDetailDto(
    int Id,
    string VisitaIdExterno,
    string ResourceNif,
    string ServiceName,
    string MissionName,
    DateOnly Fecha,
    int? UserId,
    int? ServiceId,
    string? Notas,
    int? MapeadoPor,
    DateTime? FechaMapeo,
    string? EstadoMapeo
);

// Request para actualizar mapeos y notas
public record CeleroVisitaUpdateRequest(
    int? UserId,
    int? ServiceId,
    string? Notas,
    string? EstadoMapeo
);

// Response con resultado paginado
public record PagedCeleroVisitasDto(
    List<CeleroVisitaListDto> Items,
    int Total,
    int Page,
    int PageSize
);
