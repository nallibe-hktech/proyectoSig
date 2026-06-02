using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Http;

// Cliente HTTP tipado. NO se invoca en MVP — se registra solo cuando Integrations:UseFake = false.
// El método real lanzaría peticiones HTTP al sistema externo. En MVP devuelve listas vacías.

public class CeleroClient : ICeleroClient
{
    public Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CeleroVisitaDto>>(Array.Empty<CeleroVisitaDto>());
}

public class BizneoClient : IBizneoClient
{
    public BizneoClient(HttpClient _) { }
    public Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BizneoEmpleadoDto>>(Array.Empty<BizneoEmpleadoDto>());
    public Task<IReadOnlyList<BizneoHoraDto>> GetHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BizneoHoraDto>>(Array.Empty<BizneoHoraDto>());
}

public class IntratimeClient : IIntratimeClient
{
    public IntratimeClient(HttpClient _) { }
    public Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<IntratimeFichajeDto>>(Array.Empty<IntratimeFichajeDto>());
}

public class PayHawkClient : IPayHawkClient
{
    public PayHawkClient(HttpClient _) { }
    public Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PayHawkGastoDto>>(Array.Empty<PayHawkGastoDto>());
}

public class SgpvClient : ISgpvClient
{
    public SgpvClient(HttpClient _) { }
    // TODO: implementar login HTTPS + download cuando se tengan credenciales SGPV
    public Task<IReadOnlyList<SgpvVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SgpvVisitaDto>>(Array.Empty<SgpvVisitaDto>());
}
