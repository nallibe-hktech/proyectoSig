using FluentAssertions;
using SIG.Application.Integrations;
using SIG.Application.Interfaces.Repositories;

namespace SIG.Tests.Unit.Integrations;

public class TravelPerkCecoResolverTests
{
    [Fact]
    public void NormalizarCeco_SinCostObject_DevuelveCecoInternoSig()
    {
        TravelPerkCecoResolver.NormalizarCeco(null).Should().Be("0423");
        TravelPerkCecoResolver.NormalizarCeco("  ").Should().Be("0423");
        TravelPerkCecoResolver.NormalizarCeco("0139_INPOST").Should().Be("0139_INPOST");
    }

    [Fact]
    public void Resolver_MatchExactoDelCodigoCompleto()
    {
        var mapa = new[] { new CecoServicio("0139_INPOST", 7), new CecoServicio("0102_COTY", 3) };
        TravelPerkCecoResolver.ResolverServiceId("0139_INPOST", mapa).Should().Be(7);
    }

    [Fact]
    public void Resolver_MatchPorPrefijoNumerico_CuandoElMaestroEsSoloElCodigo()
    {
        // Cost object "0139_INPOST"; maestro guarda el CECO como "0139"
        var mapa = new[] { new CecoServicio("0139", 7) };
        TravelPerkCecoResolver.ResolverServiceId("0139_INPOST", mapa).Should().Be(7);
    }

    [Fact]
    public void Resolver_Match4a6Digitos_CuandoElMaestroEmpiezaPorElPrefijo()
    {
        // Cost object 4 díg "0139_INPOST"; maestro 6 díg "013901" (única coincidencia)
        var mapa = new[] { new CecoServicio("013901", 7), new CecoServicio("010201", 3) };
        TravelPerkCecoResolver.ResolverServiceId("0139_INPOST", mapa).Should().Be(7);
    }

    [Fact]
    public void Resolver_PrefijoAmbiguo_DevuelveNull()
    {
        // Dos servicios distintos bajo el mismo prefijo 0139 → no se puede decidir
        var mapa = new[] { new CecoServicio("013901", 7), new CecoServicio("013902", 9) };
        TravelPerkCecoResolver.ResolverServiceId("0139_INPOST", mapa).Should().BeNull();
    }

    [Fact]
    public void Resolver_CecoNoEnMaestro_DevuelveNull()
    {
        var mapa = new[] { new CecoServicio("0102_COTY", 3) };
        TravelPerkCecoResolver.ResolverServiceId("9999_DESCONOCIDO", mapa).Should().BeNull();
    }

    [Fact]
    public void Resolver_SinCostObject_DevuelveNull()
    {
        TravelPerkCecoResolver.ResolverServiceId(null, new[] { new CecoServicio("0139", 7) }).Should().BeNull();
    }

    [Fact]
    public void EsCecoInternoSig_SinCostObject_EsTrue()
    {
        TravelPerkCecoResolver.EsCecoInternoSig(null, Array.Empty<string>()).Should().BeTrue();
        TravelPerkCecoResolver.EsCecoInternoSig("  ", new[] { "0316" }).Should().BeTrue();
    }

    [Fact]
    public void EsCecoInternoSig_CodigoEstructural4Digitos_EsTrue()
    {
        // CECO de departamento de SIG (4 díg, sin subcuenta), p.ej. "0316_COMERCIAL"
        TravelPerkCecoResolver.EsCecoInternoSig("0316_COMERCIAL", new[] { "0315", "0316" }).Should().BeTrue();
    }

    [Fact]
    public void EsCecoInternoSig_EstructuralConSubcuenta_MatchPorPrefijo()
    {
        // Admin temporal: el maestro guarda 6 díg (032201/032202); TravelPerk trae el prefijo "0322"
        TravelPerkCecoResolver.EsCecoInternoSig("0322_ADMIN", new[] { "032201", "032202" }).Should().BeTrue();
    }

    [Fact]
    public void EsCecoInternoSig_CecoDeCliente_EsFalse()
    {
        TravelPerkCecoResolver.EsCecoInternoSig("0102_COTY", new[] { "0315", "0316" }).Should().BeFalse();
        TravelPerkCecoResolver.EsCecoInternoSig("0102_COTY", Array.Empty<string>()).Should().BeFalse();
    }
}
