using SIG.Application.DTOs;
using SIG.Application.Validators;

namespace SIG.Tests.Unit.Validators;

/// <summary>
/// Ola 2 (#9) — validación de Period.DiaPago. Solo se admiten los valores de fecha de pago
/// 30, 15 y 9; cualquier otro valor (incluyendo 0, 1, 31) hace fallar la validación.
/// El validador convierte el fallo en HTTP 400 vía ValidationFilter (ValidationProblemDetails).
/// </summary>
public class PeriodDiaPagoValidatorTests
{
    private static readonly DateOnly Inicio = new(2026, 4, 1);
    private static readonly DateOnly Fin = new(2026, 4, 30);

    [Theory]
    [InlineData(30, true)]
    [InlineData(15, true)]
    [InlineData(9, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(31, false)]
    [InlineData(10, false)]
    [InlineData(-1, false)]
    public void PeriodCreateRequestValidator_DiaPago_SoloAdmite30_15_9(int diaPago, bool valid)
    {
        var req = new PeriodCreateRequest("Abril 2026", Inicio, Fin, diaPago);
        var r = new PeriodCreateRequestValidator().Validate(req);
        r.IsValid.Should().Be(valid);
        if (!valid)
            r.Errors.Should().Contain(e => e.PropertyName == nameof(PeriodCreateRequest.DiaPago));
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(15, true)]
    [InlineData(9, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(31, false)]
    public void PeriodUpdateRequestValidator_DiaPago_SoloAdmite30_15_9(int diaPago, bool valid)
    {
        var req = new PeriodUpdateRequest("Abril 2026", Inicio, Fin, diaPago);
        var r = new PeriodUpdateRequestValidator().Validate(req);
        r.IsValid.Should().Be(valid);
        if (!valid)
            r.Errors.Should().Contain(e => e.PropertyName == nameof(PeriodUpdateRequest.DiaPago));
    }

    [Fact]
    public void PeriodCreateRequestValidator_DiaPagoValidoPeroFechasInvertidas_Invalido()
    {
        var req = new PeriodCreateRequest("Abril 2026", Fin, Inicio, 30);
        new PeriodCreateRequestValidator().Validate(req).IsValid.Should().BeFalse();
    }
}
