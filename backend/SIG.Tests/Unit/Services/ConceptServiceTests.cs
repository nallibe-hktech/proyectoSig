using SIG.Application.Calculation;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Tests.Unit.Services;

public class ConceptServiceTests
{
    private readonly IConceptRepository _repo = Substitute.For<IConceptRepository>();
    private readonly IFormulaParser _parser = Substitute.For<IFormulaParser>();
    private readonly ConceptService _sut;

    public ConceptServiceTests() { _sut = new ConceptService(_repo, _parser); }

    private static Concept MakeConcept() => new()
    {
        Id = 1,
        Nombre = "Bonus visita",
        Tipo = TipoConcepto.Pago,
        FechaDesde = new DateOnly(2026, 1, 1),
        FormulaJson = """{"type":"Number","value":5}""",
        ActionConcepts = new List<ActionConcept>(),
        ConceptUsers = new List<ConceptUser>()
    };

    [Fact]
    public async Task CreateAsync_FormulaInvalida_LanzaFormulaInvalidException()
    {
        var req = new ConceptCreateRequest("X", TipoConcepto.Pago, new DateOnly(2026, 1, 1), null, "{}", Array.Empty<int>(), Array.Empty<int>());
        _parser.TryValidate(Arg.Any<string>(), out Arg.Any<string[]>())
            .Returns(call => { call[1] = new[] { "JSON mal" }; return false; });

        await FluentActions.Awaiting(() => _sut.CreateAsync(req, 99, CancellationToken.None))
            .Should().ThrowAsync<FormulaInvalidException>();
    }

    [Fact]
    public async Task CreateAsync_FormulaValida_PersisteConcept()
    {
        var req = new ConceptCreateRequest("Nuevo", TipoConcepto.Factura, new DateOnly(2026, 1, 1), null, """{"type":"Number","value":1}""", new[] { 10 }, new[] { 5 });
        _parser.TryValidate(Arg.Any<string>(), out Arg.Any<string[]>())
            .Returns(call => { call[1] = Array.Empty<string>(); return true; });

        var result = await _sut.CreateAsync(req, 99, CancellationToken.None);

        result.Nombre.Should().Be("Nuevo");
        result.Tipo.Should().Be(TipoConcepto.Factura);
        await _repo.Received(1).AddAsync(Arg.Any<Concept>(), Arg.Any<CancellationToken>());
        await _repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ConceptNoEncontrado_LanzaEntityNotFoundException()
    {
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns((Concept?)null);
        var req = new ConceptUpdateRequest("X", TipoConcepto.Pago, new DateOnly(2026, 1, 1), null, "{}", Array.Empty<int>(), Array.Empty<int>());

        await FluentActions.Awaiting(() => _sut.UpdateAsync(1, req, 99, CancellationToken.None))
            .Should().ThrowAsync<EntityNotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ConDependencias_LanzaDependenciesExistException()
    {
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(MakeConcept());
        _repo.HasActionsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        await FluentActions.Awaiting(() => _sut.DeleteAsync(1, 99, CancellationToken.None))
            .Should().ThrowAsync<DependenciesExistException>();
    }

    [Fact]
    public async Task DeleteAsync_SinDependencias_AplicaSoftDelete()
    {
        var c = MakeConcept();
        _repo.GetByIdAndUsuarioIdAsync(1, 99, Arg.Any<CancellationToken>()).Returns(c);
        _repo.HasActionsAsync(1, Arg.Any<CancellationToken>()).Returns(false);

        await _sut.DeleteAsync(1, 99, CancellationToken.None);

        c.IsDeleted.Should().BeTrue();
        c.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidarFormulaAsync_FormulaValida_DevuelveOkTrue()
    {
        _parser.TryValidate(Arg.Any<string>(), out Arg.Any<string[]>())
            .Returns(call => { call[1] = Array.Empty<string>(); return true; });

        var result = await _sut.ValidarFormulaAsync("""{"type":"Number","value":1}""", CancellationToken.None);

        result.Ok.Should().BeTrue();
        result.Errores.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidarFormulaAsync_FormulaInvalida_DevuelveOkFalseConErrores()
    {
        _parser.TryValidate(Arg.Any<string>(), out Arg.Any<string[]>())
            .Returns(call => { call[1] = new[] { "vacía" }; return false; });

        var result = await _sut.ValidarFormulaAsync("", CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Errores.Should().NotBeEmpty();
    }
}
