using System.Text.Json;
using SIG.Application.Alerts;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Integrations;
using SIG.Application.Interfaces.Integrations;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Ola 3b (#10): el margen y los KPIs se calculan AL VUELO emparejando CierreCostes + CierreFacturacion
// del mismo (ServiceId, PeriodId): margen = Total(facturacion) − Total(costes).
public class DashboardService : IDashboardService
{
    private readonly ICierreCostesRepository _costesRepo;
    private readonly ICierreFacturacionRepository _facturacionRepo;
    private readonly IPeriodRepository _periodRepo;
    private readonly IUserRepository _userRepo;
    private readonly IServiceRepository _serviceRepo;

    public DashboardService(ICierreCostesRepository costesRepo, ICierreFacturacionRepository facturacionRepo,
        IPeriodRepository periodRepo, IUserRepository userRepo, IServiceRepository serviceRepo)
    {
        _costesRepo = costesRepo;
        _facturacionRepo = facturacionRepo;
        _periodRepo = periodRepo;
        _userRepo = userRepo;
        _serviceRepo = serviceRepo;
    }

    // Empareja costes + facturación por (ServiceId, PeriodId) en un período.
    private sealed class Pair
    {
        public int ServiceId;
        public Service? Service;
        public CierreCostes? Costes;
        public CierreFacturacion? Facturacion;
        public decimal Coste => Costes?.Total ?? 0;
        public decimal Factura => Facturacion?.Total ?? 0;
        public decimal Margen => Factura - Coste;   // "Margen al vuelo"
    }

    private async Task<List<Pair>> BuildPairsAsync(int usuarioId, int periodId, CancellationToken ct)
    {
        var costes = await _costesRepo.ListByPeriodForUserAsync(usuarioId, periodId, ct);
        var fact = await _facturacionRepo.ListByPeriodForUserAsync(usuarioId, periodId, ct);
        var byService = new Dictionary<int, Pair>();
        foreach (var c in costes)
        {
            var p = GetOrAdd(byService, c.ServiceId);
            p.Costes = c; p.Service ??= c.Service;
        }
        foreach (var f in fact)
        {
            var p = GetOrAdd(byService, f.ServiceId);
            p.Facturacion = f; p.Service ??= f.Service;
        }
        return byService.Values.ToList();
    }

    private static Pair GetOrAdd(Dictionary<int, Pair> map, int serviceId)
    {
        if (!map.TryGetValue(serviceId, out var p)) { p = new Pair { ServiceId = serviceId }; map[serviceId] = p; }
        return p;
    }

    public async Task<DashboardKpisDto> GetKpisAsync(int? periodId, int usuarioId, CancellationToken ct, int? serviceId = null)
    {
        Period? period = periodId.HasValue ? await _periodRepo.GetByIdAsync(periodId.Value, ct)
                                            : await _periodRepo.GetActivoAsync(ct);
        if (period is null) return new DashboardKpisDto(0, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new List<KpiClienteDto>(), new List<EvolucionPeriodoDto>());

        var pairs = await BuildPairsAsync(usuarioId, period.Id, ct);
        // PPT slide 3: el filtro de servicio aplica a los KPIs del período.
        if (serviceId.HasValue) pairs = pairs.Where(p => p.ServiceId == serviceId.Value).ToList();

        bool Completado(EstadoClosure? e) => e is EstadoClosure.Aprobado or EstadoClosure.Exportado;
        bool Pendiente(EstadoClosure? e) => e is EstadoClosure.EnAprobacion or EstadoClosure.Borrador or EstadoClosure.Rechazado;

        // Un par cuenta como completado si ambos cierres existentes lo están; pendiente si alguno lo está.
        int completados = pairs.Count(p => (p.Costes == null || Completado(p.Costes.Estado)) && (p.Facturacion == null || Completado(p.Facturacion.Estado)) && (p.Costes != null || p.Facturacion != null));
        int pendientes = pairs.Count(p => Pendiente(p.Costes?.Estado) || Pendiente(p.Facturacion?.Estado));

        // PPT slide 3: contadores separados por tipo de cierre.
        int costesCompletados = pairs.Count(p => p.Costes != null && Completado(p.Costes.Estado));
        int costesPendientes = pairs.Count(p => p.Costes != null && Pendiente(p.Costes.Estado));
        int facturacionCompletados = pairs.Count(p => p.Facturacion != null && Completado(p.Facturacion.Estado));
        int facturacionPendientes = pairs.Count(p => p.Facturacion != null && Pendiente(p.Facturacion.Estado));
        decimal fact = pairs.Sum(p => p.Factura);
        decimal coste = pairs.Sum(p => p.Coste);
        decimal margen = fact - coste;
        decimal margenPct = fact > 0 ? Math.Round(margen / fact * 100, 1) : 0;

        var desglose = pairs
            .Where(p => p.Service?.Client != null)
            .GroupBy(p => new { p.Service!.ClientId, p.Service.Client!.Nombre })
            .Select(g => {
                var f = g.Sum(p => p.Factura);
                var co = g.Sum(p => p.Coste);
                return new KpiClienteDto(g.Key.ClientId, g.Key.Nombre,
                    f, co, f - co, fact > 0 ? Math.Round(f / fact * 100, 1) : 0);
            })
            .OrderByDescending(x => x.Facturacion)
            .Take(6)
            .ToList();

        var allPeriods = (await _periodRepo.ListAsync(ct))
            .Where(p => p.Estado != EstadoPeriodo.Abierto)
            .OrderByDescending(p => p.Id)
            .Take(6)
            .Reverse()
            .ToList();

        var evolucion = new List<EvolucionPeriodoDto>();
        foreach (var p in allPeriods)
        {
            var pPairs = await BuildPairsAsync(usuarioId, p.Id, ct);
            var pFact = pPairs.Sum(x => x.Factura);
            var pCoste = pPairs.Sum(x => x.Coste);
            evolucion.Add(new EvolucionPeriodoDto(p.Nombre, pFact, pCoste, pFact - pCoste));
        }

        return new DashboardKpisDto(period.Id, period.Nombre, completados, pendientes,
            costesCompletados, costesPendientes, facturacionCompletados, facturacionPendientes,
            fact, coste, margen, margenPct, desglose, evolucion);
    }

    public async Task<IReadOnlyList<DashboardAvisoDto>> GetAvisosAsync(int usuarioId, CancellationToken ct)
    {
        var avisos = new List<DashboardAvisoDto>();
        var unbounded = new ApprovalFilterRequest(null, null, null, null, null, null, null, null, 1, int.MaxValue);
        var costes = await _costesRepo.ListPaginatedForUserAsync(usuarioId, unbounded, ct);
        var fact = await _facturacionRepo.ListPaginatedForUserAsync(usuarioId, unbounded, ct);

        void AvisosFor<TC>(IEnumerable<TC> items, string etiqueta) where TC : ICierre
        {
            foreach (var c in items.Where(x => x.Estado == EstadoClosure.EnAprobacion || x.Estado == EstadoClosure.Borrador).Take(10))
                avisos.Add(new DashboardAvisoDto("CierrePendiente", $"Cierre {etiqueta} #{c.Id} {c.Service?.Nombre} pendiente en paso {c.PasoActual}", c.Id));
            foreach (var c in items.Where(x => x.Estado == EstadoClosure.Rechazado).Take(5))
                avisos.Add(new DashboardAvisoDto("CierreRechazado", $"Cierre {etiqueta} #{c.Id} {c.Service?.Nombre} fue rechazado", c.Id));
            foreach (var c in items.Where(x => x.Lines.Any(l => l.TieneIncidencia)).Take(5))
                avisos.Add(new DashboardAvisoDto("IncidenciaCalculo", $"Cierre {etiqueta} #{c.Id} {c.Service?.Nombre} tiene líneas con incidencias", c.Id));
        }
        AvisosFor(costes.Items, "Costes");
        AvisosFor(fact.Items, "Facturación");

        var periods = await _periodRepo.ListAsync(ct);
        foreach (var p in periods.Where(p => p.Estado == EstadoPeriodo.Bloqueado).Take(5))
            avisos.Add(new DashboardAvisoDto("PeriodoBloqueado", $"Período {p.Nombre} bloqueado", p.Id));

        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var p in periods.Where(p => p.FechaFin <= hoy.AddDays(7) && p.Estado != EstadoPeriodo.Cerrado).Take(5))
            avisos.Add(new DashboardAvisoDto("PeriodoProximoVencer", $"Período {p.Nombre} vence en {(p.FechaFin.ToDateTime(TimeOnly.MinValue) - hoy.ToDateTime(TimeOnly.MinValue)).Days} días", p.Id));

        return avisos;
    }

    public async Task<IReadOnlyList<MiServicioDto>> GetMisServiciosAsync(int? periodId, int usuarioId, CancellationToken ct, int? serviceId = null)
    {
        var period = periodId.HasValue ? await _periodRepo.GetByIdAsync(periodId.Value, ct)
                                       : await _periodRepo.GetActivoAsync(ct);
        var services = await _serviceRepo.ListPaginatedForUserAsync(usuarioId, 1, int.MaxValue, null, null, ct);

        var pairsByService = period is null
            ? new Dictionary<int, Pair>()
            : (await BuildPairsAsync(usuarioId, period.Id, ct)).ToDictionary(p => p.ServiceId);

        var items = services.Items
            .Where(p => !serviceId.HasValue || p.Id == serviceId.Value) // PPT slide 3: filtro de servicio
            .Select(p =>
        {
            pairsByService.TryGetValue(p.Id, out var pair);
            // ClosureId del DTO -> id del cierre de costes (mensual) si existe, si no el de facturación.
            int? cierreId = pair?.Costes?.Id ?? pair?.Facturacion?.Id;
            EstadoClosure? estado = pair?.Costes?.Estado ?? pair?.Facturacion?.Estado;
            ApprovalStep? paso = pair?.Costes?.PasoActual ?? pair?.Facturacion?.PasoActual;
            decimal? coste = pair?.Costes != null ? pair.Coste : (decimal?)null;
            decimal? factura = pair?.Facturacion != null ? pair.Factura : (decimal?)null;
            decimal? margen = pair != null && (pair.Costes != null || pair.Facturacion != null) ? pair.Margen : (decimal?)null;
            return new MiServicioDto(p.Id, p.Nombre, p.ClientId, p.Client?.Nombre ?? "",
                cierreId, estado, paso, coste, factura, margen);
        }).ToList();

        return items;
    }
}

