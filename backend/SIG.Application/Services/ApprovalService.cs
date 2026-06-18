using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;

namespace SIG.Application.Services;

// Ola 3b (#10): el panel/pendientes agrega AMBOS tipos de cierre, indicando el tipo de cada uno.
public class ApprovalService : IApprovalService
{
    private readonly ICierreCostesRepository _costesRepo;
    private readonly ICierreFacturacionRepository _facturacionRepo;

    public ApprovalService(ICierreCostesRepository costesRepo, ICierreFacturacionRepository facturacionRepo)
    {
        _costesRepo = costesRepo;
        _facturacionRepo = facturacionRepo;
    }

    private static CierrePanelItemDto Map(ICierre c, TipoCierre tipo) => new(
        c.Id, tipo, c.ServiceId, c.Service?.Nombre ?? "",
        c.Service?.ClientId ?? 0, c.Service?.Client?.Nombre ?? "",
        c.PeriodId, c.Period?.Nombre ?? "",
        c.Estado, c.PasoActual, c.PasoActual.ToString(), c.Total, c.UpdatedAt);

    public async Task<PagedResult<CierrePanelItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct)
    {
        // Para agregar dos fuentes, paginamos sobre la lista combinada en memoria.
        var unbounded = filter with { Page = 1, PageSize = int.MaxValue };
        var costes = await _costesRepo.ListPaginatedForUserAsync(usuarioId, unbounded, ct);
        var fact = await _facturacionRepo.ListPaginatedForUserAsync(usuarioId, unbounded, ct);

        var all = costes.Items.Select(c => Map(c, TipoCierre.Costes))
            .Concat(fact.Items.Select(c => Map(c, TipoCierre.Facturacion)))
            .OrderByDescending(c => c.UpdatedAt)
            .ToList();

        return Paginate(all, filter.Page, filter.PageSize);
    }

    public async Task<PagedResult<CierrePanelItemDto>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct)
    {
        var costes = await _costesRepo.ListPendingForUserAsync(usuarioId, 1, int.MaxValue, ct);
        var fact = await _facturacionRepo.ListPendingForUserAsync(usuarioId, 1, int.MaxValue, ct);

        var all = costes.Items.Select(c => Map(c, TipoCierre.Costes))
            .Concat(fact.Items.Select(c => Map(c, TipoCierre.Facturacion)))
            .OrderByDescending(c => c.UpdatedAt)
            .ToList();

        return Paginate(all, page, pageSize);
    }

    private static PagedResult<CierrePanelItemDto> Paginate(List<CierrePanelItemDto> all, int page, int pageSize)
    {
        var total = all.Count;
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PagedResult<CierrePanelItemDto>(items, total, page, pageSize);
    }
}
