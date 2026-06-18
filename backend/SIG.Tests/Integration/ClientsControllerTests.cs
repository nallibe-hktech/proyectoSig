using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class ClientsControllerTests : IntegrationTestBase
{
    public ClientsControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task GetClients_ConToken_DevuelvePagedResult()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/clients?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<PagedResult<ClientListItemDto>>(resp);
        body.Should().NotBeNull();
        body!.Items.Should().NotBeEmpty(); // seed
    }

    [Fact]
    public async Task GetClients_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/clients");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetClientById_Existente_Devuelve200()
    {
        var client = await CreateAuthenticatedClientAsync();
        var list = await ReadJsonAsync<PagedResult<ClientListItemDto>>(await client.GetAsync("/api/clients"));
        var firstId = list!.Items.First().Id;
        var resp = await client.GetAsync($"/api/clients/{firstId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await ReadJsonAsync<ClientDetailDto>(resp);
        detail!.Id.Should().Be(firstId);
    }

    [Fact]
    public async Task GetClientById_NoExistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/clients/9999");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostClient_ComoReader_Devuelve403()
    {
        // reader@sig.local tiene rol Reader — solo lectura
        var client = await CreateAuthenticatedClientAsync("reader@sig.local", "Demo#2026!");
        var req = new ClientCreateRequest("NoDeberiaCrearse", "Z99999999", null, null, null, null, null, null, null, null, null);
        var resp = await client.PostAsJsonAsync("/api/clients", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostClient_ComoAdministrator_Devuelve201YPermitiendoLuegoEliminarSoftDelete()
    {
        var client = await CreateAuthenticatedClientAsync(); // admin
        // crear
        var nif = $"X{DateTime.UtcNow.Ticks % 90000000:00000000}";
        var req = new ClientCreateRequest("ClienteTest", nif, null, "Calle T", "Madrid", "Madrid", "ES", "28001", "Pepe", "p@ex.com", "600");
        var create = await client.PostAsJsonAsync("/api/clients", req);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<ClientDetailDto>(create);
        created!.Id.Should().BeGreaterThan(0);

        // get
        var get = await client.GetAsync($"/api/clients/{created.Id}");
        get.StatusCode.Should().Be(HttpStatusCode.OK);

        // delete (soft delete)
        var del = await client.DeleteAsync($"/api/clients/{created.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // tras delete, GET devuelve 404
        var getAfter = await client.GetAsync($"/api/clients/{created.Id}");
        getAfter.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PostClient_NIFDuplicado_Devuelve409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var list = await ReadJsonAsync<PagedResult<ClientListItemDto>>(await client.GetAsync("/api/clients"));
        var existingNif = list!.Items.First().NIF;
        var req = new ClientCreateRequest("DupeTest", existingNif, null, null, null, null, null, null, null, null, null);
        var resp = await client.PostAsJsonAsync("/api/clients", req);
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task PostClient_DatosInvalidos_Devuelve400()
    {
        var client = await CreateAuthenticatedClientAsync();
        var req = new ClientCreateRequest("X", "1", null, null, null, null, null, null, null, null, null); // nombre y NIF muy cortos
        var resp = await client.PostAsJsonAsync("/api/clients", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
