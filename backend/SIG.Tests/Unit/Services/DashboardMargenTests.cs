using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;

namespace SIG.Tests.Unit.Services;

/// <summary>
/// Ola 3b (#10) — "Margen al vuelo": el dashboard empareja CierreCostes + CierreFacturacion por
/// (ServiceId, PeriodId) y calcula margen = Total(facturación) − Total(costes). No se almacena.
/// </summary>
public class DashboardMargenTests
{
    private readonly ICierreCostesRepository _costesRepo = Substitute.For<ICierreCostesRepository>();
    private readonly ICierreFacturacionRepository _factRepo = Substitute.For<ICierreFacturacionRepository>();
    private readonly IPeriodRepository _periodRepo = Substitute.For<IPeriodRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly IServiceRepository _serviceRepo = Substitute.For<IServiceRepository>();
    private readonly DashboardService _sut;

    public DashboardMargenTests()
    {
        _sut = new DashboardService(_costesRepo, _factRepo, _periodRepo, _userRepo, _serviceRepo);
    }

    private static Service Svc(int id) => new() { Id = id, Nombre = $"S{id}", ClientId = 1, Client = new Client { Id = 1, Nombre = "Alpha", NIF = "X" } };

    [Fact]
    public async Task GetKpisAsync_MargenEsFacturacionMenosCoste_EmparejandoPorServicioYPeriodo()
    {
        var period = new Period { Id = 7, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = EstadoPeriodo.Cerrado };
        _periodRepo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(period);
        _periodRepo.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<Period> { period });

        // Servicio 100: costes 600, facturación 1000 -> margen 400.
        // Servicio 200: sólo costes 300 (facturación aún pendiente) -> margen -300.
        _costesRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreCostes>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 600m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
            new() { Id = 2, ServiceId = 200, Service = Svc(200), PeriodId = 7, Period = period, Total = 300m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
        });
        _factRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreFacturacion>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 1000m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
        });

        var kpis = await _sut.GetKpisAsync(7, 99, CancellationToken.None);

        kpis.CosteTotal.Should().Be(900m);          // 600 + 300
        kpis.FacturacionTotal.Should().Be(1000m);   // 1000 + 0
        kpis.Margen.Should().Be(100m, "margen global = facturación(1000) − coste(900)");

        // Desglose por cliente (Alpha agrega ambos servicios).
        kpis.DesglosePorCliente.Should().ContainSingle();
        kpis.DesglosePorCliente[0].Margen.Should().Be(100m);
    }

    [Fact]
    public async Task GetMisServiciosAsync_CalculaMargenAlVueloPorServicio()
    {
        var period = new Period { Id = 7, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = EstadoPeriodo.Cerrado };
        _periodRepo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(period);
        _serviceRepo.ListPaginatedForUserAsync(99, 1, int.MaxValue, null, null, Arg.Any<CancellationToken>())
            .Returns(new SIG.Application.Common.PagedResult<Service>(new[] { Svc(100) }, 1, 1, int.MaxValue));

        _costesRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreCostes>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 600m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
        });
        _factRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreFacturacion>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 1000m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
        });

        var items = await _sut.GetMisServiciosAsync(7, 99, CancellationToken.None);

        var s100 = items.Single(i => i.ServiceId == 100);
        s100.CosteTotal.Should().Be(600m);
        s100.FacturacionTotal.Should().Be(1000m);
        s100.Margen.Should().Be(400m, "margen = facturación − coste por (servicio, período)");
    }

    [Fact]
    public async Task GetKpisAsync_DesdoblaContadoresPorTipoDeCierre()
    {
        // PPT slide 3: distinguir cierre de costes vs cierre de facturación.
        var period = new Period { Id = 7, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = EstadoPeriodo.Cerrado };
        _periodRepo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(period);
        _periodRepo.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<Period> { period });

        _costesRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreCostes>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 600m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },     // costes completado
            new() { Id = 2, ServiceId = 200, Service = Svc(200), PeriodId = 7, Period = period, Total = 300m, Estado = EstadoClosure.EnAprobacion, Lines = new List<ClosureLine>() }, // costes pendiente
        });
        _factRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreFacturacion>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 1000m, Estado = EstadoClosure.Borrador, Lines = new List<ClosureLine>() },   // facturación pendiente
        });

        var kpis = await _sut.GetKpisAsync(7, 99, CancellationToken.None);

        kpis.CierresCostesCompletados.Should().Be(1);
        kpis.CierresCostesPendientes.Should().Be(1);
        kpis.CierresFacturacionCompletados.Should().Be(0);
        kpis.CierresFacturacionPendientes.Should().Be(1);
    }

    [Fact]
    public async Task GetKpisAsync_FiltroServicio_SoloComputaEseServicio()
    {
        // PPT slide 3: el filtro de servicio aplica a los KPIs.
        var period = new Period { Id = 7, Nombre = "Marzo 2026", FechaInicio = new DateOnly(2026, 3, 1), FechaFin = new DateOnly(2026, 3, 31), Estado = EstadoPeriodo.Cerrado };
        _periodRepo.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(period);
        _periodRepo.ListAsync(Arg.Any<CancellationToken>()).Returns(new List<Period> { period });

        _costesRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreCostes>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 600m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
            new() { Id = 2, ServiceId = 200, Service = Svc(200), PeriodId = 7, Period = period, Total = 999m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
        });
        _factRepo.ListByPeriodForUserAsync(99, 7, Arg.Any<CancellationToken>()).Returns(new List<CierreFacturacion>
        {
            new() { Id = 1, ServiceId = 100, Service = Svc(100), PeriodId = 7, Period = period, Total = 1000m, Estado = EstadoClosure.Aprobado, Lines = new List<ClosureLine>() },
        });

        var kpis = await _sut.GetKpisAsync(7, 99, CancellationToken.None, serviceId: 100);

        kpis.CosteTotal.Should().Be(600m, "solo el servicio 100");
        kpis.FacturacionTotal.Should().Be(1000m);
        kpis.DesglosePorCliente.Sum(d => d.Coste).Should().Be(600m);
    }
}
