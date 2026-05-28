using System.Text.Json;
using SIG.Application.Calculation;
using SIG.Application.Calculation.Nodes;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Calculation;

public class CalculationEngineTests
{
    private static (CalculationEngine engine, FakeDataLoader loader) CreateSut()
    {
        var parser = new FormulaParser();
        var loader = new FakeDataLoader();
        var resolver = new VariableResolver();
        return (new CalculationEngine(parser, loader, resolver), loader);
    }

    private static Closure CreateClosure(int projectId = 100, DateOnly? desde = null, DateOnly? hasta = null) => new()
    {
        Id = 1,
        ProjectId = projectId,
        PeriodId = 1,
        Period = new Period
        {
            Id = 1,
            Nombre = "Marzo 2026",
            FechaInicio = desde ?? new DateOnly(2026, 3, 1),
            FechaFin = hasta ?? new DateOnly(2026, 3, 31),
            Estado = EstadoPeriodo.Abierto
        }
    };

    private static Concept CreateConcept(string formulaJson, TipoConcepto tipo = TipoConcepto.Pago) => new()
    {
        Id = 1, Nombre = "T", Tipo = tipo, FechaDesde = new DateOnly(2020, 1, 1), FormulaJson = formulaJson
    };

    // === PRIMITIVA: NumberNode ===

