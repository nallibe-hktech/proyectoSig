using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class ClienteIncidenciasControllerTests : IntegrationTestBase
{
    public ClienteIncidenciasControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private async Task<int> FirstClientIdAsync(HttpClient client)
    {
        var list = await ReadJsonAsync<PagedResult<ClientListItemDto>>(await client.GetAsync("/api/clients"));
        return list!.Items.First().Id;
    }

    [Fact]
    public async Task ListIncidencias_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/clients/1/incidencias");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListIncidencias_ConToken_Devuelve200()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(client);
        var resp = await client.GetAsync($"/api/clients/{clientId}/incidencias");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<List<ClienteIncidenciaDto>>(resp);
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task PostIncidencia_ComoReader_Devuelve403()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local", "Demo#2026!");
        var admin = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(admin);
        var req = new ClienteIncidenciaCreateRequest("Facturación", "No debería crearse", null, null, null);
        var resp = await reader.PostAsJsonAsync($"/api/clients/{clientId}/incidencias", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostIncidencia_ClienteInexistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var req = new ClienteIncidenciaCreateRequest("Facturación", "X", null, null, null);
        var resp = await client.PostAsJsonAsync("/api/clients/999999/incidencias", req);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CicloCompleto_Crear_Listar_Actualizar_Eliminar()
    {
        var client = await CreateAuthenticatedClientAsync(); // admin
        var clientId = await FirstClientIdAsync(client);

        // crear (estado por defecto Abierta)
        var create = await client.PostAsJsonAsync($"/api/clients/{clientId}/incidencias",
            new ClienteIncidenciaCreateRequest("Logística", "Pedido no entregado a tiempo", null, null, null));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<ClienteIncidenciaDto>(create);
        created!.Id.Should().BeGreaterThan(0);
        created.ClientId.Should().Be(clientId);
        created.Estado.Should().Be(EstadoIncidencia.Abierta);

        // aparece en el listado
        var list = await ReadJsonAsync<List<ClienteIncidenciaDto>>(await client.GetAsync($"/api/clients/{clientId}/incidencias"));
        list!.Should().Contain(i => i.Id == created.Id);

        // actualizar a Resuelta
        var update = await client.PutAsJsonAsync($"/api/clients/{clientId}/incidencias/{created.Id}",
            new ClienteIncidenciaUpdateRequest("Logística", "Resuelta con reenvío", EstadoIncidencia.Resuelta, null));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadJsonAsync<ClienteIncidenciaDto>(update);
        updated!.Estado.Should().Be(EstadoIncidencia.Resuelta);

        // eliminar (soft delete)
        var del = await client.DeleteAsync($"/api/clients/{clientId}/incidencias/{created.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // tras delete, GET devuelve 404
        var getAfter = await client.GetAsync($"/api/clients/{clientId}/incidencias/{created.Id}");
        getAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListadoGlobal_ConToken_DevuelveCreadaConNombreCliente()
    {
        var client = await CreateAuthenticatedClientAsync(); // admin
        var clientId = await FirstClientIdAsync(client);

        var create = await client.PostAsJsonAsync($"/api/clients/{clientId}/incidencias",
            new ClienteIncidenciaCreateRequest("Disputa", "Tarifa GPV en revisión", null, "Comercial", null));
        var created = await ReadJsonAsync<ClienteIncidenciaDto>(create);

        var page = await ReadJsonAsync<PagedResult<IncidenciaListItemDto>>(
            await client.GetAsync("/api/incidencias?page=1&pageSize=100"));

        page!.Items.Should().Contain(i => i.Id == created!.Id && !string.IsNullOrEmpty(i.ClientNombre));
        page.Items.First(i => i.Id == created!.Id).Origen.Should().Be("Comercial");
    }

    [Fact]
    public async Task CambiarEstado_RegistraEntradaEnHistorico()
    {
        var client = await CreateAuthenticatedClientAsync(); // admin
        var clientId = await FirstClientIdAsync(client);

        var create = await client.PostAsJsonAsync($"/api/clients/{clientId}/incidencias",
            new ClienteIncidenciaCreateRequest("Impago", "Factura sin abonar", null, "Contabilidad", null));
        var created = await ReadJsonAsync<ClienteIncidenciaDto>(create);
        created!.Historial.Should().HaveCount(1); // entrada inicial "Incidencia creada"

        var cambio = await client.PostAsJsonAsync($"/api/clients/{clientId}/incidencias/{created.Id}/estado",
            new IncidenciaCambioEstadoRequest(EstadoIncidencia.Resuelta, "Reclamación abonada", "Contabilidad"));
        cambio.StatusCode.Should().Be(HttpStatusCode.OK);
        var actualizada = await ReadJsonAsync<ClienteIncidenciaDto>(cambio);

        actualizada!.Estado.Should().Be(EstadoIncidencia.Resuelta);
        actualizada.Historial.Should().HaveCount(2);
        actualizada.Historial.Last().Nota.Should().Be("Reclamación abonada");
    }

    [Fact]
    public async Task CambiarEstado_ComoReader_Devuelve403()
    {
        var admin = await CreateAuthenticatedClientAsync();
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local", "Demo#2026!");
        var clientId = await FirstClientIdAsync(admin);
        var created = await ReadJsonAsync<ClienteIncidenciaDto>(await admin.PostAsJsonAsync(
            $"/api/clients/{clientId}/incidencias",
            new ClienteIncidenciaCreateRequest("Operativa", "Falta de datos", null, null, null)));

        var resp = await reader.PostAsJsonAsync($"/api/clients/{clientId}/incidencias/{created!.Id}/estado",
            new IncidenciaCambioEstadoRequest(EstadoIncidencia.Resuelta, "intento", null));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