public class CalculationService : ICalculationService
{
    private readonly ICalculationLogRepository _repo;

    public CalculationService(ICalculationLogRepository repo) { _repo = repo; }

    public async Task<CalculationDetailDto> GetByClosureLineForUserAsync(int closureLineId, int usuarioId, CancellationToken ct)
    {
        var log = await _repo.GetByClosureLineAndUsuarioIdAsync(closureLineId, usuarioId, ct)
                  ?? throw new EntityNotFoundException("CalculationLog", closureLineId);
        return new CalculationDetailDto(
            log.ClosureLineId, log.ConceptId, log.Concept?.Nombre ?? "",
            log.FormulaSnapshotJson, log.InputsJson, log.Resultado,
            log.Incidencias, log.SistemaOrigen, log.Timestamp);
    }
}

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;

    public AuditService(IAuditLogRepository repo) { _repo = repo; }

    public async Task<PagedResult<AuditLogDto>> ListAsync(AuditLogFilterRequest filter, CancellationToken ct)
    {
        var result = await _repo.ListAsync(filter, ct);
        var items = result.Items.Select(a => new AuditLogDto(
            a.Id, a.UserId,
            a.User != null ? $"{a.User.Nombre} {a.User.Apellidos}" : null,
            a.EntityType, a.EntityId, a.Action,
            a.OldValueJson, a.NewValueJson, a.Timestamp, a.Ip)).ToList();
        return new PagedResult<AuditLogDto>(items, result.Total, result.Page, result.PageSize);
    }
}

