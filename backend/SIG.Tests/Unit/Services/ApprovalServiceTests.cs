using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Services;

// Ola 3b (#10): el panel agrega AMBOS tipos de cierre. Cada item indica su TipoCierre.
public class ApprovalServiceTests
{
    private readonly ICierreCostesRepository _costesRepo = Substitute.For<ICierreCostesRepository>();
    private readonly ICierreFacturacionRepository _factRepo = Substitute.For<ICierreFacturacionRepository>();
    private readonly ICierreCostesService _costesService = Substitute.For<ICierreCostesService>();
    private readonly ICierreFacturacionService _factService = Substitute.For<ICierreFacturacionService>();
    private readonly IApprovalRepository _approvalRepo = Substitute.For<IApprovalRepository>();
    private readonly ApprovalService _sut;

    public ApprovalServiceTests()
    {
        _sut = new ApprovalService(_costesRepo, _factRepo, _costesService, _factService, _approvalRepo);
    }

    private static Service MakeService()
    {
        var client = new Client { Id = 1, Nombre = "Alpha", NIF = "X" };
        return new Service { Id = 100, Nombre = "Proj1", ClientId = 1, Client = client };
    }

    private static Period MakePeriod() =>
        new() { Id = 1, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31) };

    [Fact]
    public async Task ListAsync_AgregaAmbosTiposIndicandoElTipo()
    {
        var filter = new ApprovalFilterRequest(null, null, null, null, null, null, null, null);
        var costes = new CierreCostes
        {
            Id = 555, ServiceId = 100, Service = MakeService(), PeriodId = 1, Period = MakePeriod(),
            Estado = EstadoClosure.EnAprobacion, PasoActual = ApprovalStep.Fico, Total = 800m, UpdatedAt = DateTime.UtcNow
        };
        var fact = new CierreFacturacion
        {
            Id = 777, ServiceId = 100, Service = MakeService(), PeriodId = 1, Period = MakePeriod(),
            Estado = EstadoClosure.EnAprobacion, PasoActual = ApprovalStep.Grupo, Total = 1000m, UpdatedAt = DateTime.UtcNow.AddMinutes(-1)
        };
        _costesRepo.ListPaginatedForUserAsync(99, Arg.Any<ApprovalFilterRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CierreCostes>(new[] { costes }, 1, 1, int.MaxValue));
        _factRepo.ListPaginatedForUserAsync(99, Arg.Any<ApprovalFilterRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CierreFacturacion>(new[] { fact }, 1, 1, int.MaxValue));

        var result = await _sut.ListAsync(filter, 99, CancellationToken.None);

        result.Total.Should().Be(2);
        result.Items.Should().Contain(i => i.CierreId == 555 && i.TipoCierre == TipoCierre.Costes && i.Total == 800m);
        result.Items.Should().Contain(i => i.CierreId == 777 && i.TipoCierre == TipoCierre.Facturacion && i.Total == 1000m);
        result.Items[0].ServiceNombre.Should().Be("Proj1");
        result.Items[0].ClientNombre.Should().Be("Alpha");
    }

    [Fact]
    public async Task ListPendingForUserAsync_AgregaPendientesDeAmbosTipos()
    {
        _costesRepo.ListPendingForUserAsync(7, 1, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CierreCostes>(Array.Empty<CierreCostes>(), 0, 1, int.MaxValue));
        _factRepo.ListPendingForUserAsync(7, 1, int.MaxValue, Arg.Any<CancellationToken>())
            .Returns(new PagedResult<CierreFacturacion>(Array.Empty<CierreFacturacion>(), 0, 1, int.MaxValue));

        var result = await _sut.ListPendingForUserAsync(7, 1, 25, CancellationToken.None);

        result.Total.Should().Be(0);
        await _costesRepo.Received(1).ListPendingForUserAsync(7, 1, int.MaxValue, Arg.Any<CancellationToken>());
        await _factRepo.Received(1).ListPendingForUserAsync(7, 1, int.MaxValue, Arg.Any<CancellationToken>());
    }
}
