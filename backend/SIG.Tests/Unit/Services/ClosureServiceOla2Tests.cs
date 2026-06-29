using SIG.Application.Calculation;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

/// <summary>
/// Ola 2 (#3a) — override manual de líneas e incentivos manuales.
/// Ola 3b (#10): ahora sobre la raíz de COSTES (CierreCostesService); el Total del cierre = suma de sus líneas.
/// </summary>
public class ClosureServiceOla2Tests
{
    private readonly ICierreCostesRepository _repo = Substitute.For<ICierreCostesRepository>();
    private readonly IClosureLineRepository _lineRepo = Substitute.For<IClosureLineRepository>();
    private readonly ICalculationLogRepository _calcLogRepo = Substitute.For<ICalculationLogRepository>();
    private readonly IServiceRepository _serviceRepo = Substitute.For<IServiceRepository>();
    private readonly IPeriodRepository _periodRepo = Substitute.For<IPeriodRepository>();
    private readonly IApprovalRepository _approvalRepo = Substitute.For<IApprovalRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IConceptRepository _conceptRepo = Substitute.For<IConceptRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICalculationEngine _engine = Substitute.For<ICalculationEngine>();
    private readonly IClosureValidationService _validationSvc = Substitute.For<IClosureValidationService>();
    private readonly INotificationService _notificationSvc = Substitute.For<INotificationService>();
    private readonly CierreCostesService _sut;

    public ClosureServiceOla2Tests()
    {
        _repo.Tipo.Returns(TipoCierre.Costes);
        _sut = new CierreCostesService(_repo, _lineRepo, _calcLogRepo, _serviceRepo, _periodRepo, _approvalRepo, _roleRepo, _conceptRepo, _userRepo, _engine, _validationSvc, _notificationSvc);
    }

    private static Period MakePeriod() =>
        new() { Id = 1, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = EstadoPeriodo.Abierto };

    private static CierreCostes MakeClosure(EstadoClosure estado = EstadoClosure.Borrador, uint rowVersion = 1) => new()
    {
        Id = 555, ServiceId = 100, Service = new Service { Id = 100, Nombre = "Serv1", ClientId = 1 },
        PeriodId = 1, Period = MakePeriod(),
        Estado = estado, PasoActual = ApprovalStep.Grupo, RowVersion = rowVersion,
        Lines = new List<ClosureLine>(), Approvals = new List<Approval>()
    };

    private static ClosureLine MakeLine(int id, decimal importe, bool esManual = false) => new()
    {
        Id = id, CierreCostesId = 555, ConceptId = 10, Importe = importe, Tipo = TipoConcepto.Pago,
        DatosEntradaJson = "{}", EsManual = esManual
    };

    // ====================== OVERRIDE ======================

    [Fact]
    public async Task OverrideLineAsync_ClosureNoVisible_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns((CierreCostes?)null);

        await FluentActions.Awaiting(() => _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(50m, "x"), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task OverrideLineAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(MakeClosure(rowVersion: 7));

        await FluentActions.Awaiting(() => _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(50m, "x"), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Theory]
    [InlineData(EstadoClosure.EnAprobacion)]
    [InlineData(EstadoClosure.Aprobado)]
    [InlineData(EstadoClosure.Exportado)]
    public async Task OverrideLineAsync_EstadoNoEditable_LanzaInvalidApprovalTransitionException(EstadoClosure estado)
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(MakeClosure(estado));

        await FluentActions.Awaiting(() => _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(50m, "x"), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    [Fact]
    public async Task OverrideLineAsync_LineaInexistente_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(MakeClosure());
        _lineRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ClosureLine?)null);

