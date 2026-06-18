using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Domain.Entities.Staging;

namespace SIG.Tests.Integration;

/// <summary>
/// Ola 2 (#2) — endpoints de contratos de un día y "ignorar en cierre", más la exclusión
/// de los contratos ignorados de las validaciones de cierre (a nivel repositorio).
/// </summary>
[Collection("Integration")]
public class ContratosControllerTests : IntegrationTestBase
{
    public ContratosControllerTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    private async Task<int> SeedContratoAsync(DateTime inicio, DateTime fin, bool ignorado = false)
    {
        using var db = NewDbContext();
        var contrato = new StagingA3InnuvaContrato
        {
            ContratoIdExterno = $"CT-TEST-{Guid.NewGuid():N}".Substring(0, 20),
            NIF = $"T{Random.Shared.Next(10000000, 99999999)}",
            FechaInicio = DateTime.SpecifyKind(inicio, DateTimeKind.Utc),
            FechaFin = DateTime.SpecifyKind(fin, DateTimeKind.Utc),
            ImporteBruto = 1500m,
            IgnoradoEnCierre = ignorado,
            PayloadJson = "{}",
            Hash = $"hash-{Guid.NewGuid():N}",
            FechaUltimaSincronizacion = DateTime.UtcNow,
            FlagProcesado = false
        };
        db.StagingA3InnuvaContratos.Add(contrato);
        await db.SaveChangesAsync();
        return contrato.Id;
    }

    [Fact]
    public async Task GetUnDia_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/contratos/un-dia");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnDia_ComoBackoffice_DevuelveSoloContratosDeUnDia()
    {
        var unDia = new DateTime(2026, 3, 10);
        var idUnDia = await SeedContratoAsync(unDia, unDia);
        await SeedContratoAsync(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31)); // multi-día, NO debe aparecer

        var client = await CreateAuthenticatedClientAsync("backoffice1@sig.local");
        var resp = await client.GetAsync("/api/contratos/un-dia");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var lista = await ReadJsonAsync<List<ContratoUnDiaDto>>(resp);
        lista.Should().NotBeNull();
        lista!.Should().OnlyContain(c => c.FechaInicio == c.FechaFin);
        lista.Should().Contain(c => c.Id == idUnDia);
    }

    [Fact]
    public async Task GetUnDia_ComoReader_Devuelve403()
    {
        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.GetAsync("/api/contratos/un-dia");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ignorar_ComoBackoffice_MarcaIgnoradoYMotivo()
    {
        var fecha = new DateTime(2026, 4, 5);
        var id = await SeedContratoAsync(fecha, fecha);

        var client = await CreateAuthenticatedClientAsync("backoffice1@sig.local");
        var resp = await client.PostAsJsonAsync($"/api/contratos/{id}/ignorar", new ContratoIgnorarRequest(true, "Contrato de un día sin actividad"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await ReadJsonAsync<ContratoUnDiaDto>(resp);
        dto!.IgnoradoEnCierre.Should().BeTrue();
        dto.MotivoIgnorar.Should().Be("Contrato de un día sin actividad");

        using var db = NewDbContext();
        var persisted = await db.StagingA3InnuvaContratos.AsNoTracking().FirstAsync(c => c.Id == id);
        persisted.IgnoradoEnCierre.Should().BeTrue();
        persisted.MotivoIgnorar.Should().Be("Contrato de un día sin actividad");
    }

    [Fact]
    public async Task Ignorar_ComoReader_Devuelve403()
    {
        var fecha = new DateTime(2026, 4, 6);
        var id = await SeedContratoAsync(fecha, fecha);

        var reader = await CreateAuthenticatedClientAsync("reader@sig.local");
        var resp = await reader.PostAsJsonAsync($"/api/contratos/{id}/ignorar", new ContratoIgnorarRequest(true, "intento"));
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Ignorar_ContratoInexistente_Devuelve404()
    {
        var client = await CreateAuthenticatedClientAsync("backoffice1@sig.local");
        var resp = await client.PostAsJsonAsync("/api/contratos/999999/ignorar", new ContratoIgnorarRequest(true, "x"));
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    /// <summary>
    /// CLAVE Ola 2 (#2): un contrato con IgnoradoEnCierre=true queda EXCLUIDO de las consultas que
    /// alimentan las validaciones de cierre (GetActivosEnPeriodoAsync / GetAllAsync del repositorio),
    /// mientras que el mismo contrato sin ignorar SÍ aparecería.
    /// </summary>
    [Fact]
    public async Task ContratoIgnorado_ExcluidoDeLasConsultasDeValidacionDeCierre()
    {
        var inicio = new DateTime(2026, 5, 1);
        var fin = new DateTime(2026, 5, 31);

        var idActivo = await SeedContratoAsync(inicio, fin, ignorado: false);
        var idIgnorado = await SeedContratoAsync(inicio, fin, ignorado: true);

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStagingA3InnuvaContratoRepository>();

        var activosEnPeriodo = await repo.GetActivosEnPeriodoAsync(inicio, fin, CancellationToken.None);
        activosEnPeriodo.Should().Contain(c => c.Id == idActivo);
        activosEnPeriodo.Should().NotContain(c => c.Id == idIgnorado,
            "los contratos marcados IgnoradoEnCierre=true no participan en las validaciones de cierre");

        var todos = await repo.GetAllAsync(CancellationToken.None);
        todos.Should().Contain(c => c.Id == idActivo);
        todos.Should().NotContain(c => c.Id == idIgnorado);
    }

    /// <summary>
    /// El listado de "un día" SÍ debe incluir los ignorados (para poder desmarcarlos), a diferencia
    /// de las consultas de validación de cierre.
    /// </summary>
    [Fact]
    public async Task ListContratosUnDia_IncluyeLosIgnorados_ParaPoderDesmarcarlos()
    {
        var fecha = new DateTime(2026, 6, 15);
        var idIgnorado = await SeedContratoAsync(fecha, fecha, ignorado: true);

        using var scope = Factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IStagingA3InnuvaContratoRepository>();
        var unDia = await repo.ListContratosUnDiaAsync(CancellationToken.None);

        unDia.Should().Contain(c => c.Id == idIgnorado);
    }
}
