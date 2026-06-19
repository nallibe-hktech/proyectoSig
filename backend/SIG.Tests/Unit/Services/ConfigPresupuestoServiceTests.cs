using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class ConfigPresupuestoServiceTests
{
    private readonly IPartidaPresupuestoRepository _repo = Substitute.For<IPartidaPresupuestoRepository>();
    private readonly IServiceRepository _serviceRepo = Substitute.For<IServiceRepository>();
    private readonly ConfigPresupuestoService _sut;

    public ConfigPresupuestoServiceTests() { _sut = new ConfigPresupuestoService(_repo, _serviceRepo); }

    private static Service MakeService(int id = 1, decimal? margenObjetivo = null) =>
        new() { Id = id, Nombre = "Amex Shop Small", ClientId = 1, Client = new Client { Id = 1, Nombre = "American Express", NIF = "A1" }, MargenObjetivoPct = margenObjetivo };

    private static PartidaPresupuesto MakePartida(int id, int serviceId, string nombre, TipoPartidaPresupuesto tipo, decimal pres, decimal cons) =>
        new() { Id = id, ServiceId = serviceId, Nombre = nombre, Tipo = tipo, Presupuesto = pres, Consumido = cons };

    private void Accesible(int serviceId = 1, int usuarioId = 99, decimal? margenObjetivo = null) =>
        _serviceRepo.GetByIdAndUsuarioIdAsync(serviceId, usuarioId, Arg.Any<CancellationToken>()).Returns(MakeService(serviceId, margenObjetivo));

    [Fact]
    public async Task GetConfigAsync_ServicioInaccesible_LanzaEntityNotFound()
    {
        _serviceRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns((Service?)null);

        await FluentActions.Awaiting(() => _sut.GetConfigAsync(1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetConfigAsync_CalculaTotalesMargenesYContadores()
    {
        Accesible(margenObjetivo: 28m);
        _repo.ListByServiceAsync(1, Arg.Any<CancellationToken>()).Returns(new[]
        {
            MakePartida(1, 1, "Personal de campo", TipoPartidaPresupuesto.Anual, 52000m, 38900m),
            MakePartida(2, 1, "Kilometraje", TipoPartidaPresupuesto.TotalAccion, 9000m, 5300m),
        });
        _repo.GetMargenRealPctAsync(1, Arg.Any<CancellationToken>()).Returns(26.5m);

        var cfg = await _sut.GetConfigAsync(1, 99, CancellationToken.None);

        cfg.ClientNombre.Should().Be("American Express");
        cfg.TotalPresupuesto.Should().Be(61000m);
        cfg.TotalConsumido.Should().Be(44200m);
        cfg.TotalRestante.Should().Be(16800m);
        cfg.MargenObjetivoPct.Should().Be(28m);
        cfg.MargenRealPct.Should().Be(26.5m);
        cfg.DesviacionPp.Should().Be(-1.5m);
        cfg.PartidasAnuales.Should().Be(1);
        cfg.PartidasTotalAccion.Should().Be(1);
        cfg.Partidas.Should().HaveCount(2);
        cfg.Partidas.First(p => p.Id == 1).Restante.Should().Be(13100m);
        cfg.Partidas.First(p => p.Id == 1).AvancePct.Should().Be(75m);
    }

    [Fact]
    public async Task GetConfigAsync_SinFacturacion_DesviacionNull()
    {
        Accesible(margenObjetivo: 28m);
        _repo.ListByServiceAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<PartidaPresupuesto>());
        _repo.GetMargenRealPctAsync(1, Arg.Any<CancellationToken>()).Returns((decimal?)null);

        var cfg = await _sut.GetConfigAsync(1, 99, CancellationToken.None);

        cfg.MargenRealPct.Should().BeNull();
        cfg.DesviacionPp.Should().BeNull();
        cfg.AvancePct.Should().Be(0m);
    }

    [Fact]
    public async Task CreatePartidaAsync_Persiste()
    {
        Accesible();
        var req = new PartidaPresupuestoCreateRequest("Logística", TipoPartidaPresupuesto.TotalAccion, null, 5000m, 0m, "Galán");

        var result = await _sut.CreatePartidaAsync(1, req, 99, CancellationToken.None);

        result.Nombre.Should().Be("Logística");
        result.Restante.Should().Be(5000m);
        await _repo.Received(1).AddAsync(
            Arg.Is<PartidaPresupuesto>(p => p.ServiceId == 1 && p.Nombre == "Logística" && p.Presupuesto == 5000m),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdatePartidaAsync_PartidaDeOtroServicio_LanzaEntityNotFound()
    {
        Accesible();
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(MakePartida(5, serviceId: 2, "Ajena", TipoPartidaPresupuesto.Anual, 1m, 0m));

        await FluentActions.Awaiting(() =>
                _sut.UpdatePartidaAsync(5, 1, new PartidaPresupuestoUpdateRequest("X", TipoPartidaPresupuesto.Anual, 2026, 1m, 0m, null), 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdatePartidaAsync_ActualizaCampos()
    {
        Accesible();
        var p = MakePartida(5, 1, "Vieja", TipoPartidaPresupuesto.Anual, 1000m, 0m);
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(p);

        var result = await _sut.UpdatePartidaAsync(5, 1, new PartidaPresupuestoUpdateRequest("Nueva", TipoPartidaPresupuesto.TotalAccion, null, 2000m, 500m, "desc"), 99, CancellationToken.None);

        p.Nombre.Should().Be("Nueva");
        p.Presupuesto.Should().Be(2000m);
        result.Restante.Should().Be(1500m);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeletePartidaAsync_AplicaSoftDelete()
    {
        Accesible();
        var p = MakePartida(5, 1, "Cat", TipoPartidaPresupuesto.Anual, 1m, 0m);
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(p);

        await _sut.DeletePartidaAsync(5, 1, 99, CancellationToken.None);

        p.IsDeleted.Should().BeTrue();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetMargenObjetivoAsync_GuardaEnServicioYDevuelveConfig()
    {
        var service = MakeService();
        _serviceRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(service);
        _repo.ListByServiceAsync(1, Arg.Any<CancellationToken>()).Returns(Array.Empty<PartidaPresupuesto>());
        _repo.GetMargenRealPctAsync(1, Arg.Any<CancellationToken>()).Returns((decimal?)null);

        var cfg = await _sut.SetMargenObjetivoAsync(1, new MargenObjetivoRequest(30m), 99, CancellationToken.None);

        service.MargenObjetivoPct.Should().Be(30m);
        cfg.MargenObjetivoPct.Should().Be(30m);
        await _serviceRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
