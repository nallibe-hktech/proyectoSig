namespace SIG.Application.DTOs;

/// <summary>Una línea de TravelPerk en staging, tal como la muestra el dashboard.</summary>
public record TravelPerkLineaListDto(
    int Id,
    string TripId,
    string Service,
    string? CostObject,
    string Ceco,
    int? ServiceId,
    decimal CosteSinIVA,
    DateOnly? FechaGasto,
    string? TravelerEmail,
    string? Currency,
    bool EsGastoInternoSig,   // sin "Cost object" → gasto propio de SIG (CECO 0423)
    bool CecoNoMaestro,       // CECO de cliente que no casa con la tabla maestra → sin imputar (alerta)
    DateTime FechaUltimaSincronizacion);

/// <summary>KPIs de cabecera del dashboard de TravelPerk.</summary>
public record TravelPerkKpisDto(
    int TotalLineas,
    decimal TotalSinIVA,
    int LineasImputadas,
    decimal CosteImputado,
    int LineasGastoInternoSig,
    decimal CosteGastoInternoSig,
    int LineasCecoNoMaestro);
