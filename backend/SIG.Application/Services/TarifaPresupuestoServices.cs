using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class TarifaServicioService : ITarifaServicioService
{
    private readonly ITarifaServicioRepository _repo;
    private readonly IServiceRepository _serviceRepo;

    public TarifaServicioService(ITarifaServicioRepository repo, IServiceRepository serviceRepo)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
    }

    public async Task<IReadOnlyList<TarifaServicioDto>> ListByServiceAsync(int serviceId, CancellationToken ct)
    {
        _ = await _serviceRepo.GetByIdAsync(serviceId, ct) ?? throw new EntityNotFoundException("Service", serviceId);
        var tarifas = await _repo.ListByServiceAsync(serviceId, ct);
        return tarifas.Select(Map).ToList();
    }

    public async Task<TarifaServicioDto> GetByIdAsync(int id, int serviceId, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("TarifaServicio", id);
        if (t.ServiceId != serviceId) throw new EntityNotFoundException("TarifaServicio", id);
        return Map(t);
    }

    public async Task<TarifaServicioDto> CreateAsync(int serviceId, TarifaServicioCreateRequest req, CancellationToken ct)
    {
        _ = await _serviceRepo.GetByIdAsync(serviceId, ct) ?? throw new EntityNotFoundException("Service", serviceId);
        var t = new TarifaServicio
        {
            ServiceId = serviceId,
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

    public async Task<TarifaServicioDto> UpdateAsync(int id, int serviceId, TarifaServicioUpdateRequest req, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("TarifaServicio", id);
        if (t.ServiceId != serviceId) throw new EntityNotFoundException("TarifaServicio", id);
        t.Nombre = req.Nombre;
        t.Valor = req.Valor;
        t.Unidad = req.Unidad;
        t.FechaDesde = req.FechaDesde;
        t.FechaHasta = req.FechaHasta;
        await _repo.SaveChangesAsync(ct);
        return Map(t);
    }

    public async Task DeleteAsync(int id, int serviceId, CancellationToken ct)
    {
        var t = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("TarifaServicio", id);
        if (t.ServiceId != serviceId) throw new EntityNotFoundException("TarifaServicio", id);
        t.IsDeleted = true;
        await _repo.SaveChangesAsync(ct);
    }

    private static TarifaServicioDto Map(TarifaServicio t) =>
        new(t.Id, t.ServiceId, t.Nombre, t.Valor, t.Unidad, t.FechaDesde, t.FechaHasta);
}

public class PresupuestoServicioService : IPresupuestoServicioService
{
    private readonly IPresupuestoServicioRepository _repo;
    private readonly IServiceRepository _serviceRepo;

    public PresupuestoServicioService(IPresupuestoServicioRepository repo, IServiceRepository serviceRepo)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
    }

    public async Task<IReadOnlyList<PresupuestoServicioDto>> ListByServiceAsync(int serviceId, CancellationToken ct)
    {
        _ = await _serviceRepo.GetByIdAsync(serviceId, ct) ?? throw new EntityNotFoundException("Service", serviceId);
        var presupuestos = await _repo.ListByServiceAsync(serviceId, ct);
        return presupuestos.Select(Map).ToList();
    }

    public async Task<PresupuestoServicioDto> GetByIdAsync(int id, int serviceId, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PresupuestoServicio", id);
        if (p.ServiceId != serviceId) throw new EntityNotFoundException("PresupuestoServicio", id);
        return Map(p);
    }

    public async Task<PresupuestoServicioDto> CreateAsync(int serviceId, PresupuestoServicioCreateRequest req, CancellationToken ct)
    {
        _ = await _serviceRepo.GetByIdAsync(serviceId, ct) ?? throw new EntityNotFoundException("Service", serviceId);
        var p = new PresupuestoServicio
        {
            ServiceId = serviceId,
            PeriodId = req.PeriodId,
            Tipo = req.Tipo,
            Importe = req.Importe,
            Descripcion = req.Descripcion
        };
        await _repo.AddAsync(p, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<PresupuestoServicioDto> UpdateAsync(int id, int serviceId, PresupuestoServicioUpdateRequest req, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PresupuestoServicio", id);
        if (p.ServiceId != serviceId) throw new EntityNotFoundException("PresupuestoServicio", id);
        p.PeriodId = req.PeriodId;
        p.Tipo = req.Tipo;
        p.Importe = req.Importe;
        p.Descripcion = req.Descripcion;
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task DeleteAsync(int id, int serviceId, CancellationToken ct)
    {
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PresupuestoServicio", id);
        if (p.ServiceId != serviceId) throw new EntityNotFoundException("PresupuestoServicio", id);
        p.IsDeleted = true;
        await _repo.SaveChangesAsync(ct);
    }

    private static PresupuestoServicioDto Map(PresupuestoServicio p) =>
        new(p.Id, p.ServiceId, p.PeriodId, p.Tipo, p.Importe, p.Descripcion);
}
