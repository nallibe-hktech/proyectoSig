using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

/// <summary>
/// Ola 2 (#3a) — override manual de líneas e incentivos manuales (Ola 3b #10: sobre CierreCostes vía API).
/// Cubre: ajuste de importe con motivo/ImporteOriginal/EsManual, alta de incentivo manual,
/// recálculo del Total, guardas de estado, concurrencia If-Match y preservación de líneas manuales tras recálculo.
/// </summary>
[Collection("Integration")]
public class ClosureOverrideIncentivoTests : IntegrationTestBase
{
    private const string Base = "/api/cierres-costes";

    public ClosureOverrideIncentivoTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    /// <summary>Obtiene un cierre de costes en Borrador (el seed crea varios en el último período).</summary>
    private async Task<CierreDetailDto?> GetBorradorCierreAsync(HttpClient admin)
    {
        var all = await ReadJsonAsync<PagedResult<CierreListItemDto>>(await admin.GetAsync($"{Base}?pageSize=200"));
        var item = all!.Items.FirstOrDefault(c => c.Estado == EstadoClosure.Borrador);
        if (item is null) return null;
        return await ReadJsonAsync<CierreDetailDto>(await admin.GetAsync($"{Base}/{item.Id}"));
    }

    private async Task<int> AnyConceptIdAsync(HttpClient admin)
    {
        var list = await ReadJsonAsync<PagedResult<ConceptListItemDto>>(await admin.GetAsync("/api/concepts?page=1&pageSize=50&tipo=Pago"));
        return list!.Items.First().Id;
    }

    private static HttpRequestMessage WithIfMatch(HttpMethod method, string url, uint rowVersion, object body)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.TryAddWithoutValidation("If-Match", rowVersion.ToString());
        req.Content = JsonContent(body);
        return req;
    }

    [Fact]
    public async Task AddIncentivo_EnBorrador_AnadeLineaManualYRecalculaTotal()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await GetBorradorCierreAsync(admin);
        if (closure is null) return;

        var conceptId = await AnyConceptIdAsync(admin);

        var req = WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/lines/incentivo", closure.RowVersion,
            new CierreLineIncentivoRequest(conceptId, 500m, "Incentivo de prueba", null));
        var resp = await admin.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await ReadJsonAsync<CierreDetailDto>(resp);
        detail!.Lines.Should().Contain(l => l.EsManual && l.Importe == 500m && l.MotivoManual == "Incentivo de prueba");
        detail.Total.Should().BeGreaterOrEqualTo(500m, "el incentivo (Pago) cuenta en el Total del cierre de costes");
    }

    [Fact]
    public async Task OverrideLine_EnBorrador_GuardaImporteOriginalMotivoYMarcaManual()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await GetBorradorCierreAsync(admin);
        if (closure is null) return;

        if (closure.Lines.Length == 0)
        {
            var conceptId = await AnyConceptIdAsync(admin);
            var addResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/lines/incentivo", closure.RowVersion,
                new CierreLineIncentivoRequest(conceptId, 100m, "Línea base", null)));
            closure = await ReadJsonAsync<CierreDetailDto>(addResp);
        }

        var line = closure!.Lines[0];
        var importeOriginal = line.Importe;

        var resp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/lines/{line.Id}/override", closure.RowVersion,
            new CierreLineOverrideRequest(importeOriginal + 33m, "Ajuste manual pactado")));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await ReadJsonAsync<CierreDetailDto>(resp);
        var overridden = detail!.Lines.First(l => l.Id == line.Id);
        overridden.EsManual.Should().BeTrue();
        overridden.Importe.Should().Be(importeOriginal + 33m);
        overridden.ImporteOriginal.Should().Be(importeOriginal);
        overridden.MotivoManual.Should().Be("Ajuste manual pactado");
    }

    [Fact]
    public async Task OverrideLine_SinIfMatch_Devuelve412()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await GetBorradorCierreAsync(admin);
        if (closure is null) return;

        if (closure.Lines.Length == 0)
        {
            var conceptId = await AnyConceptIdAsync(admin);
            var addResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/lines/incentivo", closure.RowVersion,
                new CierreLineIncentivoRequest(conceptId, 100m, "Línea base", null)));
            closure = await ReadJsonAsync<CierreDetailDto>(addResp);
        }
        var lineId = closure!.Lines[0].Id;

        var resp = await admin.PostAsync($"{Base}/{closure.Id}/lines/{lineId}/override",
            JsonContent(new CierreLineOverrideRequest(1m, "x")));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task AddIncentivo_IfMatchIncorrecto_Devuelve412()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await GetBorradorCierreAsync(admin);
        if (closure is null) return;

        var conceptId = await AnyConceptIdAsync(admin);
        var resp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/lines/incentivo", closure.RowVersion + 999u,
            new CierreLineIncentivoRequest(conceptId, 10m, "x", null)));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    /// <summary>
    /// CLAVE Ola 2 (#3a): al RECALCULAR, las líneas con EsManual==true se PRESERVAN y cuentan en el Total.
    /// </summary>
    [Fact]
    public async Task Recalcular_PreservaLineasManuales()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await GetBorradorCierreAsync(admin);
        if (closure is null) return;

        var conceptId = await AnyConceptIdAsync(admin);

        var addResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/lines/incentivo", closure.RowVersion,
            new CierreLineIncentivoRequest(conceptId, 777m, "Incentivo a preservar", null)));
        addResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterAdd = await ReadJsonAsync<CierreDetailDto>(addResp);

        var recalcResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/recalcular", afterAdd!.RowVersion,
            new CierreRecalcRequest(null)));
        recalcResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterRecalc = await ReadJsonAsync<CierreDetailDto>(recalcResp);
        afterRecalc!.Lines.Should().Contain(l => l.EsManual && l.Importe == 777m && l.MotivoManual == "Incentivo a preservar",
            "las líneas manuales/incentivos no se borran en el recálculo (#3a)");
        afterRecalc.Total.Should().BeGreaterOrEqualTo(777m,
            "la línea manual (Pago) sigue contando en el Total tras el recálculo");
    }

    [Fact]
    public async Task AddIncentivo_ComoReader_Devuelve403()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await GetBorradorCierreAsync(admin);
        if (closure is null) return;
        var conceptId = await AnyConceptIdAsync(admin);

        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.SendAsync(WithIfMatch(HttpMethod.Post, $"{Base}/{closure.Id}/lines/incentivo", closure.RowVersion,
            new CierreLineIncentivoRequest(conceptId, 10m, "x", null)));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
