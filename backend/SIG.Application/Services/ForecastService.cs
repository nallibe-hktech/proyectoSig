using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Forecast (PPT slide 36): previsión mensual de ventas/margen/GPP por servicio, con resumen pivote.
// "El forecast se puede modificar en el momento actual y a futuro, pero no en meses cerrados":
// se considera mes cerrado todo (año, mes) anterior al mes natural actual.
public class ForecastService : IForecastService
{
    private readonly IForecastRepository _repo;
    private readonly IServiceRepository _serviceRepo;

    public ForecastService(IForecastRepository repo, IServiceRepository serviceRepo)
    {
        _repo = repo;
        _serviceRepo = serviceRepo;
    }

    public async Task<IReadOnlyList<ForecastDto>> ListByServiceAsync(int serviceId, int anio, CancellationToken ct)
    {
        _ = await _serviceRepo.GetByIdAsync(serviceId, ct) ?? throw new EntityNotFoundException("Service", serviceId);
        var rows = await _repo.ListByServiceAndYearAsync(serviceId, anio, ct);
        return rows.Select(Map).ToList();
    }

    public async Task<ForecastDto> UpsertAsync(int serviceId, ForecastUpsertRequest req, CancellationToken ct)
    {
        _ = await _serviceRepo.GetByIdAsync(serviceId, ct) ?? throw new EntityNotFoundException("Service", serviceId);
        if (req.Mes < 1 || req.Mes > 12)
            throw new InvalidOperationException("El mes debe estar entre 1 y 12.");
        if (EsMesCerrado(req.Anio, req.Mes))
            throw new PeriodClosedException($"{req.Mes:00}/{req.Anio}");

        var f = await _repo.GetByServiceMonthAsync(serviceId, req.Anio, req.Mes, ct);
        if (f is null)
        {
            f = new Forecast
            {
                ServiceId = serviceId,
                Anio = req.Anio,
                Mes = req.Mes,
                VentasPrevistas = req.VentasPrevistas,
                MargenPrevisto = req.MargenPrevisto,
                PersonasCampo = req.PersonasCampo
            };
            await _repo.AddAsync(f, ct);
        }
        else
        {
            f.VentasPrevistas = req.VentasPrevistas;
            f.MargenPrevisto = req.MargenPrevisto;
            f.PersonasCampo = req.PersonasCampo;
        }
        await _repo.SaveChangesAsync(ct);
        return Map(f);
    }

    public async Task<ForecastResumenDto> GetResumenAsync(int anio, int? departmentId, int? clientId, int? serviceId, CancellationToken ct)
    {
        var rows = await _repo.ListForResumenAsync(anio, departmentId, clientId, serviceId, ct);

        var filas = rows
            .GroupBy(f => new
            {
                DepartmentId = f.Service.DepartmentId,
                DepartmentNombre = f.Service.Department != null ? f.Service.Department.Nombre : null,
                ClientId = f.Service.ClientId,
                ClientNombre = f.Service.Client.Nombre
            })
            .Select(g =>
            {
                var celdas = g
                    .GroupBy(x => x.Mes)
                    .Select(mg => new ForecastResumenCeldaDto(
                        mg.Key,
                        mg.Sum(x => x.VentasPrevistas),
                        mg.Sum(x => x.MargenPrevisto ?? 0m),
                        mg.Sum(x => x.PersonasCampo ?? 0)))
                    .OrderBy(c => c.Mes)
                    .ToList();
                return new ForecastResumenFilaDto(
                    g.Key.DepartmentId, g.Key.DepartmentNombre, g.Key.ClientId, g.Key.ClientNombre,
                    celdas,
                    celdas.Sum(c => c.Ventas), celdas.Sum(c => c.Margen), celdas.Sum(c => c.Personas));
            })
            .OrderBy(f => f.DepartmentNombre).ThenBy(f => f.ClientNombre)
            .ToList();

        return new ForecastResumenDto(anio, filas);
    }

    private static bool EsMesCerrado(int anio, int mes)
    {
        var hoy = DateTime.UtcNow;
        return anio < hoy.Year || (anio == hoy.Year && mes < hoy.Month);
    }

    private static ForecastDto Map(Forecast f) =>
        new(f.Id, f.ServiceId, f.Anio, f.Mes, f.VentasPrevistas, f.MargenPrevisto, f.PersonasCampo);
}
