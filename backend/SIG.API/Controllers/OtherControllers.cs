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
    public async Task<IActionResult> Kpis([FromQuery] int? periodId = null, [FromQuery] int? serviceId = null, CancellationToken ct = default) =>
        Ok(await _svc.GetKpisAsync(periodId, UserId, ct, serviceId));

    [HttpGet("avisos")]
    public async Task<IActionResult> Avisos(CancellationToken ct) =>
        Ok(await _svc.GetAvisosAsync(UserId, ct));

    [HttpGet("mis-servicios")]
    public async Task<IActionResult> MisServicios([FromQuery] int? periodId = null, [FromQuery] int? serviceId = null, CancellationToken ct = default) =>
        Ok(await _svc.GetMisServiciosAsync(periodId, UserId, ct, serviceId));
}

// Informes nativos (PPT slide 23) — reporting dentro de la app, sin Power BI.
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _svc;
    public ReportsController(IReportsService svc) { _svc = svc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet("resultado")]
    public async Task<IActionResult> Resultado([FromQuery] int anio, [FromQuery] int? departmentId, [FromQuery] int? clientId, [FromQuery] int? serviceId, CancellationToken ct) =>
        Ok(await _svc.GetResultadoAsync(anio, departmentId, clientId, serviceId, UserId, ct));

    [HttpGet("prevision-vs-real")]
    public async Task<IActionResult> PrevisionVsReal([FromQuery] int anio, [FromQuery] int? departmentId, [FromQuery] int? clientId, [FromQuery] int? serviceId, CancellationToken ct) =>
        Ok(await _svc.GetPrevisionVsRealAsync(anio, departmentId, clientId, serviceId, UserId, ct));
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
public class SyncController : ControllerBase
{
    private readonly ISyncService _svc;
    private readonly IDataProcessorService _processor;
    private readonly AppDbContext _db;
    public SyncController(ISyncService svc, IDataProcessorService processor, AppDbContext db)
    {
        _svc = svc;
        _processor = processor;
        _db = db;
    }

    /// <summary>
    /// Sincronizar datos de sistemas externos.
    /// Nota: 'galan' y 'mediapost' no requieren autenticación (archivos locales).
    /// Otros sistemas requieren rol Administrator.
    /// </summary>
    [HttpPost("{system}")]
    public async Task<IActionResult> Sync(string system, CancellationToken ct)
    {
        // Permitir galan y mediapost sin autenticación (archivos locales)
        if (!system.Equals("galan", StringComparison.OrdinalIgnoreCase) &&
            !system.Equals("mediapost", StringComparison.OrdinalIgnoreCase))
        {
            // Sin token → 401; autenticado pero sin rol Administrator → 403 (semántica HTTP correcta)
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized("Solo administradores pueden sincronizar sistemas externos");
            }
            if (!User.IsInRole("Administrator"))
            {
                return Forbid();
            }
        }

        return Ok(await _svc.SyncAsync(system, ct));
    }

    [HttpPost("process")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ProcessPending(CancellationToken ct) =>
        Ok(await _processor.ProcessAllPendingAsync(ct));

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
                conServicio = g.Count(v => v.ServiceId.HasValue),
                porcentajeResuelto = g.Count() > 0
                    ? Math.Round(g.Count(v => v.UserId.HasValue) * 100.0 / g.Count(), 1)
                    : 0
            })
            .FirstOrDefaultAsync(ct);

        return Ok(stats ?? new { totalVisitas = 0, conUsuario = 0, conServicio = 0, porcentajeResuelto = 0.0 });
    }

    [HttpGet("intratime/discrepancias")]
    public async Task<IActionResult> GetIntratimeDiscrepancias(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken ct) =>
        Ok(await _processor.ValidarDiscrepanciasIntratimeAsync(desde, hasta, ct));
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
        return File(content, "application/vnd.ms-excel", filename);
    }

    [HttpGet("a3-erp/{closureId:int}")]
    public async Task<IActionResult> A3Erp(int closureId, CancellationToken ct)
    {
        var (content, filename) = await _svc.ExportA3ErpAsync(closureId, UserId, ct);
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
    }
}

