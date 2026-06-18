using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

/// <summary>
/// Ola 2 (#8) — asociar/desasociar conceptos del catálogo a un servicio (ServiceConcept).
/// No crea conceptos; solo gestiona la relación N:M.
/// </summary>
public class ServiceConceptServiceTests
{
    private readonly IServiceRepository _repo = Substitute.For<IServiceRepository>();
    private readonly IClientRepository _clientRepo = Substitute.For<IClientRepository>();
    private readonly IConceptRepository _conceptRepo = Substitute.For<IConceptRepository>();
    private readonly ServiceService _sut;

    public ServiceConceptServiceTests()
    {
        _sut = new ServiceService(_repo, _clientRepo, _conceptRepo);
    }

    private static Service MakeService(params int[] conceptIds)
    {
        var s = new Service
        {
            Id = 100, Nombre = "Serv1", ClientId = 1, Client = new Client { Id = 1, Nombre = "Alpha" },
            Estado = EstadoServicio.Activo, FechaAlta = new DateOnly(2026, 1, 1)
        };
        foreach (var cid in conceptIds)
            s.ServiceConcepts.Add(new ServiceConcept { ServiceId = 100, ConceptId = cid });
        return s;
    }

    private static Concept MakeConcept(int id) => new()
    {
        Id = id, Nombre = $"Concept {id}", Tipo = TipoConcepto.Pago,
        FechaDesde = new DateOnly(2025, 1, 1), FormulaJson = "{}"
    };

    // === AddConcept ===

    [Fact]
    public async Task AddConceptAsync_ServicioInexistente_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(100, 1, Arg.Any<CancellationToken>()).Returns((Service?)null);

        await FluentActions.Awaiting(() => _sut.AddConceptAsync(100, 5, 1, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AddConceptAsync_ConceptoInexistente_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(100, 1, Arg.Any<CancellationToken>()).Returns(MakeService());
        _conceptRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Concept?)null);

        await FluentActions.Awaiting(() => _sut.AddConceptAsync(100, 5, 1, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task AddConceptAsync_Nuevo_AsociaYPersiste()
    {
        var service = MakeService();
        _repo.GetByIdAndUsuarioIdAsync(100, 1, Arg.Any<CancellationToken>()).Returns(service);
        _conceptRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(MakeConcept(5));

        var dto = await _sut.AddConceptAsync(100, 5, 1, CancellationToken.None);

        service.ServiceConcepts.Should().ContainSingle(sc => sc.ConceptId == 5);
        dto.ConceptIds.Should().Contain(5);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddConceptAsync_YaAsociado_NoDuplicaNiVuelveAGuardar()
    {
        var service = MakeService(5);
        _repo.GetByIdAndUsuarioIdAsync(100, 1, Arg.Any<CancellationToken>()).Returns(service);
        _conceptRepo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(MakeConcept(5));

        var dto = await _sut.AddConceptAsync(100, 5, 1, CancellationToken.None);

        service.ServiceConcepts.Count(sc => sc.ConceptId == 5).Should().Be(1);
        dto.ConceptIds.Should().Contain(5);
        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // === RemoveConcept ===

    [Fact]
    public async Task RemoveConceptAsync_ServicioInexistente_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(100, 1, Arg.Any<CancellationToken>()).Returns((Service?)null);

        await FluentActions.Awaiting(() => _sut.RemoveConceptAsync(100, 5, 1, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task RemoveConceptAsync_Existente_QuitaYPersiste()
    {
        var service = MakeService(5, 6);
        _repo.GetByIdAndUsuarioIdAsync(100, 1, Arg.Any<CancellationToken>()).Returns(service);

        var dto = await _sut.RemoveConceptAsync(100, 5, 1, CancellationToken.None);

        service.ServiceConcepts.Should().NotContain(sc => sc.ConceptId == 5);
        dto.ConceptIds.Should().NotContain(5);
        dto.ConceptIds.Should().Contain(6);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveConceptAsync_NoAsociado_NoGuarda()
    {
        var service = MakeService(6);
        _repo.GetByIdAndUsuarioIdAsync(100, 1, Arg.Any<CancellationToken>()).Returns(service);

        await _sut.RemoveConceptAsync(100, 5, 1, CancellationToken.None);

        await _repo.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
