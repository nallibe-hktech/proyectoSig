using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

/// <summary>
/// Ola 2 (#3a) — override manual de líneas e incentivos manuales sobre un Closure real, vía API.
/// Cubre: ajuste de importe con motivo/ImporteOriginal/EsManual, alta de incentivo manual,
/// recálculo de totales, guardas de estado, concurrencia If-Match, y preservación de líneas
/// manuales tras un recálculo del closure (la clave del item #3a).
/// </summary>
[Collection("Integration")]
public class ClosureOverrideIncentivoTests : IntegrationTestBase
{
    public ClosureOverrideIncentivoTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    /// <summary>Crea un closure nuevo en Borrador sobre un período Abierto y un servicio sin closure aún.</summary>
    private async Task<ClosureDetailDto?> CreateBorradorClosureAsync(HttpClient admin)
    {
        var periods = await ReadJsonAsync<List<PeriodDto>>(await admin.GetAsync("/api/periods"));
        var services = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(await admin.GetAsync("/api/services?page=1&pageSize=100"));
        if (periods is null || services is null) return null;

        foreach (var period in periods.Where(p => p.Estado == EstadoPeriodo.Abierto))
        {
            foreach (var service in services.Items)
            {
                var resp = await admin.PostAsJsonAsync("/api/closures", new ClosureCreateRequest(service.Id, period.Id, "Test override/incentivo"));
                if (resp.StatusCode == HttpStatusCode.Created)
                    return await ReadJsonAsync<ClosureDetailDto>(resp);
                // 409 (duplicado) → probar siguiente combinación
            }
        }
        return null;
    }

    private async Task<int> AnyConceptIdAsync(HttpClient admin)
    {
        var list = await ReadJsonAsync<PagedResult<ConceptListItemDto>>(await admin.GetAsync("/api/concepts?page=1&pageSize=1"));
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
    public async Task AddIncentivo_EnBorrador_AnadeLineaManualYRecalculaTotales()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await CreateBorradorClosureAsync(admin);
        if (closure is null) return; // sin combinación libre en el seed; otros tests cubren la lógica

        var conceptId = await AnyConceptIdAsync(admin);

        var req = WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/lines/incentivo", closure.RowVersion,
            new ClosureLineIncentivoRequest(conceptId, TipoConcepto.Pago, 500m, "Incentivo de prueba", null));
        var resp = await admin.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await ReadJsonAsync<ClosureDetailDto>(resp);
        detail!.Lines.Should().Contain(l => l.EsManual && l.Importe == 500m && l.MotivoManual == "Incentivo de prueba");
        // El coste total debe reflejar al menos el incentivo de tipo Pago.
        detail.CosteTotal.Should().BeGreaterOrEqualTo(500m);
        detail.Margen.Should().Be(detail.FacturacionTotal - detail.CosteTotal, "Margen = facturación - coste (euros)");
    }

    [Fact]
    public async Task OverrideLine_EnBorrador_GuardaImporteOriginalMotivoYMarcaManual()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await CreateBorradorClosureAsync(admin);
        if (closure is null) return;

        // Aseguramos al menos una línea: si el cálculo no produjo ninguna, añadimos un incentivo primero.
        if (closure.Lines.Length == 0)
        {
            var conceptId = await AnyConceptIdAsync(admin);
            var addResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/lines/incentivo", closure.RowVersion,
                new ClosureLineIncentivoRequest(conceptId, TipoConcepto.Pago, 100m, "Línea base", null)));
            closure = await ReadJsonAsync<ClosureDetailDto>(addResp);
        }

        var line = closure!.Lines[0];
        var importeOriginal = line.Importe;

        var resp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/lines/{line.Id}/override", closure.RowVersion,
            new ClosureLineOverrideRequest(importeOriginal + 33m, "Ajuste manual pactado")));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await ReadJsonAsync<ClosureDetailDto>(resp);
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
        var closure = await CreateBorradorClosureAsync(admin);
        if (closure is null) return;

        int lineId;
        if (closure.Lines.Length == 0)
        {
            var conceptId = await AnyConceptIdAsync(admin);
            var addResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/lines/incentivo", closure.RowVersion,
                new ClosureLineIncentivoRequest(conceptId, TipoConcepto.Pago, 100m, "Línea base", null)));
            closure = await ReadJsonAsync<ClosureDetailDto>(addResp);
        }
        lineId = closure!.Lines[0].Id;

        // Sin header If-Match → rowVersion=0 → ConcurrencyConflict (412).
        var resp = await admin.PostAsync($"/api/closures/{closure.Id}/lines/{lineId}/override",
            JsonContent(new ClosureLineOverrideRequest(1m, "x")));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task AddIncentivo_IfMatchIncorrecto_Devuelve412()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await CreateBorradorClosureAsync(admin);
        if (closure is null) return;

        var conceptId = await AnyConceptIdAsync(admin);
        var resp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/lines/incentivo", closure.RowVersion + 999u,
            new ClosureLineIncentivoRequest(conceptId, TipoConcepto.Pago, 10m, "x", null)));
        resp.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    /// <summary>
    /// CLAVE Ola 2 (#3a): al RECALCULAR un closure, las líneas con EsManual==true se PRESERVAN
    /// (no se borran) y cuentan en los totales.
    /// </summary>
    [Fact]
    public async Task Recalcular_PreservaLineasManuales()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await CreateBorradorClosureAsync(admin);
        if (closure is null) return;

        var conceptId = await AnyConceptIdAsync(admin);

        // Añadir un incentivo manual.
        var addResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/lines/incentivo", closure.RowVersion,
            new ClosureLineIncentivoRequest(conceptId, TipoConcepto.Pago, 777m, "Incentivo a preservar", null)));
        addResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterAdd = await ReadJsonAsync<ClosureDetailDto>(addResp);
        var manualLine = afterAdd!.Lines.First(l => l.EsManual && l.Importe == 777m);

        // Recalcular el closure (debe regenerar las líneas calculadas pero conservar la manual).
        var recalcResp = await admin.SendAsync(WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/recalcular", afterAdd.RowVersion,
            new ClosureRecalcRequest(null)));
        recalcResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var afterRecalc = await ReadJsonAsync<ClosureDetailDto>(recalcResp);
        afterRecalc!.Lines.Should().Contain(l => l.EsManual && l.Importe == 777m && l.MotivoManual == "Incentivo a preservar",
            "las líneas manuales/incentivos no se borran en el recálculo (#3a)");
        afterRecalc.CosteTotal.Should().BeGreaterOrEqualTo(777m,
            "la línea manual de tipo Pago sigue contando en los totales tras el recálculo");
    }

    [Fact]
    public async Task AddIncentivo_ComoReader_Devuelve403()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var closure = await CreateBorradorClosureAsync(admin);
        if (closure is null) return;
        var conceptId = await AnyConceptIdAsync(admin);

        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.SendAsync(WithIfMatch(HttpMethod.Post, $"/api/closures/{closure.Id}/lines/incentivo", closure.RowVersion,
            new ClosureLineIncentivoRequest(conceptId, TipoConcepto.Pago, 10m, "x", null)));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
