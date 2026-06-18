using System.Net;
using System.Net.Http.Json;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

/// <summary>
/// Ola 2 (#9) — fechas de pago del período (DiaPago 30/15/9) en los endpoints reales.
/// </summary>
[Collection("Integration")]
public class PeriodDiaPagoTests : IntegrationTestBase
{
    public PeriodDiaPagoTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private static string UniqueName() => $"Periodo Test {Guid.NewGuid():N}".Substring(0, 40);

    [Theory]
    [InlineData(30)]
    [InlineData(15)]
    [InlineData(9)]
    public async Task CreatePeriod_DiaPagoValido_DevuelveCreatedYPersisteDiaPago(int diaPago)
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var req = new PeriodCreateRequest(UniqueName(), new DateOnly(2027, 1, 1), new DateOnly(2027, 1, 31), diaPago);

        var resp = await admin.PostAsJsonAsync("/api/periods", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await ReadJsonAsync<PeriodDto>(resp);
        dto.Should().NotBeNull();
        dto!.DiaPago.Should().Be(diaPago);
        dto.Estado.Should().Be(EstadoPeriodo.Abierto);

        // Verificación de persistencia: el GET por id devuelve el mismo DiaPago.
        var getResp = await admin.GetAsync($"/api/periods/{dto.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var got = await ReadJsonAsync<PeriodDto>(getResp);
        got!.DiaPago.Should().Be(diaPago);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(31)]
    [InlineData(10)]
    public async Task CreatePeriod_DiaPagoInvalido_Devuelve400(int diaPago)
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var req = new PeriodCreateRequest(UniqueName(), new DateOnly(2027, 2, 1), new DateOnly(2027, 2, 28), diaPago);

        var resp = await admin.PostAsJsonAsync("/api/periods", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdatePeriod_CambiaDiaPago_PersisteNuevoValor()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var createResp = await admin.PostAsJsonAsync("/api/periods",
            new PeriodCreateRequest(UniqueName(), new DateOnly(2027, 3, 1), new DateOnly(2027, 3, 31), 30));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<PeriodDto>(createResp);

        var updResp = await admin.PutAsJsonAsync($"/api/periods/{created!.Id}",
            new PeriodUpdateRequest(created.Nombre, created.FechaInicio, created.FechaFin, 15));
        updResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadJsonAsync<PeriodDto>(updResp);
        updated!.DiaPago.Should().Be(15);
    }

    [Fact]
    public async Task UpdatePeriod_DiaPagoInvalido_Devuelve400()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var createResp = await admin.PostAsJsonAsync("/api/periods",
            new PeriodCreateRequest(UniqueName(), new DateOnly(2027, 4, 1), new DateOnly(2027, 4, 30), 9));
        var created = await ReadJsonAsync<PeriodDto>(createResp);

        var updResp = await admin.PutAsJsonAsync($"/api/periods/{created!.Id}",
            new PeriodUpdateRequest(created.Nombre, created.FechaInicio, created.FechaFin, 20));
        updResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListPeriods_IncluyeDiaPagoEnLosDtos()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var resp = await admin.GetAsync("/api/periods");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var periods = await ReadJsonAsync<List<PeriodDto>>(resp);
        periods.Should().NotBeNullOrEmpty();
        // Los períodos del seed tienen DiaPago = 30 (valor válido del conjunto 30/15/9).
        periods!.Should().OnlyContain(p => p.DiaPago == 30 || p.DiaPago == 15 || p.DiaPago == 9);
    }
}
