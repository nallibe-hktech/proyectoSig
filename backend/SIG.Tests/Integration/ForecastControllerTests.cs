using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class ForecastControllerTests : IntegrationTestBase
{
    private static readonly int AnioFuturo = DateTime.UtcNow.Year + 1;

    public ForecastControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private async Task<int> FirstServiceIdAsync(HttpClient client)
    {
        var list = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(await client.GetAsync("/api/services?page=1&pageSize=25"));
        return list!.Items.First().Id;
    }

    [Fact]
    public async Task GetForecast_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync($"/api/services/1/forecast?anio={AnioFuturo}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutForecast_ComoReader_Devuelve403()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local", "Demo#2026!");
        var admin = await CreateAuthenticatedClientAsync();
        var serviceId = await FirstServiceIdAsync(admin);
        var resp = await reader.PutAsJsonAsync($"/api/services/{serviceId}/forecast",
            new ForecastUpsertRequest(AnioFuturo, 6, 1000m, 200m, 5));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PutForecast_MesCerrado_Devuelve409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var serviceId = await FirstServiceIdAsync(client);
        var resp = await client.PutAsJsonAsync($"/api/services/{serviceId}/forecast",
            new ForecastUpsertRequest(2000, 1, 1000m, null, null)); // 2000 = mes cerrado
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CicloForecast_Upsert_List_Resumen()
    {
        var client = await CreateAuthenticatedClientAsync(); // admin
        var serviceId = await FirstServiceIdAsync(client);

        // upsert mes 6 (futuro => abierto)
        var put1 = await client.PutAsJsonAsync($"/api/services/{serviceId}/forecast",
            new ForecastUpsertRequest(AnioFuturo, 6, 5000m, 1500m, 8));
        put1.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await ReadJsonAsync<ForecastDto>(put1);
        created!.VentasPrevistas.Should().Be(5000m);

        // upsert mismo mes => actualiza (no duplica)
        var put2 = await client.PutAsJsonAsync($"/api/services/{serviceId}/forecast",
            new ForecastUpsertRequest(AnioFuturo, 6, 6000m, 1800m, 9));
        put2.StatusCode.Should().Be(HttpStatusCode.OK);

        // list del año: un único registro para mes 6 con el valor actualizado
        var list = await ReadJsonAsync<List<ForecastDto>>(await client.GetAsync($"/api/services/{serviceId}/forecast?anio={AnioFuturo}"));
        list!.Where(f => f.Mes == 6).Should().ContainSingle().Which.VentasPrevistas.Should().Be(6000m);

        // resumen del año: incluye el servicio con su total
        var resumen = await ReadJsonAsync<ForecastResumenDto>(await client.GetAsync($"/api/forecast/resumen?anio={AnioFuturo}"));
        resumen!.Anio.Should().Be(AnioFuturo);
        resumen.Filas.Should().NotBeEmpty();
        resumen.Filas.Sum(f => f.TotalVentas).Should().BeGreaterThanOrEqualTo(6000m);
    }
}
