using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Ola 3b (#10): el panel/pendientes agrega AMBOS tipos de cierre, indicando el tipo de cada uno.
public class ApprovalService : IApprovalService
{
    private readonly ICierreCostesRepository _costesRepo;
    private readonly ICierreFacturacionRepository _facturacionRepo;
    private readonly ICierreCostesService _costesService;
    private readonly ICierreFacturacionService _facturacionService;
    private readonly IApprovalRepository _approvalRepo;

    public ApprovalService(
        ICierreCostesRepository costesRepo,
        ICierreFacturacionRepository facturacionRepo,
        ICierreCostesService costesService,
        ICierreFacturacionService facturacionService,
        IApprovalRepository approvalRepo)
    {
        _costesRepo = costesRepo;
        _facturacionRepo = facturacionRepo;
        _costesService = costesService;
        _facturacionService = facturacionService;
        _approvalRepo = approvalRepo;
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

    public async Task<IReadOnlyList<ApprovalHistoryDto>> GetHistoryAsync(int closureId, int usuarioId, CancellationToken ct)
    {
        // Intenta obtener el historial del cierre como si fuera de costes; si no existe, lo intenta como facturación
        var history = await _costesService.GetHistoryAsync(closureId, usuarioId, ct);
        if (history.Count == 0)
        {
            history = await _facturacionService.GetHistoryAsync(closureId, usuarioId, ct);
        }

        // Mapea CierreHistoryDto → ApprovalHistoryDto (compatible con cliente)
        return history.Select(h => new ApprovalHistoryDto(
            h.Id, h.CierreId, h.UserId, "", // UserNombre se deja vacío (ya viene en CierreHistoryDto.UserNombre pero no en DTO nuevo)
            h.PasoOrigen, h.PasoDestino, h.Accion, h.Motivo, h.Timestamp)).ToList();
    }

    public async Task<IReadOnlyList<CierreDetailDto>> BatchApproveAsync(BatchApproveRequest req, int usuarioId, CancellationToken ct)
    {
        var results = new List<CierreDetailDto>();

        foreach (var closureId in req.Ids)
        {
            try
            {
                // Intenta aprobar como costes primero
                var costes = await _costesRepo.GetByIdAsync(closureId, ct);
                if (costes != null)
                {
                    var approveReq = new CierreApproveRequest(null);
                    var result = await _costesService.ApproveAsync(closureId, approveReq, costes.RowVersion, usuarioId, ct);
                    results.Add(result);
                    continue;
                }

                // Si no es costes, intenta como facturación
                var facturacion = await _facturacionRepo.GetByIdAsync(closureId, ct);
                if (facturacion != null)
                {
                    var approveReq = new CierreApproveRequest(null);
                    var result = await _facturacionService.ApproveAsync(closureId, approveReq, facturacion.RowVersion, usuarioId, ct);
                    results.Add(result);
                }
            }
            catch
            {
                // Silenciosamente ignora cierres que no se pueden aprobar (protección contra concurrencia)
                continue;
            }
        }

        return results;
    }

    public async Task<IReadOnlyList<CierreDetailDto>> BatchRejectAsync(BatchRejectRequest req, int usuarioId, CancellationToken ct)
    {
        var results = new List<CierreDetailDto>();

        foreach (var closureId in req.Ids)
        {
            try
            {
                // Intenta rechazar como costes primero
                var costes = await _costesRepo.GetByIdAsync(closureId, ct);
                if (costes != null)
                {
                    var rejectReq = new CierreRejectRequest("Rechazado en lote por aprobador");
                    var result = await _costesService.RejectAsync(closureId, rejectReq, costes.RowVersion, usuarioId, ct);
                    results.Add(result);
                    continue;
                }

                // Si no es costes, intenta como facturación
                var facturacion = await _facturacionRepo.GetByIdAsync(closureId, ct);
                if (facturacion != null)
                {
                    var rejectReq = new CierreRejectRequest("Rechazado en lote por aprobador");
                    var result = await _facturacionService.RejectAsync(closureId, rejectReq, facturacion.RowVersion, usuarioId, ct);
                    results.Add(result);
                }
            }
            catch
            {
                // Silenciosamente ignora cierres que no se pueden rechazar (protección contra concurrencia)
                continue;
            }
        }

        return results;
    }
}
