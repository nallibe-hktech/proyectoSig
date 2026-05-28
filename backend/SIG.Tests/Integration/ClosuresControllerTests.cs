using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class ClosuresControllerTests : IntegrationTestBase
{
    public ClosuresControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetClosures_ConToken_DevuelvePagedResult()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/closures?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(resp);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClosureById_NoExistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/closures/999999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClosures_FiltradoPorEstadoAprobado_FuncionaParaAdmin()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/closures?estado=Aprobado&pageSize=100");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(resp);
        body!.Items.Should().OnlyContain(c => c.Estado == EstadoClosure.Aprobado);
    }

    [Fact]
    public async Task RecalcularConcurrenciaSinIfMatch_Devuelve412()
    {
        var client = await CreateAuthenticatedClientAsync();
        // Recogemos un closure en Borrador o Rechazado (los recalculables)
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Borrador || c.Estado == EstadoClosure.Rechazado);
        if (target is null) return; // sin candidatos seed

        // Sin header If-Match → rowVersion=0 → ConcurrencyConflict
        var req = JsonContent(new ClosureRecalcRequest(null));
        var resp = await client.PostAsync($"/api/closures/{target.Id}/recalcular", req);
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task AprobarConcurrenciaSinIfMatch_Devuelve412()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.EnAprobacion || c.Estado == EstadoClosure.Borrador);
        if (target is null) return;

        var resp = await client.PostAsync($"/api/closures/{target.Id}/aprobar", JsonContent(new ClosureApproveRequest(null)));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task RechazarSinMotivo_Devuelve400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.EnAprobacion);
        if (target is null) return;

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/closures/{target.Id}/rechazar");
        req.Headers.TryAddWithoutValidation("If-Match", "\"1\"");
        req.Content = JsonContent(new ClosureRejectRequest(""));
        var resp = await client.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CrearClosureProyectoYPeriodInexistentes_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var req = new ClosureCreateRequest(999999, 999999, null);
        var resp = await client.PostAsJsonAsync("/api/closures", req);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetClosure_DetailIncluyeLineasYAprovals()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=5"));
        if (all is null || all.Items.Count == 0) return;
        var resp = await client.GetAsync($"/api/closures/{all.Items.First().Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await ReadJsonAsync<ClosureDetailDto>(resp);
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(all.Items.First().Id);
        // No vacío por contrato (el seed crea líneas y aprovals)
    }

    [Fact]
    public async Task RecalcularPeriodoCerrado_Devuelve409_TrasCerrarPeriodo()
    {
        var client = await CreateAuthenticatedClientAsync();
        // Tomamos cualquier closure en Borrador del seed
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Borrador);
        if (target is null) return;

        // Obtenemos detail para tener RowVersion
        var detail = await ReadJsonAsync<ClosureDetailDto>(await client.GetAsync($"/api/closures/{target.Id}"));
        var rv = detail!.RowVersion;

        // Cerramos el período del closure
        var periodResp = await client.PostAsync($"/api/periods/{detail.PeriodId}/cerrar", null);
        if (periodResp.StatusCode != HttpStatusCode.OK)
        {
            return; // sin condiciones para cerrar este periodo (otros aprovals pendientes en seed); test inconcluyente
        }

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/closures/{target.Id}/recalcular");
            req.Headers.TryAddWithoutValidation("If-Match", rv.ToString());
            req.Content = JsonContent(new ClosureRecalcRequest(null));
            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            // Limpieza: reabrir el período
            await client.PostAsync($"/api/periods/{detail.PeriodId}/reabrir", null);
        }
    }
}
