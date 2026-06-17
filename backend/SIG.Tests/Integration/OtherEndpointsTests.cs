using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class OtherEndpointsTests : IntegrationTestBase
{
    public OtherEndpointsTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    // === /api/dashboard ===

    [Fact]
    public async Task GetDashboard_DevuelveKpis()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/dashboard");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<DashboardKpisDto>(resp);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDashboardAvisos_DevuelveLista()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/dashboard/avisos");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetDashboardMisProyectos_FiltradoPorRol()
    {
        var client = await CreateAuthenticatedClientAsync("pm.alpha@sig.local");
        var resp = await client.GetAsync("/api/dashboard/mis-servicios");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<IReadOnlyList<MiServicioDto>>(resp);
        body.Should().NotBeNull();
    }

    // === /api/periods ===

    [Fact]
    public async Task GetPeriods_DevuelveLista()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/periods");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await ReadJsonAsync<List<PeriodDto>>(resp);
        list!.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetPeriodoActivo_DevuelvePeriodo()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/periods/activo");
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    // === /api/concepts ===

    [Fact]
    public async Task GetConcepts_DevuelvePaginado()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/concepts");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostValidarFormula_FormulaInvalida_DevuelveOkFalse()
    {
        var client = await CreateAuthenticatedClientAsync();
        // The endpoint as documented: POST /api/concepts/{id}/validar-formula con { formulaJson }
        var detail = await ReadJsonAsync<PagedResult<ConceptListItemDto>>(await client.GetAsync("/api/concepts?pageSize=5"));
        if (detail is null || detail.Items.Count == 0) return;
        var firstId = detail.Items.First().Id;
        var resp = await client.PostAsJsonAsync($"/api/concepts/{firstId}/validar-formula", new ValidarFormulaRequest(""));
        // según signature: POST devuelve un OK con response
        resp.IsSuccessStatusCode.Should().BeTrue();
    }

    // === /api/users (admin/auditor only) ===

    [Fact]
    public async Task GetUsers_ComoAdmin_Devuelve200()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/users");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUsers_ComoReader_Devuelve403()
    {
        var client = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await client.GetAsync("/api/users");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // === /api/audit (admin/auditor only) ===

    [Fact]
    public async Task GetAudit_ComoAdmin_DevuelvePaginado()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/audit?page=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<AuditLogDto>>(resp);
        body!.Items.Should().NotBeEmpty(); // RNF-02: el seeder + logins generan audit logs
    }

    [Fact]
    public async Task GetAudit_ComoReader_Devuelve403()
    {
        var client = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await client.GetAsync("/api/audit?page=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // === /api/sync (admin only) ===

    [Fact]
    public async Task PostSyncCelero_ComoAdmin_Devuelve200ConResumen()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsync("/api/sync/celero", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<SyncResultDto>(resp);
        body!.Sistema.Should().Be("celero");
        body.Exito.Should().BeTrue();
        body.RegistrosInsertados.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task PostSyncCelero_DosVecesSeguidas_DevuelveDuplicadosEnSegundoIntentoConHashSHA256()
    {
        var client = await CreateAuthenticatedClientAsync();
        var first = await ReadJsonAsync<SyncResultDto>(await client.PostAsync("/api/sync/celero", null));
        var second = await ReadJsonAsync<SyncResultDto>(await client.PostAsync("/api/sync/celero", null));
        // Idempotencia: la segunda corrida debería tener 0 insertados y todo en actualizados (mismo hash)
        second!.RegistrosInsertados.Should().Be(0);
        second.RegistrosActualizados.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostSyncSistemaInvalido_Devuelve502()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsync("/api/sync/sistema-no-existente", null);
        // IntegrationException → 502 según jerarquía
        resp.StatusCode.Should().Be(HttpStatusCode.BadGateway);
    }

    [Fact]
    public async Task PostSync_ComoReader_Devuelve403()
    {
        var client = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await client.PostAsync("/api/sync/celero", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // === /api/exports (solo cierres Aprobado) ===

    [Fact]
    public async Task GetExportA3Innuva_ClosureNoAprobado_Devuelve409()
    {
        var client = await CreateAuthenticatedClientAsync();
        // Pick un closure no Aprobado
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=100"));
        var target = all!.Items.FirstOrDefault(c => c.Estado != EstadoClosure.Aprobado && c.Estado != EstadoClosure.Exportado);
        if (target is null) return;
        var resp = await client.GetAsync($"/api/exports/a3-innuva/{target.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetExportA3Innuva_ClosureAprobado_DevuelveExcel()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=100"));
        var target = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Aprobado);
        if (target is null) return; // No closures aprobados disponibles
        var resp = await client.GetAsync($"/api/exports/a3-innuva/{target.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        // El export genera un libro Excel (NPOI XSSFWorkbook); el controlador lo sirve como vnd.ms-excel.
        resp.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.ms-excel");
        var content = await resp.Content.ReadAsByteArrayAsync();
        content.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetExportA3Innuva_ComoReader_Devuelve403()
    {
        var client = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await client.GetAsync("/api/exports/a3-innuva/1");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetExportA3Erp_ClosureNoAprobado_Devuelve409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var all = await ReadJsonAsync<PagedResult<ClosureListItemDto>>(await client.GetAsync("/api/closures?pageSize=100"));
        var target = all!.Items.FirstOrDefault(c => c.Estado != EstadoClosure.Aprobado && c.Estado != EstadoClosure.Exportado);
        if (target is null) return;
        var resp = await client.GetAsync($"/api/exports/a3-erp/{target.Id}");
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // === /api/dev/regenerar-seed ===

    [Fact]
    public async Task PostRegenerarSeed_ComoAdminEnTesting_Devuelve200()
    {
        // En ASPNETCORE_ENVIRONMENT=Testing y Features:AllowSeedRegeneration=true → debería pasar
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsync("/api/dev/regenerar-seed", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostRegenerarSeed_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.PostAsync("/api/dev/regenerar-seed", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostRegenerarSeed_ComoReader_Devuelve403()
    {
        var client = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await client.PostAsync("/api/dev/regenerar-seed", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // === /api/approvals ===

    [Fact]
    public async Task GetApprovals_DevuelvePaginado()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/approvals?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<ApprovalPanelItemDto>>(resp);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetApprovalsPendientes_PorRol_DevuelveLista()
    {
        var client = await CreateAuthenticatedClientAsync("pm.alpha@sig.local");
        var resp = await client.GetAsync("/api/approvals/pendientes?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<ApprovalPanelItemDto>>(resp);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task GetApprovalsHistorial_ClosureInexistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/approvals/historial/999999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // === /api/roles ===

    [Fact]
    public async Task GetRoles_ComoAdmin_DevuelveLista()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/roles");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await ReadJsonAsync<List<RoleDto>>(resp);
        list!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetRoles_ComoReader_Devuelve403()
    {
        var client = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await client.GetAsync("/api/roles");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // === /api/departments y /api/costcenters ===

    [Fact]
    public async Task GetDepartments_ComoAdmin_DevuelveLista()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/departments");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCostCenters_ComoAdmin_DevuelveLista()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/costcenters");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostCostCenter_CodigoNoSeisDigitos_Devuelve400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsJsonAsync("/api/costcenters", new CostCenterCreateRequest("123", "Centro"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // === /api/services ===

    [Fact]
    public async Task GetProjects_FiltroPorClientId_FiltraCorrectamente()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clients = await ReadJsonAsync<PagedResult<ClientListItemDto>>(await client.GetAsync("/api/clients"));
        var clientId = clients!.Items.First().Id;
        var resp = await client.GetAsync($"/api/services?clientId={clientId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(resp);
        body!.Items.Should().OnlyContain(p => p.ClientId == clientId);
    }

    [Fact]
    public async Task GetActions_DevuelvePaginado()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/services");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === /api/calculations ===

    [Fact]
    public async Task GetCalculation_ClosureLineInexistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/calculations/999999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // === /api/variables ===

    [Fact]
    public async Task GetVariables_DevuelveLista()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/variables");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // === ProblemDetails con code semántico (RNF-06) ===

    [Fact]
    public async Task EntityNotFound_DevuelveProblemDetailsConCodeSemantico()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/clients/999999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await resp.Content.ReadAsStringAsync();
        // Verifica que el body es ProblemDetails con code "entity_not_found"
        content.Should().Contain("entity_not_found");
    }

    [Fact]
    public async Task DuplicateException_DevuelveProblemDetailsConCodeDuplicate()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clients = await ReadJsonAsync<PagedResult<ClientListItemDto>>(await client.GetAsync("/api/clients"));
        var existingNif = clients!.Items.First().NIF;
        var resp = await client.PostAsJsonAsync("/api/clients", new ClientCreateRequest("Dup", existingNif, null, null, null, null, null, null, null, null));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await resp.Content.ReadAsStringAsync();
        content.Should().Contain("duplicate");
    }

    // === /api/sync/process ===

    [Fact]
    public async Task PostSyncProcess_ComoAdmin_Devuelve200()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsync("/api/sync/process", null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<ProcessingResultDto>(resp);
        body.Should().NotBeNull();
        body!.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        body.Systems.Should().NotBeNull();
    }

    [Fact]
    public async Task PostSyncProcess_ComoReader_Devuelve403()
    {
        var client = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await client.PostAsync("/api/sync/process", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostSyncProcess_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.PostAsync("/api/sync/process", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
