using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Http;

// Cliente HTTP tipado. NO se invoca en MVP — se registra solo cuando Integrations:UseFake = false.
// El método real lanzaría peticiones HTTP al sistema externo. En MVP devuelve listas vacías.

public class CeleroClient : ICeleroClient
{
    private readonly HttpClient _http;
    public CeleroClient(HttpClient http) { _http = http; }
    public Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CeleroVisitaDto>>(Array.Empty<CeleroVisitaDto>());
}

public class BizneoClient : IBizneoClient
{
    private readonly HttpClient _http;
    public BizneoClient(HttpClient http) { _http = http; }
    public Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BizneoEmpleadoDto>>(Array.Empty<BizneoEmpleadoDto>());
    public Task<IReadOnlyList<BizneoHoraDto>> GetHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BizneoHoraDto>>(Array.Empty<BizneoHoraDto>());
}

public class IntratimeClient : IIntratimeClient
{
    private readonly HttpClient _http;
    public IntratimeClient(HttpClient http) { _http = http; }
    public Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<IntratimeFichajeDto>>(Array.Empty<IntratimeFichajeDto>());
}

public class PayHawkClient : IPayHawkClient
{
    private readonly HttpClient _http;
    public PayHawkClient(HttpClient http) { _http = http; }
    public Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<PayHawkGastoDto>>(Array.Empty<PayHawkGastoDto>());
}

public class SgpvClient : ISgpvClient
{
    private readonly HttpClient _http;
    public SgpvClient(HttpClient http) { _http = http; }
    public Task<IReadOnlyList<SgpvVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SgpvVisitaDto>>(Array.Empty<SgpvVisitaDto>());
    // TODO: implementar login HTTPS + download cuando se tengan credenciales SGPV
}
