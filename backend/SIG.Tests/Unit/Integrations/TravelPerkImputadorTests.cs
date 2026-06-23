using FluentAssertions;
using SIG.Application.DTOs;
using SIG.Application.Integrations;

namespace SIG.Tests.Unit.Integrations;

/// <summary>
/// Golden de imputación de TravelPerk. Las líneas reproducen la estructura del fichero real de muestra
/// (cuyo total verificado es 4.473,15 € sin IVA, 103 líneas, 1 sola sin CECO = "Subscription fee"),
/// agregadas por Service y con CECOs ANONIMIZADOS (no se persiste ningún identificador real de cliente).
/// </summary>
public class TravelPerkImputadorTests
{
    private static List<TravelPerkLineaDto> MuestraGolden() => new()
    {
        // CECO 0139_CLIENTE_A: Hotels + Premium
        Linea("Hotels", "0139_CLIENTE_A", 3094.45m),
        Linea("Premium Service", "0139_CLIENTE_A", 140.21m),
        // CECO 0102_CLIENTE_B: Flights + FlexiTravel
        Linea("Flights", "0102_CLIENTE_B", 807.46m),
        Linea("FlexiTravel Service", "0102_CLIENTE_B", 93.78m),
        // CECO 0138_CLIENTE_C: Cars + FlexiTravel Trips
        Linea("Cars", "0138_CLIENTE_C", 236.36m),
        Linea("FlexiTravel Trips Service", "0138_CLIENTE_C", 25.00m),
        // CECO 0216_CLIENTE_D: Trains + Refund (negativo, netea)
        Linea("Trains", "0216_CLIENTE_D", 65.55m),
        Linea("Refund for train", "0216_CLIENTE_D", -228.22m),
        // CECO 0103_CLIENTE_E: Other Service
        Linea("Other Service", "0103_CLIENTE_E", 139.56m),
        // Sin CECO: la suscripción mensual → gasto interno SIG (0423)
        Linea("Subscription fee", null, 99.00m),
    };

    private static TravelPerkLineaDto Linea(string service, string? ceco, decimal coste)
        => new("TRIP-X", service, ceco, coste, null, "EUR", null);

    [Fact]
    public void Imputar_TotalSinIVA_CuadraConElFicheroDeMuestra()
    {
        var r = TravelPerkImputador.Imputar(MuestraGolden());

        r.TotalSinIVA.Should().Be(4473.15m);
        r.TotalLineas.Should().Be(10);
        // La suma de los CECOs debe reconstruir el total
        r.PorCeco.Sum(c => c.CosteSinIVA).Should().Be(4473.15m);
    }

    [Fact]
    public void Imputar_SubscriptionFeeSinCeco_VaAlCecoInternoSig0423()
    {
        var r = TravelPerkImputador.Imputar(MuestraGolden());

        var sig = r.PorCeco.Single(c => c.EsGastoInternoSig);
        sig.Ceco.Should().Be("0423");
        sig.CosteSinIVA.Should().Be(99.00m);
        sig.NumLineas.Should().Be(1);
        r.LineasSinCeco.Should().Be(1);
        // Ninguna línea de cliente debe caer en el CECO interno
        r.PorCeco.Where(c => !c.EsGastoInternoSig).Should().OnlyContain(c => c.Ceco != "0423");
    }

    [Fact]
    public void Imputar_RefundForTrain_NeteaContraSuCeco()
    {
        var r = TravelPerkImputador.Imputar(MuestraGolden());

        // 0216_CLIENTE_D = Trains 65.55 + Refund -228.22 = -162.67
        var d = r.PorCeco.Single(c => c.Ceco == "0216_CLIENTE_D");
        d.CosteSinIVA.Should().Be(-162.67m);
        d.NumLineas.Should().Be(2);
    }

    [Fact]
    public void Imputar_LineaConCeco_SeImputaAlClienteDeEseCeco()
    {
        var r = TravelPerkImputador.Imputar(MuestraGolden());

        r.LineasConCeco.Should().Be(9);
        // 0139_CLIENTE_A = Hotels 3094.45 + Premium 140.21 = 3234.66 (Premium NO se excluye)
        r.PorCeco.Single(c => c.Ceco == "0139_CLIENTE_A").CosteSinIVA.Should().Be(3234.66m);
    }

    [Fact]
    public void Imputar_SinLineas_DevuelveResultadoVacio()
    {
        var r = TravelPerkImputador.Imputar(new List<TravelPerkLineaDto>());

        r.TotalSinIVA.Should().Be(0m);
        r.TotalLineas.Should().Be(0);
        r.PorCeco.Should().BeEmpty();
    }
}
