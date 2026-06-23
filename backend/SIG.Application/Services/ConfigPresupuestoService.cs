using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Configuración de Presupuesto (prototipo 24/28, PPT slide 35). Partidas de presupuesto por acción/servicio,
// ENTRADA MANUAL (el propio prototipo dice que no proceden de ningún origen de datos). Restante y Avance se
// calculan; el margen objetivo es manual (en el Service) y el real se calcula de los cierres. Anidado bajo
// el servicio: se valida acceso al servicio. Escritura sólo Administrator (lo aplica el controlador).
public class ConfigPresupuestoService : IConfigPresupuestoService
{
    private readonly IPartidaPresupuestoRepository _repo;
    private readonly IServiceRepository _serviceRepo;

    public ConfigPresupuestoService(IPartidaPresupuestoRepository repo, IServiceRepository serviceRepo)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
    }

    public async Task<ConfigPresupuestoDto> GetConfigAsync(int serviceId, int usuarioId, CancellationToken ct)
    {
        var service = await EnsureServiceAsync(serviceId, usuarioId, ct);
        return await BuildConfigAsync(service, ct);
    }

    public async Task<PartidaPresupuestoDto> CreatePartidaAsync(int serviceId, PartidaPresupuestoCreateRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureServiceAsync(serviceId, usuarioId, ct);
        var p = new PartidaPresupuesto
        {
            ServiceId = serviceId,
            Nombre = req.Nombre,
            Tipo = req.Tipo,
            Anio = req.Anio,
            Presupuesto = req.Presupuesto,
            Consumido = req.Consumido,
            Descripcion = req.Descripcion,
        };
        await _repo.AddAsync(p, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<PartidaPresupuestoDto> UpdatePartidaAsync(int id, int serviceId, PartidaPresupuestoUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureServiceAsync(serviceId, usuarioId, ct);
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PartidaPresupuesto", id);
        if (p.ServiceId != serviceId) throw new EntityNotFoundException("PartidaPresupuesto", id);
        p.Nombre = req.Nombre;
        p.Tipo = req.Tipo;
        p.Anio = req.Anio;
        p.Presupuesto = req.Presupuesto;
        p.Consumido = req.Consumido;
        p.Descripcion = req.Descripcion;
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task DeletePartidaAsync(int id, int serviceId, int usuarioId, CancellationToken ct)
    {
        await EnsureServiceAsync(serviceId, usuarioId, ct);
        var p = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("PartidaPresupuesto", id);
        if (p.ServiceId != serviceId) throw new EntityNotFoundException("PartidaPresupuesto", id);
        p.IsDeleted = true;
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<ConfigPresupuestoDto> SetMargenObjetivoAsync(int serviceId, MargenObjetivoRequest req, int usuarioId, CancellationToken ct)
    {
        var service = await EnsureServiceAsync(serviceId, usuarioId, ct);
        service.MargenObjetivoPct = req.MargenObjetivoPct;
        await _serviceRepo.SaveChangesAsync(ct);
        return await BuildConfigAsync(service, ct);
    }

    private async Task<ConfigPresupuestoDto> BuildConfigAsync(Service service, CancellationToken ct)
    {
        var partidas = await _repo.ListByServiceAsync(service.Id, ct);
        var totalPres = partidas.Sum(p => p.Presupuesto);
        var totalCons = partidas.Sum(p => p.Consumido);
        var restante = totalPres - totalCons;
        var avance = totalPres > 0 ? Math.Round(totalCons / totalPres * 100m, 0) : 0m;
        var margenReal = await _repo.GetMargenRealPctAsync(service.Id, ct);
        var desviacion = (margenReal.HasValue && service.MargenObjetivoPct.HasValue)
            ? Math.Round(margenReal.Value - service.MargenObjetivoPct.Value, 1)
            : (decimal?)null;
        return new ConfigPresupuestoDto(
            service.Id, service.Nombre, service.Client?.Nombre ?? string.Empty,
            totalPres, totalCons, restante, avance,
            service.MargenObjetivoPct, margenReal, desviacion,
            partidas.Count(p => p.Tipo == Domain.Enums.TipoPartidaPresupuesto.Anual),
            partidas.Count(p => p.Tipo == Domain.Enums.TipoPartidaPresupuesto.TotalAccion),
            partidas.Select(Map).ToList());
    }

    private async Task<Service> EnsureServiceAsync(int serviceId, int usuarioId, CancellationToken ct) =>
        await _serviceRepo.GetByIdAndUsuarioIdAsync(serviceId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Service", serviceId);

    private static PartidaPresupuestoDto Map(PartidaPresupuesto p)
    {
        var restante = p.Presupuesto - p.Consumido;
        var avance = p.Presupuesto > 0 ? Math.Round(p.Consumido / p.Presupuesto * 100m, 0) : 0m;
        return new PartidaPresupuestoDto(p.Id, p.ServiceId, p.Nombre, p.Tipo, p.Anio, p.Presupuesto, p.Consumido, restante, avance, p.Descripcion);
    }
}
