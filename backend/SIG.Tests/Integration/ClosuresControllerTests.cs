using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

// Ola 3b (#10): el antiguo ClosuresController se dividió. Estos tests apuntan a api/cierres-costes.
[Collection("Integration")]
public class ClosuresControllerTests : IntegrationTestBase
{
    private const string Base = "/api/cierres-costes";

    public ClosuresControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetCierres_ConToken_DevuelvePagedResult()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync($"{Base}?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<CierreListItemDto>>(resp);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetCierreById_NoExistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync($"{Base}/999999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCierres_FiltradoPorEstadoAprobado_FuncionaParaAdmin()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync($"{Base}?estado=Aprobado&pageSize=100");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<CierreListItemDto>>(resp);
        body!.Items.Should().OnlyContain(c => c.Estado == EstadoClosure.Aprobado);
    }

    [Fact]
    public async Task RecalcularConcurrenciaSinIfMatch_Devuelve412()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await client.GetAsync($"{Base}?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Borrador || c.Estado == EstadoClosure.Rechazado);
        if (target is null) return;

        var req = JsonContent(new CierreRecalcRequest(null));
        var resp = await client.PostAsync($"{Base}/{target.Id}/recalcular", req);
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task AprobarConcurrenciaSinIfMatch_Devuelve412()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await client.GetAsync($"{Base}?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.EnAprobacion || c.Estado == EstadoClosure.Borrador);
        if (target is null) return;

        var resp = await client.PostAsync($"{Base}/{target.Id}/aprobar", JsonContent(new CierreApproveRequest(null)));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task RechazarSinMotivo_Devuelve400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await client.GetAsync($"{Base}?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.EnAprobacion);
        if (target is null) return;

        var req = new HttpRequestMessage(HttpMethod.Post, $"{Base}/{target.Id}/rechazar");
        req.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        req.Content = JsonContent(new CierreRejectRequest(""));
        var resp = await client.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrearCierreProyectoYPeriodInexistentes_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var req = new CierreCreateRequest(999999, 999999, null);
        var resp = await client.PostAsJsonAsync(Base, req);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCierre_DetailIncluyeLineasYAprovals()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await client.GetAsync($"{Base}?pageSize=5"));
        if (all is null || all.Items.Count == 0) return;
        var resp = await client.GetAsync($"{Base}/{all.Items.First().Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await ReadJsonAsync<CierreDetailDto>(resp);
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(all.Items.First().Id);
    }

    [Fact]
    public async Task RecalcularPeriodoCerrado_Devuelve409_TrasCerrarPeriodo()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await client.GetAsync($"{Base}?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Borrador);
        if (target is null) return;

        var detail = await ReadJsonAsync<CierreDetailDto>(await client.GetAsync($"{Base}/{target.Id}"));
        var rv = detail!.RowVersion;

        var periodResp = await client.PostAsync($"/api/periods/{detail.PeriodId}/cerrar", null);
        if (periodResp.StatusCode != HttpStatusCode.OK)
        {
            return;
        }

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{Base}/{target.Id}/recalcular");
            req.Headers.TryAddWithoutValidation("If-Match", rv.ToString());
            req.Content = JsonContent(new CierreRecalcRequest(null));
            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            await client.PostAsync($"/api/periods/{detail.PeriodId}/reabrir", null);
        }
    }
}
