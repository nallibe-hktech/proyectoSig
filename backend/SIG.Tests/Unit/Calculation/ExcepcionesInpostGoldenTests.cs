using System.Globalization;
using SIG.Application.Calculation;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// Tests que toman las EXCEPCIONES LITERALES de Inpost recogidas en el Excel maestro
/// `CierresIntegralesSIG`, hoja "Lógicas Pagos-Facturación", columna "Excepciones_Modelo":
///
///     "fallida -> mismo coste | cancelacion -> no se factura si no se ha salido a ruta | pernocta -> tarifas especiales"
/// (y "2ª/3ª visita", presente para Inpost y varios clientes).
///
/// OBJETIVO: convertir cada "pregunta abierta" sobre excepciones en algo COMPROBABLE — ¿sabe el
/// motor actual aplicar la regla? Conclusión: el motor SÍ puede, SIEMPRE que los flags lleguen en
/// el PayloadJson de la visita (esa dependencia de dato sigue siendo la pregunta abierta real).
///
/// Honestidad sobre los números:
///  - La tarifa base 96,16 € (Barcelona) es REAL (Excel del cliente).
///  - Las tarifas "especiales" de pernocta y de 2ª/3ª visita son PLACEHOLDER (el Excel dice
///    "tarifas especiales" sin dar el número): aquí solo se prueba el MECANISMO de segmentación.
/// </summary>
public class ExcepcionesInpostGoldenTests
{
    private const decimal BaseBcn = 96.16m;

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
        Id = 1, Nombre = "Facturación Inpost", Tipo = TipoConcepto.Factura,
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
    private static string Mul(string a, string b) => Bin("Mul", a, b);
    private static string Add(string a, string b) => Bin("Add", a, b);
    private static string Sub(string a, string b) => Bin("Sub", a, b);

    private static StagingCeleroVisita V(string payloadJson, int dia) => new()
    {
        Fecha = new DateOnly(2026, 3, dia), UserId = 1, ServiceId = 100,
        VisitaIdExterno = $"v{dia}-{payloadJson.Length}", PayloadJson = payloadJson, Hash = $"{dia}-{payloadJson.GetHashCode():X}"
    };

    /// <summary>
    /// Regla genérica (la mayoría de clientes): la visita FALLIDA NO se factura.
    /// 2 visitas ok + 1 fallida → se cobran 2. Demuestra que el motor puede EXCLUIR por estado.
    /// </summary>
    [Fact]
    public async Task Fallida_NoSeFactura_ExcluyePorEstado()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            V("{\"estado\":\"ok\"}", 3), V("{\"estado\":\"ok\"}", 4), V("{\"estado\":\"fallida\"}", 5),
        });

        var formula = Mul(Count(F("Estado", "Neq", "\"fallida\"")), N(BaseBcn));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(192.32m); // 2 × 96,16
    }

    /// <summary>
    /// Regla LITERAL de Inpost: "fallida -> mismo coste" (la fallida SÍ se factura igual).
    /// Mismos datos que el test anterior, pero sin filtrar estado → se cobran las 3.
    /// Demuestra que el mismo motor expresa la regla opuesta (solo cambia la fórmula).
    /// </summary>
    [Fact]
    public async Task Fallida_MismoCoste_Inpost_SeFacturanTodas()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            V("{\"estado\":\"ok\"}", 3), V("{\"estado\":\"ok\"}", 4), V("{\"estado\":\"fallida\"}", 5),
        });

        var formula = Mul(Count(/* sin filtro de estado */), N(BaseBcn));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(288.48m); // 3 × 96,16
    }

    /// <summary>
    /// Regla LITERAL de Inpost: "cancelacion -> no se factura SI no se ha salido a ruta".
    /// Es una exclusión CONDICIONAL (cancelada Y !salioRuta). El motor no tiene OR/condicional en
    /// los filtros (solo AND), así que se expresa con resta: total − (cancelada Y no salió).
    /// Datos: 2 ok + 1 cancelada-sin-salir (no factura) + 1 cancelada-con-salida (sí factura) → 3.
    /// </summary>
    [Fact]
    public async Task Cancelacion_NoFacturaSiNoSalioARuta_SeExpresaConResta()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            V("{\"estado\":\"ok\"}", 3),
            V("{\"estado\":\"ok\"}", 4),
            V("{\"estado\":\"cancelada\",\"salioRuta\":false}", 5), // NO factura
            V("{\"estado\":\"cancelada\",\"salioRuta\":true}", 6),  // SÍ factura (salió a ruta)
        });

        var facturables = Sub(Count(), Count(F("Estado", "Eq", "\"cancelada\""), F("salioRuta", "Eq", "false")));
        var formula = Mul(facturables, N(BaseBcn));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(288.48m); // (4 − 1) × 96,16
    }

    /// <summary>
    /// Flag "2ª/3ª visita": tarifa distinta para revisitas. Mecanismo de segmentación por NumeroVisita
    /// (operador In para agrupar 2ª y 3ª). Tarifa de revisita = PLACEHOLDER (48,08) — el Excel no la fija.
    /// </summary>
    [Fact]
    public async Task SegundaTerceraVisita_SegmentaPorNumeroVisita()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            V("{\"numeroVisita\":1}", 3), V("{\"numeroVisita\":1}", 4), V("{\"numeroVisita\":2}", 5),
        });

        var formula = Add(
            Mul(Count(F("NumeroVisita", "Eq", "1")), N(BaseBcn)),
            Mul(Count(F("NumeroVisita", "In", "[2,3]")), N(48.08m)));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(240.40m); // 2×96,16 + 1×48,08
    }

    /// <summary>
    /// Regla LITERAL de Inpost: "pernocta -> tarifas especiales". Segmentación por el flag booleano
    /// Pernocta. Tarifa especial = PLACEHOLDER (150) — el Excel solo dice "tarifas especiales".
    /// </summary>
    [Fact]
    public async Task Pernocta_AplicaTarifaEspecial_SegmentaPorFlag()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            V("{\"estado\":\"ok\"}", 3), V("{\"estado\":\"ok\"}", 4), V("{\"pernocta\":true}", 5),
        });

        var formula = Add(
            Mul(Count(F("Pernocta", "Eq", "false")), N(BaseBcn)),
            Mul(Count(F("Pernocta", "Eq", "true")), N(150m)));

        var r = await sut.EvaluateAsync(Concept(formula), Target(), null, CancellationToken.None);
        r.Resultado.Should().Be(342.32m); // 2×96,16 + 1×150
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
