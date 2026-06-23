using System.Globalization;
using SIG.Application.Calculation;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Calculation;

/// <summary>
/// GOLDEN TEST — ejemplo IMPRESO en el Excel maestro `CierresIntegralesSIG`, hoja "Pagos - Facturación",
/// bloque "EJEMPLO DE LÓGICAS / PAGO MERCH COSMETIX" (cliente Optimising/Cosmética).
///
/// Modelo documentado (mismo bloque + "LÓGICAS - Pagos/Facturación COSMÉTICA"):
///   Pago      = Tarifa/hora PAGO (11,92 € bruto) × Horas pactadas por tipo de acción.
///   Facturación = Tarifa/hora FACTURA (55 €)     × Horas pactadas por tipo de acción.
///   Horas pactadas: IMPLANTACIÓN = 4 h · ACTUALIZACIÓN = 2 h · (ACTUALIZAR+IMPLANTAR) = 5 h.
///
/// Anclas (valores impresos en la hoja, no inventados):
///   PAGO  IMPLANTACION = 47,68 €  ·  ACTUALIZACION = 23,84 €  ·  COMBINADO(5h) = 59,60 €
///   FACT  IMPLANTACION = 220,00 € ·  ACTUALIZACION = 110,00 € ·  COMBINADO(5h) = 275,00 €
///
/// La tarifa/hora se modela como Variable (el catálogo "TIPO CONCEPTO" admite "tarifa hora" como
/// variable, incl. "idQuestion de Celero a una variable"); las horas pactadas por acción son el
/// parámetro de configuración (columna Pendientes_para_parametrizar del propio Excel).
/// </summary>
public class CosmeticaCatalogoGoldenTests
{
    private const decimal TarifaPagoHora = 11.92m;   // €/h bruto (impreso)
    private const decimal TarifaFacturaHora = 55m;   // €/h cliente (impreso: 220/4 = 110/2 = 275/5)

    private static (CalculationEngine engine, FakeLoader loader) CreateSut()
    {
        var loader = new FakeLoader();
        return (new CalculationEngine(new FormulaParser(), loader, new VariableResolver()), loader);
    }

    private static CalculationTarget Target() => new()
    {
        ServiceId = 100, PeriodId = 1,
        Period = new Period { Id = 1, Nombre = "Mayo 2026",
            FechaInicio = new DateOnly(2026, 5, 1), FechaFin = new DateOnly(2026, 5, 31), Estado = EstadoPeriodo.Abierto }
    };

    private static Concept Concept(string formulaJson, TipoConcepto tipo) => new()
    {
        Id = 1, Nombre = "Cosmética", Tipo = tipo, FechaDesde = new DateOnly(2020, 1, 1), FormulaJson = formulaJson
    };

    private static string Lit(decimal d) => d.ToString(CultureInfo.InvariantCulture);
    private static string N(decimal d) => "{\"type\":\"Number\",\"value\":" + Lit(d) + "}";
    // Tarifa/hora × Horas pactadas (la tarifa como Variable del catálogo; las horas como parámetro pactado).
    private static string TarifaPorHoras(int variableId, decimal horasPactadas) =>
        "{\"type\":\"BinaryOp\",\"op\":\"Mul\"," +
        "\"left\":{\"type\":\"Variable\",\"variableId\":" + variableId + "}," +
        "\"right\":" + N(horasPactadas) + "}";

    private static Variable Tarifa(int id, string nombre, decimal valor) => new()
    {
        Id = id, Nombre = nombre, QuestionIdExterno = string.Empty,
        MapeoValoresJson = "[{\"respuesta\":\"Default\",\"valor\":" + Lit(valor) + "}]"
    };

    [Theory]
    [InlineData(4, 47.68)]   // IMPLANTACION  : 11,92 × 4
    [InlineData(2, 23.84)]   // ACTUALIZACION : 11,92 × 2
    [InlineData(5, 59.60)]   // ACTUALIZAR+IMPLANTAR (5 h)
    public async Task Pago_TarifaHora1192_PorHorasPactadas_ReproduceImpreso(int horas, decimal esperado)
    {
        var (sut, loader) = CreateSut();
        loader.Variables.Add(Tarifa(1, "TarifaPagoCosmetica", TarifaPagoHora));

        var r = await sut.EvaluateAsync(
            Concept(TarifaPorHoras(1, horas), TipoConcepto.Pago), Target(), null, CancellationToken.None);

        r.Resultado.Should().Be(esperado);
    }

    [Theory]
    [InlineData(4, 220.00)]  // FACT IMPLANTACION  : 55 × 4
    [InlineData(2, 110.00)]  // FACT ACTUALIZACION : 55 × 2
    [InlineData(5, 275.00)]  // FACT COMBINADO (5 h)
    public async Task Facturacion_TarifaHora55_PorHorasPactadas_ReproduceImpreso(int horas, decimal esperado)
    {
        var (sut, loader) = CreateSut();
        loader.Variables.Add(Tarifa(2, "TarifaFacturaCosmetica", TarifaFacturaHora));

        var r = await sut.EvaluateAsync(
            Concept(TarifaPorHoras(2, horas), TipoConcepto.Factura), Target(), null, CancellationToken.None);

        r.Resultado.Should().Be(esperado);
    }

    /// <summary>
    /// Regla LITERAL Cosmética (hoja Pagos-Facturación): "Si se realiza la acción en menos tiempo se paga
    /// igualmente el 100% de lo pactado". El pago NO depende de las horas reales, sino de las pactadas:
    /// aunque el recurso tarde 3 h en una implantación de 4 h pactadas, el pago sigue siendo 47,68 €.
    /// </summary>
    [Fact]
    public async Task Pago_AccionEnMenosTiempo_SePagaCienPorCientoDeLoPactado()
    {
        var (sut, loader) = CreateSut();
        loader.Variables.Add(Tarifa(1, "TarifaPagoCosmetica", TarifaPagoHora));
        // El motor evalúa sobre las horas PACTADAS (4), no sobre las reales reportadas en Bizneo (3).
        loader.HorasBizneo.Add(new StagingBizneoAbsence
        {
            Fecha = new DateOnly(2026, 5, 6), UserId = 1, ServiceId = 100, Horas = 3m,
            PayloadJson = "{}", RegistroIdExterno = "r1", Hash = "h1"
        });

        var r = await sut.EvaluateAsync(
            Concept(TarifaPorHoras(1, 4), TipoConcepto.Pago), Target(), null, CancellationToken.None);

        r.Resultado.Should().Be(47.68m); // 100% de lo pactado (4 h), no las 3 h reales
    }

    private sealed class FakeLoader : ICalculationDataLoader
    {
        public List<Variable> Variables { get; } = new();
        public List<StagingBizneoAbsence> HorasBizneo { get; } = new();
        public Task<CalculationContext> LoadAsync(CalculationTarget target, CancellationToken ct)
            => Task.FromResult(new CalculationContext { Variables = Variables, HorasBizneo = HorasBizneo });
    }
}
