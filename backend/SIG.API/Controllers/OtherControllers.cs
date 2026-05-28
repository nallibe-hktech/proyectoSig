using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using SIG.Application.Interfaces.Services;
using SIG.Infrastructure.Persistence;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) { _svc = svc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> Kpis([FromQuery] int? periodId = null, CancellationToken ct = default) =>
        Ok(await _svc.GetKpisAsync(periodId, UserId, ct));

    [HttpGet("avisos")]
    public async Task<IActionResult> Avisos(CancellationToken ct) =>
        Ok(await _svc.GetAvisosAsync(UserId, ct));

    [HttpGet("mis-proyectos")]
    public async Task<IActionResult> MisProyectos([FromQuery] int? periodId = null, CancellationToken ct = default) =>
        Ok(await _svc.GetMisProyectosAsync(periodId, UserId, ct));
}

[ApiController]
[Route("api/calculations")]
[Authorize]
public class CalculationsController : ControllerBase
{
    private readonly ICalculationService _svc;
    public CalculationsController(ICalculationService svc) { _svc = svc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet("{closureLineId:int}")]
    public async Task<IActionResult> Get(int closureLineId, CancellationToken ct) =>
        Ok(await _svc.GetByClosureLineForUserAsync(closureLineId, UserId, ct));
}

[ApiController]
[Route("api/audit")]
[Authorize(Roles = "Administrator,Auditor")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _svc;
    public AuditController(IAuditService svc) { _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] SIG.Application.DTOs.AuditLogFilterRequest filter, CancellationToken ct) =>
        Ok(await _svc.ListAsync(filter, ct));
}

[ApiController]
[Route("api/sync")]
[Authorize(Roles = "Administrator")]
public class SyncController : ControllerBase
{
    private readonly ISyncService _svc;
    private readonly AppDbContext _db;
    public SyncController(ISyncService svc, AppDbContext db) { _svc = svc; _db = db; }

    [HttpPost("{system}")]
    public async Task<IActionResult> Sync(string system, CancellationToken ct) =>
        Ok(await _svc.SyncAsync(system, ct));

    [HttpGet("celero/stats")]
    public async Task<IActionResult> GetCeleroStats(CancellationToken ct)
    {
        var stats = await _db.StagingCeleroVisitas
            .AsNoTracking()
            .GroupBy(v => 1)
            .Select(g => new
            {
                totalVisitas = g.Count(),
                conUsuario = g.Count(v => v.UserId.HasValue),
                conProyecto = g.Count(v => v.ProjectId.HasValue),
                conAccion = g.Count(v => v.ActionId.HasValue),
                porcentajeResuelto = g.Count() > 0
                    ? Math.Round(g.Count(v => v.UserId.HasValue) * 100.0 / g.Count(), 1)
                    : 0
            })
            .FirstOrDefaultAsync(ct);

        return Ok(stats ?? new { totalVisitas = 0, conUsuario = 0, conProyecto = 0, conAccion = 0, porcentajeResuelto = 0.0 });
    }
}

[ApiController]
[Route("api/exports")]
[Authorize(Roles = "Administrator,Fico,Direction")]
public class ExportsController : ControllerBase
{
    private readonly IExportService _svc;
    public ExportsController(IExportService svc) { _svc = svc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet("a3-innuva/{closureId:int}")]
    public async Task<IActionResult> A3Innuva(int closureId, CancellationToken ct)
    {
        var (content, filename) = await _svc.ExportA3InnuvaAsync(closureId, UserId, ct);
        return File(content, "application/xml", filename);
    }

    [HttpGet("a3-erp/{closureId:int}")]
    public async Task<IActionResult> A3Erp(int closureId, CancellationToken ct)
    {
        var (content, filename) = await _svc.ExportA3ErpAsync(closureId, UserId, ct);
        return File(content, "application/xml", filename);
    }
}

[ApiController]
[Route("api/dev")]
public class DevController : ControllerBase
{
    private readonly IHostEnvironment _env;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
    private readonly ISeedService _seed;

    public DevController(IHostEnvironment env, Microsoft.Extensions.Configuration.IConfiguration config, ISeedService seed)
    {
        _env = env;
        _config = config;
        _seed = seed;
    }

    [HttpPost("regenerar-seed")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> RegenerateSeed(CancellationToken ct)
    {
        if (!IsAllowedEnvironment()) return NotFound();
        if (!_config.GetValue<bool>("Features:AllowSeedRegeneration")) return NotFound();
        await _seed.RegenerateAsync(ct);
        return Ok(new { ok = true, mensaje = "Seed regenerada." });
    }

    private bool IsAllowedEnvironment()
    {
        return _env.IsDevelopment() || _env.EnvironmentName == "Testing" || _env.EnvironmentName == "E2E";
    }
}
