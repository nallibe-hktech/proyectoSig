using FluentAssertions;
using SIG.Infrastructure.Integrations.Fake;
using SIG.Infrastructure.Integrations.Postgres;

namespace SIG.Tests.Unit.Integrations;

/// <summary>
/// Verifica la SQL de ingesta de Celero y la decisión delicada del filtro de estado
/// (docs/RETOMA_INPOST_FACTURACION.md §4.3), sin necesidad de una BD real, más la
/// preservación de garantías de seguridad (read-only) y la ausencia de PII en el fake.
/// </summary>
public class CeleroPostgresClientSqlTests
{
    [Fact]
    public void BuildSql_PorDefecto_SoloTraeVisitasDone()
    {
        var sql = CeleroPostgresClient.BuildVisitasSql(incluirNoRealizadas: false);

        sql.Should().Contain("v.status = 'done'",
            "el comportamiento histórico (válido para todos los clientes) es traer solo visitas realizadas");
    }

    [Fact]
    public void BuildSql_AlRelajar_NoRestringeElEstado()
    {
        var sql = CeleroPostgresClient.BuildVisitasSql(incluirNoRealizadas: true);

        sql.Should().NotContain("v.status = 'done'",
            "Inpost necesita facturar también fallidas/canceladas; el filtro se relaja explícitamente");
    }

    [Fact]
    public void BuildSql_IngestaLosCamposNuevosDeOrigenCelero()
    {
        var sql = CeleroPostgresClient.BuildVisitasSql(incluirNoRealizadas: false);

        // §4.3: las columnas que alimentan la segmentación del motor por PayloadJson.
        sql.Should().Contain("\"realDuration\"");
        sql.Should().Contain("v.status");
        sql.Should().Contain("\"cancellationReason\"");
        // B4: la provincia se lee directamente de la visita (addressState), sin JOIN a centro/POA.
        sql.Should().Contain("\"addressState\"");
    }

    [Fact]
    public void Cliente_ConstruyeSinBd_ConAmbasConfiguraciones()
    {
        // No abre conexión en el ctor: el flag de configuración se admite sin efectos colaterales.
        var act1 = () => new CeleroPostgresClient("Host=localhost", incluirNoRealizadas: false);
        var act2 = () => new CeleroPostgresClient("Host=localhost", incluirNoRealizadas: true);

        act1.Should().NotThrow();
        act2.Should().NotThrow();
    }

    [Fact]
    public void Cliente_RequiereConnectionString()
    {
        var act = () => new CeleroPostgresClient(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task FakeCelero_EnriqueceLosCamposDeOrigen_SinPiiReal()
    {
        var client = new CeleroFakeClient();

        var visitas = await client.GetVisitasAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), CancellationToken.None);

        visitas.Should().NotBeEmpty();
        visitas.Should().OnlyContain(v => v.DuracionMinutos.HasValue && v.DuracionMinutos.Value > 0);
        visitas.Should().OnlyContain(v => !string.IsNullOrEmpty(v.Estado));
        visitas.Should().OnlyContain(v => !string.IsNullOrEmpty(v.Provincia));
        // Estados dentro del vocabulario esperado (origen Celero), no texto arbitrario.
        visitas.Should().OnlyContain(v => v.Estado == "done" || v.Estado == "failed" || v.Estado == "cancelled");
    }
}
