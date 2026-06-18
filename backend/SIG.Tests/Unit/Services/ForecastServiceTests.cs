using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class ForecastServiceTests
{
    private readonly IForecastRepository _repo = Substitute.For<IForecastRepository>();
    private readonly IServiceRepository _serviceRepo = Substitute.For<IServiceRepository>();
    private readonly ForecastService _sut;

    private static readonly int AnioFuturo = DateTime.UtcNow.Year + 1;

    public ForecastServiceTests() { _sut = new ForecastService(_repo, _serviceRepo); }

    private static Service MakeService(int id = 1) => new() { Id = id, Nombre = "Svc", ClientId = 1 };

    [Fact]
    public async Task UpsertAsync_ServicioInexistente_LanzaEntityNotFound()
    {
        _serviceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Service?)null);

        await FluentActions.Awaiting(() => _sut.UpsertAsync(1, new ForecastUpsertRequest(AnioFuturo, 6, 1000m, 200m, 5), CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpsertAsync_MesCerrado_LanzaPeriodClosed()
    {
        _serviceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeService());

        // Año 2000 siempre es pasado => mes cerrado
        await FluentActions.Awaiting(() => _sut.UpsertAsync(1, new ForecastUpsertRequest(2000, 1, 1000m, null, null), CancellationToken.None))
            .Should().ThrowAsync<PeriodClosedException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public async Task UpsertAsync_MesFueraDeRango_LanzaInvalidOperation(int mes)
    {
        _serviceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeService());

        await FluentActions.Awaiting(() => _sut.UpsertAsync(1, new ForecastUpsertRequest(AnioFuturo, mes, 1000m, null, null), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpsertAsync_MesAbierto_NoExistente_CreaForecast()
    {
        _serviceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeService());
        _repo.GetByServiceMonthAsync(1, AnioFuturo, 6, Arg.Any<CancellationToken>()).Returns((Forecast?)null);

        var result = await _sut.UpsertAsync(1, new ForecastUpsertRequest(AnioFuturo, 6, 5000m, 1500m, 8), CancellationToken.None);

        result.VentasPrevistas.Should().Be(5000m);
        result.PersonasCampo.Should().Be(8);
        await _repo.Received(1).AddAsync(Arg.Is<Forecast>(f => f.ServiceId == 1 && f.Mes == 6 && f.VentasPrevistas == 5000m), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpsertAsync_MesAbierto_Existente_ActualizaSinCrear()
    {
        var existing = new Forecast { Id = 9, ServiceId = 1, Anio = AnioFuturo, Mes = 6, VentasPrevistas = 100m };
        _serviceRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakeService());
        _repo.GetByServiceMonthAsync(1, AnioFuturo, 6, Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.UpsertAsync(1, new ForecastUpsertRequest(AnioFuturo, 6, 7777m, null, 3), CancellationToken.None);

        result.Id.Should().Be(9);
        existing.VentasPrevistas.Should().Be(7777m);
        existing.PersonasCampo.Should().Be(3);
        await _repo.DidNotReceive().AddAsync(Arg.Any<Forecast>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetResumenAsync_AgrupaPorDptoYClienteYSumaPorMes()
    {
        var dep = new Department { Id = 2, Nombre = "Campo" };
        var cli = new Client { Id = 1, Nombre = "Alpha", NIF = "A1" };
        var svcA = new Service { Id = 1, Nombre = "S1", ClientId = 1, Client = cli, DepartmentId = 2, Department = dep };
        var svcB = new Service { Id = 2, Nombre = "S2", ClientId = 1, Client = cli, DepartmentId = 2, Department = dep };
        var data = new[]
        {
            new Forecast { ServiceId = 1, Service = svcA, Anio = AnioFuturo, Mes = 6, VentasPrevistas = 100m, MargenPrevisto = 30m, PersonasCampo = 2 },
            new Forecast { ServiceId = 2, Service = svcB, Anio = AnioFuturo, Mes = 6, VentasPrevistas = 50m, MargenPrevisto = 10m, PersonasCampo = 1 },
        };
        _repo.ListForResumenAsync(AnioFuturo, null, null, null, Arg.Any<CancellationToken>()).Returns(data);

        var result = await _sut.GetResumenAsync(AnioFuturo, null, null, null, CancellationToken.None);

        result.Filas.Should().HaveCount(1); // mismo dpto+cliente
        var fila = result.Filas[0];
        fila.ClientNombre.Should().Be("Alpha");
        fila.DepartmentNombre.Should().Be("Campo");
        fila.Meses.Should().ContainSingle(c => c.Mes == 6 && c.Ventas == 150m && c.Personas == 3);
        fila.TotalVentas.Should().Be(150m);
        fila.TotalPersonas.Should().Be(3);
    }
}
