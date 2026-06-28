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

    // Ola 3b (#10): el motor ya no depende de Closure; recibe un CalculationTarget (servicio + período).
    private static CalculationTarget CreateClosure(int projectId = 100, DateOnly? desde = null, DateOnly? hasta = null) => new()
    {
        ServiceId = projectId,
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
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Importe = 100m, Categoria = "viaje", PayloadJson = "{}", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ServiceId = 100, Importe = 50m, Categoria = "comida", PayloadJson = "{}", Hash = "h2" },
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
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, VisitaIdExterno = "v1", PayloadJson = "{}", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, VisitaIdExterno = "v2", PayloadJson = "{}", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ServiceId = 100, VisitaIdExterno = "v3", PayloadJson = "{}", Hash = "h" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(3m);
    }

    [Fact]
    public async Task Evaluate_CountConFiltro_AplicaFiltroEq()
    {
        var (sut, loader) = CreateSut();
        // Colapso del modelo: el servicio del cierre es el filtro implícito; el subconjunto
        // se discrimina por UserId (antes se usaba ActionId, dimensión ya inexistente).
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 2, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v3", Hash = "h" },
        });
        var concept = CreateConcept("""
        {"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"UserId","op":"Eq","value":1}]}}
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
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ServiceId = 100, Importe = 30m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },
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
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ServiceId = 100, Importe = 30m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },
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
        // Cuenta(VisitasCelero del usuario 1) × 5 — subconjunto discriminado por UserId
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 6), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 7), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v3", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 8), UserId = 2, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v4", Hash = "h" },
        });
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"UserId","op":"Eq","value":1}]}},
         "right":{"type":"Number","value":5}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(15m); // 3 visitas del usuario 1 × 5
        r.SistemaOrigen.Should().Be("Celero");
    }

    [Fact]
    public async Task Evaluate_PagoPorHorasTarifa_AplicaCalculoCorrectamente()
    {
        // Suma(HorasBizneo.horas) × Variable(TarifaHora=18.5)
        var (sut, loader) = CreateSut();
        loader.HorasBizneo.AddRange(new[]
        {
            new StagingBizneoAbsence { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Horas = 8m, PayloadJson = "{}", RegistroIdExterno = "r1", Hash = "h" },
            new StagingBizneoAbsence { Fecha = new DateOnly(2026, 3, 6), UserId = 1, ServiceId = 100, Horas = 6m, PayloadJson = "{}", RegistroIdExterno = "r2", Hash = "h" },
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
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Importe = 200m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 2, ServiceId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },
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
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 2, 28), UserId = 1, ServiceId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" }, // FUERA
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 2, ServiceId = 100, Importe = 50m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },  // DENTRO
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 4, 1), UserId = 3, ServiceId = 100, Importe = 200m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g3", Hash = "h3" }, // FUERA
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
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Importe = 100m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" }, // MATCH
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 999, Importe = 500m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" }, // OTRO PROYECTO
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
            new StagingBizneoAbsence { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Horas = 8m, PayloadJson = "{}", RegistroIdExterno = "r1", Hash = "h" },
            new StagingBizneoAbsence { Fecha = new DateOnly(2026, 3, 5), UserId = 2, ServiceId = 100, Horas = 4m, PayloadJson = "{}", RegistroIdExterno = "r2", Hash = "h" },
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

    // === EXCEL: MODIFICADORES (FILTROS) ===

    [Fact]
    public async Task Evaluate_ModifierMin_AplicaSuelo()
    {
        // Cantidad mínima: si el resultado < 250 -> 250
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Modifier","kind":"Min","threshold":250,"inner":{"type":"Number","value":100}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(250m);
    }

    [Fact]
    public async Task Evaluate_ModifierMax_AplicaTecho()
    {
        // Cantidad máxima: si el resultado > 250 -> 250
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Modifier","kind":"Max","threshold":250,"inner":{"type":"Number","value":400}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(250m);
    }

    [Fact]
    public async Task Evaluate_ModifierFloorZero_PorDebajoDelUmbralDevuelveCero()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Modifier","kind":"FloorZero","threshold":300,"inner":{"type":"Number","value":275}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(0m);
    }

    [Fact]
    public async Task Evaluate_ModifierFranquicia_RestaPrimerosX()
    {
        // Franquicia: los primeros 300 km no contabilizan; 315 -> 15
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"Modifier","kind":"Franquicia","threshold":300,"inner":{"type":"Number","value":315}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(15m);
    }

    // === EXCEL: TRAMOS INCREMENTALES ===

    [Fact]
    public async Task Evaluate_Tramos_PrimeraHoraYSiguientes()
    {
        // 1ª unidad a 90, siguientes a 37; cantidad = 3 -> 90 + 37 + 37 = 164
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""
        {"type":"Tramos","cantidad":{"type":"Number","value":3},
         "tramos":[{"hasta":1,"precio":90},{"hasta":null,"precio":37}]}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(164m);
    }

    // === EXCEL: CONTEO DE DÍAS CON ACTIVIDAD ===

    [Fact]
    public async Task Evaluate_CountDistinctFecha_CuentaDiasUnicos()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v2", Hash = "h" }, // mismo día
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = "{}", VisitaIdExterno = "v3", Hash = "h" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Count","distinct":"Fecha","source":{"type":"Source","entity":"VisitasCelero","filters":[]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(2m); // 2 días distintos con actividad
    }

    // === EXCEL: SEGMENTACIÓN DE VISITA POR TIPO (desde PayloadJson) ===

    [Fact]
    public async Task Evaluate_FiltroTipoVisitaDesdePayload_SegmentaVisitas()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":2}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":2}""", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":1}""", VisitaIdExterno = "v3", Hash = "h" },
        });
        var concept = CreateConcept("""{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"TipoVisita","op":"Eq","value":2}]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(2m);
    }

    // === EXCEL: FEE SOBRE CONCEPTOS ===

    [Fact]
    public async Task Evaluate_ConceptRef_SumaImportesPreviosYAplicaFee()
    {
        // Fee 10% sobre la suma de los conceptos 10 y 11 (importes previos 200 y 300) -> 500 * 0.10 = 50
        var (sut, _) = CreateSut();
        var target = CreateClosure();
        target.ImportesPrevios[10] = 200m;
        target.ImportesPrevios[11] = 300m;
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"ConceptRef","conceptIds":[10,11]},
         "right":{"type":"Number","value":0.10}}
        """);
        var r = await sut.EvaluateAsync(concept, target, null, CancellationToken.None);
        r.Resultado.Should().Be(50m);
    }

    [Fact]
    public async Task Evaluate_ConceptRefVacio_SumaTodosLosPreviosExcluyendoSeMismo()
    {
        var (sut, _) = CreateSut();
        var target = CreateClosure();
        target.ImportesPrevios[10] = 100m;
        target.ImportesPrevios[11] = 250m;
        var concept = CreateConcept("""{"type":"ConceptRef","conceptIds":[]}""");
        var r = await sut.EvaluateAsync(concept, target, null, CancellationToken.None);
        r.Resultado.Should().Be(350m);
    }

    [Fact]
    public async Task Evaluate_ConceptRefSinPrevios_RegistraIncidencia()
    {
        var (sut, _) = CreateSut();
        var concept = CreateConcept("""{"type":"ConceptRef","conceptIds":[]}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(0m);
        r.Incidencias.Should().ContainSingle(i => i.Tipo == "SinConceptosPrevios");
    }

    // === EXCEL #1: idQuestion de Celero -> variable (valor resuelto desde la respuesta real) ===

    [Fact]
    public async Task Evaluate_VariableConIdQuestion_ResuelveDesdeRespuestaCelero()
    {
        var (sut, loader) = CreateSut();
        // Variable "ZonaBonus" ligada al idQuestion Q21; A=1.5, B=1.2, C=1.0.
        loader.Variables.Add(new Variable { Id = 7, Nombre = "ZonaBonus", QuestionIdExterno = "Q21",
            MapeoValoresJson = """[{"respuesta":"A","valor":1.5},{"respuesta":"B","valor":1.2},{"respuesta":"C","valor":1.0}]""" });
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"Q21":"A"}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"Q21":"A"}""", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ServiceId = 100, PayloadJson = """{"Q21":"B"}""", VisitaIdExterno = "v3", Hash = "h" },
        });
        // Respuesta dominante = "A" -> 1.5
        var concept = CreateConcept("""{"type":"Variable","variableId":7}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(1.5m);
    }

    [Fact]
    public async Task Evaluate_VariableConIdQuestion_SinRespuesta_UsaDefault()
    {
        var (sut, loader) = CreateSut();
        loader.Variables.Add(new Variable { Id = 8, Nombre = "TarifaHora", QuestionIdExterno = "T01",
            MapeoValoresJson = """[{"respuesta":"A","valor":30},{"respuesta":"Default","valor":25}]""" });
        // Sin visitas con respuesta a T01 -> cae al valor "Default".
        var concept = CreateConcept("""{"type":"Variable","variableId":8}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(25m);
    }

    [Fact]
    public async Task Evaluate_VisitasPorBonusDeZona_MultiplicaPorVariableIdQuestion()
    {
        var (sut, loader) = CreateSut();
        loader.Variables.Add(new Variable { Id = 9, Nombre = "ZonaBonus", QuestionIdExterno = "Q21",
            MapeoValoresJson = """[{"respuesta":"A","valor":1.5},{"respuesta":"B","valor":1.2}]""" });
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"Q21":"A"}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"Q21":"A"}""", VisitaIdExterno = "v2", Hash = "h" },
        });
        // 2 visitas * bonus zona A (1.5) = 3
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[]}},
         "right":{"type":"Variable","variableId":9}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(3m);
    }

    // === EXCEL #4: flags de excepción de la visita (estado/nocturnidad/nº visita) ===

    [Fact]
    public async Task Evaluate_FiltroEstadoFallida_SeparaVisitasFallidas()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"estado":"ok"}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"estado":"fallida"}""", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ServiceId = 100, PayloadJson = """{"estado":"fallida"}""", VisitaIdExterno = "v3", Hash = "h" },
        });
        // Excel: "visita fallida -> mismo coste": se cuentan las fallidas para tarificarlas igual.
        var concept = CreateConcept("""{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"Estado","op":"Eq","value":"fallida"}]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(2m);
    }

    [Fact]
    public async Task Evaluate_RecargoNocturnidad_FacturaVisitasNocturnasConIncremento50()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"nocturnidad":true}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"nocturnidad":false}""", VisitaIdExterno = "v2", Hash = "h" },
        });
        // 1 visita nocturna * tarifa 100 * (1 + 50%) = 150
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Pct",
         "left":{"type":"BinaryOp","op":"Mul",
            "left":{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"Nocturnidad","op":"Eq","value":true}]}},
            "right":{"type":"Number","value":100}},
         "right":{"type":"Number","value":50}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(150m);
    }

    [Fact]
    public async Task Evaluate_SegundaVisita_FacturaAlCincuentaPorCiento()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"numeroVisita":2}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"numeroVisita":1}""", VisitaIdExterno = "v2", Hash = "h" },
        });
        // 1 segunda visita * tarifa 100 * 0.5 = 50
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"NumeroVisita","op":"Gte","value":2}]}},
         "right":{"type":"Number","value":50}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(50m);
    }

    // === EXCEL tipos 5 y 6: Entidad-A × Entidad-B (Aggregate × Aggregate) ===

    [Fact]
    public async Task Evaluate_ConteoEntidadAxEntidadB_MultiplicaDosConteos()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":1}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":1}""", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":2}""", VisitaIdExterno = "v3", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 4), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":2}""", VisitaIdExterno = "v4", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, PayloadJson = """{"tipoVisita":2}""", VisitaIdExterno = "v5", Hash = "h" },
        });
        // Conteo tipo 1 (=2) × Conteo tipo 2 (=3) = 6
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"TipoVisita","op":"Eq","value":1}]}},
         "right":{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"TipoVisita","op":"Eq","value":2}]}}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(6m);
    }

    [Fact]
    public async Task Evaluate_SumaEntidadAxEntidadB_MultiplicaDosSumas()
    {
        var (sut, loader) = CreateSut();
        loader.HorasBizneo.AddRange(new[]
        {
            new StagingBizneoAbsence { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Horas = 2m, PayloadJson = "{}", RegistroIdExterno = "r1", Hash = "h" },
            new StagingBizneoAbsence { Fecha = new DateOnly(2026, 3, 6), UserId = 1, ServiceId = 100, Horas = 3m, PayloadJson = "{}", RegistroIdExterno = "r2", Hash = "h" },
        });
        loader.Gastos.AddRange(new[]
        {
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 5), UserId = 1, ServiceId = 100, Importe = 10m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g1", Hash = "h1" },
            new StagingPayHawkGasto { Fecha = new DateOnly(2026, 3, 10), UserId = 1, ServiceId = 100, Importe = 20m, PayloadJson = "{}", Categoria = "c", GastoIdExterno = "g2", Hash = "h2" },
        });
        // Suma horas (=5) × Suma importe (=30) = 150
        var concept = CreateConcept("""
        {"type":"BinaryOp","op":"Mul",
         "left":{"type":"Aggregate","op":"Sum","field":"Horas","source":{"type":"Source","entity":"HorasBizneo","filters":[]}},
         "right":{"type":"Aggregate","op":"Sum","field":"Importe","source":{"type":"Source","entity":"GastosPayHawk","filters":[]}}}
        """);
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(150m);
    }

    [Fact]
    public async Task Evaluate_FiltroIn_AceptaListaDeValores()
    {
        var (sut, loader) = CreateSut();
        loader.Visitas.AddRange(new[]
        {
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 1), UserId = 1, ServiceId = 100, PayloadJson = """{"zona":"A"}""", VisitaIdExterno = "v1", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 2), UserId = 1, ServiceId = 100, PayloadJson = """{"zona":"B"}""", VisitaIdExterno = "v2", Hash = "h" },
            new StagingCeleroVisita { Fecha = new DateOnly(2026, 3, 3), UserId = 1, ServiceId = 100, PayloadJson = """{"zona":"C"}""", VisitaIdExterno = "v3", Hash = "h" },
        });
        // Operador In sobre lista de zonas (antes el conversor no deserializaba el array y daba 0).
        var concept = CreateConcept("""{"type":"Aggregate","op":"Count","source":{"type":"Source","entity":"VisitasCelero","filters":[{"field":"Zona","op":"In","value":["A","C"]}]}}""");
        var r = await sut.EvaluateAsync(concept, CreateClosure(), null, CancellationToken.None);
        r.Resultado.Should().Be(2m);
    }

    private sealed class FakeDataLoader : ICalculationDataLoader
    {
        public List<StagingPayHawkGasto> Gastos { get; } = new();
        public List<StagingCeleroVisita> Visitas { get; } = new();
        public List<StagingBizneoAbsence> HorasBizneo { get; } = new();
        public List<StagingIntratimeFichaje> Fichajes { get; } = new();
        public List<Variable> Variables { get; } = new();

        public Task<CalculationContext> LoadAsync(CalculationTarget target, CancellationToken ct)
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
        public Task<List<RowAdapter>> LoadCrossServiceAsync(int userId, DateOnly desde, DateOnly hasta, string entity, string field, CancellationToken ct)
            => Task.FromResult(new List<RowAdapter>());
    }
}
