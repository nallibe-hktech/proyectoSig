using System.Globalization;
using SIG.Application.Calculation;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// GOLDEN TESTS de facturación real (carpeta FACTURACIÓN del cliente), casos FUTURE, GRANINI y DYSON.
/// Cada test ancla su resultado esperado a un número IMPRESO en el Excel original (no a una suma
/// inventada), y comprueba que el motor de cálculo lo reproduce con los nodos actuales.
///
/// Complementan a <see cref="InpostFacturacionGoldenTests"/> (el único modelo con regla no trivial).
/// Aquí los modelos son sumas y mezclas; el valor está en confirmar que reconcilian al céntimo.
/// </summary>
public class FacturacionRealGoldenTests
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
            Id = 1, Nombre = "Mayo 2026",
            FechaInicio = new DateOnly(2026, 5, 1), FechaFin = new DateOnly(2026, 5, 31),
            Estado = EstadoPeriodo.Abierto
        }
    };

    private static Concept Concept(string formulaJson, TipoConcepto tipo = TipoConcepto.Factura) => new()
    {
        Id = 1, Nombre = "Facturación", Tipo = tipo, FechaDesde = new DateOnly(2020, 1, 1), FormulaJson = formulaJson
    };

    // ---- builders de fórmula (JSON escapado, sin ambigüedad de llaves) ----
    private static string Lit(decimal d) => d.ToString(CultureInfo.InvariantCulture);
    private static string N(decimal d) => "{\"type\":\"Number\",\"value\":" + Lit(d) + "}";
    private static string Sum(string entity, string field) =>
        "{\"type\":\"Aggregate\",\"op\":\"Sum\",\"field\":\"" + field + "\"," +
        "\"source\":{\"type\":\"Source\",\"entity\":\"" + entity + "\",\"filters\":[]}}";
    private static string Bin(string op, string a, string b) =>
        "{\"type\":\"BinaryOp\",\"op\":\"" + op + "\",\"left\":" + a + ",\"right\":" + b + "}";
    private static string Add(string a, string b) => Bin("Add", a, b);
    private static string Mul(string a, string b) => Bin("Mul", a, b);
    private static string Sub(string a, string b) => Bin("Sub", a, b);

    private static StagingCeleroVisita Visita(string payloadJson, int dia) => new()
    {
        Fecha = new DateOnly(2026, 5, dia), UserId = 1, ServiceId = 100,
        VisitaIdExterno = $"v{dia}-{payloadJson.GetHashCode():X}", PayloadJson = payloadJson, Hash = $"h{dia}-{payloadJson.Length}"
    };

    /// <summary>
    /// FUTURE (hoja MAYO_GOTEO): la FACTURACIÓN FINAL de cada tienda = implantación + logística + extra.
    /// Anclas (columna FACTURACIÓN FINAL impresa): 1062,88 + 630,80 + 238,24 = 1931,92 €.
    /// </summary>
    [Fact]
    public async Task Future_SumaDeServiciosPorTienda_ReproduceFacturacionFinal()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            Visita("{\"implantacion\":238.24,\"logistico\":371.84,\"extra\":452.8}", 5),  // FRA 1062,88
            Visita("{\"implantacion\":238.24,\"logistico\":392.56,\"extra\":0}", 5),       // FRA 630,80
            Visita("{\"implantacion\":238.24,\"logistico\":0,\"extra\":0}", 11),           // FRA 238,24
        });

        var formula = Add(Add(Sum("VisitasCelero", "implantacion"), Sum("VisitasCelero", "logistico")),
                          Sum("VisitasCelero", "extra"));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(1931.92m);
    }

    /// <summary>
    /// GRANINI (hoja GRANINI, mayo): KMS € = TOTAL KMS × Precio KMS (0,24).
    /// Ancla (columna KMS € impresa para Daniel Santa María): 3487,9973913 × 0,24 = 837,12 €.
    /// Verifica la única regla no trivial del caso (multiplicar un agregado por la tarifa de km).
    /// </summary>
    [Fact]
    public async Task Granini_KilometrajePorTarifa_ReproduceImporteKms()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.Add(Visita("{\"totalKms\":3487.9973913043477}", 15));

        var formula = Mul(Sum("VisitasCelero", "totalKms"), N(0.24m));

        var r = await sut.EvaluateAsync(Concept(formula, TipoConcepto.Pago), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(837.12m);
    }

    /// <summary>
    /// DYSON (hoja DB1, MAYO): el cierre de COSTES suma 6 conceptos de coste; el MARGEN = ingresos − costes
    /// (margen "al vuelo", no se almacena — ver comentario en Entities.cs:333).
    /// Anclas impresas en DB1: TOTAL COSTES = 31.624,05 € ; MARGEN = 46.640,41 € (ingresos 78.264,46).
    /// </summary>
    [Fact]
    public async Task Dyson_TotalCostes_SumaLosSeisConceptos()
    {
        var (sut, _) = CreateSut();
        // Cada componente sería en producción su propio concepto; aquí se anclan a los importes impresos.
        var costes = Add(Add(Add(N(4060.30m), N(593.70m)), Add(N(24305.24m), N(0m))), Add(N(77.81m), N(2587m)));

        var r = await sut.EvaluateAsync(Concept(costes, TipoConcepto.Pago), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(31624.05m); // TOTAL COSTES impreso
    }

    [Fact]
    public async Task Dyson_Margen_EsIngresosMenosCostes()
    {
        var (sut, _) = CreateSut();
        var margen = Sub(N(78264.46m), N(31624.05m)); // ingresos − total costes

        var r = await sut.EvaluateAsync(Concept(margen), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(46640.41m); // MARGEN impreso
    }

    private sealed class FakeLoader : ICalculationDataLoader
    {
        public List<StagingCeleroVisita> Visitas { get; } = new();
        public Task<CalculationContext> LoadAsync(CalculationTarget target, CancellationToken ct)
            => Task.FromResult(new CalculationContext { Visitas = Visitas });
    }
}