public class SyncService : ISyncService
{
    private readonly ICeleroClient _celero;
    private readonly IBizneoClient _bizneo;
    private readonly IIntratimeClient _intratime;
    private readonly IPayHawkClient _payhawk;
    private readonly ISgpvClient _sgpv;
    private readonly IGalanClient _galan;
    private readonly IMediapostClient _mediapost;
    private readonly IStagingRepository<StagingCeleroVisita> _celeroRepo;
    private readonly IStagingRepository<StagingBizneoEmpleado> _empRepo;
    private readonly IStagingRepository<StagingBizneoAbsence> _absenceRepo;
    private readonly IStagingRepository<StagingIntratimeFichaje> _ficRepo;
    private readonly IStagingRepository<StagingIntratimeEmpleado> _intratimeEmpRepo;
    private readonly IStagingRepository<StagingIntratimeClockingRequest> _clkReqRepo;
    private readonly IStagingRepository<StagingIntratimeExpense> _expenseRepo;
    private readonly IStagingRepository<StagingPayHawkGasto> _gastoRepo;
    private readonly IStagingRepository<StagingSgpvVisita> _sgpvRepo;
    private readonly IStagingRepository<StagingSgpvProducto> _sgpvProductoRepo;
    private readonly IStagingRepository<StagingGalanEntrada> _galanEntradaRepo;
    private readonly IStagingRepository<StagingGalanSalida> _galanSalidaRepo;
    private readonly IStagingRepository<StagingGalanStock> _galanStockRepo;
    private readonly IStagingRepository<StagingMediapostPedido> _mediapostPedidoRepo;
    private readonly IStagingRepository<StagingMediapostRecepcion> _mediapostRecepcionRepo;
    private readonly IUserRepository _userRepo;
    private readonly IClientRepository _clientRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly ICeleroMappingRepository _mappingRepo;
    private readonly ITravelPerkExcelClient _travelPerkExcel;
    private readonly IStagingRepository<StagingTravelPerkLinea> _travelPerkLineaRepo;
    private readonly ICostCenterRepository _costCenterRepo;
    private readonly IDepartmentRepository _deptRepo;

    public SyncService(
        ICeleroClient celero, IBizneoClient bizneo, IIntratimeClient intratime, IPayHawkClient payhawk, ISgpvClient sgpv,
        IGalanClient galan, IMediapostClient mediapost,
        ITravelPerkExcelClient travelPerkExcel,
        IStagingRepository<StagingTravelPerkLinea> travelPerkLineaRepo,
        ICostCenterRepository costCenterRepo,
        IDepartmentRepository deptRepo,
        IStagingRepository<StagingCeleroVisita> celeroRepo,
        IStagingRepository<StagingBizneoEmpleado> empRepo,
        IStagingRepository<StagingBizneoAbsence> absenceRepo,
        IStagingRepository<StagingIntratimeFichaje> ficRepo,
        IStagingRepository<StagingIntratimeEmpleado> intratimeEmpRepo,
        IStagingRepository<StagingIntratimeClockingRequest> clkReqRepo,
        IStagingRepository<StagingIntratimeExpense> expenseRepo,
        IStagingRepository<StagingPayHawkGasto> gastoRepo,
        IStagingRepository<StagingSgpvVisita> sgpvRepo,
        IStagingRepository<StagingSgpvProducto> sgpvProductoRepo,
        IStagingRepository<StagingGalanEntrada> galanEntradaRepo,
        IStagingRepository<StagingGalanSalida> galanSalidaRepo,
        IStagingRepository<StagingGalanStock> galanStockRepo,
        IStagingRepository<StagingMediapostPedido> mediapostPedidoRepo,
        IStagingRepository<StagingMediapostRecepcion> mediapostRecepcionRepo,
        IUserRepository userRepo,
        IClientRepository clientRepo,
        IServiceRepository serviceRepo,
        ICeleroMappingRepository mappingRepo)
    {
        _celero = celero;
        _bizneo = bizneo;
        _intratime = intratime;
        _payhawk = payhawk;
        _sgpv = sgpv;
        _galan = galan;
        _mediapost = mediapost;
        _celeroRepo = celeroRepo;
        _userRepo = userRepo;
        _clientRepo = clientRepo;
        _serviceRepo = serviceRepo;
        _empRepo = empRepo;
        _absenceRepo = absenceRepo;
        _ficRepo = ficRepo;
        _intratimeEmpRepo = intratimeEmpRepo;
        _clkReqRepo = clkReqRepo;
        _expenseRepo = expenseRepo;
        _gastoRepo = gastoRepo;
        _sgpvRepo = sgpvRepo;
        _sgpvProductoRepo = sgpvProductoRepo;
        _galanEntradaRepo = galanEntradaRepo;
        _galanSalidaRepo = galanSalidaRepo;
        _galanStockRepo = galanStockRepo;
        _mediapostPedidoRepo = mediapostPedidoRepo;
        _mediapostRecepcionRepo = mediapostRecepcionRepo;
        _mappingRepo = mappingRepo;
        _travelPerkExcel = travelPerkExcel;
        _travelPerkLineaRepo = travelPerkLineaRepo;
        _costCenterRepo = costCenterRepo;
        _deptRepo = deptRepo;
    }

