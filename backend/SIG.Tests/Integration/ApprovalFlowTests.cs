using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;
using SIG.Infrastructure.Persistence;

namespace SIG.Tests.Integration;

/// <summary>
/// Tests del flujo de aprobación completo extremo-a-extremo (RF-D02..D06).
/// Verifica que el flujo secuencial PM → Backoffice → Fico → Direction → SystemExports
/// funciona correctamente con If-Match para concurrencia optimista (RNF-03).
/// </summary>
[Collection("Integration")]
public class ApprovalFlowTests : IntegrationTestBase
{
    public ApprovalFlowTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private async Task<HttpResponseMessage> PostWithIfMatchAsync<T>(HttpClient client, string url, T body, uint rowVersion)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.TryAddWithoutValidation("If-Match", $"\"{rowVersion}\"");
        req.Content = JsonContent(body);
        return await client.SendAsync(req);
    }

    [Fact]
    public async Task FlujoCompleto_PmApruebaUnaVezYAvanzaABackoffice()
    {
        var admin = await CreateAuthenticatedClientAsync();
        // Localizar un closure en Borrador con paso ProjectManager y reseteado por nosotros
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await admin.GetAsync("/api/closures?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.ProjectManager);
        if (target is null) return; // ningún closure adecuado en seed

        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var rv = detail!.RowVersion;
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest("OK PM"), rv);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newDetail = await ReadJsonAsync<ClosureDetailDto>(resp);
        newDetail!.PasoActual.Should().Be(ApprovalStep.Backoffice);
        newDetail.Estado.Should().Be(EstadoClosure.EnAprobacion);
        newDetail.Approvals.Should().Contain(a => a.Paso == ApprovalStep.Backoffice && a.Estado == EstadoApproval.Pendiente);
    }

    [Fact]
    public async Task RechazarDevuelveClosureAEstadoRechazadoYPasoAnterior()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await admin.GetAsync("/api/closures?pageSize=200"));
        // Buscar un closure en estado EnAprobacion con paso > ProjectManager
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.EnAprobacion && (int)c.PasoActual > (int)ApprovalStep.ProjectManager);
        if (target is null) return;

        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var pasoOriginal = detail!.PasoActual;
        var rv = detail.RowVersion;
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/rechazar", new ClosureRejectRequest("Hay datos incorrectos"), rv);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newDetail = await ReadJsonAsync<ClosureDetailDto>(resp);
        newDetail!.Estado.Should().Be(EstadoClosure.Rechazado);
        ((int)newDetail.PasoActual).Should().BeLessThan((int)pasoOriginal);

        // En el historial debe quedar la decisión
        var historyResp = await admin.GetAsync($"/api/approvals/historial/{target.Id}");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await ReadJsonAsync<List<ApprovalHistoryDto>>(historyResp);
        history!.Should().Contain(h => h.Accion == "Rechazar" && h.Motivo == "Hay datos incorrectos");
    }

    [Fact]
    public async Task ApproveYaAprobado_Devuelve409()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await admin.GetAsync("/api/closures?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Aprobado);
        if (target is null) return;

        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest(null), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("invalid_approval_transition");
    }

    [Fact]
    public async Task RecalcularConcurrencyConflict_RowVersionStale_Devuelve412()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await admin.GetAsync("/api/closures?pageSize=200"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Borrador || c.Estado == EstadoClosure.Rechazado);
        if (target is null) return;

        // RowVersion stale (un valor obviamente distinto)
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/recalcular", new ClosureRecalcRequest(null), 9999999);
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("concurrency_conflict");
    }
}
