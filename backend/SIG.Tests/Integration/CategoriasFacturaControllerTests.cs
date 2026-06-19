using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class CategoriasFacturaControllerTests : IntegrationTestBase
{
    public CategoriasFacturaControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private async Task<int> FirstClientIdAsync(HttpClient client)
    {
        var list = await ReadJsonAsync<PagedResult<ClientListItemDto>>(await client.GetAsync("/api/clients"));
        return list!.Items.First().Id;
    }

    private async Task<List<ConceptoDisponibleDto>> DisponiblesAsync(HttpClient client, int clientId) =>
        (await ReadJsonAsync<List<ConceptoDisponibleDto>>(
            await client.GetAsync($"/api/clients/{clientId}/categorias-factura/conceptos-disponibles")))!;

    [Fact]
    public async Task List_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/clients/1/categorias-factura");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_ConToken_Devuelve200()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(client);
        var resp = await client.GetAsync($"/api/clients/{clientId}/categorias-factura");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync<List<CategoriaFacturaDto>>(resp)).Should().NotBeNull();
    }

    // El cliente tiene conceptos de facturación disponibles (globales del seed), con algún "sin asignar".
    [Fact]
    public async Task ConceptosDisponibles_DevuelveConceptosDeFacturacion()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(client);
        var disponibles = await DisponiblesAsync(client, clientId);

        disponibles.Should().NotBeEmpty();
        disponibles.Should().Contain(c => !c.Asignado);
    }

    [Fact]
    public async Task Resumen_DevuelveContadoresNoNegativos()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(client);
        var resumen = await ReadJsonAsync<ConfigFacturaResumenDto>(
            await client.GetAsync($"/api/clients/{clientId}/categorias-factura/resumen"));

        resumen!.NumCategorias.Should().BeGreaterThanOrEqualTo(0);
        resumen.ConceptosMapeados.Should().BeGreaterThanOrEqualTo(0);
        resumen.ConceptosSinAsignar.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Post_ComoReader_Devuelve403()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local", "Demo#2026!");
        var admin = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(admin);
        var resp = await reader.PostAsJsonAsync($"/api/clients/{clientId}/categorias-factura",
            new CategoriaFacturaCreateRequest("No permitida", Array.Empty<int>()));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Post_ClienteInexistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsJsonAsync("/api/clients/999999/categorias-factura",
            new CategoriaFacturaCreateRequest("X", Array.Empty<int>()));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_ConceptoDeOtroCliente_Devuelve409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(client);
        // 88888 no es un concepto de facturación del cliente → 409 (validación de pertenencia).
        var resp = await client.PostAsJsonAsync($"/api/clients/{clientId}/categorias-factura",
            new CategoriaFacturaCreateRequest("Inválida", new[] { 88888 }));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_ConceptoYaAsignado_Devuelve409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var clientId = await FirstClientIdAsync(client);
        var libre = (await DisponiblesAsync(client, clientId)).First(c => !c.Asignado);

        // 1ª categoría toma el concepto…
        var first = await client.PostAsJsonAsync($"/api/clients/{clientId}/categorias-factura",
            new CategoriaFacturaCreateRequest("Primera", new[] { libre.ConceptId }));
        first.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<CategoriaFacturaDto>(first);

        // …una 2ª no puede reutilizarlo → 409
        var second = await client.PostAsJsonAsync($"/api/clients/{clientId}/categorias-factura",
            new CategoriaFacturaCreateRequest("Segunda", new[] { libre.ConceptId }));
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // limpieza para no contaminar otros tests
        await client.DeleteAsync($"/api/clients/{clientId}/categorias-factura/{created!.Id}");
    }

    [Fact]
    public async Task CicloCompleto_Crear_Listar_Actualizar_Eliminar()
    {
        var client = await CreateAuthenticatedClientAsync(); // admin
        var clientId = await FirstClientIdAsync(client);
        var libre = (await DisponiblesAsync(client, clientId)).First(c => !c.Asignado);

        // crear con un concepto que estaba sin asignar
        var create = await client.PostAsJsonAsync($"/api/clients/{clientId}/categorias-factura",
            new CategoriaFacturaCreateRequest("Ciclo categoría", new[] { libre.ConceptId }));
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await ReadJsonAsync<CategoriaFacturaDto>(create);
        created!.Id.Should().BeGreaterThan(0);
        created.ClientId.Should().Be(clientId);
        created.Conceptos.Should().ContainSingle(c => c.ConceptId == libre.ConceptId);

        // el concepto pasa a "asignado"
        (await DisponiblesAsync(client, clientId))
            .First(c => c.ConceptId == libre.ConceptId).Asignado.Should().BeTrue();

        // actualizar (renombrar y vaciar conceptos)
        var update = await client.PutAsJsonAsync($"/api/clients/{clientId}/categorias-factura/{created.Id}",
            new CategoriaFacturaUpdateRequest("Ciclo renombrada", Array.Empty<int>()));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await ReadJsonAsync<CategoriaFacturaDto>(update);
        updated!.Nombre.Should().Be("Ciclo renombrada");
        updated.Conceptos.Should().BeEmpty();

        // al vaciar, el concepto vuelve a estar disponible
        (await DisponiblesAsync(client, clientId))
            .First(c => c.ConceptId == libre.ConceptId).Asignado.Should().BeFalse();

        // eliminar (soft delete)
        var del = await client.DeleteAsync($"/api/clients/{clientId}/categorias-factura/{created.Id}");
        del.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // ya no aparece en el listado
        var list = await ReadJsonAsync<List<CategoriaFacturaDto>>(
            await client.GetAsync($"/api/clients/{clientId}/categorias-factura"));
        list!.Should().NotContain(c => c.Id == created.Id);
    }
}
