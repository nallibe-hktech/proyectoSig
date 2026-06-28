using System.Globalization;
using SIG.Application.Calculation;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// END-TO-END: la FACTURA MENSUAL COMPLETA de Inpost, ensamblada con lo que la documentación SÍ confirma.
/// Demuestra que la calculadora del proyecto ya puede emitir una factura Inpost de punta a punta HOY.
///
/// Fuente de cada bloque (todo trazado a documentación real):
///  - Modelo de visita y excepciones: entrevista de Esmeralda Rodríguez (CierresIntegralesSIG, hoja
///    "Detalles PagosFact_IL", fila 3): "tarifa base, se incrementa por intervalos de tiempo… el tiempo
///    afecta a la cuota… visita fallida mismo precio que visita ok… cancelada sin salir a ruta no tiene coste".
///  - Tarifas de franja: PROFORMA 12.Inpost (tabla OPERATIVA, base Barcelona 96,16). ⚠️ PROVISIONAL:
///    la tabla OFICIAL (incl. pernocta) está en "TARIFAS INPOST DIURNAS Y CON PERNOCTA 2026.pptx" (SharePoint).
///  - Coste logística: factura MDP real DINE26-00429 (base 740,36 €), refacturada "con margen 30%" +
///    "fee del 8% sobre el total de la logística" (entrevista). ⚠️ La forma de combinar 30% y 8% está
///    marcada como HUECO ABIERTO en ACTA_HUECOS_SESIONES_VALIDACION (fila A3): se aplica aquí el 8% sobre
///    el coste base; la alternativa (8% sobre coste+30%) cambiaría el total y queda pendiente de confirmar.
///
/// Lo único que falta para producción NO es matemática: (1) ingestar la duración de visita de Celero
/// (realDuration) en el staging, y (2) sustituir los números provisionales por los del .pptx oficial.
/// </summary>
public class FacturaInpostEndToEndGoldenTest
{
    private const decimal CosteMdp = 740.36m; // factura DINE26-00429, base imponible

    private static (CalculationEngine engine, FakeLoader loader) CreateSut()
    {
        var loader = new FakeLoader();
        return (new CalculationEngine(new FormulaParser(), loader, new VariableResolver()), loader);
    }

    private static CalculationTarget Target() => new()
    {
        ServiceId = 100, PeriodId = 1,
        Period = new Period { Id = 1, Nombre = "Marzo 2026",
            FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = EstadoPeriodo.Abierto }
    };

    private static Concept Concept(string formulaJson) => new()
    {
        Id = 1, Nombre = "Factura mensual Inpost", Tipo = TipoConcepto.Factura,
        FechaDesde = new DateOnly(2020, 1, 1), FormulaJson = formulaJson
    };

    // ---- builders ----
    private static string Lit(decimal d) => d.ToString(CultureInfo.InvariantCulture);
    private static string N(decimal d) => "{\"type\":\"Number\",\"value\":" + Lit(d) + "}";
    private static string F(string field, string op, string valueJson) =>
        "{\"field\":\"" + field + "\",\"op\":\"" + op + "\",\"value\":" + valueJson + "}";
    private static string Count(params string[] filters) =>
        "{\"type\":\"Aggregate\",\"op\":\"Count\",\"source\":{\"type\":\"Source\",\"entity\":\"VisitasCelero\"," +
        "\"filters\":[" + string.Join(",", filters) + "]}}";
    private static string Bin(string op, string a, string b) =>
        "{\"type\":\"BinaryOp\",\"op\":\"" + op + "\",\"left\":" + a + ",\"right\":" + b + "}";
    private static string Add(string a, string b) => Bin("Add", a, b);
    private static string Mul(string a, string b) => Bin("Mul", a, b);
    private static string Sub(string a, string b) => Bin("Sub", a, b);
    private static string Pct(string a, string b) => Bin("Pct", a, b); // l*(1+r/100): "+r %"

    private static string Franja(int min, int max) =>
        F("minutos", "Gte", min.ToString()) + "," + F("minutos", "Lte", max.ToString());

    private static StagingCeleroVisita V(string payload, int dia) => new()
    {
        Fecha = new DateOnly(2026, 3, dia), UserId = 1, ServiceId = 100,
        VisitaIdExterno = $"v{dia}", PayloadJson = payload, Hash = $"{dia}-{payload.Length}"
    };

    [Fact]
    public async Task FacturaInpost_VisitasPorFranja_MasExcepciones_MasLogistica_ProduceTotalMensual()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            V("{\"minutos\":80,\"estado\":\"ok\"}", 5),                                   // franja 75-105 -> 144,24
            V("{\"minutos\":154,\"estado\":\"fallida\"}", 6),                             // fallida = MISMO coste -> 240,40
            V("{\"minutos\":154,\"estado\":\"cancelada\",\"salioRuta\":false}", 7),       // cancelada SIN salir -> NO factura
        });

        // --- BLOQUE 1+2: visitas por franja, con la excepción "cancelada sin salir a ruta" descontada ---
        // Franja 75-105 (×1,5 base = 144,24): 1 visita ok.
        var franja75 = Mul(Count(Franja(75, 105)), N(144.24m));
        // Franja 137-167 (×2,5 base = 240,40): 2 visitas (fallida cuenta igual) menos 1 cancelada-sin-ruta = 1.
        var visitas137 = Sub(Count(Franja(137, 167)),
                             Count(Franja(137, 167), F("Estado", "Eq", "\"cancelada\""), F("salioRuta", "Eq", "false")));
        var franja137 = Mul(visitas137, N(240.40m));
        var subtotalVisitas = Add(franja75, franja137); // 144,24 + 240,40 = 384,64

        // --- BLOQUE 3: logística MDP refacturada (coste real 740,36) ---
        var margen = Pct(N(CosteMdp), N(30m));            // +30% margen (documentado)  = 962,468
        var feeAdmin = Mul(N(CosteMdp), N(0.08m));        // 8% admin sobre coste base (A3 pendiente) = 59,2288
        var subtotalLogistica = Add(margen, feeAdmin);     // = 1021,6968

        var facturaTotal = Add(subtotalVisitas, subtotalLogistica);

        var r = await sut.EvaluateAsync(Concept(facturaTotal), Target(), null, CancellationToken.None);

        // 384,64 (visitas) + 1021,6968 (logística) = 1406,3368 -> 1406,34 €
        r.Resultado.Should().Be(1406.34m);
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
