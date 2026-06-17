using Microsoft.Extensions.DependencyInjection;
using SIG.Application.Interfaces.Services;

namespace SIG.Tests.Integration;

/// <summary>
/// Tests de integración de <see cref="IDataProcessorService"/>.
/// Se ejecutan contra la BD de tests real porque el servicio depende directamente de
/// <c>AppDbContext</c> (sin constructor sin parámetros ni DbSets virtuales), lo que hace
/// inviable mockearlo con NSubstitute.
/// </summary>
[Collection("Integration")]
public class DataProcessorServiceTests : IntegrationTestBase
{
    public DataProcessorServiceTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ProcessAllPendingAsync_RetornaResultadoValido()
    {
        using var scope = Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataProcessorService>();

        // Primera pasada drena lo pendiente; la segunda debe ser idempotente: nada nuevo que procesar.
        await svc.ProcessAllPendingAsync(CancellationToken.None);
        var result = await svc.ProcessAllPendingAsync(CancellationToken.None);

        result.Should().NotBeNull();
        result.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.Systems.Should().NotBeNull();
        result.TotalProcessed.Should().Be(0);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ProcessAllPendingAsync_CuandoFalla_RetornaError()
    {
        using var scope = Factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IDataProcessorService>();

        // Token ya cancelado → la primera query EF lanza y el método captura el error en el resultado.
        var result = await svc.ProcessAllPendingAsync(new CancellationToken(canceled: true));

        result.Should().NotBeNull();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
