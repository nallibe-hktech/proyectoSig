using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class CategoriaFacturaServiceTests
{
    private readonly ICategoriaFacturaRepository _repo = Substitute.For<ICategoriaFacturaRepository>();
    private readonly IClientRepository _clientRepo = Substitute.For<IClientRepository>();
    private readonly CategoriaFacturaService _sut;

    public CategoriaFacturaServiceTests() { _sut = new CategoriaFacturaService(_repo, _clientRepo); }

    private static Client MakeClient(int id = 1) => new() { Id = id, Nombre = "Alpha", NIF = "A12345678" };

    private static Concept MakeConcept(int id, string nombre) =>
        new() { Id = id, Nombre = nombre, Tipo = TipoConcepto.Factura, FormulaJson = "{}" };

    private static CategoriaFactura MakeCategoria(int id, int clientId, string nombre, params Concept[] conceptos) =>
        new()
        {
            Id = id, ClientId = clientId, Nombre = nombre,
            Conceptos = conceptos.Select(c => new CategoriaFacturaConcepto { CategoriaFacturaId = id, ConceptId = c.Id, Concept = c }).ToList()
        };

    private void Accesible(int clientId = 1, int usuarioId = 99) =>
        _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, Arg.Any<CancellationToken>()).Returns(MakeClient(clientId));

    [Fact]
    public async Task ListByClientAsync_ClienteInaccesible_LanzaEntityNotFound()
    {
        _clientRepo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns((Client?)null);

        await FluentActions.Awaiting(() => _sut.ListByClientAsync(1, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task ListByClientAsync_Accesible_MapeaCategoriasConConceptos()
    {
        Accesible();
        _repo.ListByClientAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeCategoria(10, 1, "Servicio de campo", MakeConcept(7, "Facturación por visita")) });

        var result = await _sut.ListByClientAsync(1, 99, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Servicio de campo");
        result[0].Conceptos.Should().ContainSingle(c => c.ConceptId == 7 && c.Nombre == "Facturación por visita");
    }

    [Fact]
    public async Task GetResumenAsync_CuentaMapeadosYSinAsignar()
    {
        Accesible();
        _repo.ListByClientAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeCategoria(10, 1, "Cat A", MakeConcept(7, "C7"), MakeConcept(8, "C8")) });
        _repo.ListConceptosFacturacionDelClienteAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeConcept(7, "C7"), MakeConcept(8, "C8"), MakeConcept(9, "C9") });

        var resumen = await _sut.GetResumenAsync(1, 99, CancellationToken.None);

        resumen.NumCategorias.Should().Be(1);
        resumen.ConceptosMapeados.Should().Be(2);
        resumen.ConceptosSinAsignar.Should().Be(1);
    }

    [Fact]
    public async Task ListConceptosDisponiblesAsync_MarcaAsignados()
    {
        Accesible();
        var c7 = MakeConcept(7, "C7"); var c9 = MakeConcept(9, "C9");
        _repo.ListConceptosFacturacionDelClienteAsync(1, Arg.Any<CancellationToken>()).Returns(new[] { c7, c9 });
        _repo.GetAsignacionesAsync(1, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, CategoriaFactura> { [7] = MakeCategoria(10, 1, "Cat A") });

        var result = await _sut.ListConceptosDisponiblesAsync(1, 99, CancellationToken.None);

        result.Should().HaveCount(2);
        result.First(x => x.ConceptId == 7).Asignado.Should().BeTrue();
        result.First(x => x.ConceptId == 7).CategoriaNombre.Should().Be("Cat A");
        result.First(x => x.ConceptId == 9).Asignado.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Valido_PersisteConConceptos()
    {
        Accesible();
        _repo.ListConceptosFacturacionDelClienteAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeConcept(7, "C7"), MakeConcept(8, "C8") });
        _repo.GetAsignacionesAsync(1, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, CategoriaFactura>());
        _repo.GetByIdWithConceptosAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(MakeCategoria(0, 1, "Gastos de personal", MakeConcept(7, "C7"), MakeConcept(8, "C8")));

        var result = await _sut.CreateAsync(1, new CategoriaFacturaCreateRequest("Gastos de personal", new[] { 7, 8 }), 99, CancellationToken.None);

        result.Nombre.Should().Be("Gastos de personal");
        result.Conceptos.Should().HaveCount(2);
        await _repo.Received(1).AddAsync(
            Arg.Is<CategoriaFactura>(c => c.ClientId == 1 && c.Nombre == "Gastos de personal" && c.Conceptos.Count == 2),
            Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ConceptoNoDelCliente_Lanza409Duplicate()
    {
        Accesible();
        _repo.ListConceptosFacturacionDelClienteAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeConcept(7, "C7") });

        await FluentActions.Awaiting(() =>
                _sut.CreateAsync(1, new CategoriaFacturaCreateRequest("X", new[] { 7, 999 }), 99, CancellationToken.None))
            .Should().ThrowAsync<DuplicateException>();
    }

    [Fact]
    public async Task CreateAsync_ConceptoYaEnOtraCategoria_Lanza409Duplicate()
    {
        Accesible();
        _repo.ListConceptosFacturacionDelClienteAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeConcept(7, "C7") });
        _repo.GetAsignacionesAsync(1, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, CategoriaFactura> { [7] = MakeCategoria(20, 1, "Otra categoría") });

        await FluentActions.Awaiting(() =>
                _sut.CreateAsync(1, new CategoriaFacturaCreateRequest("Nueva", new[] { 7 }), 99, CancellationToken.None))
            .Should().ThrowAsync<DuplicateException>();
    }

    [Fact]
    public async Task UpdateAsync_CategoriaDeOtroCliente_LanzaEntityNotFound()
    {
        Accesible();
        _repo.GetByIdWithConceptosAsync(10, Arg.Any<CancellationToken>()).Returns(MakeCategoria(10, clientId: 2, "Ajena"));

        await FluentActions.Awaiting(() =>
                _sut.UpdateAsync(10, 1, new CategoriaFacturaUpdateRequest("X", Array.Empty<int>()), 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task UpdateAsync_ReemplazaConceptosYNombre()
    {
        Accesible();
        var categoria = MakeCategoria(10, 1, "Vieja", MakeConcept(7, "C7"));
        _repo.GetByIdWithConceptosAsync(10, Arg.Any<CancellationToken>()).Returns(categoria);
        _repo.ListConceptosFacturacionDelClienteAsync(1, Arg.Any<CancellationToken>())
            .Returns(new[] { MakeConcept(7, "C7"), MakeConcept(8, "C8") });
        _repo.GetAsignacionesAsync(1, Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, CategoriaFactura> { [8] = categoria }); // ya asignado a la MISMA categoría → permitido

        await _sut.UpdateAsync(10, 1, new CategoriaFacturaUpdateRequest("Nueva", new[] { 8 }), 99, CancellationToken.None);

        categoria.Nombre.Should().Be("Nueva");
        categoria.Conceptos.Should().ContainSingle(c => c.ConceptId == 8);
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_Existente_AplicaSoftDeleteYLiberaConceptos()
    {
        Accesible();
        var categoria = MakeCategoria(10, 1, "Cat", MakeConcept(7, "C7"));
        _repo.GetByIdWithConceptosAsync(10, Arg.Any<CancellationToken>()).Returns(categoria);

        await _sut.DeleteAsync(10, 1, 99, CancellationToken.None);

        categoria.IsDeleted.Should().BeTrue();
        categoria.Conceptos.Should().BeEmpty();
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
