using FluentAssertions;
using SIG.Application.Calculation;
using SIG.Application.Calculation.Nodes;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// Verifica que TravelPerk está enchufado al motor de cálculo como origen "ViajesTravelPerk":
/// un concepto "Viajes Travel Perk" = Sum(Importe) sobre ese origen imputa el coste sin IVA por Service/Período,
/// filtrando por servicio y período y neteando los "Refund for train" (importes negativos).
/// </summary>
public class TravelPerkSourceTests
{
    private static StagingTravelPerkLinea Linea(int? serviceId, string ceco, string service, decimal coste, DateOnly fecha)
        => new()
        {
            TripId = "T", Service = service, Ceco = ceco, CostObject = ceco == "0423" ? null : ceco,
            ServiceId = serviceId, CosteSinIVA = coste, FechaGasto = fecha,
            PayloadJson = "{}", Hash = "h", FechaUltimaSincronizacion = DateTime.UtcNow
        };

    private static CalculationTarget TargetMayo(int serviceId) => new()
    {
        ServiceId = serviceId,
        PeriodId = 1,
        Period = new Period { FechaInicio = new DateOnly(2026, 5, 1), FechaFin = new DateOnly(2026, 5, 31) }
    };

    [Fact]
    public void FilteredRows_FiltraPorServicioYPeriodo_YNeteaRefunds()
    {
        var ctx = new CalculationContext
        {
            ViajesTravelPerk = new()
            {
                Linea(1, "0139_A", "Hotels",           60m,  new DateOnly(2026, 5, 10)),
                Linea(1, "0139_A", "Refund for train", -10m, new DateOnly(2026, 5, 12)),
                Linea(2, "0102_B", "Flights",           50m, new DateOnly(2026, 5, 11)), // otro servicio → fuera
                Linea(1, "0139_A", "Hotels",           100m, new DateOnly(2026, 4, 30)), // fuera de período → fuera
            }
        };

        var rows = ctx.FilteredRows(new SourceNode { Entity = "ViajesTravelPerk" }, TargetMayo(1), null);

        rows.Should().HaveCount(2);
        rows.Sum(r => r.GetDecimal("Importe")).Should().Be(50m); // 60 − 10 (refund netea)
        ctx.SistemasUsados.Should().Contain("TravelPerk");
    }

    [Fact]
    public void FilteredRows_FiltroExplicitoPorService_SegmentaPorTipoDeServicio()
    {
        var ctx = new CalculationContext
        {
            ViajesTravelPerk = new()
            {
                Linea(1, "0139_A", "Hotels",  60m, new DateOnly(2026, 5, 10)),
                Linea(1, "0139_A", "Flights", 25m, new DateOnly(2026, 5, 11)),
            }
        };

        var source = new SourceNode
        {
            Entity = "ViajesTravelPerk",
            Filters = new() { new FilterSpec { Field = "Service", Op = "Eq", Value = "Hotels" } }
        };

        var rows = ctx.FilteredRows(source, TargetMayo(1), null);

        rows.Should().ContainSingle();
        rows[0].GetDecimal("Importe").Should().Be(60m);
    }

    [Fact]
    public void FilteredRows_ServicioSinLineas_DevuelveVacio()
    {
        var ctx = new CalculationContext
        {
            ViajesTravelPerk = new() { Linea(1, "0139_A", "Hotels", 60m, new DateOnly(2026, 5, 10)) }
        };

        var rows = ctx.FilteredRows(new SourceNode { Entity = "ViajesTravelPerk" }, TargetMayo(99), null);

        rows.Should().BeEmpty();
    }
}
