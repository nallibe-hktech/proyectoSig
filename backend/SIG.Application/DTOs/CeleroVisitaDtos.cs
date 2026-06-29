namespace SIG.Application.DTOs;

// DTOs para importación de clientes y servicios desde la BD de Celero
public record CeleroClienteDto(
    string IdExterno,        // Celero client.id (UUID o int como string)
    string Nombre,
    string Nif,
    string? Direccion,
    string? Ciudad,
    string? Provincia,
    string? CodigoPostal,
    string? ContactoEmail,
    string? ContactoTelefono
);

public record CeleroServicioDto(
    string IdExterno,          // Celero service.id
    string Nombre,
    string ClienteIdExterno    // Celero client.id (FK → CeleroClienteDto.IdExterno)
);

public record CeleroDepartmentDto(
    string IdExterno,          // Celero department.id
    string Nombre,
    string? Notas
);

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

// Request para actualizar mapeos, notas y datos faltantes
public record CeleroVisitaUpdateRequest(
    string? ResourceNif,
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