        await FluentActions.Awaiting(() => _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(50m, "x"), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task OverrideLineAsync_LineaDeOtroClosure_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(MakeClosure());
        var line = MakeLine(1, 100m);
        line.CierreCostesId = 999; // pertenece a otro cierre
        _lineRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(line);

        await FluentActions.Awaiting(() => _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(50m, "x"), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task OverrideLineAsync_PrimerOverride_GuardaImporteOriginalYMarcaManual()
    {
        var closure = MakeClosure();
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var line = MakeLine(1, 100m);
        _lineRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(line);
        _lineRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<ClosureLine> { line });

        await _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(70m, "Ajuste pactado"), 1, 9, CancellationToken.None);

        line.Importe.Should().Be(70m);
        line.ImporteOriginal.Should().Be(100m);
        line.EsManual.Should().BeTrue();
        line.MotivoManual.Should().Be("Ajuste pactado");
        await _approvalRepo.Received(1).AddHistoryAsync(Arg.Is<ApprovalHistory>(h => h.Accion == "OverrideLinea"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OverrideLineAsync_SegundoOverride_NoSobrescribeImporteOriginal()
    {
        var closure = MakeClosure();
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var line = MakeLine(1, 70m, esManual: true);
        line.ImporteOriginal = 100m;
        _lineRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(line);
        _lineRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<ClosureLine> { line });

        await _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(50m, "Segundo ajuste"), 1, 9, CancellationToken.None);

        line.Importe.Should().Be(50m);
        line.ImporteOriginal.Should().Be(100m, "el importe original solo se captura en el primer override");
    }

    [Fact]
    public async Task OverrideLineAsync_RecalculaTotalDelCierre()
    {
        var closure = MakeClosure();
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var linea1 = MakeLine(1, 100m);
        var linea2 = MakeLine(2, 300m);
        _lineRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(linea1);
        _lineRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(_ => new List<ClosureLine> { linea1, linea2 });

        await _sut.OverrideLineAsync(555, 1, new CierreLineOverrideRequest(40m, "Ajuste"), 1, 9, CancellationToken.None);

        // Total del cierre de costes = suma de sus líneas tras el override.
        closure.Total.Should().Be(340m);
    }

    // ====================== INCENTIVO ======================

    [Fact]
    public async Task AddIncentivoAsync_ClosureNoVisible_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns((CierreCostes?)null);

        await FluentActions.Awaiting(() => _sut.AddIncentivoAsync(555, new CierreLineIncentivoRequest(10, 200m, "Bono", null), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AddIncentivoAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(MakeClosure(rowVersion: 7));

        await FluentActions.Awaiting(() => _sut.AddIncentivoAsync(555, new CierreLineIncentivoRequest(10, 200m, "Bono", null), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Theory]
    [InlineData(EstadoClosure.EnAprobacion)]
    [InlineData(EstadoClosure.Aprobado)]
    public async Task AddIncentivoAsync_EstadoNoEditable_LanzaInvalidApprovalTransitionException(EstadoClosure estado)
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(MakeClosure(estado));

        await FluentActions.Awaiting(() => _sut.AddIncentivoAsync(555, new CierreLineIncentivoRequest(10, 200m, "Bono", null), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    [Fact]
    public async Task AddIncentivoAsync_ConceptoInexistente_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(MakeClosure());
        _conceptRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Concept?)null);

        await FluentActions.Awaiting(() => _sut.AddIncentivoAsync(555, new CierreLineIncentivoRequest(10, 200m, "Bono", null), 1, 9, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AddIncentivoAsync_AltaLineaManualYRecalculaTotal()
    {
        var closure = MakeClosure();
        _repo.GetByIdAndUsuarioIdAsync(555, 9, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _conceptRepo.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns(new Concept { Id = 10, Nombre = "Incentivo", Tipo = TipoConcepto.Pago, FechaDesde = new DateOnly(2025, 1, 1), FormulaJson = "{}" });
        ClosureLine? added = null;
        await _lineRepo.AddAsync(Arg.Do<ClosureLine>(l => added = l), Arg.Any<CancellationToken>());
        _lineRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(_ => new List<ClosureLine> { added! });

        await _sut.AddIncentivoAsync(555, new CierreLineIncentivoRequest(10, 200m, "Bono extra", 42), 1, 9, CancellationToken.None);

        added.Should().NotBeNull();
        added!.EsManual.Should().BeTrue();
        added.Importe.Should().Be(200m);
        added.MotivoManual.Should().Be("Bono extra");
        added.UserId.Should().Be(42);
        added.Tipo.Should().Be(TipoConcepto.Pago);          // costes -> Pago
        added.CierreCostesId.Should().Be(555);
        closure.Total.Should().Be(200m);
        await _approvalRepo.Received(1).AddHistoryAsync(Arg.Is<ApprovalHistory>(h => h.Accion == "AddIncentivo"), Arg.Any<CancellationToken>());
    }
}
