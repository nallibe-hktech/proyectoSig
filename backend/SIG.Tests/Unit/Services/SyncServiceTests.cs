using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class SyncServiceTests
{
    private readonly ICeleroClient _celero = Substitute.For<ICeleroClient>();
    private readonly IBizneoClient _bizneo = Substitute.For<IBizneoClient>();
    private readonly IIntratimeClient _intratime = Substitute.For<IIntratimeClient>();
    private readonly IPayHawkClient _payhawk = Substitute.For<IPayHawkClient>();
    private readonly ISgpvClient _sgpv = Substitute.For<ISgpvClient>();
    private readonly IGalanClient _galan = Substitute.For<IGalanClient>();
    private readonly IMediapostClient _mediapost = Substitute.For<IMediapostClient>();
    private readonly IStagingRepository<StagingCeleroVisita> _celeroRepo = Substitute.For<IStagingRepository<StagingCeleroVisita>>();
    private readonly IStagingRepository<StagingBizneoEmpleado> _empRepo = Substitute.For<IStagingRepository<StagingBizneoEmpleado>>();
    private readonly IStagingRepository<StagingBizneoAbsence> _absenceRepo = Substitute.For<IStagingRepository<StagingBizneoAbsence>>();
    private readonly IStagingRepository<StagingIntratimeFichaje> _ficRepo = Substitute.For<IStagingRepository<StagingIntratimeFichaje>>();
    private readonly IStagingRepository<StagingIntratimeEmpleado> _intratimeEmpRepo = Substitute.For<IStagingRepository<StagingIntratimeEmpleado>>();
    private readonly IStagingRepository<StagingIntratimeClockingRequest> _clkReqRepo = Substitute.For<IStagingRepository<StagingIntratimeClockingRequest>>();
    private readonly IStagingRepository<StagingIntratimeExpense> _expenseRepo = Substitute.For<IStagingRepository<StagingIntratimeExpense>>();
    private readonly IStagingRepository<StagingPayHawkGasto> _gastoRepo = Substitute.For<IStagingRepository<StagingPayHawkGasto>>();
    private readonly IStagingRepository<StagingSgpvVisita> _sgpvRepo = Substitute.For<IStagingRepository<StagingSgpvVisita>>();
    private readonly IStagingRepository<StagingSgpvProducto> _sgpvProductoRepo = Substitute.For<IStagingRepository<StagingSgpvProducto>>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IActionRepository _actionRepo = Substitute.For<IActionRepository>();
    private readonly ICeleroMappingRepository _mappingRepo = Substitute.For<ICeleroMappingRepository>();

    private SyncService CreateSut() => new(_celero, _bizneo, _intratime, _payhawk, _sgpv, _galan, _mediapost, _celeroRepo, _empRepo, _absenceRepo, _ficRepo, _intratimeEmpRepo, _clkReqRepo, _expenseRepo, _gastoRepo, _sgpvRepo, _sgpvProductoRepo, _userRepo, _projectRepo, _actionRepo, _mappingRepo);

    [Fact]
    public async Task SyncAsync_SistemaDesconocido_LanzaIntegrationException()
    {
        var sut = CreateSut();
        await FluentActions.Awaiting(() => sut.SyncAsync("no-existe", CancellationToken.None))
            .Should().ThrowAsync<IntegrationException>();
    }

    [Fact]
    public async Task SyncAsync_Celero_InsertaVisitasNuevasYRespetaHash()
    {
        _celero.GetVisitasAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new CeleroVisitaDto("v1", "12345678A", "Proyecto1", "Acción1", new DateOnly(2026, 3, 1)),
                new CeleroVisitaDto("v2", "23456789B", "Proyecto2", "Acción2", new DateOnly(2026, 3, 2)),
            });
        _celeroRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var result = await sut.SyncAsync("celero", CancellationToken.None);

        result.Sistema.Should().Be("celero");
        result.Exito.Should().BeTrue();
        result.RegistrosInsertados.Should().Be(2);
        result.RegistrosActualizados.Should().Be(0);
        await _celeroRepo.Received(1).AddRangeAsync(Arg.Is<IEnumerable<StagingCeleroVisita>>(r => r.Count() == 2), Arg.Any<CancellationToken>());
        await _celeroRepo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_Celero_HashExistente_NoInsertaIncrementaActualizados()
    {
        _celero.GetVisitasAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new CeleroVisitaDto("v1", "12345678A", "Proyecto1", "Acción1", new DateOnly(2026, 3, 1)),
                new CeleroVisitaDto("v2", "23456789B", "Proyecto2", "Acción2", new DateOnly(2026, 3, 2)),
            });
        _celeroRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var sut = CreateSut();
        var result = await sut.SyncAsync("celero", CancellationToken.None);

        result.RegistrosInsertados.Should().Be(0);
        result.RegistrosActualizados.Should().Be(2);
    }

    [Fact]
    public async Task SyncAsync_PayHawk_InsertaGastosNuevos()
    {
        _payhawk.GetGastosAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new PayHawkGastoDto("g1", 1, 100, new DateOnly(2026, 3, 1), 50m, "viaje"),
            });
        _gastoRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var result = await sut.SyncAsync("payhawk", CancellationToken.None);

        result.Sistema.Should().Be("payhawk");
        result.Exito.Should().BeTrue();
        result.RegistrosInsertados.Should().Be(1);
    }

    [Fact]
    public async Task SyncAsync_NombreSistemaMayusculas_SeNormalizaALowerCase()
    {
        _celero.GetVisitasAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CeleroVisitaDto>());

        var sut = CreateSut();
        var result = await sut.SyncAsync("CELERO", CancellationToken.None);
        result.Sistema.Should().Be("celero");
    }

    [Fact]
    public async Task SyncAsync_Intratime_PersisteFichajesConKindUtc()
    {
        _intratime.GetFichajesAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new IntratimeFichajeDto("f1", "20875", new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 3, 1, 17, 0, 0, DateTimeKind.Unspecified)),
            });
        _ficRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _intratimeEmpRepo.ListAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<StagingIntratimeEmpleado>());

        var sut = CreateSut();
        var result = await sut.SyncAsync("intratime", CancellationToken.None);

        result.RegistrosInsertados.Should().Be(1);
        // Verifica que los DateTimes guardados tienen Kind=Utc (gotcha Npgsql)
        await _ficRepo.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<StagingIntratimeFichaje>>(rows =>
                rows.All(r => r.Entrada.Kind == DateTimeKind.Utc && (!r.Salida.HasValue || r.Salida.Value.Kind == DateTimeKind.Utc))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAsync_Bizneo_SincronizaEmpleadosYHoras()
    {
        _bizneo.GetEmpleadosAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new BizneoEmpleadoDto("e1", "12345678A", "Pepe", "Operaciones") });
        _bizneo.GetAbsencesAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new BizneoAbsenceDto("h1", 1, 100, new DateOnly(2026, 3, 1), 8m) });
        _empRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _absenceRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var result = await sut.SyncAsync("bizneo", CancellationToken.None);

        result.Exito.Should().BeTrue();
        result.RegistrosInsertados.Should().Be(2); // 1 empleado + 1 hora
    }

    [Fact]
    public async Task SyncAsync_A3Innuva_ThrowsNotImplemented()
    {
        var sut = CreateSut();
        await FluentActions.Awaiting(() => sut.SyncAsync("a3innuva", CancellationToken.None))
            .Should().ThrowAsync<IntegrationException>();
    }

    [Fact]
    public async Task SyncAsync_TravelPerk_ThrowsNotImplemented()
    {
        var sut = CreateSut();
        await FluentActions.Awaiting(() => sut.SyncAsync("travelperk", CancellationToken.None))
            .Should().ThrowAsync<IntegrationException>();
    }

    [Fact]
    public async Task SyncAsync_SyncResult_ContieneExitoYTimestamp()
    {
        _celero.GetVisitasAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new CeleroVisitaDto("v1", "12345678A", "Proyecto1", "Acción1", new DateOnly(2026, 3, 1)) });
        _celeroRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var result = await sut.SyncAsync("celero", CancellationToken.None);

        result.Exito.Should().BeTrue();
        result.MensajeError.Should().BeNull();
        result.FechaUltimaSincronizacion.Should().HaveValue();
        result.FechaUltimaSincronizacion!.Value.Year.Should().Be(DateTime.UtcNow.Year);
    }
}
