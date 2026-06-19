using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class ClienteIncidenciaServiceTests
{
    private readonly IClienteIncidenciaRepository _repo = Substitute.For<IClienteIncidenciaRepository>();
    private readonly IClientRepository _clientRepo = Substitute.For<IClientRepository>();
    private readonly ClienteIncidenciaService _sut;

    public ClienteIncidenciaServiceTests() { _sut = new ClienteIncidenciaService(_repo, _clientRepo); }

    private static Client MakeClient(int id = 1) => new() { Id = id, Nombre = "Alpha", NIF = "A12345678" };

    private static ClienteIncidencia MakeIncidencia(int id = 5, int clientId = 1) =>
        new() { Id = id, ClientId = clientId, Tipo = "Facturación", Descripcion = "Importe erróneo", Estado = EstadoIncidencia.Abierta };

    [Fact]
    public async Task ListByClientAsync_ClienteInaccesible_LanzaEntityNotFound()
    {
        _clientRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns((Client?)null);

        await FluentActions.Awaiting(() => _sut.ListByClientAsync(1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task ListByClientAsync_Accesible_MapeaDtos()
    {
        _clientRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());
        _repo.ListByClientAsync(1, Arg.Any<CancellationToken>()).Returns(new[] { MakeIncidencia() });

        var result = await _sut.ListByClientAsync(1, 99, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Tipo.Should().Be("Facturación");
        result[0].Estado.Should().Be(EstadoIncidencia.Abierta);
    }

    [Fact]
    public async Task CreateAsync_SinEstado_PorDefectoAbierta_YPersiste()
    {
        _clientRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());
        var req = new ClienteIncidenciaCreateRequest("Logística", "Pedido no entregado", null, null, null);

        var result = await _sut.CreateAsync(1, req, 99, CancellationToken.None);

        result.Estado.Should().Be(EstadoIncidencia.Abierta);
        result.ClientId.Should().Be(1);
        await _repo.Received(1).AddAsync(
            Arg.Is<ClienteIncidencia>(i => i.ClientId == 1 && i.Tipo == "Logística" && i.Estado == EstadoIncidencia.Abierta),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_IncidenciaDeOtroCliente_LanzaEntityNotFound()
    {
        _clientRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(MakeIncidencia(5, clientId: 2));

        await FluentActions.Awaiting(() => _sut.GetByIdAsync(5, 1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_Existente_ActualizaCampos()
    {
        var inc = MakeIncidencia();
        _clientRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(inc);
        var req = new ClienteIncidenciaUpdateRequest("Facturación", "Resuelta con abono", EstadoIncidencia.Resuelta, null);

        var result = await _sut.UpdateAsync(5, 1, req, 99, CancellationToken.None);

        result.Estado.Should().Be(EstadoIncidencia.Resuelta);
        inc.Descripcion.Should().Be("Resuelta con abono");
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Existente_AplicaSoftDelete()
    {
        var inc = MakeIncidencia();
        _clientRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeClient());
        _repo.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(inc);

        await _sut.DeleteAsync(5, 1, 99, CancellationToken.None);

        inc.IsDeleted.Should().BeTrue();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
