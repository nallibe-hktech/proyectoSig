using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class ApprovalService : IApprovalService
{
    private readonly IClosureRepository _closureRepo;
    private readonly IApprovalRepository _approvalRepo;
    private readonly IClosureService _closureService;

    public ApprovalService(IClosureRepository closureRepo, IApprovalRepository approvalRepo, IClosureService closureService)
    {
        _closureRepo = closureRepo;
        _approvalRepo = approvalRepo;
        _closureService = closureService;
    }

    public async Task<PagedResult<ApprovalPanelItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct)
    {
        var result = await _closureRepo.ListPaginatedForUserAsync(usuarioId, filter, ct);
        var items = result.Items.Select(c => new ApprovalPanelItemDto(
            c.Id, c.ServiceId, c.Service?.Nombre ?? "",
            c.Service?.ClientId ?? 0, c.Service?.Client?.Nombre ?? "",
            c.PeriodId, c.Period?.Nombre ?? "",
            c.Estado, c.PasoActual, c.PasoActual.ToString(), c.Margen, c.UpdatedAt)).ToList();
        return new PagedResult<ApprovalPanelItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<PagedResult<ApprovalPanelItemDto>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct)
    {
        var result = await _closureRepo.ListPendingForUserAsync(usuarioId, page, pageSize, ct);
        var items = result.Items.Select(c => new ApprovalPanelItemDto(
            c.Id, c.ServiceId, c.Service?.Nombre ?? "",
            c.Service?.ClientId ?? 0, c.Service?.Client?.Nombre ?? "",
            c.PeriodId, c.Period?.Nombre ?? "",
            c.Estado, c.PasoActual, c.PasoActual.ToString(), c.Margen, c.UpdatedAt)).ToList();
        return new PagedResult<ApprovalPanelItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<IReadOnlyList<ApprovalHistoryDto>> GetHistoryAsync(int closureId, int usuarioId, CancellationToken ct)
    {
        var closure = await _closureRepo.GetByIdAndUsuarioIdAsync(closureId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Closure", closureId);
        var history = await _approvalRepo.ListHistoryByClosureAsync(closureId, ct);
        return history.Select(h => new ApprovalHistoryDto(
            h.Id, h.ClosureId, h.UserId,
            h.User != null ? $"{h.User.Nombre} {h.User.Apellidos}" : "",
            h.PasoOrigen, h.PasoDestino, h.Accion, h.Motivo, h.Timestamp)).ToList();
    }

    public async Task<IReadOnlyList<ClosureDetailDto>> BatchApproveAsync(BatchApproveRequest req, int usuarioId, CancellationToken ct)
    {
        var results = new List<ClosureDetailDto>();

        foreach (var closureId in req.Ids)
        {
            var closure = await _closureRepo.GetByIdAsync(closureId, ct);
            if (closure == null)
                continue;

            var approveReq = new ClosureApproveRequest(null);
            var result = await _closureService.ApproveAsync(closureId, approveReq, closure.RowVersion, usuarioId, ct);
            results.Add(result);
        }

        return results;
    }

    public async Task<IReadOnlyList<ClosureDetailDto>> BatchRejectAsync(BatchRejectRequest req, int usuarioId, CancellationToken ct)
    {
        var results = new List<ClosureDetailDto>();

        foreach (var closureId in req.Ids)
        {
            var closure = await _closureRepo.GetByIdAsync(closureId, ct);
            if (closure == null)
                continue;

            var rejectReq = new ClosureRejectRequest("Rechazado por aprobador");
            var result = await _closureService.RejectAsync(closureId, rejectReq, closure.RowVersion, usuarioId, ct);
            results.Add(result);
        }

        return results;
    }
}
