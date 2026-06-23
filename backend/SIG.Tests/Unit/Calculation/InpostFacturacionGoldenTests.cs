using System.Globalization;
using SIG.Application.Calculation;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// GOLDEN TEST — caso real de facturación INPOST (lockers), proforma 12 / marzo 2026.
///
/// Modelo tarifario real (hoja "CALCULOS MarzoCat" del Excel del cliente):
///   se mira la DURACIÓN EN MINUTOS de CADA visita, se busca su FRANJA en una tabla,
///   y se aplica un ÍNDICE CORRECTOR (multiplicador) sobre la tarifa base de la provincia.
///   La factura es la SUMA de todas las visitas así tarifadas.
///
///   Tarifa base Barcelona = 96,16 €
///   ┌───────────┬─────────┬──────────┐
///   │ minutos   │ índice  │ tarifa   │
///   ├───────────┼─────────┼──────────┤
///   │ 0  – 74   │ ×1      │  96,16   │
///   │ 75 – 105  │ ×1,5    │ 144,24   │
///   │ 106– 136  │ ×2      │ 192,32   │
///   │ 137– 167  │ ×2,5    │ 240,40   │
///   │ 168– 198  │ ×3      │ 288,48   │
///   │ …         │ …       │ …        │
///   └───────────┴─────────┴──────────┘
///
/// Este test verifica si el MOTOR ACTUAL (sin tocar código de producción) es capaz de
/// reproducir ese importe. Conclusión documentada al final del archivo.
/// </summary>
public class InpostFacturacionGoldenTests
{
    private const decimal BaseBarcelona = 96.16m;

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
            Id = 1,
            Nombre = "Marzo 2026",
            FechaInicio = new DateOnly(2026, 3, 1),
            FechaFin = new DateOnly(2026, 3, 31),
            Estado = EstadoPeriodo.Abierto
        }
    };

    private static Concept Concept(string formulaJson) => new()
    {
        Id = 1, Nombre = "Facturación visitas Inpost", Tipo = TipoConcepto.Factura,
        FechaDesde = new DateOnly(2020, 1, 1), FormulaJson = formulaJson
    };

    private static StagingCeleroVisita Visita(int minutos, int dia) => new()
    {
        Fecha = new DateOnly(2026, 3, dia),
        UserId = 1,
        ServiceId = 100,
        VisitaIdExterno = $"ES{minutos:0000}",
        PayloadJson = "{\"minutos\":" + minutos + "}",
        Hash = $"h{minutos}-{dia}"
    };

    private static string Num(decimal d) => d.ToString(CultureInfo.InvariantCulture);

    // Cuenta visitas cuya duración cae en [min,max] minutos y la multiplica por la tarifa de la franja.
    private static string TramoCount(int min, int max, decimal tarifa) =>
        "{\"type\":\"BinaryOp\",\"op\":\"Mul\",\"left\":" +
          "{\"type\":\"Aggregate\",\"op\":\"Count\",\"source\":{\"type\":\"Source\",\"entity\":\"VisitasCelero\"," +
            "\"filters\":[{\"field\":\"minutos\",\"op\":\"Gte\",\"value\":" + min + "}," +
                        "{\"field\":\"minutos\",\"op\":\"Lte\",\"value\":" + max + "}]}}," +
          "\"right\":{\"type\":\"Number\",\"value\":" + Num(tarifa) + "}}";

    private static string Add(string a, string b) =>
        "{\"type\":\"BinaryOp\",\"op\":\"Add\",\"left\":" + a + ",\"right\":" + b + "}";

    private static readonly StagingCeleroVisita[] MuestraReal =
    {
        // minutos -> tarifa esperada por visita
        Visita(23, 16),   //  96,16  (franja 0-74)
        Visita(30, 9),    //  96,16  (franja 0-74)
        Visita(80, 10),   // 144,24  (franja 75-105)
        Visita(100, 4),   // 144,24  (franja 75-105)
        Visita(154, 31),  // 240,40  (franja 137-167)
        Visita(179, 27),  // 288,48  (franja 168-198)
    };
    // Total esperado = 2·96,16 + 2·144,24 + 240,40 + 288,48 = 1009,68 €
    private const decimal TotalEsperado = 1009.68m;

    /// <summary>
    /// ✅ POSITIVO: el motor SÍ reproduce el importe real de Inpost, pero solo descomponiendo
    /// la tarifa en N términos "Count(franja) × tarifa" sumados. Requiere que el minutaje viaje
    /// en el PayloadJson de la visita (clave "minutos").
    /// </summary>
    [Fact]
    public async Task Inpost_PorFranjasDeMinutos_ReproduceLaFacturaReal()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(MuestraReal);

        // Árbol = suma de los 4 tramos con datos (los 7 tramos vacíos darían 0).
        var formula = Add(
            Add(TramoCount(0, 74, 96.16m), TramoCount(75, 105, 144.24m)),
            Add(TramoCount(137, 167, 240.40m), TramoCount(168, 198, 288.48m)));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);

        r.Resultado.Should().Be(TotalEsperado);
        r.SistemaOrigen.Should().Be("Celero");
    }

    /// <summary>
    /// 🔬 DIAGNÓSTICO: el nodo `Tramos` (que MOTOR_CALCULO.md §7 presenta como cobertura de
    /// "conteo × cantidad incremental") NO modela Inpost. `Tramos` solo acepta UNA cantidad
    /// agregada (escalar) y aplica precios incrementales acumulativos; al agregar las visitas
    /// se pierde la franja por-visita. Alimentándolo con la única magnitud escalar razonable
    /// (Σ minutos = 566) y la tabla como tramos incrementales, el resultado NO coincide.
    /// </summary>
    [Fact]
    public async Task Inpost_ConNodoTramos_NoReproduceLaFactura()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(MuestraReal);

        // Σ minutos del período como cantidad, con la tabla de franjas como tramos incrementales.
        var formula = """
        {"type":"Tramos",
         "cantidad":{"type":"Aggregate","op":"Sum","field":"minutos","source":{"type":"Source","entity":"VisitasCelero","filters":[]}},
         "tramos":[{"hasta":74,"precio":96.16},{"hasta":105,"precio":144.24},{"hasta":136,"precio":192.32},
                   {"hasta":167,"precio":240.40},{"hasta":198,"precio":288.48},{"hasta":null,"precio":336.56}]}
        """;

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);

        // Demuestra el desajuste semántico: Tramos NO da la factura real.
        r.Resultado.Should().NotBe(TotalEsperado);
    }

    private sealed class FakeLoader : ICalculationDataLoader
    {
        public List<StagingCeleroVisita> Visitas { get; } = new();
        public Task<CalculationContext> LoadAsync(CalculationTarget target, CancellationToken ct)
            => Task.FromResult(new CalculationContext { Visitas = Visitas });
    }
}
