using SIG.Application.Calculation;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class ClosureServiceTests
{
    private readonly IClosureRepository _repo = Substitute.For<IClosureRepository>();
    private readonly IClosureLineRepository _lineRepo = Substitute.For<IClosureLineRepository>();
    private readonly ICalculationLogRepository _calcLogRepo = Substitute.For<ICalculationLogRepository>();
    private readonly IProjectRepository _projectRepo = Substitute.For<IProjectRepository>();
    private readonly IPeriodRepository _periodRepo = Substitute.For<IPeriodRepository>();
    private readonly IApprovalRepository _approvalRepo = Substitute.For<IApprovalRepository>();
    private readonly IRoleRepository _roleRepo = Substitute.For<IRoleRepository>();
    private readonly IConceptRepository _conceptRepo = Substitute.For<IConceptRepository>();
    private readonly ICalculationEngine _engine = Substitute.For<ICalculationEngine>();
    private readonly ClosureService _sut;

    public ClosureServiceTests()
    {
        _sut = new ClosureService(_repo, _lineRepo, _calcLogRepo, _projectRepo, _periodRepo, _approvalRepo, _roleRepo, _conceptRepo, _engine);
    }

    private static Period MakePeriod(EstadoPeriodo estado = EstadoPeriodo.Abierto) =>
        new() { Id = 1, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = estado };

    private static Project MakeProject() => new() { Id = 100, Nombre = "Proj1", ClientId = 1 };

    private static Closure MakeClosure(EstadoClosure estado = EstadoClosure.Borrador, ApprovalStep paso = ApprovalStep.ProjectManager, uint rowVersion = 1) => new()
    {
        Id = 555, ProjectId = 100, Project = MakeProject(),
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

        await FluentActions.Awaiting(() => _sut.CreateAsync(new ClosureCreateRequest(100, 99, null), 1, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task CreateAsync_PeriodoNoAbierto_LanzaPeriodClosedException()
    {
        _periodRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakePeriod(EstadoPeriodo.Cerrado));

        await FluentActions.Awaiting(() => _sut.CreateAsync(new ClosureCreateRequest(100, 1, null), 1, CancellationToken.None))
            .Should().ThrowAsync<PeriodClosedException>();
    }

    [Fact]
    public async Task CreateAsync_ClosureYaExisteParaProjectPeriod_LanzaDuplicateException()
    {
        _periodRepo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakePeriod());
        _repo.GetByProjectAndPeriodAsync(100, 1, Arg.Any<CancellationToken>()).Returns(MakeClosure());

        await FluentActions.Awaiting(() => _sut.CreateAsync(new ClosureCreateRequest(100, 1, null), 1, CancellationToken.None))
            .Should().ThrowAsync<DuplicateException>();
    }

    // === RECALC ===

    [Fact]
    public async Task RecalcAsync_ClosureNoVisibleParaUsuario_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns((Closure?)null);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new ClosureRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task RecalcAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        var closure = MakeClosure(rowVersion: 5);
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new ClosureRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task RecalcAsync_PeriodoCerrado_LanzaPeriodClosedException()
    {
        var closure = MakeClosure();
        closure.Period = MakePeriod(EstadoPeriodo.Cerrado);
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new ClosureRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<PeriodClosedException>();
    }

    [Fact]
    public async Task RecalcAsync_EstadoAprobado_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure(EstadoClosure.Aprobado);
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RecalcAsync(555, new ClosureRecalcRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    // === APPROVE — flujo secuencial PM → Backoffice → Fico → Direction → SystemExports ===

    [Fact]
    public async Task ApproveAsync_ClosureNoEncontrado_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns((Closure?)null);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new ClosureApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task ApproveAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        var closure = MakeClosure(rowVersion: 5);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new ClosureApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task ApproveAsync_ClosureYaAprobado_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure(EstadoClosure.Aprobado);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new ClosureApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    [Fact]
    public async Task ApproveAsync_PMAprueba_AvanzaABackofficeYCreaApprovalSiguiente()
    {
        var closure = MakeClosure(EstadoClosure.Borrador, ApprovalStep.ProjectManager);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 1, ClosureId = 555, Paso = ApprovalStep.ProjectManager, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });
        _roleRepo.GetByNombreAsync("Backoffice", Arg.Any<CancellationToken>()).Returns(new Role { Id = 2, Nombre = "Backoffice" });

        await _sut.ApproveAsync(555, new ClosureApproveRequest(null), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.Backoffice);
        closure.Estado.Should().Be(EstadoClosure.EnAprobacion);
        current.Estado.Should().Be(EstadoApproval.Aprobado);
        current.UserId.Should().Be(99);
        await _approvalRepo.Received(1).AddAsync(Arg.Is<Approval>(a => a.Paso == ApprovalStep.Backoffice && a.Estado == EstadoApproval.Pendiente), Arg.Any<CancellationToken>());
        await _approvalRepo.Received(1).AddHistoryAsync(Arg.Is<ApprovalHistory>(h => h.PasoOrigen == ApprovalStep.ProjectManager && h.PasoDestino == ApprovalStep.Backoffice && h.Accion == "Aprobar"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveAsync_BackofficeAprueba_AvanzaAFico()
    {
        var closure = MakeClosure(EstadoClosure.EnAprobacion, ApprovalStep.Backoffice);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 2, ClosureId = 555, Paso = ApprovalStep.Backoffice, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });
        _roleRepo.GetByNombreAsync("Fico", Arg.Any<CancellationToken>()).Returns(new Role { Id = 3, Nombre = "Fico" });

        await _sut.ApproveAsync(555, new ClosureApproveRequest(null), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.Fico);
        closure.Estado.Should().Be(EstadoClosure.EnAprobacion);
    }

    [Fact]
    public async Task ApproveAsync_DirectionApruebaPasoFinal_AvanzaASystemExportsConEstadoAprobado()
    {
        var closure = MakeClosure(EstadoClosure.EnAprobacion, ApprovalStep.Direction);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 4, ClosureId = 555, Paso = ApprovalStep.Direction, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });

        await _sut.ApproveAsync(555, new ClosureApproveRequest(null), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.SystemExports);
        closure.Estado.Should().Be(EstadoClosure.Aprobado);
    }

    [Fact]
    public async Task ApproveAsync_SinApprovalPendiente_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure();
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _approvalRepo.GetCurrentByClosureAsync(555, Arg.Any<CancellationToken>()).Returns((Approval?)null);

        await FluentActions.Awaiting(() => _sut.ApproveAsync(555, new ClosureApproveRequest(null), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    // === REJECT ===

    [Fact]
    public async Task RejectAsync_BackofficeRechaza_RegresaAPMConEstadoRechazado()
    {
        var closure = MakeClosure(EstadoClosure.EnAprobacion, ApprovalStep.Backoffice);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 2, ClosureId = 555, Paso = ApprovalStep.Backoffice, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });
        _roleRepo.GetByNombreAsync("ProjectManager", Arg.Any<CancellationToken>()).Returns(new Role { Id = 1, Nombre = "ProjectManager" });

        await _sut.RejectAsync(555, new ClosureRejectRequest("Faltan datos"), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.ProjectManager);
        closure.Estado.Should().Be(EstadoClosure.Rechazado);
        current.Estado.Should().Be(EstadoApproval.Rechazado);
        current.Motivo.Should().Be("Faltan datos");
        await _approvalRepo.Received(1).AddHistoryAsync(Arg.Is<ApprovalHistory>(h => h.Accion == "Rechazar" && h.Motivo == "Faltan datos"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RejectAsync_PMRechaza_PermaneceEnPMConEstadoRechazado()
    {
        var closure = MakeClosure(EstadoClosure.Borrador, ApprovalStep.ProjectManager);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        _repo.GetByIdWithLinesAsync(555, Arg.Any<CancellationToken>()).Returns(closure);
        var current = new Approval { Id = 1, ClosureId = 555, Paso = ApprovalStep.ProjectManager, Estado = EstadoApproval.Pendiente };
        _approvalRepo.GetCurrentByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(current);
        _approvalRepo.ListByClosureAsync(555, Arg.Any<CancellationToken>()).Returns(new List<Approval> { current });
        _roleRepo.GetByNombreAsync("ProjectManager", Arg.Any<CancellationToken>()).Returns(new Role { Id = 1, Nombre = "ProjectManager" });

        await _sut.RejectAsync(555, new ClosureRejectRequest("Corrige fórmula"), 1, 99, CancellationToken.None);

        closure.PasoActual.Should().Be(ApprovalStep.ProjectManager);
        closure.Estado.Should().Be(EstadoClosure.Rechazado);
    }

    [Fact]
    public async Task RejectAsync_RowVersionDistinta_LanzaConcurrencyConflictException()
    {
        var closure = MakeClosure(rowVersion: 5);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RejectAsync(555, new ClosureRejectRequest("X"), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<ConcurrencyConflictException>();
    }

    [Fact]
    public async Task RejectAsync_ClosureYaAprobado_LanzaInvalidApprovalTransitionException()
    {
        var closure = MakeClosure(EstadoClosure.Aprobado);
        _repo.GetByIdAsync(555, Arg.Any<CancellationToken>()).Returns(closure);

        await FluentActions.Awaiting(() => _sut.RejectAsync(555, new ClosureRejectRequest("X"), 1, 99, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    // === GET ===

    [Fact]
    public async Task GetByIdForUserAsync_NoVisible_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(555, 99, Arg.Any<CancellationToken>()).Returns((Closure?)null);

        await FluentActions.Awaiting(() => _sut.GetByIdForUserAsync(555, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }
}
