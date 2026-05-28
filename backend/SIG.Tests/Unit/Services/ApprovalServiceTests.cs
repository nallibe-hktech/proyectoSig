using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class ApprovalServiceTests
{
    private readonly IClosureRepository _closureRepo = Substitute.For<IClosureRepository>();
    private readonly IApprovalRepository _approvalRepo = Substitute.For<IApprovalRepository>();
    private readonly IClosureService _closureService = Substitute.For<IClosureService>();
    private readonly ApprovalService _sut;

    public ApprovalServiceTests()
    {
        _sut = new ApprovalService(_closureRepo, _approvalRepo, _closureService);
    }

    [Fact]
    public async Task ListAsync_DevuelveProyeccionItemsConDatosDeNavegacion()
    {
        var filter = new ApprovalFilterRequest(null, null, null, null, null, null, null, null);
        var client = new Client { Id = 1, Nombre = "Alpha", NIF = "X" };
        var project = new Project { Id = 100, Nombre = "Proj1", ClientId = 1, Client = client };
        var period = new Period { Id = 1, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31) };
        var closure = new Closure
        {
            Id = 555, ProjectId = 100, Project = project, PeriodId = 1, Period = period,
            Estado = EstadoClosure.EnAprobacion, PasoActual = ApprovalStep.Backoffice,
            Margen = 1000m, UpdatedAt = DateTime.UtcNow
        };
        _closureRepo.ListPaginatedForUserAsync(99, filter, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Closure>(new[] { closure }, 1, 1, 25));

        var result = await _sut.ListAsync(filter, 99, CancellationToken.None);

        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].ClosureId.Should().Be(555);
        result.Items[0].ProjectNombre.Should().Be("Proj1");
        result.Items[0].ClientNombre.Should().Be("Alpha");
        result.Items[0].PasoActual.Should().Be(ApprovalStep.Backoffice);
        result.Items[0].Margen.Should().Be(1000m);
    }

    [Fact]
    public async Task ListPendingForUserAsync_DevuelveResultadoFiltradoPorRolUser()
    {
        _closureRepo.ListPendingForUserAsync(7, 1, 25, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Closure>(Array.Empty<Closure>(), 0, 1, 25));

        var result = await _sut.ListPendingForUserAsync(7, 1, 25, CancellationToken.None);

        result.Total.Should().Be(0);
        await _closureRepo.Received(1).ListPendingForUserAsync(7, 1, 25, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetHistoryAsync_ClosureNoEncontrado_LanzaEntityNotFoundException()
    {
        _closureRepo.GetByIdAndUsuarioIdAsync(123, 99, Arg.Any<CancellationToken>()).Returns((Closure?)null);

        await FluentActions.Awaiting(() => _sut.GetHistoryAsync(123, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetHistoryAsync_DevuelveHistorialOrdenadoConUsuarioNombre()
    {
        var closure = new Closure { Id = 1, ProjectId = 100, PeriodId = 1 };
        _closureRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(closure);

        var historial = new List<ApprovalHistory>
        {
            new() { Id = 1, ClosureId = 1, UserId = 5, User = new User { Nombre = "Ana", Apellidos = "Gómez" }, PasoOrigen = ApprovalStep.ProjectManager, PasoDestino = ApprovalStep.Backoffice, Accion = "Aprobar", Timestamp = DateTime.UtcNow }
        };
        _approvalRepo.ListHistoryByClosureAsync(1, Arg.Any<CancellationToken>()).Returns(historial);

        var result = await _sut.GetHistoryAsync(1, 99, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].UserNombre.Should().Be("Ana Gómez");
        result[0].Accion.Should().Be("Aprobar");
    }
}
