using System.Net;
using System.Net.Http.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

/// <summary>
/// Ola 2 (#8) — asociación/desasociación de conceptos del catálogo a un servicio (ServiceConcept).
/// </summary>
[Collection("Integration")]
public class ServiceConceptControllerTests : IntegrationTestBase
{
    public ServiceConceptControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private async Task<int> FirstServiceIdAsync(HttpClient admin)
    {
        var list = await ReadJsonAsync<PagedResult<ServiceListItemDto>>(await admin.GetAsync("/api/services?page=1&pageSize=1"));
        list!.Items.Should().NotBeEmpty();
        return list.Items.First().Id;
    }

    private async Task<int> FirstConceptIdAsync(HttpClient admin)
    {
        var list = await ReadJsonAsync<PagedResult<ConceptListItemDto>>(await admin.GetAsync("/api/concepts?page=1&pageSize=100"));
        list!.Items.Should().NotBeEmpty();
        return list.Items.First().Id;
    }

    [Fact]
    public async Task AddYRemoveConcept_ComoAdmin_AsociaYDesasocia()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var serviceId = await FirstServiceIdAsync(admin);
        var conceptId = await FirstConceptIdAsync(admin);

        // Garantizar estado conocido: si ya estaba asociado, lo quitamos primero.
        await admin.DeleteAsync($"/api/services/{serviceId}/concepts/{conceptId}");

        var addResp = await admin.PostAsync($"/api/services/{serviceId}/concepts/{conceptId}", null);
        addResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await ReadJsonAsync<ServiceDetailDto>(addResp);
        detail!.ConceptIds.Should().Contain(conceptId);

        var removeResp = await admin.DeleteAsync($"/api/services/{serviceId}/concepts/{conceptId}");
        removeResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var afterRemove = await ReadJsonAsync<ServiceDetailDto>(removeResp);
        afterRemove!.ConceptIds.Should().NotContain(conceptId);
    }

    [Fact]
    public async Task AddConcept_Idempotente_NoDuplicaLaAsociacion()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var serviceId = await FirstServiceIdAsync(admin);
        var conceptId = await FirstConceptIdAsync(admin);

        await admin.PostAsync($"/api/services/{serviceId}/concepts/{conceptId}", null);
        var secondResp = await admin.PostAsync($"/api/services/{serviceId}/concepts/{conceptId}", null);
        secondResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await ReadJsonAsync<ServiceDetailDto>(secondResp);
        detail!.ConceptIds.Count(c => c == conceptId).Should().Be(1);
    }

    [Fact]
    public async Task AddConcept_ConceptoInexistente_Devuelve404()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var serviceId = await FirstServiceIdAsync(admin);

        var resp = await admin.PostAsync($"/api/services/{serviceId}/concepts/999999", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddConcept_ServicioInexistente_Devuelve404()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var conceptId = await FirstConceptIdAsync(admin);

        var resp = await admin.PostAsync($"/api/services/999999/concepts/{conceptId}", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddConcept_ComoReader_Devuelve403()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var serviceId = await FirstServiceIdAsync(admin);
        var conceptId = await FirstConceptIdAsync(admin);

        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.PostAsync($"/api/services/{serviceId}/concepts/{conceptId}", null);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveConcept_ComoReader_Devuelve403()
    {
        var admin = await CreateAuthenticatedClientAsync("admin@sig.local");
        var serviceId = await FirstServiceIdAsync(admin);
        var conceptId = await FirstConceptIdAsync(admin);

        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.DeleteAsync($"/api/services/{serviceId}/concepts/{conceptId}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
