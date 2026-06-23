using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class ConfigPresupuestoControllerTests : IntegrationTestBase
{
    public ConfigPresupuestoControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private async Task<int> FirstServiceIdAsync(HttpClient client)
    {
        var list = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(await client.GetAsync("/api/services?page=1&pageSize=1"));
        return list!.Items.First().Id;
    }

    [Fact]
    public async Task Get_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/services/1/config-presupuesto");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_ConToken_DevuelveConfig()
    {
        var client = await CreateAuthenticatedClientAsync();
        var serviceId = await FirstServiceIdAsync(client);
        var resp = await client.GetAsync($"/api/services/{serviceId}/config-presupuesto");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cfg = await ReadJsonAsync<ConfigPresupuestoDto>(resp);
        cfg!.ServiceId.Should().Be(serviceId);
        cfg.ServiceNombre.Should().NotBeNullOrEmpty();
        cfg.TotalRestante.Should().Be(cfg.TotalPresupuesto - cfg.TotalConsumido);
    }

    [Fact]
    public async Task Get_ServicioInexistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/services/999999/config-presupuesto");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostPartida_ComoReader_Devuelve403()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local", "Demo#2026!");
        var admin = await CreateAuthenticatedClientAsync();
        var serviceId = await FirstServiceIdAsync(admin);
        var resp = await reader.PostAsJsonAsync($"/api/services/{serviceId}/config-presupuesto/partidas",
            new PartidaPresupuestoCreateRequest("No permitida", TipoPartidaPresupuesto.Anual, 2026, 1000m, 0m, null));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetMargenObjetivo_ActualizaYSeRefleja()
    {
        var client = await CreateAuthenticatedClientAsync();
        var serviceId = await FirstServiceIdAsync(client);

        var resp = await client.PutAsJsonAsync($"/api/services/{serviceId}/config-presupuesto/margen-objetivo",
            new MargenObjetivoRequest(31.5m));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cfg = await ReadJsonAsync<ConfigPresupuestoDto>(resp);
        cfg!.MargenObjetivoPct.Should().Be(31.5m);
    }

    [Fact]
    public async Task CicloCompleto_Crear_Listar_Actualizar_Eliminar()
    {
        var client = await CreateAuthenticatedClientAsync(); // admin
        var serviceId = await FirstServiceIdAsync(client);

        // crear partida
        var create = await client.PostAsJsonAsync($"/api/services/{serviceId}/config-presupuesto/partidas",
            new PartidaPresupuestoCreateRequest("Ciclo partida", TipoPartidaPresupuesto.TotalAccion, null, 10000m, 4000m, "desc"));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<PartidaPresupuestoDto>(create);
        created!.Id.Should().BeGreaterThan(0);
        created.Restante.Should().Be(6000m);
        created.AvancePct.Should().Be(40m);

        // aparece en la config con totales
        var cfg = await ReadJsonAsync<ConfigPresupuestoDto>(await client.GetAsync($"/api/services/{serviceId}/config-presupuesto"));
        cfg!.Partidas.Should().Contain(p => p.Id == created.Id);

        // actualizar
        var update = await client.PutAsJsonAsync($"/api/services/{serviceId}/config-presupuesto/partidas/{created.Id}",
            new PartidaPresupuestoUpdateRequest("Ciclo partida v2", TipoPartidaPresupuesto.Anual, 2026, 12000m, 12000m, "desc2"));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadJsonAsync<PartidaPresupuestoDto>(update);
        updated!.Nombre.Should().Be("Ciclo partida v2");
        updated.AvancePct.Should().Be(100m);

        // eliminar (soft delete)
        var del = await client.DeleteAsync($"/api/services/{serviceId}/config-presupuesto/partidas/{created.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // ya no aparece
        var cfg2 = await ReadJsonAsync<ConfigPresupuestoDto>(await client.GetAsync($"/api/services/{serviceId}/config-presupuesto"));
        cfg2!.Partidas.Should().NotContain(p => p.Id == created.Id);
    }
}
