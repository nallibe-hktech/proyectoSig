using System.Text.Json;
using SIG.Application.Calculation;
using SIG.Application.DTOs;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// Verifica el cambio de ingesta de Celero (docs/RETOMA_INPOST_FACTURACION.md §4.3): los campos
/// realDuration / status / provincia / cancellationReason ahora viajan en el DTO → se serializan al
/// PayloadJson EXACTAMENTE como hace DashboardCalcSyncAudit (JsonSerializer.Serialize(dto)) → y el
/// motor los puede leer y segmentar. Antes de este cambio el PayloadJson de producción no traía
/// duración ni estado, por lo que estos filtros daban 0 sobre dato real.
/// </summary>
public class CeleroIngestaPayloadTests
{
    // Réplica EXACTA de cómo DashboardCalcSyncAudit construye el PayloadJson de cada visita Celero.
    private static string Payload(CeleroVisitaDto d) => JsonSerializer.Serialize(d);

    private static StagingCeleroVisita VisitaDesdeDto(CeleroVisitaDto d, int dia) => new()
    {
        Fecha = new DateOnly(2026, 3, dia),
        UserId = 1,
        ServiceId = 100,
        VisitaIdExterno = d.VisitaIdExterno,
        PayloadJson = Payload(d),
        Hash = $"h{d.VisitaIdExterno}"
    };

    private static CeleroVisitaDto Dto(string id, int? minutos, string estado, string? provincia, string? cancel = null) =>
        new(id, "00000000T", "Servicio X", "Misión X", new DateOnly(2026, 3, 1),
            DuracionMinutos: minutos, Estado: estado, Provincia: provincia, CancellationReason: cancel);

    // ---- Round-trip DTO → PayloadJson → RowAdapter -------------------------------------------------

    [Fact]
    public void PayloadDeCelero_ExponeLosCamposNuevosEnElRowAdapter()
    {
        var dto = Dto("V1", 120, "done", "Madrid");
        var visita = VisitaDesdeDto(dto, 1);

        var row = RowAdapter.FromVisita(visita);

        // "Estado" se mapea al flag de excepción tipado (gap #4 ya cerrado del motor).
        row.GetField("Estado").Should().Be("done");
        // El resto queda filtrable por nombre vía Extra.
        row.GetField("Provincia").Should().Be("Madrid");
        row.GetDecimal("DuracionMinutos").Should().Be(120m);
    }

    [Fact]
    public void PayloadDeCelero_CamposNulos_NoRompenElParseo()
    {
        var dto = Dto("V0", null, "done", null);
        var row = RowAdapter.FromVisita(VisitaDesdeDto(dto, 1));

        row.GetField("Provincia").Should().BeNull();
        row.GetDecimal("DuracionMinutos").Should().Be(0m); // ausente/nulo → 0, sin excepción
    }

    // ---- El motor segmenta por los campos ingestados ----------------------------------------------

    private static (CalculationEngine engine, FakeLoader loader) CreateSut()
    {
        var loader = new FakeLoader();
        return (new CalculationEngine(new FormulaParser(), loader, new VariableResolver()), loader);
    }