[ApiController]
[Route("api/contratos")]
[Authorize(Roles = "Administrator,Backoffice")]
public class ContratosController : ControllerBase
{
    private readonly IContratoService _svc;
    public ContratosController(IContratoService svc) { _svc = svc; }

    // Contratos de un día (FechaInicio == FechaFin): se señalan para revisión manual (Ola 2 #2).
    [HttpGet("un-dia")]
    public async Task<IActionResult> ListUnDia(CancellationToken ct) =>
        Ok(await _svc.ListContratosUnDiaAsync(ct));

    [HttpPost("{id:int}/ignorar")]
    public async Task<IActionResult> MarcarIgnorar(int id, SIG.Application.DTOs.ContratoIgnorarRequest req, CancellationToken ct) =>
        Ok(await _svc.MarcarIgnorarAsync(id, req, ct));
}

[ApiController]
[Route("api/services/{serviceId:int}/tarifas")]
[Authorize(Roles = "Administrator,Backoffice")]
public class TarifasController : ControllerBase
{
    private readonly ITarifaServicioService _svc;
    public TarifasController(ITarifaServicioService svc) { _svc = svc; }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(int serviceId, CancellationToken ct) =>
        Ok(await _svc.ListByServiceAsync(serviceId, ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Get(int serviceId, int id, CancellationToken ct) =>
        Ok(await _svc.GetByIdAsync(id, serviceId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(int serviceId, SIG.Application.DTOs.TarifaServicioCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(serviceId, req, ct);
        return CreatedAtAction(nameof(Get), new { serviceId, id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int serviceId, int id, SIG.Application.DTOs.TarifaServicioUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, serviceId, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int serviceId, int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, serviceId, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/services/{serviceId:int}/presupuestos")]
[Authorize(Roles = "Administrator,Backoffice")]
public class PresupuestosController : ControllerBase
{
    private readonly IPresupuestoServicioService _svc;
    public PresupuestosController(IPresupuestoServicioService svc) { _svc = svc; }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(int serviceId, CancellationToken ct) =>
        Ok(await _svc.ListByServiceAsync(serviceId, ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Get(int serviceId, int id, CancellationToken ct) =>
        Ok(await _svc.GetByIdAsync(id, serviceId, ct));

    [HttpPost]
    public async Task<IActionResult> Create(int serviceId, SIG.Application.DTOs.PresupuestoServicioCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(serviceId, req, ct);
        return CreatedAtAction(nameof(Get), new { serviceId, id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int serviceId, int id, SIG.Application.DTOs.PresupuestoServicioUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, serviceId, req, ct));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int serviceId, int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, serviceId, ct);
        return NoContent();
    }
}

// Configuración de Presupuesto (prototipo 24/28): partidas por acción/servicio + márgenes. Lectura para
// autenticados (restringida a servicios accesibles en el servicio); escritura solo Administrator.
[ApiController]
[Route("api/services/{serviceId:int}/config-presupuesto")]
[Authorize]
public class ConfigPresupuestoController : ControllerBase
{
    private readonly IConfigPresupuestoService _svc;
    public ConfigPresupuestoController(IConfigPresupuestoService svc) { _svc = svc; }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> Get(int serviceId, CancellationToken ct) =>
        Ok(await _svc.GetConfigAsync(serviceId, UserId, ct));

    [HttpPost("partidas")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CreatePartida(int serviceId, SIG.Application.DTOs.PartidaPresupuestoCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreatePartidaAsync(serviceId, req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { serviceId }, r);
    }

    [HttpPut("partidas/{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> UpdatePartida(int serviceId, int id, SIG.Application.DTOs.PartidaPresupuestoUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdatePartidaAsync(id, serviceId, req, UserId, ct));

    [HttpDelete("partidas/{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeletePartida(int serviceId, int id, CancellationToken ct)
    {
        await _svc.DeletePartidaAsync(id, serviceId, UserId, ct);
        return NoContent();
    }

    [HttpPut("margen-objetivo")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> SetMargenObjetivo(int serviceId, SIG.Application.DTOs.MargenObjetivoRequest req, CancellationToken ct) =>
        Ok(await _svc.SetMargenObjetivoAsync(serviceId, req, UserId, ct));
}

// Forecast por servicio (PPT slide 36). Lectura: autenticado; escritura: Administrator/Backoffice
// (mismo criterio que Tarifas/Presupuestos). Upsert por mes; rechaza meses cerrados (409 period_closed).
[ApiController]
[Route("api/services/{serviceId:int}/forecast")]
[Authorize(Roles = "Administrator,Backoffice")]
public class ServiceForecastController : ControllerBase
{
    private readonly IForecastService _svc;
    public ServiceForecastController(IForecastService svc) { _svc = svc; }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> List(int serviceId, [FromQuery] int anio, CancellationToken ct) =>
        Ok(await _svc.ListByServiceAsync(serviceId, anio, ct));

    [HttpPut]
    public async Task<IActionResult> Upsert(int serviceId, SIG.Application.DTOs.ForecastUpsertRequest req, CancellationToken ct) =>
        Ok(await _svc.UpsertAsync(serviceId, req, ct));
}

// Resumen pivote del forecast (PPT slide 36): filas dpto+cliente, columnas mes, totales. Solo lectura.
[ApiController]
[Route("api/forecast")]
[Authorize]
public class ForecastController : ControllerBase
{
    private readonly IForecastService _svc;
    public ForecastController(IForecastService svc) { _svc = svc; }

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(
        [FromQuery] int anio,
        [FromQuery] int? departmentId,
        [FromQuery] int? clientId,
        [FromQuery] int? serviceId,
        CancellationToken ct) =>
        Ok(await _svc.GetResumenAsync(anio, departmentId, clientId, serviceId, ct));
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

// A3 ERP (Contabilidad) — hub de traspaso de facturas del cierre a A3 ERP (salida).
// El export real vive en ExportsController (api/exports/a3-erp/{closureId}); aquí solo
// se expone el estado de conexión y un sync stub. NUNCA escribe en sistemas del cliente.
[ApiController]
[Route("api/a3-erp")]
[Authorize(Roles = "Administrator,Fico")]
public class A3ErpController : ControllerBase
{
    private readonly Microsoft.Extensions.Configuration.IConfiguration _config;
    public A3ErpController(Microsoft.Extensions.Configuration.IConfiguration config) { _config = config; }

    // Estado derivado solo de la presencia de configuración local: no realiza ninguna
    // llamada de red al ERP del cliente. Sin config válida → modo Test (degradación limpia).
    [HttpGet("status")]
    public ActionResult<SIG.Application.DTOs.A3ErpStatusDto> GetStatus()
    {
        var baseUrl = _config["Integrations:A3Erp:BaseUrl"];
        var apiKey = _config["Integrations:A3Erp:ApiKey"];
        var configured = !string.IsNullOrWhiteSpace(baseUrl)
                         && !string.IsNullOrWhiteSpace(apiKey)
                         && apiKey != "YOUR_API_KEY_HERE"
                         && apiKey != "__SET_VIA_ENVIRONMENT__";

        var dto = configured
            ? new SIG.Application.DTOs.A3ErpStatusDto(true, "Produccion",
                "Conectado a A3 ERP. El traspaso genera un fichero de facturas para importar manualmente; la plataforma no escribe en A3 ERP.")
            : new SIG.Application.DTOs.A3ErpStatusDto(false, "Test",
                "A3 ERP no configurado (modo test). El traspaso genera un fichero descargable; no se conecta ni escribe en A3 ERP.");
        return Ok(dto);
    }

    // Importación desde A3 ERP: pendiente de especificación de la API. Stub honesto,
    // sin tablas staging ni llamadas externas.
    [HttpPost("sync")]
    public IActionResult Sync() =>
        Problem(
            detail: "Sincronización desde A3 ERP pendiente de especificación de la API.",
            statusCode: 501,
            title: "No implementado");
}
