using SIG.Application.Alerts;
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
    private readonly IStagingRepository<StagingGalanEntrada> _galanEntradaRepo = Substitute.For<IStagingRepository<StagingGalanEntrada>>();
    private readonly IStagingRepository<StagingGalanSalida> _galanSalidaRepo = Substitute.For<IStagingRepository<StagingGalanSalida>>();
    private readonly IStagingRepository<StagingGalanStock> _galanStockRepo = Substitute.For<IStagingRepository<StagingGalanStock>>();
    private readonly IStagingRepository<StagingMediapostPedido> _mediapostPedidoRepo = Substitute.For<IStagingRepository<StagingMediapostPedido>>();
    private readonly IStagingRepository<StagingMediapostRecepcion> _mediapostRecepcionRepo = Substitute.For<IStagingRepository<StagingMediapostRecepcion>>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IServiceRepository _serviceRepo = Substitute.For<IServiceRepository>();
    private readonly ICeleroMappingRepository _mappingRepo = Substitute.For<ICeleroMappingRepository>();
    private readonly ITravelPerkExcelClient _travelPerkExcel = Substitute.For<ITravelPerkExcelClient>();
    private readonly IStagingRepository<StagingTravelPerkLinea> _travelPerkLineaRepo = Substitute.For<IStagingRepository<StagingTravelPerkLinea>>();
    private readonly ICostCenterRepository _costCenterRepo = Substitute.For<ICostCenterRepository>();

    private SyncService CreateSut() => new(_celero, _bizneo, _intratime, _payhawk, _sgpv, _galan, _mediapost, _travelPerkExcel, _travelPerkLineaRepo, _costCenterRepo, _celeroRepo, _empRepo, _absenceRepo, _ficRepo, _intratimeEmpRepo, _clkReqRepo, _expenseRepo, _gastoRepo, _sgpvRepo, _sgpvProductoRepo, _galanEntradaRepo, _galanSalidaRepo, _galanStockRepo, _mediapostPedidoRepo, _mediapostRecepcionRepo, _userRepo, _serviceRepo, _mappingRepo);

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
                new CeleroVisitaDto("v1", "12345678A", "Proyecto1", "Acción1", new DateOnly(2026, 3, 1), 120, "Madrid", "Madrid", "done"),
                new CeleroVisitaDto("v2", "23456789B", "Proyecto2", "Acción2", new DateOnly(2026, 3, 2), 90, "Barcelona", "Barcelona", "done"),
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
                new CeleroVisitaDto("v1", "12345678A", "Proyecto1", "Acción1", new DateOnly(2026, 3, 1), 120, "Madrid", "Madrid", "done"),
                new CeleroVisitaDto("v2", "23456789B", "Proyecto2", "Acción2", new DateOnly(2026, 3, 2), 90, "Barcelona", "Barcelona", "done"),
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
    public async Task SyncAsync_TravelPerk_SinFichero_DevuelveExitoSinInsertar()
    {
        _travelPerkExcel.GetLineasAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<TravelPerkLineaDto>());

        var sut = CreateSut();
        var result = await sut.SyncAsync("travelperk", CancellationToken.None);

        result.Sistema.Should().Be("travelperk");
        result.Exito.Should().BeTrue();
        result.RegistrosInsertados.Should().Be(0);
    }

    [Fact]
    public async Task SyncAsync_TravelPerk_ImputaPorCeco_YMarcaCecoNoMaestro()
    {
        _travelPerkExcel.GetLineasAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new TravelPerkLineaDto("T1", "Hotels", "0139_INPOST", 60m, null, "EUR", new DateOnly(2026, 5, 10)),
            new TravelPerkLineaDto("T2", "Flights", "9999_DESCONOCIDO", 50m, null, "EUR", new DateOnly(2026, 5, 11)),
            new TravelPerkLineaDto("T3", "Subscription fee", null, 99m, null, "EUR", new DateOnly(2026, 5, 31)),
        });
        _costCenterRepo.GetCecoToServiceMapAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { new CecoServicio("0139_INPOST", 7) });
        _travelPerkLineaRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        List<StagingTravelPerkLinea>? capturadas = null;
        await _travelPerkLineaRepo.AddRangeAsync(
            Arg.Do<IEnumerable<StagingTravelPerkLinea>>(x => capturadas = x.ToList()), Arg.Any<CancellationToken>());

        var sut = CreateSut();
        var result = await sut.SyncAsync("travelperk", CancellationToken.None);

        result.RegistrosInsertados.Should().Be(3);
        capturadas.Should().NotBeNull();

        var hotel = capturadas!.Single(l => l.TripId == "T1");
        hotel.ServiceId.Should().Be(7);                     // CECO casa → imputado al servicio
        hotel.ErrorProcesamiento.Should().BeNull();

        var desconocido = capturadas!.Single(l => l.TripId == "T2");
        desconocido.ServiceId.Should().BeNull();            // CECO no está en la tabla maestra
        desconocido.ErrorProcesamiento.Should().Be(AlertaCodigos.CecoNoMaestro);

        var sub = capturadas!.Single(l => l.TripId == "T3");
        sub.Ceco.Should().Be("0423");                       // sin Cost object → CECO interno SIG
        sub.ServiceId.Should().BeNull();
        sub.ErrorProcesamiento.Should().BeNull();           // gasto interno, NO es CECO no-maestro
    }

    [Fact]
    public async Task SyncAsync_TravelPerk_CecoEstructuralDeSig_EsGastoInterno_NoEsError()
    {
        _travelPerkExcel.GetLineasAsync(Arg.Any<CancellationToken>()).Returns(new[]
        {
            new TravelPerkLineaDto("T1", "Hotels", "0316_COMERCIAL", 80m, null, "EUR", new DateOnly(2026, 5, 10)),
            new TravelPerkLineaDto("T2", "Flights", "9999_DESCONOCIDO", 50m, null, "EUR", new DateOnly(2026, 5, 11)),
        });
        _costCenterRepo.GetCecoToServiceMapAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CecoServicio>());
        _costCenterRepo.GetInternalSigCecoCodesAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { "0315", "0316" });
        _travelPerkLineaRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        List<StagingTravelPerkLinea>? capturadas = null;
        await _travelPerkLineaRepo.AddRangeAsync(
            Arg.Do<IEnumerable<StagingTravelPerkLinea>>(x => capturadas = x.ToList()), Arg.Any<CancellationToken>());

        var sut = CreateSut();
        await sut.SyncAsync("travelperk", CancellationToken.None);

        var estructural = capturadas!.Single(l => l.TripId == "T1");
        estructural.ServiceId.Should().BeNull();            // no es de cliente
        estructural.ErrorProcesamiento.Should().BeNull();   // gasto interno SIG, NO es CECO no-maestro

        var desconocido = capturadas!.Single(l => l.TripId == "T2");
        desconocido.ErrorProcesamiento.Should().Be(AlertaCodigos.CecoNoMaestro);  // sigue siendo error
    }

    [Fact]
    public async Task SyncAsync_SyncResult_ContieneExitoYTimestamp()
    {
        _celero.GetVisitasAsync(Arg.Any<DateOnly>(), Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new CeleroVisitaDto("v1", "12345678A", "Proyecto1", "Acción1", new DateOnly(2026, 3, 1), 120, "Madrid", "Madrid", "done") });
        _celeroRepo.ExistsByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = CreateSut();
        var result = await sut.SyncAsync("celero", CancellationToken.None);

        result.Exito.Should().BeTrue();
        result.MensajeError.Should().BeNull();
        result.FechaUltimaSincronizacion.Should().HaveValue();
        result.FechaUltimaSincronizacion!.Value.Year.Should().Be(DateTime.UtcNow.Year);
    }
}
