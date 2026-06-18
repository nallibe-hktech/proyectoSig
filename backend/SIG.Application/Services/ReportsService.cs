using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;

namespace SIG.Application.Services;

// Informes nativos (PPT slide 23). NO usa Power BI: agrega en memoria los cierres del año
// (coste+facturación emparejados por servicio+período, como el dashboard) y el Forecast.
public class ReportsService : IReportsService
{
    private readonly ICierreCostesRepository _costesRepo;
    private readonly ICierreFacturacionRepository _facturacionRepo;
    private readonly IPeriodRepository _periodRepo;
    private readonly IForecastRepository _forecastRepo;
    private readonly IDepartmentRepository _departmentRepo;

    public ReportsService(
        ICierreCostesRepository costesRepo, ICierreFacturacionRepository facturacionRepo,
        IPeriodRepository periodRepo, IForecastRepository forecastRepo, IDepartmentRepository departmentRepo)
    {
        _costesRepo = costesRepo;
        _facturacionRepo = facturacionRepo;
        _periodRepo = periodRepo;
        _forecastRepo = forecastRepo;
        _departmentRepo = departmentRepo;
    }

    // Acumulado de un servicio (con sus datos maestros) para un conjunto de cierres.
    private sealed class Acc
    {
        public Service Service = null!;
        public decimal Facturacion;
        public decimal Coste;
        public decimal Margen => Facturacion - Coste;
    }

    public async Task<ReporteResultadoDto> GetResultadoAsync(int anio, int? departmentId, int? clientId, int? serviceId, int usuarioId, CancellationToken ct)
    {
        var periodIds = (await _periodRepo.ListAsync(ct))
            .Where(p => p.FechaInicio.Year == anio)
            .Select(p => p.Id).ToList();

        var porServicio = new Dictionary<int, Acc>();
        foreach (var pid in periodIds)
        {
            foreach (var c in await _costesRepo.ListByPeriodForUserAsync(usuarioId, pid, ct))
                Get(porServicio, c.Service).Coste += c.Total;
            foreach (var f in await _facturacionRepo.ListByPeriodForUserAsync(usuarioId, pid, ct))
                Get(porServicio, f.Service).Facturacion += f.Total;
        }

        var deptNombres = (await _departmentRepo.ListAsync(ct)).ToDictionary(d => d.Id, d => d.Nombre);

        var filas = porServicio.Values
            .Where(a => Pasa(a.Service, departmentId, clientId, serviceId))
            .Select(a => new ReporteResultadoFilaDto(
                a.Service.DepartmentId,
                a.Service.DepartmentId.HasValue && deptNombres.TryGetValue(a.Service.DepartmentId.Value, out var dn) ? dn : null,
                a.Service.ClientId, a.Service.Client?.Nombre ?? "",
                a.Service.Id, a.Service.Nombre,
                a.Facturacion, a.Coste, a.Margen))
            .OrderBy(f => f.DepartmentNombre).ThenBy(f => f.ClientNombre).ThenBy(f => f.ServiceNombre)
            .ToList();

        return new ReporteResultadoDto(anio, filas);
    }

