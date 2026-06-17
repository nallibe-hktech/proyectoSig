using Microsoft.EntityFrameworkCore;
using SIG.Application.Calculation;
using SIG.Domain.Entities;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class CalculationDataLoader : ICalculationDataLoader
{
    private readonly AppDbContext _db;
    public CalculationDataLoader(AppDbContext db) { _db = db; }

    public async Task<CalculationContext> LoadAsync(CalculationTarget target, CancellationToken ct)
    {
        var ctx = new CalculationContext();
        var desde = target.Period.FechaInicio;
        var hasta = target.Period.FechaFin;
        var serviceId = target.ServiceId;

        ctx.Visitas = await _db.StagingCeleroVisitas.AsNoTracking()
            .Where(v => v.ServiceId == serviceId && v.Fecha >= desde && v.Fecha <= hasta).ToListAsync(ct);
        ctx.HorasBizneo = await _db.StagingBizneoAbsences.AsNoTracking()
            .Where(h => h.ServiceId == serviceId && h.Fecha >= desde && h.Fecha <= hasta).ToListAsync(ct);
        var desdeUtc = DateTime.SpecifyKind(desde.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
        ctx.Fichajes = await _db.StagingIntratimeFichajes.AsNoTracking()
            .Where(f => f.Entrada >= desdeUtc && f.Entrada <= hastaUtc).ToListAsync(ct);
        ctx.Gastos = await _db.StagingPayHawkGastos.AsNoTracking()
            .Where(g => g.ServiceId == serviceId && g.Fecha >= desde && g.Fecha <= hasta).ToListAsync(ct);
        ctx.Tarifas = await _db.TarifasServicio.AsNoTracking()
            .Where(t => t.ServiceId == serviceId && !t.IsDeleted && t.FechaDesde <= hasta && (t.FechaHasta == null || t.FechaHasta >= desde))
            .ToListAsync(ct);
        ctx.VisitasSgpv = await _db.StagingSgpvVisitas.AsNoTracking()
            .Where(s => s.ServiceId == serviceId && s.Fecha >= desde && s.Fecha <= hasta).ToListAsync(ct);
        ctx.Variables = await _db.Variables.AsNoTracking().ToListAsync(ct);
        return ctx;
    }
}
