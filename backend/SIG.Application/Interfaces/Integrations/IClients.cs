using SIG.Application.DTOs;

namespace SIG.Application.Interfaces.Integrations;

public interface ICeleroClient
{
    Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IBizneoClient
{
    Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<BizneoHoraDto>> GetHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IIntratimeClient
{
    Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IPayHawkClient
{
    Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}
