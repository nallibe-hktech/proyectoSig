using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class TarifaProyectoService : ITarifaProyectoService
{
    private readonly ITarifaProyectoRepository _repo;
    private readonly IProjectRepository _projectRepo;

    public TarifaProyectoService(ITarifaProyectoRepository repo, IProjectRepository projectRepo)
    {
        _repo = repo;
        _projectRepo = projectRepo;
    }

    public async Task<IReadOnlyList<TarifaProyectoDto>> ListByProjectAsync(int projectId, CancellationToken ct)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct) ?? throw new EntityNotFoundException("Project", projectId);
        var tarifas = await _repo.ListByProjectAsync(projectId, ct);
        return tarifas.Select(Map).ToList();
    }

    public async Task<TarifaProyectoDto> GetByIdAsync(int id, int projectId, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("TarifaProyecto", id);
        if (t.ProjectId != projectId) throw new EntityNotFoundException("TarifaProyecto", id);
        return Map(t);
    }

    public async Task<TarifaProyectoDto> CreateAsync(int projectId, TarifaProyectoCreateRequest req, CancellationToken ct)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct) ?? throw new EntityNotFoundException("Project", projectId);
        var t = new TarifaProyecto
        {
            ProjectId = projectId,
            Nombre = req.Nombre,
            Valor = req.Valor,
            Unidad = req.Unidad,
            FechaDesde = req.FechaDesde,
            FechaHasta = req.FechaHasta
        };
        await _repo.AddAsync(t, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(t);
    }

    public async Task<TarifaProyectoDto> UpdateAsync(int id, int projectId, TarifaProyectoUpdateRequest req, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("TarifaProyecto", id);
        if (t.ProjectId != projectId) throw new EntityNotFoundException("TarifaProyecto", id);
        t.Nombre = req.Nombre;
        t.Valor = req.Valor;
        t.Unidad = req.Unidad;
        t.FechaDesde = req.FechaDesde;
        t.FechaHasta = req.FechaHasta;
        await _repo.SaveChangesAsync(ct);
        return Map(t);
    }

    public async Task DeleteAsync(int id, int projectId, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("TarifaProyecto", id);
        if (t.ProjectId != projectId) throw new EntityNotFoundException("TarifaProyecto", id);
        t.IsDeleted = true;
        await _repo.SaveChangesAsync(ct);
    }

    private static TarifaProyectoDto Map(TarifaProyecto t) =>
        new(t.Id, t.ProjectId, t.Nombre, t.Valor, t.Unidad, t.FechaDesde, t.FechaHasta);
}

public class PresupuestoProyectoService : IPresupuestoProyectoService
{
    private readonly IPresupuestoProyectoRepository _repo;
    private readonly IProjectRepository _projectRepo;

    public PresupuestoProyectoService(IPresupuestoProyectoRepository repo, IProjectRepository projectRepo)
    {
        _repo = repo;
        _projectRepo = projectRepo;
    }

    public async Task<IReadOnlyList<PresupuestoProyectoDto>> ListByProjectAsync(int projectId, CancellationToken ct)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct) ?? throw new EntityNotFoundException("Project", projectId);
        var presupuestos = await _repo.ListByProjectAsync(projectId, ct);
        return presupuestos.Select(Map).ToList();
    }

    public async Task<PresupuestoProyectoDto> GetByIdAsync(int id, int projectId, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PresupuestoProyecto", id);
        if (p.ProjectId != projectId) throw new EntityNotFoundException("PresupuestoProyecto", id);
        return Map(p);
    }

    public async Task<PresupuestoProyectoDto> CreateAsync(int projectId, PresupuestoProyectoCreateRequest req, CancellationToken ct)
    {
        var project = await _projectRepo.GetByIdAsync(projectId, ct) ?? throw new EntityNotFoundException("Project", projectId);
        var p = new PresupuestoProyecto
        {
            ProjectId = projectId,
            PeriodId = req.PeriodId,
            Tipo = req.Tipo,
            Importe = req.Importe,
            Descripcion = req.Descripcion
        };
        await _repo.AddAsync(p, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<PresupuestoProyectoDto> UpdateAsync(int id, int projectId, PresupuestoProyectoUpdateRequest req, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PresupuestoProyecto", id);
        if (p.ProjectId != projectId) throw new EntityNotFoundException("PresupuestoProyecto", id);
        p.PeriodId = req.PeriodId;
        p.Tipo = req.Tipo;
        p.Importe = req.Importe;
        p.Descripcion = req.Descripcion;
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task DeleteAsync(int id, int projectId, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PresupuestoProyecto", id);
        if (p.ProjectId != projectId) throw new EntityNotFoundException("PresupuestoProyecto", id);
        p.IsDeleted = true;
        await _repo.SaveChangesAsync(ct);
    }

    private static PresupuestoProyectoDto Map(PresupuestoProyecto p) =>
        new(p.Id, p.ProjectId, p.PeriodId, p.Tipo, p.Importe, p.Descripcion);
}
