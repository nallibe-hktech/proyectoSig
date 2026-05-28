using SIG.Application.DTOs;
using SIG.Application.Validators;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Validators;

public class ValidatorTests
{
    [Theory]
    [InlineData("", "Demo#2026!", false)]
    [InlineData("admin@sig.local", "", false)]
    [InlineData("not-email", "Demo#2026!", false)]
    [InlineData("admin@sig.local", "123", false)] // password < 8
    [InlineData("admin@sig.local", "Demo#2026!", true)]
    public void LoginRequestValidator_AplicaReglasEmailYPassword(string email, string password, bool valid)
    {
        var v = new LoginRequestValidator();
        var r = v.Validate(new LoginRequest(email, password));
        r.IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("xyz", true)]
    public void RefreshRequestValidator_RequiereToken(string token, bool valid)
    {
        var r = new RefreshRequestValidator().Validate(new RefreshRequest(token));
        r.IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData("", "A12345678", false)] // nombre vacío
    [InlineData("X", "A12345678", false)] // nombre muy corto
    [InlineData("Alpha", "1234", false)] // NIF muy corto
    [InlineData("Alpha", "A12345678", true)]
    public void ClientCreateRequestValidator_NombreYNif(string nombre, string nif, bool valid)
    {
        var req = new ClientCreateRequest(nombre, nif, null, null, null, null, null, null, null, null);
        new ClientCreateRequestValidator().Validate(req).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData("contacto@ok.com", true)]
    [InlineData("malformed", false)]
    [InlineData(null, true)]
    public void ClientCreateRequestValidator_EmailOpcionalSiNoVacio(string? email, bool valid)
    {
        var req = new ClientCreateRequest("Alpha", "A12345678", null, null, null, null, null, null, email, null);
        new ClientCreateRequestValidator().Validate(req).IsValid.Should().Be(valid);
    }

    [Theory]
    [InlineData("123456", true)]
    [InlineData("12345", false)]
    [InlineData("1234567", false)]
    [InlineData("abcdef", false)]
    public void CostCenterCreateRequestValidator_CodigoSeisDigitos(string codigo, bool valid)
    {
        var req = new CostCenterCreateRequest(codigo, "Nombre");
        new CostCenterCreateRequestValidator().Validate(req).IsValid.Should().Be(valid);
    }

    [Fact]
    public void ConceptCreateRequestValidator_FechaDesdeMayorQueHasta_Invalido()
    {
        var req = new ConceptCreateRequest("X", TipoConcepto.Pago, new DateOnly(2026, 6, 1), new DateOnly(2026, 1, 1), "{}", Array.Empty<int>(), Array.Empty<int>());
        new ConceptCreateRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ConceptCreateRequestValidator_FechaDesdeMenorQueHasta_Valido()
    {
        var req = new ConceptCreateRequest("Concept Demo", TipoConcepto.Pago, new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 1), "{\"x\":1}", Array.Empty<int>(), Array.Empty<int>());
        var r = new ConceptCreateRequestValidator().Validate(req);
        r.IsValid.Should().BeTrue($"errores: {string.Join("; ", r.Errors.Select(e => e.PropertyName + ":" + e.ErrorMessage))}");
    }

    [Fact]
    public void ConceptCreateRequestValidator_FormulaJsonVacio_Invalido()
    {
        var req = new ConceptCreateRequest("X", TipoConcepto.Pago, new DateOnly(2026, 1, 1), null, "", Array.Empty<int>(), Array.Empty<int>());
        new ConceptCreateRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UserCreateRequestValidator_RoleIdsVacio_Invalido()
    {
        var req = new UserCreateRequest("12345678X", "Pepe", "Ruiz", "p@ex.com", "Demo#2026!", EstadoUsuario.Activo, null, Array.Empty<int>());
        new UserCreateRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void UserPasswordChangeRequestValidator_PasswordCorta_Invalido()
    {
        new UserPasswordChangeRequestValidator().Validate(new UserPasswordChangeRequest("123")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void PeriodCreateRequestValidator_FechaInicioMayorQueFin_Invalido()
    {
        var req = new PeriodCreateRequest("Abril 2026", new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1));
        new PeriodCreateRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ClosureRejectRequestValidator_MotivoVacio_Invalido()
    {
        new ClosureRejectRequestValidator().Validate(new ClosureRejectRequest("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ClosureRejectRequestValidator_MotivoOk_Valido()
    {
        new ClosureRejectRequestValidator().Validate(new ClosureRejectRequest("Faltan datos")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void ClosureCreateRequestValidator_IdsCero_Invalido()
    {
        new ClosureCreateRequestValidator().Validate(new ClosureCreateRequest(0, 0, null)).IsValid.Should().BeFalse();
    }
}
