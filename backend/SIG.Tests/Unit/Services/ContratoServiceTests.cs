using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

/// <summary>
/// Ola 2 (#2) — contratos de un día e "ignorar en cierre".
/// </summary>
public class ContratoServiceTests
{
    private readonly IStagingA3InnuvaContratoRepository _repo = Substitute.For<IStagingA3InnuvaContratoRepository>();
    private readonly ContratoService _sut;

    public ContratoServiceTests()
    {
        _sut = new ContratoService(_repo);
    }

    private static StagingA3InnuvaContrato MakeContrato(int id = 1, bool ignorado = false, string? motivo = null) => new()
    {
        Id = id,
        ContratoIdExterno = $"CT-{id}",
        NIF = "12345678A",
        FechaInicio = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
        FechaFin = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
        ImporteBruto = 1200m,
        IgnoradoEnCierre = ignorado,
        MotivoIgnorar = motivo,
        PayloadJson = "{}",
        Hash = $"hash-{id}"
    };

    [Fact]
    public async Task ListContratosUnDiaAsync_MapeaContratosDelRepositorio()
    {
        _repo.ListContratosUnDiaAsync(Arg.Any<CancellationToken>())
            .Returns(new List<StagingA3InnuvaContrato> { MakeContrato(1), MakeContrato(2) });

        var result = await _sut.ListContratosUnDiaAsync(CancellationToken.None);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.FechaInicio == c.FechaFin);
        result.First().ContratoIdExterno.Should().Be("CT-1");
    }

    [Fact]
    public async Task MarcarIgnorarAsync_ContratoInexistente_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((StagingA3InnuvaContrato?)null);

        await FluentActions.Awaiting(() => _sut.MarcarIgnorarAsync(999, new ContratoIgnorarRequest(true, "x"), CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task MarcarIgnorarAsync_Ignorar_EstableceFlagYMotivoYPersiste()
    {
        var contrato = MakeContrato(5);
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(contrato);

        var dto = await _sut.MarcarIgnorarAsync(5, new ContratoIgnorarRequest(true, "Contrato puntual de un día"), CancellationToken.None);

        contrato.IgnoradoEnCierre.Should().BeTrue();
        contrato.MotivoIgnorar.Should().Be("Contrato puntual de un día");
        dto.IgnoradoEnCierre.Should().BeTrue();
        dto.MotivoIgnorar.Should().Be("Contrato puntual de un día");
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarcarIgnorarAsync_Desmarcar_LimpiaMotivo()
    {
        var contrato = MakeContrato(7, ignorado: true, motivo: "motivo previo");
        _repo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(contrato);

        var dto = await _sut.MarcarIgnorarAsync(7, new ContratoIgnorarRequest(false, "irrelevante"), CancellationToken.None);

        contrato.IgnoradoEnCierre.Should().BeFalse();
        contrato.MotivoIgnorar.Should().BeNull();
        dto.IgnoradoEnCierre.Should().BeFalse();
        dto.MotivoIgnorar.Should().BeNull();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
