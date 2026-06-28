using System.Globalization;
using System.Text.Json;
using SIG.Application.Calculation;
using SIG.Application.DTOs;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// GOLDEN — Inpost, franja de minutos × PROVINCIA. Ancla la tabla OFICIAL documentada en el Excel del
/// cliente `PROFORMA 12.Inpost con detalle 31-3-26.xlsx`, hoja `CALCULOS MarzoCat`, columna "otra provincia"
/// (base 164,32 €; incrementos de 48,08 € = 96,16 × 0,5 por franja):
///   0-74 = 164,32 | 75-105 = 212,40 | 106-136 = 260,48 | 137-167 = 308,56 | 168-198 = 356,64 ...
///
/// Verifica dos cosas que el cambio de ingesta (docs/RETOMA_INPOST_FACTURACION.md §4.3 + B4) habilita:
///   1. La provincia viaja en el PayloadJson (campo `Provincia`, leído de `addressState` de la visita).
///   2. El motor segmenta por provincia Y por franja de minutos a la vez, reproduciendo la tarifa documentada.
/// El payload se construye serializando el DTO igual que la sincronización real (JsonSerializer.Serialize).
/// </summary>
public class InpostOtraProvinciaGoldenTests
{
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
        Id = 1, Nombre = "Facturación Inpost otra provincia", Tipo = TipoConcepto.Factura,
        FechaDesde = new DateOnly(2020, 1, 1), FormulaJson = formulaJson
    };

    // Visita con minutos + provincia en el PayloadJson, serializada como en la ingesta real.
    private static StagingCeleroVisita Visita(int minutos, string provincia, int dia)
    {
        var dto = new CeleroVisitaDto(
            $"ES{minutos:0000}", "00000000T", "Servicio X", "Misión X", new DateOnly(2026, 3, dia),
            DuracionMinutos: minutos, Estado: "done", Provincia: provincia, CancellationReason: null);
        return new StagingCeleroVisita
        {
            Fecha = new DateOnly(2026, 3, dia), UserId = 1, ServiceId = 100,
            VisitaIdExterno = dto.VisitaIdExterno, Hash = $"h{minutos}-{dia}",
            PayloadJson = JsonSerializer.Serialize(dto)
        };
    }

    private static string Num(decimal d) => d.ToString(CultureInfo.InvariantCulture);

    // Count(provincia = X AND minutos en [min,max]) × tarifa de la franja para esa provincia.
    private static string TramoProvincia(string provincia, int min, int max, decimal tarifa) =>
        "{\"type\":\"BinaryOp\",\"op\":\"Mul\",\"left\":" +
          "{\"type\":\"Aggregate\",\"op\":\"Count\",\"source\":{\"type\":\"Source\",\"entity\":\"VisitasCelero\"," +
            "\"filters\":[{\"field\":\"Provincia\",\"op\":\"Eq\",\"value\":\"" + provincia + "\"}," +
                        "{\"field\":\"DuracionMinutos\",\"op\":\"Gte\",\"value\":" + min + "}," +
                        "{\"field\":\"DuracionMinutos\",\"op\":\"Lte\",\"value\":" + max + "}]}}," +
          "\"right\":{\"type\":\"Number\",\"value\":" + Num(tarifa) + "}}";

    private static string Add(string a, string b) =>
        "{\"type\":\"BinaryOp\",\"op\":\"Add\",\"left\":" + a + ",\"right\":" + b + "}";

    [Fact]
    public async Task Inpost_OtraProvincia_SegmentaPorProvinciaYFranja()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            // 3 visitas en "otra provincia" (Madrid) → tabla otra provincia
            Visita(30, "Madrid", 1),    // franja 0-74    → 164,32
            Visita(80, "Madrid", 2),    // franja 75-105  → 212,40
            Visita(154, "Madrid", 3),   // franja 137-167 → 308,56
            // 1 visita en Barcelona (tarifa base distinta) que NO debe contar en este concepto
            Visita(80, "Barcelona", 4),
        });

        var formula = Add(
            Add(TramoProvincia("Madrid", 0, 74, 164.32m), TramoProvincia("Madrid", 75, 105, 212.40m)),
            TramoProvincia("Madrid", 137, 167, 308.56m));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);

        // 164,32 + 212,40 + 308,56 = 685,28 (la visita de Barcelona queda fuera por el filtro de provincia)
        r.Resultado.Should().Be(685.28m);
        r.SistemaOrigen.Should().Be("Celero");
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
