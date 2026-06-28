using Microsoft.EntityFrameworkCore;
using SIG.Application.Calculation;
using SIG.Application.Calculation.Nodes;
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
        ctx.ViajesTravelPerk = await _db.StagingTravelPerkLineas.AsNoTracking()
            .Where(t => t.ServiceId == serviceId && (t.FechaGasto == null || (t.FechaGasto >= desde && t.FechaGasto <= hasta)))
            .ToListAsync(ct);
        ctx.SalariosA3 = await _db.StagingA3InnuvaSalaries.AsNoTracking()
            .Where(s => s.FechaInicio <= hastaUtc && (s.FechaFin == null || s.FechaFin >= desdeUtc))
            .ToListAsync(ct);
        ctx.Variables = await _db.Variables.AsNoTracking().ToListAsync(ct);
        // Logística Galán
        var desdeDt = desde.ToDateTime(TimeOnly.MinValue);
        var hastaDt = hasta.ToDateTime(TimeOnly.MaxValue);
        ctx.GalanEntradas = await _db.StagingGalanEntradas.AsNoTracking()
            .Where(e => e.Fecha >= desdeDt && e.Fecha <= hastaDt).ToListAsync(ct);
        ctx.GalanSalidas = await _db.StagingGalanSalidas.AsNoTracking()
            .Where(s => s.Fecha >= desdeDt && s.Fecha <= hastaDt).ToListAsync(ct);
        ctx.GalanStock = await _db.StagingGalanStocks.AsNoTracking().ToListAsync(ct);
        // Logística Mediapost
        ctx.MediapostPedidos = await _db.StagingMediapostPedidos.AsNoTracking()
            .Where(p => p.FechaPedido >= desdeDt && p.FechaPedido <= hastaDt).ToListAsync(ct);
        ctx.MediapostRecepciones = await _db.StagingMediapostRecepciones.AsNoTracking()
            .Where(r => r.FechaRecepcion >= desdeDt && r.FechaRecepcion <= hastaDt).ToListAsync(ct);
        return ctx;
    }

    public async Task<List<RowAdapter>> LoadCrossServiceAsync(int userId, DateOnly desde, DateOnly hasta, string entity, string field, CancellationToken ct)
    {
        var desdeUtc = DateTime.SpecifyKind(desde.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

        return entity switch
        {
            "VisitasCelero" => (await _db.StagingCeleroVisitas.AsNoTracking()
                .Where(v => v.UserId == userId && v.Fecha >= desde && v.Fecha <= hasta)
                .ToListAsync(ct))
                .Select(RowAdapter.FromVisita).ToList(),

            "VisitasSgpv" => (await _db.StagingSgpvVisitas.AsNoTracking()
                .Where(s => s.UserId == userId && s.Fecha >= desde && s.Fecha <= hasta)
                .ToListAsync(ct))
                .Select(RowAdapter.FromSgpvVisita).ToList(),

            "HorasIntratime" => (await _db.StagingIntratimeFichajes.AsNoTracking()
                .Where(f => f.UserId == userId && f.Entrada >= desdeUtc && f.Entrada <= hastaUtc)
                .ToListAsync(ct))
                .Select(RowAdapter.FromFichaje).ToList(),

            "HorasBizneo" => (await _db.StagingBizneoAbsences.AsNoTracking()
                .Where(h => h.UserId == userId && h.Fecha >= desde && h.Fecha <= hasta)
                .ToListAsync(ct))
                .Select(RowAdapter.FromHora).ToList(),

            _ => new List<RowAdapter>()
        };
    }
}
