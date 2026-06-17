using SIG.Application.Calculation;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

// Ola 3b (#10): el antiguo ClosureService se ha dividido. Estos tests verifican el flujo de aprobación
// (Grupo→FICO) y la autorización por paso sobre la raíz de COSTES (CierreCostesService). La lógica es común
// a ambas raíces (CierreServiceBase<TCierre>), por lo que basta con cubrir una.
public class ClosureServiceTests
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
    private readonly CierreCostesService _sut;

    public ClosureServiceTests()
    {
        _repo.Tipo.Returns(TipoCierre.Costes);
        _sut = new CierreCostesService(_repo, _lineRepo, _calcLogRepo, _serviceRepo, _periodRepo, _approvalRepo, _roleRepo, _conceptRepo, _userRepo, _engine, _validationSvc);
        // Por defecto el actor (usuarioId=99) es Administrator (autorizado en cualquier paso).
        _userRepo.ListRoleNamesForUserAsync(99, Arg.Any<CancellationToken>()).Returns(new List<string> { "Administrator" });
        _userRepo.ListServiceIdsForUserAsync(99, Arg.Any<CancellationToken>()).Returns(new List<int> { 100 });
    }

    private static Period MakePeriod(EstadoPeriodo estado = EstadoPeriodo.Abierto) =>
        new() { Id = 1, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = estado };

    private static Service MakeService() => new() { Id = 100, Nombre = "Serv1", ClientId = 1 };

    private static CierreCostes MakeClosure(EstadoClosure estado = EstadoClosure.Borrador, ApprovalStep paso = ApprovalStep.Grupo, uint rowVersion = 1) => new()
    {
        Id = 555, ServiceId = 100, Service = MakeService(),
        PeriodId = 1, Period = MakePeriod(),
        Estado = estado, PasoActual = paso, RowVersion = rowVersion,
        Lines = new List<ClosureLine>(),
        Approvals = new List<Approval>()
    };

    // === CREATE ===

    [Fact]
    public async Task CreateAsync_PeriodoNoEncontrado_LanzaEntityNotFoundException()
    {
        _periodRepo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Period?)null);

        await FluentActions.Awaiting(() => _sut.CreateAsync(new CierreCreateRequest(100, 99, null), 1, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_PeriodoNoAbierto_LanzaPeriodClosedException()
    {
        _periodRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakePeriod(EstadoPeriodo.Cerrado));

        await FluentActions.Awaiting(() => _sut.CreateAsync(new CierreCreateRequest(100, 1, null), 1, CancellationToken.None))
            .Should().ThrowAsync<PeriodClosedException>();
    }

    [Fact]
    public async Task CreateAsync_ClosureYaExisteParaProjectPeriod_LanzaDuplicateException()
    {
        _periodRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakePeriod());
        _repo.GetByServiceAndPeriodAsync(100, 1, Arg.Any<CancellationToken>()).Returns(MakeClosure());

        await FluentActions.Awaiting(() => _sut.CreateAsync(new CierreCreateRequest(100, 1, null), 1, CancellationToken.None))
            .Should().ThrowAsync<DuplicateException>();
    }

    // === RECALC ===

    [Fact]
    public async Task RecalcAsync_ClosureNoVisibleParaUsuario_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns((CierreCostes?)null);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new CierreRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task RecalcAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        var closure = MakeClosure(rowVersion: 5);
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new CierreRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task RecalcAsync_PeriodoCerrado_LanzaPeriodClosedException()
    {
        var closure = MakeClosure();
        closure.Period = MakePeriod(EstadoPeriodo.Cerrado);
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new CierreRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<PeriodClosedException>();
    }

    [Fact]
    public async Task RecalcAsync_EstadoAprobado_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure(EstadoClosure.Aprobado);
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new CierreRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    // === APPROVE — flujo Grupo → Fico → Exportado (Ola 3a #1) ===

    [Fact]
    public async Task ApproveAsync_ClosureNoEncontrado_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns((CierreCostes?)null);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task ApproveAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        var closure = MakeClosure(rowVersion: 5);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task ApproveAsync_ClosureYaAprobado_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure(EstadoClosure.Aprobado);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    [Fact]
    public async Task ApproveAsync_GrupoAprueba_AvanzaAFicoYCreaApprovalSiguiente()
    {
        var closure = MakeClosure(EstadoClosure.Borrador, ApprovalStep.Grupo);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 1, CierreCostesId = 555, Paso = ApprovalStep.Grupo, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });
        _roleRepo.GetByNombreAsync("Fico", Arg.Any<CancellationToken>()).Returns(new Role { Id = 3, Nombre = "Fico" });

        await _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.Fico);
        closure.Estado.Should().Be(EstadoClosure.EnAprobacion);
        current.Estado.Should().Be(EstadoApproval.Aprobado);
        current.UserId.Should().Be(99);
        await _approvalRepo.Received(1).AddAsync(Arg.Is<Approval>(a => a.Paso == ApprovalStep.Fico && a.Estado == EstadoApproval.Pendiente && a.CierreCostesId == 555), Arg.Any<CancellationToken>());
        await _approvalRepo.Received(1).AddHistoryAsync(Arg.Is<ApprovalHistory>(h => h.PasoOrigen == ApprovalStep.Grupo && h.PasoDestino == ApprovalStep.Fico && h.Accion == "Aprobar"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_FicoApruebaPasoFinal_AvanzaASystemExportsConEstadoAprobadoYSinNuevoApproval()
    {
        var closure = MakeClosure(EstadoClosure.EnAprobacion, ApprovalStep.Fico);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 2, CierreCostesId = 555, Paso = ApprovalStep.Fico, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });

        await _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.SystemExports);
        closure.Estado.Should().Be(EstadoClosure.Aprobado);
        await _approvalRepo.DidNotReceive().AddAsync(Arg.Any<Approval>(), Arg.Any<CancellationToken>());
        await _approvalRepo.Received(1).AddHistoryAsync(Arg.Is<ApprovalHistory>(h => h.PasoOrigen == ApprovalStep.Fico && h.PasoDestino == ApprovalStep.SystemExports && h.Accion == "Aprobar"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_SinApprovalPendiente_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure();
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns((Approval?)null);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    // === AUTORIZACIÓN POR PASO (rol global + asignación al servicio) ===

    [Fact]
    public async Task ApproveAsync_PasoGrupo_UsuarioSinRolDeGrupoNiAsignacion_LanzaNotOwner()
    {
        var closure = MakeClosure(EstadoClosure.Borrador, ApprovalStep.Grupo);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 1, CierreCostesId = 555, Paso = ApprovalStep.Grupo, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _userRepo.ListRoleNamesForUserAsync(50, Arg.Any<CancellationToken>()).Returns(new List<string> { "Reader" });
        _userRepo.ListServiceIdsForUserAsync(50, Arg.Any<CancellationToken>()).Returns(new List<int>());

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 50, CancellationToken.None))
            .Should().ThrowAsync<NotOwnerException>();
    }

    [Fact]
    public async Task ApproveAsync_PasoGrupo_RolGrupoPeroNoAsignadoAlServicio_LanzaNotOwner()
    {
        var closure = MakeClosure(EstadoClosure.Borrador, ApprovalStep.Grupo);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 1, CierreCostesId = 555, Paso = ApprovalStep.Grupo, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _userRepo.ListRoleNamesForUserAsync(51, Arg.Any<CancellationToken>()).Returns(new List<string> { "Gestor" });
        _userRepo.ListServiceIdsForUserAsync(51, Arg.Any<CancellationToken>()).Returns(new List<int> { 200 });

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 51, CancellationToken.None))
            .Should().ThrowAsync<NotOwnerException>();
    }

    [Fact]
    public async Task ApproveAsync_PasoGrupo_RolGrupoYAsignadoAlServicio_Aprueba()
    {
        var closure = MakeClosure(EstadoClosure.Borrador, ApprovalStep.Grupo);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 1, CierreCostesId = 555, Paso = ApprovalStep.Grupo, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });
        _roleRepo.GetByNombreAsync("Fico", Arg.Any<CancellationToken>()).Returns(new Role { Id = 3, Nombre = "Fico" });
        _userRepo.ListRoleNamesForUserAsync(52, Arg.Any<CancellationToken>()).Returns(new List<string> { "Facilitador" });
        _userRepo.ListServiceIdsForUserAsync(52, Arg.Any<CancellationToken>()).Returns(new List<int> { 100 });

        await _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 52, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.Fico);
    }

    [Fact]
    public async Task ApproveAsync_PasoFico_NoFico_LanzaNotOwner()
    {
        var closure = MakeClosure(EstadoClosure.EnAprobacion, ApprovalStep.Fico);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 2, CierreCostesId = 555, Paso = ApprovalStep.Fico, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _userRepo.ListRoleNamesForUserAsync(53, Arg.Any<CancellationToken>()).Returns(new List<string> { "Gestor" });
        _userRepo.ListServiceIdsForUserAsync(53, Arg.Any<CancellationToken>()).Returns(new List<int> { 100 });

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 53, CancellationToken.None))
            .Should().ThrowAsync<NotOwnerException>();
    }

    [Fact]
    public async Task ApproveAsync_PasoFico_RolFico_Aprueba()
    {
        var closure = MakeClosure(EstadoClosure.EnAprobacion, ApprovalStep.Fico);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 2, CierreCostesId = 555, Paso = ApprovalStep.Fico, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });
        _userRepo.ListRoleNamesForUserAsync(54, Arg.Any<CancellationToken>()).Returns(new List<string> { "Fico" });

        await _sut.ApproveAsync(555, new CierreApproveRequest(null), 1, 54, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.SystemExports);
        closure.Estado.Should().Be(EstadoClosure.Aprobado);
    }

    // === REJECT — Grupo permanece en Grupo; Fico vuelve a Grupo ===

    [Fact]
    public async Task RejectAsync_FicoRechaza_RegresaAGrupoConEstadoRechazado()
    {
        var closure = MakeClosure(EstadoClosure.EnAprobacion, ApprovalStep.Fico);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 2, CierreCostesId = 555, Paso = ApprovalStep.Fico, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });

        await _sut.RejectAsync(555, new CierreRejectRequest("Faltan datos"), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.Grupo);
        closure.Estado.Should().Be(EstadoClosure.Rechazado);
        current.Estado.Should().Be(EstadoApproval.Rechazado);
        current.Motivo.Should().Be("Faltan datos");
        await _approvalRepo.Received(1).AddAsync(Arg.Is<Approval>(a => a.Paso == ApprovalStep.Grupo && a.Estado == EstadoApproval.Pendiente && a.RoleId == null && a.CierreCostesId == 555), Arg.Any<CancellationToken>());
        await _approvalRepo.Received(1).AddHistoryAsync(Arg.Is<ApprovalHistory>(h => h.PasoOrigen == ApprovalStep.Fico && h.PasoDestino == ApprovalStep.Grupo && h.Accion == "Rechazar" && h.Motivo == "Faltan datos"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_GrupoRechaza_PermaneceEnGrupoConEstadoRechazado()
    {
        var closure = MakeClosure(EstadoClosure.Borrador, ApprovalStep.Grupo);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 1, CierreCostesId = 555, Paso = ApprovalStep.Grupo, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByCierreAsync(TipoCierre.Costes, 555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });

        await _sut.RejectAsync(555, new CierreRejectRequest("Corrige fórmula"), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.Grupo);
        closure.Estado.Should().Be(EstadoClosure.Rechazado);
    }

    [Fact]
    public async Task RejectAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        var closure = MakeClosure(rowVersion: 5);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RejectAsync(555, new CierreRejectRequest("X"), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task RejectAsync_ClosureYaAprobado_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure(EstadoClosure.Aprobado);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RejectAsync(555, new CierreRejectRequest("X"), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    // === GET ===

    [Fact]
    public async Task GetByIdForUserAsync_NoVisible_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns((CierreCostes?)null);

        await FluentActions.Awaiting(() => _sut.GetByIdForUserAsync(555, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }
}
