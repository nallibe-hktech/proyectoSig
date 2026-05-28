using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class PeriodServiceTests
{
    private readonly IPeriodRepository _repo = Substitute.For<IPeriodRepository>();
    private readonly PeriodService _sut;

    public PeriodServiceTests() { _sut = new PeriodService(_repo); }

    private static Period MakePeriod(int id = 1, EstadoPeriodo estado = EstadoPeriodo.Abierto) =>
        new() { Id = id, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = estado };

    [Fact]
    public async Task ListAsync_DevuelvePeriodosMapeados()
    {
        _repo.ListAsync(Arg.Any<CancellationToken>()).Returns(new[] { MakePeriod(1), MakePeriod(2) });

        var result = await _sut.ListAsync(CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActivoAsync_NoHayActivo_LanzaEntityNotFoundException()
    {
        _repo.GetActivoAsync(Arg.Any<CancellationToken>()).Returns((Period?)null);
        await FluentActions.Awaiting(() => _sut.GetActivoAsync(CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetActivoAsync_DevuelvePeriodoActivo()
    {
        _repo.GetActivoAsync(Arg.Any<CancellationToken>()).Returns(MakePeriod());
        var result = await _sut.GetActivoAsync(CancellationToken.None);
        result.Estado.Should().Be(EstadoPeriodo.Abierto);
    }

    [Fact]
    public async Task CreateAsync_NombreDuplicado_LanzaDuplicateException()
    {
        var req = new PeriodCreateRequest("Abril 2026", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));
        _repo.ExistsByNombreAsync("Abril 2026", null, Arg.Any<CancellationToken>()).Returns(true);

        await FluentActions.Awaiting(() => _sut.CreateAsync(req, CancellationToken.None))
            .Should().ThrowAsync<DuplicateException>();
    }

    [Fact]
    public async Task CreateAsync_PersistePeriodoEnEstadoAbierto()
    {
        var req = new PeriodCreateRequest("Abril 2026", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30));
        _repo.ExistsByNombreAsync("Abril 2026", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(req, CancellationToken.None);

        result.Estado.Should().Be(EstadoPeriodo.Abierto);
        await _repo.Received(1).AddAsync(Arg.Is<Period>(p => p.Nombre == "Abril 2026" && p.Estado == EstadoPeriodo.Abierto), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CerrarAsync_PeriodoNoAbierto_LanzaInvalidApprovalTransition()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakePeriod(1, EstadoPeriodo.Cerrado));
        await FluentActions.Awaiting(() => _sut.CerrarAsync(1, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    [Fact]
    public async Task CerrarAsync_PeriodoAbierto_CambiaEstadoACerrado()
    {
        var p = MakePeriod(1, EstadoPeriodo.Abierto);
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(p);

        var result = await _sut.CerrarAsync(1, CancellationToken.None);

        result.Estado.Should().Be(EstadoPeriodo.Cerrado);
        p.Estado.Should().Be(EstadoPeriodo.Cerrado);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReabrirAsync_PeriodoNoCerrado_LanzaInvalidApprovalTransition()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(MakePeriod(1, EstadoPeriodo.Abierto));
        await FluentActions.Awaiting(() => _sut.ReabrirAsync(1, CancellationToken.None))
            .Should().ThrowAsync<InvalidApprovalTransitionException>();
    }

    [Fact]
    public async Task ReabrirAsync_PeriodoCerrado_CambiaEstadoAAbierto()
    {
        var p = MakePeriod(1, EstadoPeriodo.Cerrado);
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(p);

        var result = await _sut.ReabrirAsync(1, CancellationToken.None);

        result.Estado.Should().Be(EstadoPeriodo.Abierto);
    }
}