    public async Task<SyncResultDto> SyncAsync(string sistema, CancellationToken ct)
    {
        sistema = (sistema ?? "").ToLowerInvariant();
        var desde = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));
        var hasta = DateOnly.FromDateTime(DateTime.UtcNow);
        int ins = 0, dup = 0, err = 0;
        switch (sistema)
        {
            case "celero":
            {
                // ── Fase 0: importar departamentos, clientes y servicios maestros desde Celero ──
                // Celero es la fuente de verdad para departamentos/clientes/servicios.
                // Se hace upsert por Nombre (departamentos), NIF (clientes) o Nombre+ClientId (servicios).

                // Departamentos: upsert por Nombre
                var celeroDepartments = await _celero.GetDepartmentsAsync(ct);
                foreach (var cd in celeroDepartments)
                {
                    var existing = await _deptRepo.GetByNombreAsync(cd.Nombre, ct);
                    if (existing is null)
                    {
                        var nuevo = new Department
                        {
                            Nombre = cd.Nombre,
                            CeleroDepartmentId = cd.IdExterno,
                            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                        };
                        await _deptRepo.AddAsync(nuevo, ct);
                        await _deptRepo.SaveChangesAsync(ct);
                    }
                    else
                    {
                        // Actualizar CeleroDepartmentId si estaba vacío
                        if (existing.CeleroDepartmentId is null)
                        {
                            existing.CeleroDepartmentId = cd.IdExterno;
                            existing.UpdatedAt = DateTime.UtcNow;
                            await _deptRepo.SaveChangesAsync(ct);
                        }
                    }
                }

                // Clientes: upsert por NIF
                var celeroClientes = await _celero.GetClientesAsync(ct);
                var celeroIdToSigClientId = new Dictionary<string, int>(celeroClientes.Count);
                foreach (var cc in celeroClientes)
                {
                    var existing = await _clientRepo.GetByNifAsync(cc.Nif, ct);
                    if (existing is null)
                    {
                        var nuevo = new Client
                        {
                            Nombre = cc.Nombre, NIF = cc.Nif,
                            Estado = EstadoCliente.Activo,
                            Direccion = cc.Direccion, Ciudad = cc.Ciudad, Provincia = cc.Provincia,
                            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                        };
                        await _clientRepo.AddAsync(nuevo, ct);
                        await _clientRepo.SaveChangesAsync(ct);
                        celeroIdToSigClientId[cc.IdExterno] = nuevo.Id;
                    }
                    else
                    {
                        // Actualizar nombre y dirección si cambiaron en Celero
                        existing.Nombre = cc.Nombre;
                        if (cc.Direccion is not null) existing.Direccion = cc.Direccion;
                        if (cc.Ciudad is not null)    existing.Ciudad    = cc.Ciudad;
                        if (cc.Provincia is not null) existing.Provincia = cc.Provincia;
                        existing.UpdatedAt = DateTime.UtcNow;
                        await _clientRepo.SaveChangesAsync(ct);
                        celeroIdToSigClientId[cc.IdExterno] = existing.Id;
                    }
                }

                var celeroServicios = await _celero.GetServiciosAsync(ct);
                foreach (var cs in celeroServicios)
                {
                    if (!celeroIdToSigClientId.TryGetValue(cs.ClienteIdExterno, out var sigClientId)) continue;
                    var existing = await _serviceRepo.GetByNombreAndClienteAsync(cs.Nombre, sigClientId, ct);
                    if (existing is null)
                    {
                        var nuevo = new Service
                        {
                            Nombre = cs.Nombre, ClientId = sigClientId,
                            Estado = EstadoServicio.Activo,
                            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
                        };
                        await _serviceRepo.AddAsync(nuevo, ct);
                        await _serviceRepo.SaveChangesAsync(ct);
                    }
                    // Si ya existe no se modifica: los datos del servicio (CECO, usuarios, etc.)
                    // son propiedad de SIG, no de Celero.
                }
                // ────────────────────────────────────────────────────────────────────────

                var data = await _celero.GetVisitasAsync(desde, hasta, ct);
                var nuevas = new List<StagingCeleroVisita>();

                // Cargar mapeos explícitos (alta prioridad)
                var resourceMappings = await _mappingRepo.GetResourceMappingsAsync(ct);
                var serviceMappings = await _mappingRepo.GetServiceMappingsAsync(ct);
                var missionMappings = await _mappingRepo.GetMissionMappingsAsync(ct);

                var nifToUserIdMapping = resourceMappings.ToDictionary(m => m.CeleroNif, m => m.UserId);
                var serviceNameToServiceIdMapping = serviceMappings.ToDictionary(m => m.CeleroServiceName, m => m.ServiceId);
                var missionNameToServiceIdMapping = missionMappings.ToDictionary(m => m.CeleroMissionName, m => m.ServiceId);

                // Cargar lookups de fallback para resolución por nombre/NIF (baja prioridad)
                var users = await _userRepo.ListAsync(ct);
                var services = await _serviceRepo.ListAsync(ct);

                var nifToUserId = users.Where(u => !u.IsDeleted).ToDictionary(u => u.NIF, u => u.Id);
                var serviceNameToServiceId = services.Where(a => !a.IsDeleted).ToDictionary(a => a.Nombre, a => a.Id);

                foreach (var d in data)
                {
                    var json = JsonSerializer.Serialize(d);
                    var hash = Sha256(json);
                    if (await _celeroRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }

                    // Resolver IDs: primero mapeos explícitos, luego fallback a nombre/NIF
                    int? userId = null;
                    if (!string.IsNullOrEmpty(d.ResourceNif))
                    {
                        if (nifToUserIdMapping.TryGetValue(d.ResourceNif, out var uid)) userId = uid;
                        else if (nifToUserId.TryGetValue(d.ResourceNif, out uid)) userId = uid;
                    }

                    // El Servicio resuelve por misión (más específico) y, en su defecto, por nombre de servicio.
                    int? serviceId = null;
                    if (!string.IsNullOrEmpty(d.MissionName))
                    {
                        if (missionNameToServiceIdMapping.TryGetValue(d.MissionName, out var sid)) serviceId = sid;
                        else if (serviceNameToServiceId.TryGetValue(d.MissionName, out sid)) serviceId = sid;
                    }
                    if (serviceId is null && !string.IsNullOrEmpty(d.ServiceName))
                    {
                        if (serviceNameToServiceIdMapping.TryGetValue(d.ServiceName, out var sid)) serviceId = sid;
                        else if (serviceNameToServiceId.TryGetValue(d.ServiceName, out sid)) serviceId = sid;
                    }

                    nuevas.Add(new StagingCeleroVisita
                    {
                        VisitaIdExterno = d.VisitaIdExterno,
                        ResourceNif = d.ResourceNif,
                        ServiceName = d.ServiceName,
                        MissionName = d.MissionName,
                        Fecha = d.Fecha,
                        UserId = userId,
                        ServiceId = serviceId,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    });
                    ins++;
                }
                await _celeroRepo.AddRangeAsync(nuevas, ct);
                await _celeroRepo.SaveChangesAsync(ct);
                break;
            }
            case "bizneo":
            {
                var emps = await _bizneo.GetEmpleadosAsync(ct);
                foreach (var e in emps)
                {
                    var json = JsonSerializer.Serialize(e);
                    var hash = Sha256(json);
                    if (await _empRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _empRepo.AddRangeAsync(new[] { new StagingBizneoEmpleado
                    {
                        EmpleadoIdExterno = e.EmpleadoIdExterno, Email = e.Email, NIF = null, Nombre = e.Nombre, Departamento = e.Departamento,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _empRepo.SaveChangesAsync(ct);
                var absences = await _bizneo.GetAbsencesAsync(desde, hasta, ct);
                foreach (var a in absences)
                {
                    var json = JsonSerializer.Serialize(a);
                    var hash = Sha256(json);
                    if (await _absenceRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _absenceRepo.AddRangeAsync(new[] { new StagingBizneoAbsence
                    {
                        RegistroIdExterno = a.RegistroIdExterno, UserId = a.UserId, ServiceId = a.ServiceId,
                        Fecha = a.Fecha, FechaFin = a.FechaFin, Horas = a.Horas, Estado = a.Estado,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _absenceRepo.SaveChangesAsync(ct);
                break;
            }
            case "intratime":
            {
                var data = await _intratime.GetFichajesAsync(desde, hasta, ct);
                // Para resolver UserId interno, buscar en staging empleados
                var empleados = await _intratimeEmpRepo.ListAsync(ct);
                var empleadoLookup = empleados
                    .Where(e => e.UserId.HasValue)
                    .ToDictionary(e => e.UserIdExterno, e => e.UserId!.Value);

                foreach (var f in data)
                {
                    var json = JsonSerializer.Serialize(f);
                    var hash = Sha256(json);
                    if (await _ficRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }

                    var horasCalculadas = f.Salida.HasValue
                        ? (decimal)(f.Salida.Value - f.Entrada).TotalHours
                        : null as decimal?;

                    empleadoLookup.TryGetValue(f.UserIdExterno, out var userId);

                    await _ficRepo.AddRangeAsync(new[] { new StagingIntratimeFichaje
                    {
                        FichajeIdExterno = f.FichajeIdExterno,
                        UserIdExterno = f.UserIdExterno,
                        UserId = userId,
                        Entrada = DateTime.SpecifyKind(f.Entrada, DateTimeKind.Utc),
                        Salida = f.Salida.HasValue ? DateTime.SpecifyKind(f.Salida.Value, DateTimeKind.Utc) : null,
                        HorasCalculadas = horasCalculadas,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _ficRepo.SaveChangesAsync(ct);
                break;
            }
            case "intratime-empleados":
            {
                var data = await _intratime.GetEmpleadosAsync(ct);
                foreach (var u in data)
                {
                    var json = JsonSerializer.Serialize(u);
                    var hash = Sha256(json);
                    if (await _intratimeEmpRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _intratimeEmpRepo.AddRangeAsync(new[] { new StagingIntratimeEmpleado
                    {
                        UserIdExterno = u.UserIdExterno,
                        Nombre = u.Nombre,
                        Email = u.Email,
                        NIF = u.NIF,
                        Affiliation = u.Affiliation,
                        Role = u.Role,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _intratimeEmpRepo.SaveChangesAsync(ct);
                break;
            }
            case "payhawk":
            {
                var data = await _payhawk.GetGastosAsync(desde, hasta, ct);
                foreach (var g in data)
                {
                    var json = JsonSerializer.Serialize(g);
                    var hash = Sha256(json);
                    if (await _gastoRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _gastoRepo.AddRangeAsync(new[] { new StagingPayHawkGasto
                    {
                        GastoIdExterno = g.GastoIdExterno,
                        NIF = string.IsNullOrEmpty(g.NIF) ? null : g.NIF.Trim().ToUpperInvariant(),
                        UserId = null,          // se resuelve a posteriori via NIF si es necesario
                        ServiceId = g.ServiceId == 0 ? null : g.ServiceId,
                        Fecha = g.Fecha, Importe = g.Importe, Categoria = g.Categoria,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _gastoRepo.SaveChangesAsync(ct);
                break;
            }
            case "sgpv":
            {
                // Sincronizar visitas (generalmente vacío, pero necesario para compatibilidad)
                var visitas = await _sgpv.GetVisitasAsync(desde, hasta, ct);
                var nuevasVisitas = new List<StagingSgpvVisita>();

                // Cargar mapeos explícitos (alta prioridad)
                var resourceMappings = await _mappingRepo.GetResourceMappingsAsync(ct);
                var serviceMappings = await _mappingRepo.GetServiceMappingsAsync(ct);

                var nifToUserIdMapping = resourceMappings.ToDictionary(m => m.CeleroNif, m => m.UserId);
                var serviceNameToServiceIdMapping = serviceMappings.ToDictionary(m => m.CeleroServiceName, m => m.ServiceId);

                // Cargar lookups de fallback para resolución por nombre/NIF
                var users = await _userRepo.ListAsync(ct);
                var services = await _serviceRepo.ListAsync(ct);

                var nifToUserId = users.Where(u => !u.IsDeleted).ToDictionary(u => u.NIF, u => u.Id);
                var serviceNameToServiceId = services.Where(a => !a.IsDeleted).ToDictionary(a => a.Nombre, a => a.Id);

                foreach (var d in visitas)
                {
                    var json = JsonSerializer.Serialize(d);
                    var hash = Sha256(json);
                    if (await _sgpvRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }

                    // Resolver IDs: primero mapeos explícitos, luego fallback a nombre/NIF
                    int? userId = null;
                    if (!string.IsNullOrEmpty(d.ResourceNif))
                    {
                        if (nifToUserIdMapping.TryGetValue(d.ResourceNif, out var uid)) userId = uid;
                        else if (nifToUserId.TryGetValue(d.ResourceNif, out uid)) userId = uid;
                    }

                    int? serviceId = null;
                    if (!string.IsNullOrEmpty(d.ServiceName))
                    {
                        if (serviceNameToServiceIdMapping.TryGetValue(d.ServiceName, out var sid)) serviceId = sid;
                        else if (serviceNameToServiceId.TryGetValue(d.ServiceName, out sid)) serviceId = sid;
                    }

                    nuevasVisitas.Add(new StagingSgpvVisita
                    {
                        VisitaIdExterno = d.VisitaIdExterno,
                        ResourceNif = d.ResourceNif,
                        CentroId = d.CentroId,
                        CentroNombre = d.CentroNombre,
                        ServiceName = d.ServiceName,
                        Fecha = d.Fecha,
                        HorasDuracion = d.HorasDuracion,
                        UserId = userId,
                        ServiceId = serviceId,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    });
                    ins++;
                }
                await _sgpvRepo.AddRangeAsync(nuevasVisitas, ct);
                await _sgpvRepo.SaveChangesAsync(ct);

                // Sincronizar también productos automáticamente
                var productos = await _sgpv.GetProductosAsync(ct);
                var nuevasProductos = new List<StagingSgpvProducto>();
                foreach (var p in productos)
                {
                    var json = JsonSerializer.Serialize(p);
                    var hash = Sha256(json);
                    if (await _sgpvProductoRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    nuevasProductos.Add(new StagingSgpvProducto
                    {
                        IdProducto = p.IdProducto,
                        IdCliente = p.IdCliente,
                        Cliente = p.Cliente,
                        Categoria = p.Categoria,
                        Subcategoria = p.Subcategoria,
                        CodigoReferencia = p.CodigoReferencia,
                        Referencia = p.Referencia,
                        EAN = p.EAN,
                        Marca = p.Marca,
                        PVPRecomendado = p.PVPRecomendado,
                        Competencia = p.Competencia,
                        Activo = p.Activo,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    });
                    ins++;
                }
                await _sgpvProductoRepo.AddRangeAsync(nuevasProductos, ct);
                await _sgpvProductoRepo.SaveChangesAsync(ct);
                break;
            }
            case "sgpv-productos":
            {
                var data = await _sgpv.GetProductosAsync(ct);
                var nuevas = new List<StagingSgpvProducto>();

                foreach (var d in data)
                {
                    var json = JsonSerializer.Serialize(d);
                    var hash = Sha256(json);
                    if (await _sgpvProductoRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }

                    nuevas.Add(new StagingSgpvProducto
                    {
                        IdProducto = d.IdProducto,
                        IdCliente = d.IdCliente,
                        Cliente = d.Cliente,
                        Categoria = d.Categoria,
                        Subcategoria = d.Subcategoria,
                        CodigoReferencia = d.CodigoReferencia,
                        Referencia = d.Referencia,
                        EAN = d.EAN,
                        Marca = d.Marca,
                        PVPRecomendado = d.PVPRecomendado,
                        Competencia = d.Competencia,
                        Activo = d.Activo,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    });
                    ins++;
                }
                await _sgpvProductoRepo.AddRangeAsync(nuevas, ct);
                await _sgpvProductoRepo.SaveChangesAsync(ct);
                break;
            }
            case "intratime-clocking-requests":
            {
                var year = DateTime.UtcNow.Year;
                var data = await _intratime.GetClockingRequestsAsync(year, ct);

                // Cargar empleados de Intratime para resolver UserId
                var empleados = await _intratimeEmpRepo.ListAsync(ct);
                var empleadoLookup = empleados
                    .Where(e => e.UserId.HasValue)
                    .ToDictionary(e => e.UserIdExterno, e => e.UserId!.Value);

                foreach (var cr in data)
                {
                    var json = JsonSerializer.Serialize(cr);
                    var hash = Sha256(json);
                    if (await _clkReqRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }

                    empleadoLookup.TryGetValue(cr.UserIdExterno, out var userId);

                    await _clkReqRepo.AddRangeAsync(new[] { new StagingIntratimeClockingRequest
                    {
                        RequestIdExterno = cr.RequestIdExterno,
                        UserIdExterno = cr.UserIdExterno,
                        UserId = userId,
                        FechaRequest = cr.FechaRequest,
                        TipoRequest = cr.TipoRequest,
                        Estado = cr.Estado,
                        Razon = cr.Razon,
                        HoraDesde = cr.HoraDesde,
                        HoraHasta = cr.HoraHasta,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _clkReqRepo.SaveChangesAsync(ct);
                break;
            }
            case "intratime-expenses":
            {
                var data = await _intratime.GetExpensesAsync(desde, hasta, ct);

                // Cargar empleados de Intratime para resolver UserId
                var empleados = await _intratimeEmpRepo.ListAsync(ct);
                var empleadoLookup = empleados
                    .Where(e => e.UserId.HasValue && !string.IsNullOrEmpty(e.UserIdExterno))
                    .ToDictionary(e => e.UserIdExterno, e => e.UserId!.Value);

                // Cargar servicios para resolver ServiceId por nombre
                var services = await _serviceRepo.ListAsync(ct);
                var serviceNameLookup = services
                    .Where(p => !p.IsDeleted)
                    .ToDictionary(p => p.Nombre, p => p.Id);

                foreach (var e in data)
                {
                    var json = JsonSerializer.Serialize(e);
                    var hash = Sha256(json);
                    if (await _expenseRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }

                    // Resolver UserId
                    int? userId = null;
                    if (!string.IsNullOrEmpty(e.UserIdExterno))
                    {
                        empleadoLookup.TryGetValue(e.UserIdExterno, out var uid);
                        userId = uid;
                    }

                    // Resolver ServiceId por nombre
                    int? serviceId = null;
                    if (!string.IsNullOrEmpty(e.ProyectoNombre))
                    {
                        serviceNameLookup.TryGetValue(e.ProyectoNombre, out var sid);
                        serviceId = sid;
                    }

                    await _expenseRepo.AddRangeAsync(new[] { new StagingIntratimeExpense
                    {
                        ExpenseIdExterno = e.ExpenseIdExterno,
                        UserIdExterno = e.UserIdExterno,
                        UserId = userId,
                        FechaExpense = e.FechaExpense,
                        Cantidad = e.Cantidad,
                        NombreExpense = e.NombreExpense,
                        Descripcion = e.Descripcion,
                        ProyectoNombre = e.ProyectoNombre,
                        ServiceId = serviceId,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _expenseRepo.SaveChangesAsync(ct);
                break;
            }
            case "intratime-proyectos":
            {
                var data = await _intratime.GetProyectosAsync(ct);
                // Los proyectos de Intratime son información de referencia
                // Se devuelven sin persistencia en staging (no se mapean a SIG-es aún)
                ins = data.Count;
                break;
            }
            case "galan":
            {
                var desdeG = DateTime.UtcNow.AddDays(-30);
                var hastaG = DateTime.UtcNow;

                // Entradas
                var entradas = await _galan.GetEntradasAsync(desdeG, hastaG, ct);
                foreach (var e in entradas)
                {
                    var json = JsonSerializer.Serialize(e);
                    var hash = Sha256(json);
                    if (await _galanEntradaRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _galanEntradaRepo.AddRangeAsync(new[] { new StagingGalanEntrada
                    {
                        CodigoArticulo = e.CodigoArticulo,
                        CodigoDepartamento = e.CodigoDepartamento,
                        CodigoFamilia = e.CodigoFamilia,
                        Descripcion = e.Descripcion,
                        Fecha = DateTime.SpecifyKind(e.Fecha, DateTimeKind.Utc),
                        Unidades = e.Unidades,
                        Empresa = e.Empresa,
                        Almacen = e.Almacen,
                        Celda = e.Celda,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _galanEntradaRepo.SaveChangesAsync(ct);

                // Salidas
                var salidas = await _galan.GetSalidasAsync(desdeG, hastaG, ct);
                foreach (var s in salidas)
                {
                    var json = JsonSerializer.Serialize(s);
                    var hash = Sha256(json);
                    if (await _galanSalidaRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _galanSalidaRepo.AddRangeAsync(new[] { new StagingGalanSalida
                    {
                        Albaran = s.Albaran,
                        NumeroPedidoTercero = s.NumeroPedidoTercero,
                        CodigoArticulo = s.CodigoArticulo,
                        CodigoDepartamento = s.CodigoDepartamento,
                        CodigoFamilia = s.CodigoFamilia,
                        Descripcion = s.Descripcion,
                        Unidades = s.Unidades,
                        CodigoTransporte = s.CodigoTransporte,
                        Matricula = s.Matricula,
                        Fecha = DateTime.SpecifyKind(s.Fecha, DateTimeKind.Utc),
                        Destinatario = s.Destinatario,
                        Almacen = s.Almacen,
                        Celda = s.Celda,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _galanSalidaRepo.SaveChangesAsync(ct);

                // Stock
                var stock = await _galan.GetStockAsync(ct);
                foreach (var st in stock)
                {
                    var json = JsonSerializer.Serialize(st);
                    var hash = Sha256(json);
                    if (await _galanStockRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _galanStockRepo.AddRangeAsync(new[] { new StagingGalanStock
                    {
                        CodigoArticulo = st.CodigoArticulo,
                        CodigoDepartamento = st.CodigoDepartamento,
                        CodigoFamilia = st.CodigoFamilia,
                        CodigoCelda = st.CodigoCelda,
                        StockB = st.StockB,
                        StockA = st.StockA,
                        Stock = st.Stock,
                        Almacen = st.Almacen,
                        Familia = st.Familia,
                        SubFamilia = st.SubFamilia,
                        Descripcion = st.Descripcion,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _galanStockRepo.SaveChangesAsync(ct);
                break;
            }
            case "galan-entradas":
            {
                var desdeG = DateTime.UtcNow.AddDays(-30);
                var hastaG = DateTime.UtcNow;
                var data = await _galan.GetEntradasAsync(desdeG, hastaG, ct);

                foreach (var e in data)
                {
                    var json = JsonSerializer.Serialize(e);
                    var hash = Sha256(json);
                    if (await _galanEntradaRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _galanEntradaRepo.AddRangeAsync(new[] { new StagingGalanEntrada
                    {
                        CodigoArticulo = e.CodigoArticulo,
                        CodigoDepartamento = e.CodigoDepartamento,
                        CodigoFamilia = e.CodigoFamilia,
                        Descripcion = e.Descripcion,
                        Fecha = DateTime.SpecifyKind(e.Fecha, DateTimeKind.Utc),
                        Unidades = e.Unidades,
                        Empresa = e.Empresa,
                        Almacen = e.Almacen,
                        Celda = e.Celda,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _galanEntradaRepo.SaveChangesAsync(ct);
                break;
            }
            case "galan-salidas":
            {
                var desdeG = DateTime.UtcNow.AddDays(-30);
                var hastaG = DateTime.UtcNow;
                var data = await _galan.GetSalidasAsync(desdeG, hastaG, ct);

                foreach (var s in data)
                {
                    var json = JsonSerializer.Serialize(s);
                    var hash = Sha256(json);
                    if (await _galanSalidaRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _galanSalidaRepo.AddRangeAsync(new[] { new StagingGalanSalida
                    {
                        Albaran = s.Albaran,
                        NumeroPedidoTercero = s.NumeroPedidoTercero,
                        CodigoArticulo = s.CodigoArticulo,
                        CodigoDepartamento = s.CodigoDepartamento,
                        CodigoFamilia = s.CodigoFamilia,
                        Descripcion = s.Descripcion,
                        Unidades = s.Unidades,
                        CodigoTransporte = s.CodigoTransporte,
                        Matricula = s.Matricula,
                        Fecha = DateTime.SpecifyKind(s.Fecha, DateTimeKind.Utc),
                        Destinatario = s.Destinatario,
                        Almacen = s.Almacen,
                        Celda = s.Celda,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _galanSalidaRepo.SaveChangesAsync(ct);
                break;
            }
            case "galan-stock":
            {
                var data = await _galan.GetStockAsync(ct);

                foreach (var st in data)
                {
                    var json = JsonSerializer.Serialize(st);
                    var hash = Sha256(json);
                    if (await _galanStockRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _galanStockRepo.AddRangeAsync(new[] { new StagingGalanStock
                    {
                        CodigoArticulo = st.CodigoArticulo,
                        CodigoDepartamento = st.CodigoDepartamento,
                        CodigoFamilia = st.CodigoFamilia,
                        CodigoCelda = st.CodigoCelda,
                        StockB = st.StockB,
                        StockA = st.StockA,
                        Stock = st.Stock,
                        Almacen = st.Almacen,
                        Familia = st.Familia,
                        SubFamilia = st.SubFamilia,
                        Descripcion = st.Descripcion,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _galanStockRepo.SaveChangesAsync(ct);
                break;
            }
            case "mediapost":
            {
                var desdeM = DateTime.UtcNow.AddDays(-30);
                var hastaM = DateTime.UtcNow;

                // Pedidos
                var pedidos = await _mediapost.GetPedidosAsync(desdeM, hastaM, ct);
                foreach (var p in pedidos)
                {
                    var json = JsonSerializer.Serialize(p);
                    var hash = Sha256(json);
                    if (await _mediapostPedidoRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _mediapostPedidoRepo.AddRangeAsync(new[] { new StagingMediapostPedido
                    {
                        PedidoId = p.PedidoId,
                        ReferenciaPedido = p.ReferenciaPedido,
                        CodigoArticulo = p.CodigoArticulo,
                        FechaPedido = DateTime.SpecifyKind(p.FechaPedido, DateTimeKind.Utc),
                        Cantidad = p.Cantidad,
                        Estado = p.Estado,
                        DestinatarioNombre = p.DestinatarioNombre,
                        DireccionEntrega = p.DireccionEntrega,
                        CodigoPostal = p.CodigoPostal,
                        Ciudad = p.Ciudad,
                        Provincia = p.Provincia,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _mediapostPedidoRepo.SaveChangesAsync(ct);

                // Recepciones
                var recepciones = await _mediapost.GetRecepcionesAsync(desdeM, hastaM, ct);
                foreach (var r in recepciones)
                {
                    var json = JsonSerializer.Serialize(r);
                    var hash = Sha256(json);
                    if (await _mediapostRecepcionRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _mediapostRecepcionRepo.AddRangeAsync(new[] { new StagingMediapostRecepcion
                    {
                        RecepcionId = r.RecepcionId,
                        ReferenciaRecepcion = r.ReferenciaRecepcion,
                        CodigoArticulo = r.CodigoArticulo,
                        FechaRecepcion = DateTime.SpecifyKind(r.FechaRecepcion, DateTimeKind.Utc),
                        Cantidad = r.Cantidad,
                        CantidadDañada = r.CantidadDañada,
                        Estado = r.Estado,
                        Almacen = r.Almacen,
                        Observaciones = r.Observaciones,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _mediapostRecepcionRepo.SaveChangesAsync(ct);
                break;
            }
            case "mediapost-pedidos":
            {
                var desdeM = DateTime.UtcNow.AddDays(-30);
                var hastaM = DateTime.UtcNow;
                var data = await _mediapost.GetPedidosAsync(desdeM, hastaM, ct);

                foreach (var p in data)
                {
                    var json = JsonSerializer.Serialize(p);
                    var hash = Sha256(json);
                    if (await _mediapostPedidoRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _mediapostPedidoRepo.AddRangeAsync(new[] { new StagingMediapostPedido
                    {
                        PedidoId = p.PedidoId,
                        ReferenciaPedido = p.ReferenciaPedido,
                        CodigoArticulo = p.CodigoArticulo,
                        FechaPedido = DateTime.SpecifyKind(p.FechaPedido, DateTimeKind.Utc),
                        Cantidad = p.Cantidad,
                        Estado = p.Estado,
                        DestinatarioNombre = p.DestinatarioNombre,
                        DireccionEntrega = p.DireccionEntrega,
                        CodigoPostal = p.CodigoPostal,
                        Ciudad = p.Ciudad,
                        Provincia = p.Provincia,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _mediapostPedidoRepo.SaveChangesAsync(ct);
                break;
            }
            case "mediapost-recepciones":
            {
                var desdeM = DateTime.UtcNow.AddDays(-30);
                var hastaM = DateTime.UtcNow;
                var data = await _mediapost.GetRecepcionesAsync(desdeM, hastaM, ct);

                foreach (var r in data)
                {
                    var json = JsonSerializer.Serialize(r);
                    var hash = Sha256(json);
                    if (await _mediapostRecepcionRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _mediapostRecepcionRepo.AddRangeAsync(new[] { new StagingMediapostRecepcion
                    {
                        RecepcionId = r.RecepcionId,
                        ReferenciaRecepcion = r.ReferenciaRecepcion,
                        CodigoArticulo = r.CodigoArticulo,
                        FechaRecepcion = DateTime.SpecifyKind(r.FechaRecepcion, DateTimeKind.Utc),
                        Cantidad = r.Cantidad,
                        CantidadDañada = r.CantidadDañada,
                        Estado = r.Estado,
                        Almacen = r.Almacen,
                        Observaciones = r.Observaciones,
                        PayloadJson = json,
                        Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = false
                    } }, ct);
                    ins++;
                }
                await _mediapostRecepcionRepo.SaveChangesAsync(ct);
                break;
            }
            case "travelperk":
            {
                // Descarga Excel (SharePoint) → líneas → imputación por CECO (Cost object → Servicio).
                var lineas = await _travelPerkExcel.GetLineasAsync(ct) ?? Array.Empty<TravelPerkLineaDto>();
                if (lineas.Count > 0)
                {
                    var mapaCeco = await _costCenterRepo.GetCecoToServiceMapAsync(ct);
                    var cecosInternosSig = await _costCenterRepo.GetInternalSigCecoCodesAsync(ct) ?? Array.Empty<string>();
                    var nuevas = new List<StagingTravelPerkLinea>();
                    foreach (var l in lineas)
                    {
                        var json = JsonSerializer.Serialize(l);
                        var hash = Sha256(json);
                        if (await _travelPerkLineaRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }

                        var serviceId = TravelPerkCecoResolver.ResolverServiceId(l.CostObject, mapaCeco);
                        // Gasto interno de SIG: sin Cost object (suscripción → 0423) o CECO estructural de SIG.
                        var esInternoSig = serviceId is null
                            && TravelPerkCecoResolver.EsCecoInternoSig(l.CostObject, cecosInternosSig);
                        // CECO de cliente que no casa con la tabla maestra (y no es interno) → sin imputar (alerta de calidad).
                        var cecoNoMaestro = l.CostObject is not null && serviceId is null && !esInternoSig;

                        nuevas.Add(new StagingTravelPerkLinea
                        {
                            TripId = l.TripId,
                            Service = l.Service,
                            CostObject = l.CostObject,
                            Ceco = TravelPerkCecoResolver.NormalizarCeco(l.CostObject),
                            ServiceId = serviceId,
                            CosteSinIVA = l.CosteSinIVA,
                            FechaGasto = l.FechaGasto,
                            TravelerEmail = l.TravelerEmail,
                            Currency = l.Currency,
                            PayloadJson = json,
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false,
                            ErrorProcesamiento = cecoNoMaestro ? AlertaCodigos.CecoNoMaestro : null
                        });
                        ins++;
                    }
                    await _travelPerkLineaRepo.AddRangeAsync(nuevas, ct);
                    await _travelPerkLineaRepo.SaveChangesAsync(ct);
                }
                break;
            }
            case "a3innuva":
                throw new IntegrationException(sistema, "Sistema aún no implementado en modo sincronización.");
            default:
                throw new IntegrationException(sistema, "Sistema no soportado. Use celero, bizneo, intratime-*, payhawk, sgpv*, a3innuva, travelperk, galan-*, mediapost-*.");
        }
        return new SyncResultDto(
            Sistema: sistema,
            Exito: true,
            RegistrosInsertados: ins,
            RegistrosActualizados: dup,
            RegistrosError: err,
            FechaUltimaSincronizacion: DateTime.UtcNow);
    }

    private static string Sha256(string s)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes);
    }
}
