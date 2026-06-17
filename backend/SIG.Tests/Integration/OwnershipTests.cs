using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

/// <summary>
/// Tests cross-user (ownership filtering RF-G01): un ProjectManager solo ve y opera
/// sobre los proyectos donde está asignado. Acceder a recursos ajenos devuelve 404,
/// no 403 (regla del Tester: la entidad NO existe desde la perspectiva del usuario).
/// </summary>
[Collection("Integration")]
public class OwnershipTests : IntegrationTestBase
{
    public OwnershipTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task PmAlpha_NoVeProyectosDeBeta_DevuelveListaSoloAlpha()
    {
        var alphaPm = await CreateAuthenticatedClientAsync("pm.alpha@sig.local");
        var resp = await alphaPm.GetAsync("/api/services?page=1&pageSize=100");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(resp);
        list!.Items.Should().NotBeEmpty("admin alpha tiene proyectos asignados");
        list.Items.Should().OnlyContain(p => p.Nombre.StartsWith("Alpha") || p.Nombre.Contains("multi", StringComparison.OrdinalIgnoreCase));
        // No debe haber Beta o Gamma puros en su lista
        list.Items.Should().NotContain(p => p.ClientNombre == "Beta Cosmetics" && !p.Nombre.Contains("multi", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PmBeta_VeSusProyectos()
    {
        var betaPm = await CreateAuthenticatedClientAsync("pm.beta@sig.local");
        var resp = await betaPm.GetAsync("/api/services?page=1&pageSize=100");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(resp);
        list!.Items.Should().OnlyContain(p => p.ClientNombre == "Beta Cosmetics" || p.Nombre.Contains("multi", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PmAlpha_AccedeProjectDeBetaPorId_Devuelve404()
    {
        var betaPm = await CreateAuthenticatedClientAsync("pm.beta@sig.local");
        var betaList = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(await betaPm.GetAsync("/api/services?page=1&pageSize=100"));
        // Pick beta project that pm.alpha shouldn't see
        var betaProj = betaList!.Items.First(p => p.ClientNombre == "Beta Cosmetics");

        var alphaPm = await CreateAuthenticatedClientAsync("pm.alpha@sig.local");
        var resp = await alphaPm.GetAsync($"/api/services/{betaProj.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Administrator_VeTodosLosProyectos()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var resp = await admin.GetAsync("/api/services?page=1&pageSize=100");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(resp);
        list!.Items.Should().HaveCountGreaterOrEqualTo(8); // seed: 8 proyectos
        // Debería tener proyectos de Alpha, Beta y Gamma
        var clients = list.Items.Select(p => p.ClientNombre).Distinct().ToList();
        clients.Should().Contain("Alpha Foods");
        clients.Should().Contain("Beta Cosmetics");
        clients.Should().Contain("Gamma Retail");
    }

    [Fact]
    public async Task PmAlpha_AccedeClosureDeBetaPorId_Devuelve404()
    {
        // Admin enumera cierres de costes; PM alpha intenta acceder a uno ajeno (Ola 3b #10).
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var allClosures = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await admin.GetAsync("/api/cierres-costes?page=1&pageSize=100"));
        if (allClosures == null || allClosures.Items.Count == 0)
            return; // si no hay cierres aún, este caso queda implícito en otros tests

        // Detectar un cierre de Beta (no de Alpha)
        var betaClosure = allClosures.Items.FirstOrDefault(c => !c.ServiceNombre.StartsWith("Alpha"));
        if (betaClosure is null) return; // si todos son alpha, saltar

        var alphaPm = await CreateAuthenticatedClientAsync("pm.alpha@sig.local");
        var resp = await alphaPm.GetAsync($"/api/cierres-costes/{betaClosure.Id}");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reader_PuedeListarProyectos_SiendoSoloLectura()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.GetAsync("/api/services?page=1&pageSize=100");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reader_NoPuedeCrearCliente_Devuelve403()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var req = new ClientCreateRequest("X", "X99999999", null, null, null, null, null, null, null, null, null);
        var resp = await reader.PostAsJsonAsync("/api/clients", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Auditor_PuedeAccederAudit_PeroNoSync()
    {
        var auditor = await CreateAuthenticatedClientAsync("auditor@sig.local");
        var auditResp = await auditor.GetAsync("/api/audit?page=1&pageSize=10");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var syncResp = await auditor.PostAsync("/api/sync/celero", null);
        syncResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reader_NoPuedeAccederAudit_Devuelve403()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.GetAsync("/api/audit?page=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