    public async Task<PrevisionRealDto> GetPrevisionVsRealAsync(int anio, int? departmentId, int? clientId, int? serviceId, int usuarioId, CancellationToken ct)
    {
        var periods = (await _periodRepo.ListAsync(ct)).Where(p => p.FechaInicio.Year == anio).ToList();

        // Real por (servicio, mes): mes derivado del inicio del período.
        var realPorServicioMes = new Dictionary<(int ServiceId, int Mes), (decimal Ventas, decimal Coste, Service Svc)>();
        foreach (var period in periods)
        {
            int mes = period.FechaInicio.Month;
            foreach (var c in await _costesRepo.ListByPeriodForUserAsync(usuarioId, period.Id, ct))
                Acumula(realPorServicioMes, (c.ServiceId, mes), coste: c.Total, ventas: 0, c.Service);
            foreach (var f in await _facturacionRepo.ListByPeriodForUserAsync(usuarioId, period.Id, ct))
                Acumula(realPorServicioMes, (f.ServiceId, mes), coste: 0, ventas: f.Total, f.Service);
        }

        // Previsión (Forecast) por (servicio, mes).
        var forecast = await _forecastRepo.ListForResumenAsync(anio, departmentId, clientId, serviceId, ct);

        var deptNombres = (await _departmentRepo.ListAsync(ct)).ToDictionary(d => d.Id, d => d.Nombre);

        // Clave de agrupación de fila: (dpto, cliente).
        var filasMap = new Dictionary<(int? Dept, int Client), Dictionary<int, PrevisionRealCelda>>();
        var serviceMaster = new Dictionary<(int? Dept, int Client), Service>();

        void EnsureFila((int? Dept, int Client) key, Service svc)
        {
            if (!filasMap.ContainsKey(key)) { filasMap[key] = new(); serviceMaster[key] = svc; }
        }
        PrevisionRealCelda Celda(Dictionary<int, PrevisionRealCelda> meses, int mes)
        {
            if (!meses.TryGetValue(mes, out var c)) { c = new PrevisionRealCelda { Mes = mes }; meses[mes] = c; }
            return c;
        }

        foreach (var kv in realPorServicioMes)
        {
            var svc = kv.Value.Svc;
            if (!Pasa(svc, departmentId, clientId, serviceId)) continue;
            var key = (svc.DepartmentId, svc.ClientId);
            EnsureFila(key, svc);
            var celda = Celda(filasMap[key], kv.Key.Mes);
            celda.VentasReales += kv.Value.Ventas;
            celda.MargenReal += kv.Value.Ventas - kv.Value.Coste;
        }
        foreach (var f in forecast)
        {
            var svc = f.Service;
            var key = (svc.DepartmentId, svc.ClientId);
            EnsureFila(key, svc);
            var celda = Celda(filasMap[key], f.Mes);
            celda.VentasPrevistas += f.VentasPrevistas;
            celda.MargenPrevisto += f.MargenPrevisto ?? 0m;
        }

        var filas = filasMap.Select(kv =>
        {
            var svc = serviceMaster[kv.Key];
            var celdas = kv.Value.Values.OrderBy(c => c.Mes)
                .Select(c => new PrevisionRealCeldaDto(c.Mes, c.VentasPrevistas, c.VentasReales, c.MargenPrevisto, c.MargenReal))
                .ToList();
            return new PrevisionRealFilaDto(
                kv.Key.Dept,
                kv.Key.Dept.HasValue && deptNombres.TryGetValue(kv.Key.Dept.Value, out var dn) ? dn : null,
                kv.Key.Client, svc.Client?.Nombre ?? "",
                celdas,
                celdas.Sum(c => c.VentasPrevistas), celdas.Sum(c => c.VentasReales),
                celdas.Sum(c => c.MargenPrevisto), celdas.Sum(c => c.MargenReal));
        })
        .OrderBy(f => f.DepartmentNombre).ThenBy(f => f.ClientNombre)
        .ToList();

        return new PrevisionRealDto(anio, filas);
    }

    private sealed class PrevisionRealCelda
    {
        public int Mes;
        public decimal VentasPrevistas;
        public decimal VentasReales;
        public decimal MargenPrevisto;
        public decimal MargenReal;
    }

    private static void Acumula(
        Dictionary<(int, int), (decimal Ventas, decimal Coste, Service Svc)> map,
        (int, int) key, decimal coste, decimal ventas, Service svc)
    {
        if (map.TryGetValue(key, out var cur))
            map[key] = (cur.Ventas + ventas, cur.Coste + coste, cur.Svc);
        else
            map[key] = (ventas, coste, svc);
    }

    private static Acc Get(Dictionary<int, Acc> map, Service svc)
    {
        if (!map.TryGetValue(svc.Id, out var a)) { a = new Acc { Service = svc }; map[svc.Id] = a; }
        return a;
    }

    private static bool Pasa(Service svc, int? departmentId, int? clientId, int? serviceId) =>
        (!departmentId.HasValue || svc.DepartmentId == departmentId.Value)
        && (!clientId.HasValue || svc.ClientId == clientId.Value)
        && (!serviceId.HasValue || svc.Id == serviceId.Value);
}