    private static CalculationTarget Target() => new()
    {
        ServiceId = 100,
        PeriodId = 1,
        Period = new Period
        {
            Id = 1, Nombre = "Marzo 2026",
            FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31),
            Estado = EstadoPeriodo.Abierto
        }
    };

    private static Concept Concept(string formulaJson) => new()
    {
        Id = 1, Nombre = "Test segmentación", Tipo = TipoConcepto.Factura,
        FechaDesde = new DateOnly(2020, 1, 1), FormulaJson = formulaJson
    };

    private static string CountConFiltro(string filtros) =>
        "{\"type\":\"Aggregate\",\"op\":\"Count\",\"source\":{\"type\":\"Source\",\"entity\":\"VisitasCelero\"," +
        "\"filters\":[" + filtros + "]}}";

    [Fact]
    public async Task Motor_SegmentaVisitasPorProvinciaIngestada()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            VisitaDesdeDto(Dto("V1", 30, "done", "Madrid"), 1),
            VisitaDesdeDto(Dto("V2", 40, "done", "Madrid"), 2),
            VisitaDesdeDto(Dto("V3", 50, "done", "Barcelona"), 3),
        });

        var formula = CountConFiltro("{\"field\":\"Provincia\",\"op\":\"Eq\",\"value\":\"Madrid\"}");
        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);

        r.Resultado.Should().Be(2m);
        r.SistemaOrigen.Should().Be("Celero");
    }

    [Fact]
    public async Task Motor_SegmentaVisitasPorFranjaDeMinutosIngestada()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            VisitaDesdeDto(Dto("V1", 60, "done", "Madrid"), 1),   // fuera [75,105]
            VisitaDesdeDto(Dto("V2", 80, "done", "Madrid"), 2),   // dentro
            VisitaDesdeDto(Dto("V3", 100, "done", "Madrid"), 3),  // dentro
            VisitaDesdeDto(Dto("V4", 140, "done", "Madrid"), 4),  // fuera
        });

        var formula = CountConFiltro(
            "{\"field\":\"DuracionMinutos\",\"op\":\"Gte\",\"value\":75}," +
            "{\"field\":\"DuracionMinutos\",\"op\":\"Lte\",\"value\":105}");
        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);

        r.Resultado.Should().Be(2m);
    }

    [Fact]
    public async Task Motor_FiltraVisitasFallidasPorEstadoIngestado()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            VisitaDesdeDto(Dto("V1", 30, "done", "Madrid"), 1),
            VisitaDesdeDto(Dto("V2", 30, "failed", "Madrid"), 2),
            VisitaDesdeDto(Dto("V3", 30, "cancelled", "Madrid"), 3),
        });

        var soloDone = CountConFiltro("{\"field\":\"Estado\",\"op\":\"Eq\",\"value\":\"done\"}");
        var soloFallidas = CountConFiltro("{\"field\":\"Estado\",\"op\":\"Eq\",\"value\":\"failed\"}");

        var rDone = await sut.EvaluateAsync(Concept(soloDone), Target(), null, CancellationToken.None);
        var rFail = await sut.EvaluateAsync(Concept(soloFallidas), Target(), null, CancellationToken.None);

        rDone.Resultado.Should().Be(1m);
        rFail.Resultado.Should().Be(1m);
    }

    // ---- Booleanos en inglés (observado en los datos de feedback: "Yes"/"No") ----------------------

    [Theory]
    [InlineData("Yes", 1)]   // antes del arreglo "Yes" se evaluaba como false → 0
    [InlineData("yes", 1)]
    [InlineData("No", 0)]
    [InlineData("Sí", 1)]    // español sigue funcionando
    public async Task Motor_FiltraBooleanInglesDelPayload(string respuesta, int esperado)
    {
        var (sut, loader) = CreateSut();
        var visita = new StagingCeleroVisita
        {
            Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100,
            VisitaIdExterno = "VB", Hash = "hb",
            PayloadJson = "{\"actualizacionRealizada\":\"" + respuesta + "\"}"
        };
        loader.Visitas.Add(visita);

        var formula = CountConFiltro("{\"field\":\"actualizacionRealizada\",\"op\":\"Eq\",\"value\":true}");
        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);

        r.Resultado.Should().Be(esperado);
    }

    private sealed class FakeLoader : ICalculationDataLoader
    {
        public List<StagingCeleroVisita> Visitas { get; } = new();
        public Task<CalculationContext> LoadAsync(CalculationTarget target, CancellationToken ct)
            => Task.FromResult(new CalculationContext { Visitas = Visitas });
        public Task<List<RowAdapter>> LoadCrossServiceAsync(int userId, DateOnly desde, DateOnly hasta, string entity, string field, CancellationToken ct)
            => Task.FromResult(new List<RowAdapter>());
    }
}