    [Fact]
    public async Task Evaluate_Number_DevuelveValor()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Number","value":7.25}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(7.25m);
    }

    [Fact]
    public async Task Evaluate_NumberConDecimales_SeRedondeaA2Decimales()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Number","value":3.14159}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(3.14m);
    }

    // === PRIMITIVA: VariableNode ===

    [Fact]
    public async Task Evaluate_Variable_ResolverDevuelveValorDelMapeo()
    {
        var (sut, loader) = CreateSut();
        loader.Variables.Add(new Variable { Id = 4, Nombre = "TarifaHora", QuestionIdExterno = "q4", MapeoValoresJson = """[{"respuesta":"default","valor":18.5}]""" });
        var concept = CreateConcept("""{"type":"Variable","variableId":4}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(18.5m);
    }

    [Fact]
    public async Task Evaluate_VariableNoEncontrada_DevuelveCero()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Variable","variableId":999}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(0m);
    }

    // === PRIMITIVA: AggregateNode ===

    [Fact]
    public async Task Evaluate_SumaGastosPayHawk_DevuelveSumaImporte()
    {
        var (sut, loader) = CreateSut();
        loader.Gastos.AddRange(new[]
        {
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, Importe = 100m, Categoria = "viaje", PayloadJson = "{}", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ProjectId = 100, Importe = 50m, Categoria = "comida", PayloadJson = "{}", Hash = "h2" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Sum","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(150m);
        r.SistemaOrigen.Should().Be("PayHawk");
    }

    [Fact]
    public async Task Evaluate_CountVisitas_DevuelveNumeroFilas()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ProjectId = 100, ActionId = 200, VisitaIdExterno = "v1", PayloadJson = "{}", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ProjectId = 100, ActionId = 200, VisitaIdExterno = "v2", PayloadJson = "{}", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ProjectId = 100, ActionId = 200, VisitaIdExterno = "v3", PayloadJson = "{}", Hash = "h" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(3m);
    }

    [Fact]
    public async Task Evaluate_CountConFiltro_AplicaFiltroEq()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ProjectId = 100, ActionId = 200, PayloadJson = "{}", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ProjectId = 100, ActionId = 200, PayloadJson = "{}", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ProjectId = 100, ActionId = 300, PayloadJson = "{}", VisitaIdExterno = "v3", Hash = "h" },
        });
        var concept = CreateConcept("""
        {"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"ActionId","op":"Eq","value":200}]}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(2m);
    }

    [Fact]
    public async Task Evaluate_MinSobreImporte_DevuelveMinimo()
    {
        var (sut, loader) = CreateSut();
        loader.Gastos.AddRange(new[]
        {
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ProjectId = 100, Importe = 30m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Min","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(30m);
    }

    [Fact]
    public async Task Evaluate_MaxSobreImporte_DevuelveMaximo()
    {
        var (sut, loader) = CreateSut();
        loader.Gastos.AddRange(new[]
        {
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ProjectId = 100, Importe = 30m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Max","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(100m);
    }

    [Fact]
    public async Task Evaluate_DatasetVacio_RegistraIncidenciaEmptyDataset()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Aggregate","op":"Sum","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(0m);
        r.Incidencias.Should().ContainSingle(i => i.Tipo == "EmptyDataset");
    }

    // === PRIMITIVA: BinaryOpNode ===

    [Fact]
    public async Task Evaluate_BinaryAdd_Suma()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"BinaryOp","op":"Add","left":{"type":"Number","value":10},"right":{"type":"Number","value":5}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(15m);
    }

    [Fact]
    public async Task Evaluate_BinarySub_Resta()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"BinaryOp","op":"Sub","left":{"type":"Number","value":10},"right":{"type":"Number","value":3}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(7m);
    }

    [Fact]
    public async Task Evaluate_BinaryMul_Multiplica()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"BinaryOp","op":"Mul","left":{"type":"Number","value":4},"right":{"type":"Number","value":2.5}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(10m);
    }

    [Fact]
    public async Task Evaluate_BinaryDiv_Divide()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"BinaryOp","op":"Div","left":{"type":"Number","value":10},"right":{"type":"Number","value":4}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(2.5m);
    }

    [Fact]
    public async Task Evaluate_BinaryDivPorCero_DevuelveCeroConIncidencia()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"BinaryOp","op":"Div","left":{"type":"Number","value":10},"right":{"type":"Number","value":0}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(0m);
        r.Incidencias.Should().ContainSingle(i => i.Tipo == "DivisionByZero");
    }

    [Fact]
    public async Task Evaluate_BinaryPct_AplicaPorcentaje()
    {
        // 100 * (1 + 15/100) = 115
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"BinaryOp","op":"Pct","left":{"type":"Number","value":100},"right":{"type":"Number","value":15}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(115m);
    }

    [Fact]
    public async Task Evaluate_OperacionBinariaDesconocida_LanzaFormulaInvalida()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"BinaryOp","op":"XOR","left":{"type":"Number","value":1},"right":{"type":"Number","value":2}}""");
        await FluentActions.Awaiting(() => sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None))
            .Should().ThrowAsync<FormulaInvalidException>();
    }

    // === EJEMPLOS REALES SEED ===

    [Fact]
    public async Task Evaluate_BonusVisitaEstandar_x5_AplicaCalculoCorrectamente()
    {
        // Cuenta(VisitasCelero ActionId=200) × 5
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, ActionId = 200, PayloadJson = "{}", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 6), UserId = 1, ProjectId = 100, ActionId = 200, PayloadJson = "{}", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 7), UserId = 1, ProjectId = 100, ActionId = 200, PayloadJson = "{}", VisitaIdExterno = "v3", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 8), UserId = 1, ProjectId = 100, ActionId = 300, PayloadJson = "{}", VisitaIdExterno = "v4", Hash = "h" },
        });
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"ActionId","op":"Eq","value":200}]}},
         "right":{"type":"Number","value":5}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(15m); // 3 visitas ActionId=200 × 5
        r.SistemaOrigen.Should().Be("Celero");
    }

    [Fact]
    public async Task Evaluate_PagoPorHorasTarifa_AplicaCalculoCorrectamente()
    {
        // Suma(HorasBizneo.horas) × Variable(TarifaHora=18.5)
        var (sut, loader) = CreateSut();
        loader.HorasBizneo.AddRange(new[]
        {
            new StagingBizneoHora { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, Horas = 8m, PayloadJson = "{}", RegistroIdExterno = "r1", Hash = "h" },
            new StagingBizneoHora { Fecha = new DateOnly(2026, 3, 6), UserId = 1, ProjectId = 100, Horas = 6m, PayloadJson = "{}", RegistroIdExterno = "r2", Hash = "h" },
        });
        loader.Variables.Add(new Variable { Id = 4, Nombre = "TarifaHora", QuestionIdExterno = "q4", MapeoValoresJson = """[{"respuesta":"default","valor":18.5}]""" });
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"Aggregate","op":"Sum","field":"Horas","source":{"type":"Source","entity":"HorasBizneo","filters":[]}},
         "right":{"type":"Variable","variableId":4}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(259m); // 14h × 18.5 = 259
    }

    [Fact]
    public async Task Evaluate_RefacturacionGastosPct_AplicaPorcentaje()
    {
        // Suma(GastosPayHawk.importe) × (1 + 15/100)
        var (sut, loader) = CreateSut();
        loader.Gastos.AddRange(new[]
        {
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, Importe = 200m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ProjectId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },
        });
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Pct",
         "left":{"type":"Aggregate","op":"Sum","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}},
         "right":{"type":"Number","value":15}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(345m); // 300 × 1.15 = 345
    }

    // === FILTROS IMPLICITOS (período, proyecto, recurso) ===

    [Fact]
    public async Task Evaluate_FiltroImplicitoPeriodo_ExcluyeFilasFueraDelRango()
    {
        var (sut, loader) = CreateSut();
        loader.Gastos.AddRange(new[]
        {
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 2, 28), UserId = 1, ProjectId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" }, // FUERA
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 2, ProjectId = 100, Importe = 50m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },  // DENTRO
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 4, 1), UserId = 3, ProjectId = 100, Importe = 200m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g3", Hash = "h3" }, // FUERA
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Sum","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(50m);
    }

    [Fact]
    public async Task Evaluate_FiltroImplicitoProyecto_ExcluyeFilasDeOtroProyecto()
    {
        var (sut, loader) = CreateSut();
        loader.Gastos.AddRange(new[]
        {
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" }, // MATCH
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 999, Importe = 500m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" }, // OTRO PROYECTO
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Sum","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(projectId: 100), null, CancellationToken.None);
        r.Resultado.Should().Be(100m);
    }

    [Fact]
    public async Task Evaluate_RecursoId_FiltraSoloFilasDelRecurso()
    {
        var (sut, loader) = CreateSut();
        loader.HorasBizneo.AddRange(new[]
        {
            new StagingBizneoHora { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ProjectId = 100, Horas = 8m, PayloadJson = "{}", RegistroIdExterno = "r1", Hash = "h" },
            new StagingBizneoHora { Fecha = new DateOnly(2026, 3, 5), UserId = 2, ProjectId = 100, Horas = 4m, PayloadJson = "{}", RegistroIdExterno = "r2", Hash = "h" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Sum","field":"Horas","source":{"type":"Source","entity":"HorasBizneo","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), recursoId: 1, CancellationToken.None);
        r.Resultado.Should().Be(8m);
    }

    // === SNAPSHOT / RESULTADO ===

    [Fact]
    public async Task Evaluate_DevuelveSnapshotDeFormula_Igual()
    {
        var json = """{"type":"Number","value":99}""";
        var (sut, _) = CreateSut();
        var concept = CreateConcept(json);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.FormulaSnapshotJson.Should().Be(json);
    }

    [Fact]
    public async Task Evaluate_DevuelveInputsJson_Parseable()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Number","value":1}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        var doc = JsonDocument.Parse(r.InputsJson);
        doc.RootElement.GetProperty("concepto").GetString().Should().Be("T");
        doc.RootElement.GetProperty("periodo").GetString().Should().Be("Marzo 2026");
    }

    private sealed class FakeDataLoader : ICalculationDataLoader
    {
        public List<StagingPayHawkGasto> Gastos { get; } = new();
        public List<StagingCeleroVisita> Visitas { get; } = new();
        public List<StagingBizneoHora> HorasBizneo { get; } = new();
        public List<StagingIntratimeFichaje> Fichajes { get; } = new();
        public List<Variable> Variables { get; } = new();

        public Task<CalculationContext> LoadAsync(Closure closure, CancellationToken ct)
        {
            var ctx = new CalculationContext
            {
                Gastos = Gastos,
                Visitas = Visitas,
                HorasBizneo = HorasBizneo,
                Fichajes = Fichajes,
                Variables = Variables
            };
            return Task.FromResult(ctx);
        }
    }
}
