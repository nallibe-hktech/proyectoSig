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
/// Tests del flujo de aprobación extremo-a-extremo (Ola 3a #1, enmienda RF-D02..D06).
/// Flujo nuevo: Grupo → FICO → Exportado. La pertenencia al grupo es rol global
/// (Facilitador/Interlocutor/Gestor) + asignación al servicio (ServiceUser).
/// Verifica transiciones, autorización por paso y concurrencia optimista (RNF-03).
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

    private static async Task<ClosureListItemDto?> FindAsync(HttpClient client, Func<ClosureListItemDto, bool> pred)
    {
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=200"));
        return all!.Items.FirstOrDefault(pred);
    }

    [Fact]
    public async Task FlujoCompleto_GrupoApruebaYAvanzaAFico_LuegoFicoApruebaYQuedaAprobado()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        // 1) Grupo aprueba → avanza a FICO, EnAprobacion.
        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest("OK Grupo"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterGrupo = await ReadJsonAsync<ClosureDetailDto>(resp);
        afterGrupo!.PasoActual.Should().Be(ApprovalStep.Fico);
        afterGrupo.Estado.Should().Be(EstadoClosure.EnAprobacion);
        afterGrupo.Approvals.Should().Contain(a => a.Paso == ApprovalStep.Fico && a.Estado == EstadoApproval.Pendiente);

        // 2) FICO aprueba → estado terminal Aprobado, paso SystemExports.
        var resp2 = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest("OK Fico"), afterGrupo.RowVersion);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterFico = await ReadJsonAsync<ClosureDetailDto>(resp2);
        afterFico!.PasoActual.Should().Be(ApprovalStep.SystemExports);
        afterFico.Estado.Should().Be(EstadoClosure.Aprobado);
    }

    [Fact]
    public async Task FicoRechaza_DevuelveClosureAGrupoConEstadoRechazado()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.EnAprobacion && c.PasoActual == ApprovalStep.Fico);
        if (target is null) return;

        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/rechazar", new ClosureRejectRequest("Hay datos incorrectos"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newDetail = await ReadJsonAsync<ClosureDetailDto>(resp);
        newDetail!.Estado.Should().Be(EstadoClosure.Rechazado);
        newDetail.PasoActual.Should().Be(ApprovalStep.Grupo);
        newDetail.Approvals.Should().Contain(a => a.Paso == ApprovalStep.Grupo && a.Estado == EstadoApproval.Pendiente);

        // En el historial debe quedar la decisión Fico → Grupo.
        var historyResp = await admin.GetAsync($"/api/approvals/historial/{target.Id}");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await ReadJsonAsync<List<ApprovalHistoryDto>>(historyResp);
        history!.Should().Contain(h => h.Accion == "Rechazar" && h.Motivo == "Hay datos incorrectos"
                                       && h.PasoOrigen == ApprovalStep.Fico && h.PasoDestino == ApprovalStep.Grupo);
    }

    [Fact]
    public async Task GrupoRechaza_PermaneceEnGrupoConEstadoRechazado()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/rechazar", new ClosureRejectRequest("Corrige fórmula"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newDetail = await ReadJsonAsync<ClosureDetailDto>(resp);
        newDetail!.Estado.Should().Be(EstadoClosure.Rechazado);
        newDetail.PasoActual.Should().Be(ApprovalStep.Grupo);
    }

    [Fact]
    public async Task MiembroDeGrupoAsignado_PuedeAprobarPasoGrupo()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        // pm.alpha es Gestor y está asignado a todos los servicios del seed.
        var gestor = await CreateAuthenticatedClientAsync("pm.alpha@sig.local");
        var detail = await ReadJsonAsync<ClosureDetailDto>(await gestor.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(gestor, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest("Grupo OK"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await ReadJsonAsync<ClosureDetailDto>(resp);
        after!.PasoActual.Should().Be(ApprovalStep.Fico);
    }

    [Fact]
    public async Task UsuarioSinRolDeGrupoNiAsignacion_NoPuedeAprobarPasoGrupo()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        // reader@sig.local no tiene rol de grupo ni Fico → el controlador lo rechaza (403/Forbidden).
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(reader, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest(null), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NoFico_NoPuedeAprobarPasoFico()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.EnAprobacion && c.PasoActual == ApprovalStep.Fico);
        if (target is null) return;

        // pm.beta es Facilitador (miembro de grupo) pero NO Fico: el refuerzo de servicio
        // en ApproveAsync devuelve 403 (NotOwnerException) para el paso Fico.
        var grupo = await CreateAuthenticatedClientAsync("pm.beta@sig.local");
        var detail = await ReadJsonAsync<ClosureDetailDto>(await admin.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(grupo, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest(null), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FicoPuedeAprobarPasoFico()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.EnAprobacion && c.PasoActual == ApprovalStep.Fico);
        if (target is null) return;

        var fico = await CreateAuthenticatedClientAsync("fico@sig.local");
        var detail = await ReadJsonAsync<ClosureDetailDto>(await fico.GetAsync($"/api/closures/{target.Id}"));
        var resp = await PostWithIfMatchAsync(fico, $"/api/closures/{target.Id}/aprobar", new ClosureApproveRequest("OK"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await ReadJsonAsync<ClosureDetailDto>(resp);
        after!.Estado.Should().Be(EstadoClosure.Aprobado);
        after.PasoActual.Should().Be(ApprovalStep.SystemExports);
    }

    [Fact]
    public async Task ApproveYaAprobado_Devuelve409()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Aprobado);
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
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador || c.Estado == EstadoClosure.Rechazado);
        if (target is null) return;

        // RowVersion stale (un valor obviamente distinto)
        var resp = await PostWithIfMatchAsync(admin, $"/api/closures/{target.Id}/recalcular", new ClosureRecalcRequest(null), 9999999);
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("concurrency_conflict");
    }
}
