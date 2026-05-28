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
    private readonly IProjectRepository _projectRepo;

    public DashboardService(IClosureRepository closureRepo, IPeriodRepository periodRepo, IUserRepository userRepo, IProjectRepository projectRepo)
    {
        _closureRepo = closureRepo;
        _periodRepo = periodRepo;
        _userRepo = userRepo;
        _projectRepo = projectRepo;
    }

    public async Task<DashboardKpisDto> GetKpisAsync(int? periodId, int usuarioId, CancellationToken ct)
    {
        Period? period = periodId.HasValue ? await _periodRepo.GetByIdAsync(periodId.Value, ct)
                                            : await _periodRepo.GetActivoAsync(ct);
        if (period is null) return new DashboardKpisDto(0, "", 0, 0, 0, 0, 0);

        var filter = new ApprovalFilterRequest(period.Id, null, null, null, null, null, null, null, 1, int.MaxValue);
        var closures = await _closureRepo.ListPaginatedForUserAsync(usuarioId, filter, ct);
        int completados = closures.Items.Count(c => c.Estado == EstadoClosure.Aprobado || c.Estado == EstadoClosure.Exportado);
        int pendientes = closures.Items.Count(c => c.Estado == EstadoClosure.EnAprobacion || c.Estado == EstadoClosure.Borrador || c.Estado == EstadoClosure.Rechazado);
        decimal fact = closures.Items.Sum(c => c.FacturacionTotal);
        decimal coste = closures.Items.Sum(c => c.CosteTotal);
        decimal margen = fact - coste;
        return new DashboardKpisDto(period.Id, period.Nombre, completados, pendientes, fact, coste, margen);
    }

    public async Task<IReadOnlyList<DashboardAvisoDto>> GetAvisosAsync(int usuarioId, CancellationToken ct)
    {
        var avisos = new List<DashboardAvisoDto>();
        var filter = new ApprovalFilterRequest(null, null, null, null, null, null, null, null, 1, int.MaxValue);
        var closures = await _closureRepo.ListPaginatedForUserAsync(usuarioId, filter, ct);
        foreach (var c in closures.Items.Where(x => x.Estado == EstadoClosure.EnAprobacion || x.Estado == EstadoClosure.Borrador).Take(10))
            avisos.Add(new DashboardAvisoDto("CierrePendiente", $"Cierre #{c.Id} {c.Project?.Nombre} pendiente en paso {c.PasoActual}", c.Id));
        var periods = await _periodRepo.ListAsync(ct);
        foreach (var p in periods.Where(p => p.Estado == EstadoPeriodo.Bloqueado))
            avisos.Add(new DashboardAvisoDto("PeriodoBloqueado", $"Período {p.Nombre} bloqueado", p.Id));
        return avisos;
    }

    public async Task<IReadOnlyList<MiProyectoDto>> GetMisProyectosAsync(int? periodId, int usuarioId, CancellationToken ct)
    {
        var period = periodId.HasValue ? await _periodRepo.GetByIdAsync(periodId.Value, ct)
                                       : await _periodRepo.GetActivoAsync(ct);
        var projects = await _projectRepo.ListPaginatedForUserAsync(usuarioId, 1, int.MaxValue, null, null, ct);
        var items = new List<MiProyectoDto>();
        foreach (var p in projects.Items)
        {
            Closure? c = period is null ? null : await _closureRepo.GetByProjectAndPeriodAsync(p.Id, period.Id, ct);
            items.Add(new MiProyectoDto(p.Id, p.Nombre, p.ClientId, p.Client?.Nombre ?? "",
                c?.Id, c?.Estado, c?.PasoActual));
        }
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
    private readonly IStagingRepository<StagingCeleroVisita> _celeroRepo;
    private readonly IStagingRepository<StagingBizneoEmpleado> _empRepo;
    private readonly IStagingRepository<StagingBizneoHora> _horaRepo;
    private readonly IStagingRepository<StagingIntratimeFichaje> _ficRepo;
    private readonly IStagingRepository<StagingPayHawkGasto> _gastoRepo;

    public SyncService(
        ICeleroClient celero, IBizneoClient bizneo, IIntratimeClient intratime, IPayHawkClient payhawk,
        IStagingRepository<StagingCeleroVisita> celeroRepo,
        IStagingRepository<StagingBizneoEmpleado> empRepo,
        IStagingRepository<StagingBizneoHora> horaRepo,
        IStagingRepository<StagingIntratimeFichaje> ficRepo,
        IStagingRepository<StagingPayHawkGasto> gastoRepo)
    {
        _celero = celero;
        _bizneo = bizneo;
        _intratime = intratime;
        _payhawk = payhawk;
        _celeroRepo = celeroRepo;
        _empRepo = empRepo;
        _horaRepo = horaRepo;
        _ficRepo = ficRepo;
        _gastoRepo = gastoRepo;
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
                foreach (var d in data)
                {
                    var json = JsonSerializer.Serialize(d);
                    var hash = Sha256(json);
                    if (await _celeroRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    nuevas.Add(new StagingCeleroVisita
                    {
                        VisitaIdExterno = d.VisitaIdExterno,
                        UserId = d.UserId, ProjectId = d.ProjectId, ActionId = d.ActionId,
                        Fecha = d.Fecha, TipoVisita = d.TipoVisita, PuntoMontado = d.PuntoMontado,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        FlagProcesado = true
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
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
                    } }, ct);
                    ins++;
                }
                await _empRepo.SaveChangesAsync(ct);
                var horas = await _bizneo.GetHorasAsync(desde, hasta, ct);
                foreach (var h in horas)
                {
                    var json = JsonSerializer.Serialize(h);
                    var hash = Sha256(json);
                    if (await _horaRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _horaRepo.AddRangeAsync(new[] { new StagingBizneoHora
                    {
                        RegistroIdExterno = h.RegistroIdExterno, UserId = h.UserId, ProjectId = h.ProjectId,
                        Fecha = h.Fecha, Horas = h.Horas,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
                    } }, ct);
                    ins++;
                }
                await _horaRepo.SaveChangesAsync(ct);
                break;
            }
            case "intratime":
            {
                var data = await _intratime.GetFichajesAsync(desde, hasta, ct);
                foreach (var f in data)
                {
                    var json = JsonSerializer.Serialize(f);
                    var hash = Sha256(json);
                    if (await _ficRepo.ExistsByHashAsync(hash, ct)) { dup++; continue; }
                    await _ficRepo.AddRangeAsync(new[] { new StagingIntratimeFichaje
                    {
                        FichajeIdExterno = f.FichajeIdExterno, UserId = f.UserId,
                        Entrada = DateTime.SpecifyKind(f.Entrada, DateTimeKind.Utc),
                        Salida = f.Salida.HasValue ? DateTime.SpecifyKind(f.Salida.Value, DateTimeKind.Utc) : null,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
                    } }, ct);
                    ins++;
                }
                await _ficRepo.SaveChangesAsync(ct);
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
                        GastoIdExterno = g.GastoIdExterno, UserId = g.UserId, ProjectId = g.ProjectId,
                        Fecha = g.Fecha, Importe = g.Importe, Categoria = g.Categoria,
                        PayloadJson = json, Hash = hash,
                        FechaUltimaSincronizacion = DateTime.UtcNow, FlagProcesado = true
                    } }, ct);
                    ins++;
                }
                await _gastoRepo.SaveChangesAsync(ct);
                break;
            }
            default:
                throw new IntegrationException(sistema, "Sistema no soportado. Use celero, bizneo, intratime, payhawk.");
        }
        return new SyncResultDto(sistema, ins, dup, err, DateTime.UtcNow);
    }

    private static string Sha256(string s)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(bytes);
    }
}
