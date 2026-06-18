using System.Net;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class ReportsControllerTests : IntegrationTestBase
{
    public ReportsControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task Resultado_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/reports/resultado?anio=2026");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Resultado_ConToken_DevuelveEstructura()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/reports/resultado?anio=2026");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<ReporteResultadoDto>(resp);
        body.Should().NotBeNull();
        body!.Anio.Should().Be(2026);
        body.Filas.Should().NotBeNull();
    }

    [Fact]
    public async Task PrevisionVsReal_ConToken_DevuelveEstructura()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/reports/prevision-vs-real?anio=2026");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PrevisionRealDto>(resp);
        body.Should().NotBeNull();
        body!.Anio.Should().Be(2026);
        body.Filas.Should().NotBeNull();
    }

    [Fact]
    public async Task Resultado_FiltroDepartamentoInexistente_DevuelveVacio()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/reports/resultado?anio=2026&departmentId=999999");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<ReporteResultadoDto>(resp);
        body!.Filas.Should().BeEmpty();
    }
}
