using System.Net;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class A3ErpEndpointsTests : IntegrationTestBase
{
    public A3ErpEndpointsTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetStatus_DevuelveEstadoEnModoTest_SinConfig()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/a3-erp/status");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<A3ErpStatusDto>(resp);
        body.Should().NotBeNull();
        // Sin Integrations:A3Erp válido en tests → no conectado, modo Test (degradación limpia).
        body!.Connected.Should().BeFalse();
        body.Modo.Should().Be("Test");
        body.Mensaje.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PostSync_DevuelveNotImplemented_StubPendienteDeSpec()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsync("/api/a3-erp/sync", null);

        resp.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task GetStatus_SinAutenticar_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/a3-erp/status");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
