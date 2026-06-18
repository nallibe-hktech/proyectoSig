using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class ClientServiceTests
{
    private readonly IClientRepository _repo = Substitute.For<IClientRepository>();
    private readonly ClientService _sut;

    public ClientServiceTests() { _sut = new ClientService(_repo); }

    private static Client MakeClient(int id = 1) =>
        new() { Id = id, Nombre = "Alpha Foods", NIF = "A12345678", Ciudad = "Madrid", Services = new List<Service>() };

    [Fact]
    public async Task ListAsync_DelegaEnRepositorioYMapeaDtos()
    {
        var c = MakeClient();
        _repo.ListPaginatedForUserAsync(99, 1, 25, null, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Client>(new[] { c }, 1, 1, 25));

        var result = await _sut.ListAsync(99, 1, 25, null, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Nombre.Should().Be("Alpha Foods");
        result.Items[0].NIF.Should().Be("A12345678");
    }

    [Fact]
    public async Task GetByIdAsync_ClienteNoEncontrado_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(123, 99, Arg.Any<CancellationToken>()).Returns((Client?)null);

        await FluentActions.Awaiting(() => _sut.GetByIdAsync(123, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task GetByIdAsync_ClienteEncontrado_DevuelveDetailDto()
    {
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());

        var result = await _sut.GetByIdAsync(1, 99, CancellationToken.None);

        result.Id.Should().Be(1);
        result.Nombre.Should().Be("Alpha Foods");
    }

    [Fact]
    public async Task CreateAsync_NifDuplicado_LanzaDuplicateException()
    {
        var req = new ClientCreateRequest("Nuevo", "A99999999", null, null, null, null, null, null, null, null, null);
        _repo.ExistsByNifAsync("A99999999", null, Arg.Any<CancellationToken>()).Returns(true);

        await FluentActions.Awaiting(() => _sut.CreateAsync(req, 99, CancellationToken.None))
            .Should().ThrowAsync<DuplicateException>();
    }

    [Fact]
    public async Task CreateAsync_NifNuevo_PersisteYDevuelveDetail()
    {
        var req = new ClientCreateRequest("Nuevo", "A99999999", null, "Calle 1", "Madrid", "Madrid", "ES", "28001", "Pepe", "pepe@ex.com", "600");
        _repo.ExistsByNifAsync("A99999999", null, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(req, 99, CancellationToken.None);

        result.Nombre.Should().Be("Nuevo");
        result.NIF.Should().Be("A99999999");
        await _repo.Received(1).AddAsync(Arg.Is<Client>(c => c.Nombre == "Nuevo" && c.NIF == "A99999999"), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ClienteNoEncontrado_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns((Client?)null);

        await FluentActions.Awaiting(() => _sut.UpdateAsync(1, new ClientUpdateRequest("X", "Y", EstadoCliente.Activo, null, null, null, null, null, null, null, null), 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_NifDeOtroCliente_LanzaDuplicateException()
    {
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());
        _repo.ExistsByNifAsync("B22222222", 1, Arg.Any<CancellationToken>()).Returns(true);

        await FluentActions.Awaiting(() => _sut.UpdateAsync(1, new ClientUpdateRequest("Mod", "B22222222", EstadoCliente.Activo, null, null, null, null, null, null, null, null), 99, CancellationToken.None))
            .Should().ThrowAsync<DuplicateException>();
    }

    [Fact]
    public async Task DeleteAsync_ClienteConProyectos_LanzaDependenciesExistException()
    {
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());
        _repo.HasServicesAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await FluentActions.Awaiting(() => _sut.DeleteAsync(1, 99, CancellationToken.None))
            .Should().ThrowAsync<DependenciesExistException>();
    }

    [Fact]
    public async Task DeleteAsync_ClienteSinDependencias_AplicaSoftDelete()
    {
        var c = MakeClient();
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(c);
        _repo.HasServicesAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.DeleteAsync(1, 99, CancellationToken.None);

        c.IsDeleted.Should().BeTrue();
        c.DeletedAt.Should().NotBeNull();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
