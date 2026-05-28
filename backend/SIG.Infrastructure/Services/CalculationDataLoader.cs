using Microsoft.EntityFrameworkCore;
using SIG.Application.Calculation;
using SIG.Domain.Entities;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class CalculationDataLoader : ICalculationDataLoader
{
    private readonly AppDbContext _db;
    public CalculationDataLoader(AppDbContext db) { _db = db; }

    public async Task<CalculationContext> LoadAsync(Closure closure, CancellationToken ct)
    {
        var ctx = new CalculationContext();
        var desde = closure.Period.FechaInicio;
        var hasta = closure.Period.FechaFin;
        var projectId = closure.ProjectId;

        ctx.Visitas = await _db.StagingCeleroVisitas.AsNoTracking()
            .Where(v => v.ProjectId == projectId && v.Fecha >= desde && v.Fecha <= hasta).ToListAsync(ct);
        ctx.HorasBizneo = await _db.StagingBizneoHoras.AsNoTracking()
            .Where(h => h.ProjectId == projectId && h.Fecha >= desde && h.Fecha <= hasta).ToListAsync(ct);
        var desdeUtc = DateTime.SpecifyKind(desde.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
        ctx.Fichajes = await _db.StagingIntratimeFichajes.AsNoTracking()
            .Where(f => f.Entrada >= desdeUtc && f.Entrada <= hastaUtc).ToListAsync(ct);
        ctx.Gastos = await _db.StagingPayHawkGastos.AsNoTracking()
            .Where(g => g.ProjectId == projectId && g.Fecha >= desde && g.Fecha <= hasta).ToListAsync(ct);
        ctx.Variables = await _db.Variables.AsNoTracking().ToListAsync(ct);
        return ctx;
    }
}
