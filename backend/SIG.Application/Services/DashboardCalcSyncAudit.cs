using System.Text.Json;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IClosureRepository _closureRepo;
    private readonly IPeriodRepository _periodRepo;
    private readonly IUserRepository _userRepo;
    private readonly IServiceRepository _serviceRepo;

    public DashboardService(IClosureRepository closureRepo, IPeriodRepository periodRepo, IUserRepository userRepo, IServiceRepository serviceRepo)
    {
        _closureRepo = closureRepo;
        _periodRepo = periodRepo;
        _userRepo = userRepo;
        _serviceRepo = serviceRepo;
    }

    public async Task<DashboardKpisDto> GetKpisAsync(int? periodId, int usuarioId, CancellationToken ct)
    {
        Period? period = periodId.HasValue ? await _periodRepo.GetByIdAsync(periodId.Value, ct)
                                            : await _periodRepo.GetActivoAsync(ct);
        if (period is null) return new DashboardKpisDto(0, "", 0, 0, 0, 0, 0, 0, new List<KpiClienteDto>(), new List<EvolucionPeriodoDto>());

        var filter = new ApprovalFilterRequest(period.Id, null, null, null, null, null, null, null, 1, int.MaxValue);
        var closures = await _closureRepo.ListPaginatedForUserAsync(usuarioId, filter, ct);
        int completados = closures.Items.Count(c => c.Estado == EstadoClosure.Aprobado || c.Estado == EstadoClosure.Exportado);
        int pendientes = closures.Items.Count(c => c.Estado == EstadoClosure.EnAprobacion || c.Estado == EstadoClosure.Borrador || c.Estado == EstadoClosure.Rechazado);
        decimal fact = closures.Items.Sum(c => c.FacturacionTotal);
        decimal coste = closures.Items.Sum(c => c.CosteTotal);
        decimal margen = fact - coste;
        decimal margenPct = fact > 0 ? Math.Round(margen / fact * 100, 1) : 0;

        // Desglose por cliente (top 6)
        var desglose = closures.Items
            .Where(c => c.Service?.Client != null)
            .GroupBy(c => new { c.Service!.ClientId, c.Service.Client!.Nombre })
            .Select(g => {
                var f = g.Sum(c => c.FacturacionTotal);
                var co = g.Sum(c => c.CosteTotal);
                return new KpiClienteDto(g.Key.ClientId, g.Key.Nombre,
                    f, co, f - co, fact > 0 ? Math.Round(f / fact * 100, 1) : 0);
            })
            .OrderByDescending(x => x.Facturacion)
            .Take(6)
            .ToList();

        // Evolución últimos 6 períodos
        var allPeriods = (await _periodRepo.ListAsync(ct))
            .Where(p => p.Estado != EstadoPeriodo.Abierto)
            .OrderByDescending(p => p.Id)
            .Take(6)
            .Reverse()
            .ToList();

        var evolucion = new List<EvolucionPeriodoDto>();
        foreach (var p in allPeriods)
        {
            var pFilter = new ApprovalFilterRequest(p.Id, null, null, null, null, null, null, null, 1, int.MaxValue);
            var pClosures = await _closureRepo.ListPaginatedForUserAsync(usuarioId, pFilter, ct);
            var pFact = pClosures.Items.Sum(c => c.FacturacionTotal);
            var pCoste = pClosures.Items.Sum(c => c.CosteTotal);
            evolucion.Add(new EvolucionPeriodoDto(p.Nombre, pFact, pCoste, pFact - pCoste));
        }

        return new DashboardKpisDto(period.Id, period.Nombre, completados, pendientes, fact, coste, margen, margenPct, desglose, evolucion);
    }

    public async Task<IReadOnlyList<DashboardAvisoDto>> GetAvisosAsync(int usuarioId, CancellationToken ct)
    {
        var avisos = new List<DashboardAvisoDto>();
        var filter = new ApprovalFilterRequest(null, null, null, null, null, null, null, null, 1, int.MaxValue);
        var closures = await _closureRepo.ListPaginatedForUserAsync(usuarioId, filter, ct);

        // Cierres pendientes
        foreach (var c in closures.Items.Where(x => x.Estado == EstadoClosure.EnAprobacion || x.Estado == EstadoClosure.Borrador).Take(10))
            avisos.Add(new DashboardAvisoDto("CierrePendiente", $"Cierre #{c.Id} {c.Service?.Nombre} pendiente en paso {c.PasoActual}", c.Id));

        // Cierres rechazados
        foreach (var c in closures.Items.Where(x => x.Estado == EstadoClosure.Rechazado).Take(5))
            avisos.Add(new DashboardAvisoDto("CierreRechazado", $"Cierre #{c.Id} {c.Service?.Nombre} fue rechazado", c.Id));

        // Incidencias en líneas de cálculo
        foreach (var c in closures.Items.Where(c => c.Lines.Any(l => l.TieneIncidencia)).Take(5))
            avisos.Add(new DashboardAvisoDto("IncidenciaCalculo", $"Cierre #{c.Id} {c.Service?.Nombre} tiene líneas con incidencias", c.Id));

        // Períodos bloqueados y próximos a vencer
        var periods = await _periodRepo.ListAsync(ct);
        foreach (var p in periods.Where(p => p.Estado == EstadoPeriodo.Bloqueado).Take(5))
            avisos.Add(new DashboardAvisoDto("PeriodoBloqueado", $"Período {p.Nombre} bloqueado", p.Id));

        // Período próximo a vencer (FechaFin <= 7 días)
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var p in periods.Where(p => p.FechaFin <= hoy.AddDays(7) && p.Estado != EstadoPeriodo.Cerrado).Take(5))
            avisos.Add(new DashboardAvisoDto("PeriodoProximoVencer", $"Período {p.Nombre} vence en {(p.FechaFin.ToDateTime(TimeOnly.MinValue) - hoy.ToDateTime(TimeOnly.MinValue)).Days} días", p.Id));

        return avisos;
    }

    public async Task<IReadOnlyList<MiServicioDto>> GetMisServiciosAsync(int? periodId, int usuarioId, CancellationToken ct)
    {
        var period = periodId.HasValue ? await _periodRepo.GetByIdAsync(periodId.Value, ct)
                                       : await _periodRepo.GetActivoAsync(ct);
        var services = await _serviceRepo.ListPaginatedForUserAsync(usuarioId, 1, int.MaxValue, null, null, ct);

        // Cargar todos los cierres del período de una sola vez (fix N+1 query)
        var allClosures = period is null
            ? new Dictionary<int, Closure>()
            : (await _closureRepo.ListPaginatedForUserAsync(usuarioId,
                 new ApprovalFilterRequest(period.Id, null, null, null, null, null, null, null, 1, int.MaxValue), ct))
               .Items.ToDictionary(c => c.ServiceId);

        var items = services.Items.Select(p =>
        {
            allClosures.TryGetValue(p.Id, out var c);
            return new MiServicioDto(p.Id, p.Nombre, p.ClientId, p.Client?.Nombre ?? "",
                c?.Id, c?.Estado, c?.PasoActual,
                c?.CosteTotal, c?.FacturacionTotal, c?.Margen);
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
    private readonly IServiceRepository _serviceRepo;
    private readonly ICeleroMappingRepository _mappingRepo;

    public SyncService(
        ICeleroClient celero, IBizneoClient bizneo, IIntratimeClient intratime, IPayHawkClient payhawk, ISgpvClient sgpv,
        IGalanClient galan, IMediapostClient mediapost,
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
                        EmpleadoIdExterno = e.EmpleadoIdExterno, NIF = e.NIF, Nombre = e.Nombre, Departamento = e.Departamento,
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
                        Fecha = a.Fecha, Horas = a.Horas,
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
                        GastoIdExterno = g.GastoIdExterno, UserId = g.UserId, ServiceId = g.ServiceId,
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
                var data = await _sgpv.GetVisitasAsync(desde, hasta, ct);
                var nuevas = new List<StagingSgpvVisita>();

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

                foreach (var d in data)
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

                    nuevas.Add(new StagingSgpvVisita
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
                await _sgpvRepo.AddRangeAsync(nuevas, ct);
                await _sgpvRepo.SaveChangesAsync(ct);
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
            case "a3innuva":
            case "travelperk":
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
