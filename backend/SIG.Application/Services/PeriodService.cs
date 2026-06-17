using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class PeriodService : IPeriodService
{
    private readonly IPeriodRepository _repo;

    public PeriodService(IPeriodRepository repo) { _repo = repo; }

    public async Task<IReadOnlyList<PeriodDto>> ListAsync(CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        return list.Select(Map).ToList();
    }

    public async Task<PagedResult<PeriodDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        var total = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(Map)
            .ToList();
        return new PagedResult<PeriodDto>(items, total, page, pageSize);
    }

    public async Task<PeriodDto> GetActivoAsync(CancellationToken ct)
    {
        var p = await _repo.GetActivoAsync(ct) ?? throw new EntityNotFoundException("Period", "activo");
        return Map(p);
    }

    public async Task<PeriodDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Period", id);
        return Map(p);
    }

    public async Task<PeriodDto> CreateAsync(PeriodCreateRequest req, CancellationToken ct)
    {
        if (await _repo.ExistsByNombreAsync(req.Nombre, null, ct))
            throw new DuplicateException($"Ya existe un período con nombre {req.Nombre}.");
        var p = new Period
        {
            Nombre = req.Nombre,
            FechaInicio = req.FechaInicio,
            FechaFin = req.FechaFin,
            Estado = EstadoPeriodo.Abierto
        };
        await _repo.AddAsync(p, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<PeriodDto> UpdateAsync(int id, PeriodUpdateRequest req, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Period", id);
        if (await _repo.ExistsByNombreAsync(req.Nombre, id, ct))
            throw new DuplicateException($"Ya existe otro período con nombre {req.Nombre}.");
        p.Nombre = req.Nombre;
        p.FechaInicio = req.FechaInicio;
        p.FechaFin = req.FechaFin;
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<PeriodDto> CerrarAsync(int id, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Period", id);
        if (p.Estado != EstadoPeriodo.Abierto)
            throw new InvalidApprovalTransitionException("Solo períodos abiertos pueden cerrarse.");
        p.Estado = EstadoPeriodo.Cerrado;
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<PeriodDto> ReabrirAsync(int id, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Period", id);
        if (p.Estado != EstadoPeriodo.Cerrado)
            throw new InvalidApprovalTransitionException("Solo períodos cerrados pueden reabrirse.");
        p.Estado = EstadoPeriodo.Abierto;
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    private static PeriodDto Map(Period p) => new(p.Id, p.Nombre, p.FechaInicio, p.FechaFin, p.Estado);
}
