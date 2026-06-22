using SIG.Application.DTOs;

namespace SIG.Application.Interfaces.Integrations;

public interface ICeleroClient
{
    Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IBizneoClient
{
    Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<BizneoAbsenceDto>> GetAbsencesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IIntratimeClient
{
    Task<IReadOnlyList<IntratimeEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task<IReadOnlyList<IntratimeClockingRequestDto>> GetClockingRequestsAsync(int year, CancellationToken ct);
    Task<IReadOnlyList<IntratimeExpenseDto>> GetExpensesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task<IReadOnlyList<IntratimeProyectoDto>> GetProyectosAsync(CancellationToken ct);
}

public interface IPayHawkClient
{
    Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface ISgpvClient
{
    Task<IReadOnlyList<SgpvVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task<IReadOnlyList<SgpvProductoDto>> GetProductosAsync(CancellationToken ct);
}

public interface IA3InnuvaClient
{
    Task<IReadOnlyList<A3InnuvaEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
}

public interface ITravelPerkClient
{
    Task<IReadOnlyList<TravelPerkViajeDto>> GetViajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IA3InnuvaNominasClient
{
    Task<IReadOnlyList<A3InnuvaNominasCompanyDto>> GetCompaniesAsync(
        int pageNumber = 1,
        int pageSize = 25,
        DateTime? lastUpdate = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<A3InnuvaNominasPayrollDto>> GetPayrollsAsync(
        string companyCode,
        int pageNumber = 1,
        int pageSize = 25,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);
}

public interface IGalanClient
{
    Task<IReadOnlyList<GalanEntradaDto>> GetEntradasAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<GalanSalidaDto>> GetSalidasAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<GalanStockDto>> GetStockAsync(CancellationToken ct);
}

public interface IMediapostClient
{
    Task<IReadOnlyList<MediapostPedidoDto>> GetPedidosAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<MediapostRecepcionDto>> GetRecepcionesAsync(DateTime desde, DateTime hasta, CancellationToken ct);
}
