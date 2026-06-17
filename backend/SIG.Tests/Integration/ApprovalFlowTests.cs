using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

/// <summary>
/// Tests del flujo de aprobación extremo-a-extremo (Ola 3a #1 + Ola 3b #10).
/// Flujo nuevo: Grupo → FICO → Exportado, ahora sobre la raíz de COSTES (api/cierres-costes).
/// La pertenencia al grupo es rol global (Facilitador/Interlocutor/Gestor) + asignación al servicio.
/// </summary>
[Collection("Integration")]
public class ApprovalFlowTests : IntegrationTestBase
{
    private const string Base = "/api/cierres-costes";

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

    private static async Task<CierreListItemDto?> FindAsync(HttpClient client, Func<CierreListItemDto, bool> pred)
    {
        var all = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await client.GetAsync($"{Base}?pageSize=200"));
        return all!.Items.FirstOrDefault(pred);
    }

    [Fact]
    public async Task FlujoCompleto_GrupoApruebaYAvanzaAFico_LuegoFicoApruebaYQuedaAprobado()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        var detail = await ReadJsonAsync<CierreDetailDto>(await admin.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"{Base}/{target.Id}/aprobar", new CierreApproveRequest("OK Grupo"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterGrupo = await ReadJsonAsync<CierreDetailDto>(resp);
        afterGrupo!.PasoActual.Should().Be(ApprovalStep.Fico);
        afterGrupo.Estado.Should().Be(EstadoClosure.EnAprobacion);
        afterGrupo.Approvals.Should().Contain(a => a.Paso == ApprovalStep.Fico && a.Estado == EstadoApproval.Pendiente);

        var resp2 = await PostWithIfMatchAsync(admin, $"{Base}/{target.Id}/aprobar", new CierreApproveRequest("OK Fico"), afterGrupo.RowVersion);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterFico = await ReadJsonAsync<CierreDetailDto>(resp2);
        afterFico!.PasoActual.Should().Be(ApprovalStep.SystemExports);
        afterFico.Estado.Should().Be(EstadoClosure.Aprobado);
    }

    [Fact]
    public async Task FicoRechaza_DevuelveCierreAGrupoConEstadoRechazado()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.EnAprobacion && c.PasoActual == ApprovalStep.Fico);
        if (target is null) return;

        var detail = await ReadJsonAsync<CierreDetailDto>(await admin.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"{Base}/{target.Id}/rechazar", new CierreRejectRequest("Hay datos incorrectos"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newDetail = await ReadJsonAsync<CierreDetailDto>(resp);
        newDetail!.Estado.Should().Be(EstadoClosure.Rechazado);
        newDetail.PasoActual.Should().Be(ApprovalStep.Grupo);
        newDetail.Approvals.Should().Contain(a => a.Paso == ApprovalStep.Grupo && a.Estado == EstadoApproval.Pendiente);

        var historyResp = await admin.GetAsync($"{Base}/{target.Id}/historial");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await ReadJsonAsync<List<CierreHistoryDto>>(historyResp);
        history!.Should().Contain(h => h.Accion == "Rechazar" && h.Motivo == "Hay datos incorrectos"
                                       && h.PasoOrigen == ApprovalStep.Fico && h.PasoDestino == ApprovalStep.Grupo);
    }

    [Fact]
    public async Task GrupoRechaza_PermaneceEnGrupoConEstadoRechazado()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        var detail = await ReadJsonAsync<CierreDetailDto>(await admin.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"{Base}/{target.Id}/rechazar", new CierreRejectRequest("Corrige fórmula"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var newDetail = await ReadJsonAsync<CierreDetailDto>(resp);
        newDetail!.Estado.Should().Be(EstadoClosure.Rechazado);
        newDetail.PasoActual.Should().Be(ApprovalStep.Grupo);
    }

    [Fact]
    public async Task MiembroDeGrupoAsignado_PuedeAprobarPasoGrupo()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        var gestor = await CreateAuthenticatedClientAsync("pm.alpha@sig.local");
        var detail = await ReadJsonAsync<CierreDetailDto>(await gestor.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(gestor, $"{Base}/{target.Id}/aprobar", new CierreApproveRequest("Grupo OK"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await ReadJsonAsync<CierreDetailDto>(resp);
        after!.PasoActual.Should().Be(ApprovalStep.Fico);
    }

    [Fact]
    public async Task UsuarioSinRolDeGrupoNiAsignacion_NoPuedeAprobarPasoGrupo()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Borrador && c.PasoActual == ApprovalStep.Grupo);
        if (target is null) return;

        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var detail = await ReadJsonAsync<CierreDetailDto>(await admin.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(reader, $"{Base}/{target.Id}/aprobar", new CierreApproveRequest(null), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NoFico_NoPuedeAprobarPasoFico()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.EnAprobacion && c.PasoActual == ApprovalStep.Fico);
        if (target is null) return;

        var grupo = await CreateAuthenticatedClientAsync("pm.beta@sig.local");
        var detail = await ReadJsonAsync<CierreDetailDto>(await admin.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(grupo, $"{Base}/{target.Id}/aprobar", new CierreApproveRequest(null), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FicoPuedeAprobarPasoFico()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.EnAprobacion && c.PasoActual == ApprovalStep.Fico);
        if (target is null) return;

        var fico = await CreateAuthenticatedClientAsync("fico@sig.local");
        var detail = await ReadJsonAsync<CierreDetailDto>(await fico.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(fico, $"{Base}/{target.Id}/aprobar", new CierreApproveRequest("OK"), detail!.RowVersion);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var after = await ReadJsonAsync<CierreDetailDto>(resp);
        after!.Estado.Should().Be(EstadoClosure.Aprobado);
        after.PasoActual.Should().Be(ApprovalStep.SystemExports);
    }

    [Fact]
    public async Task ApproveYaAprobado_Devuelve409()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var target = await FindAsync(admin, c => c.Estado == EstadoClosure.Aprobado);
        if (target is null) return;

        var detail = await ReadJsonAsync<CierreDetailDto>(await admin.GetAsync($"{Base}/{target.Id}"));
        var resp = await PostWithIfMatchAsync(admin, $"{Base}/{target.Id}/aprobar", new CierreApproveRequest(null), detail!.RowVersion);
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

        var resp = await PostWithIfMatchAsync(admin, $"{Base}/{target.Id}/recalcular", new CierreRecalcRequest(null), 9999999);
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("concurrency_conflict");
    }
}
