using Microsoft.EntityFrameworkCore;
using SIG.Application.Alerts;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

/// <summary>
/// Lectura del staging de TravelPerk para el dashboard (líneas importadas + KPIs de imputación).
/// Solo lectura: el alta de líneas la hace el SyncService desde el Excel de SharePoint.
/// </summary>
public class TravelPerkService : ITravelPerkService
{
    private readonly AppDbContext _db;

    public TravelPerkService(AppDbContext db) { _db = db; }

    public async Task<PagedResult<TravelPerkLineaListDto>> ListAsync(
        int page, int pageSize, string? search = null, bool soloNoMaestro = false, CancellationToken ct = default)
    {
        var query = _db.StagingTravelPerkLineas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(l =>
                EF.Functions.ILike(l.Service, $"%{s}%")
                || EF.Functions.ILike(l.TripId, $"%{s}%")
                || EF.Functions.ILike(l.Ceco, $"%{s}%")
                || (l.CostObject != null && EF.Functions.ILike(l.CostObject, $"%{s}%"))
                || (l.TravelerEmail != null && EF.Functions.ILike(l.TravelerEmail, $"%{s}%")));
        }

        if (soloNoMaestro)
            query = query.Where(l => l.ErrorProcesamiento == AlertaCodigos.CecoNoMaestro);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.FechaGasto)
            .ThenByDescending(l => l.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new TravelPerkLineaListDto(
                l.Id,
                l.TripId,
                l.Service,
                l.CostObject,
                l.Ceco,
                l.ServiceId,
                l.CosteSinIVA,
                l.FechaGasto,
                l.TravelerEmail,
                l.Currency,
                l.ServiceId == null && l.ErrorProcesamiento == null,
                l.ErrorProcesamiento == AlertaCodigos.CecoNoMaestro,
                l.FechaUltimaSincronizacion))
            .ToListAsync(ct);

        return new PagedResult<TravelPerkLineaListDto>(items, total, page, pageSize);
    }

    public async Task<TravelPerkKpisDto> GetKpisAsync(CancellationToken ct = default)
    {
        var q = _db.StagingTravelPerkLineas.AsNoTracking();

        var total = await q.CountAsync(ct);
        var totalSinIva = await q.SumAsync(l => (decimal?)l.CosteSinIVA, ct) ?? 0m;

        var lineasImputadas = await q.CountAsync(l => l.ServiceId != null, ct);
        var costeImputado = await q.Where(l => l.ServiceId != null)
            .SumAsync(l => (decimal?)l.CosteSinIVA, ct) ?? 0m;

        // Gasto interno de SIG: sin "Cost object" (suscripción → 0423) o CECO estructural de SIG (departamento).
        // En ambos casos no se imputa a cliente (ServiceId null) y no es error de calidad (ErrorProcesamiento null).
        var lineasInternas = await q.CountAsync(l => l.ServiceId == null && l.ErrorProcesamiento == null, ct);
        var costeInterno = await q.Where(l => l.ServiceId == null && l.ErrorProcesamiento == null)
            .SumAsync(l => (decimal?)l.CosteSinIVA, ct) ?? 0m;

        var lineasNoMaestro = await q.CountAsync(l => l.ErrorProcesamiento == AlertaCodigos.CecoNoMaestro, ct);

        return new TravelPerkKpisDto(
            total, totalSinIva, lineasImputadas, costeImputado, lineasInternas, costeInterno, lineasNoMaestro);
    }
}
