using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities.Staging;
using SIG.Infrastructure.Persistence;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Roles = "Administrator,Auditor")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _svc;
    public RolesController(IRoleService svc) { _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _svc.ListAsync(ct));
}

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _svc;
    public DepartmentsController(IDepartmentService svc) { _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _svc.ListAsync(ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(DepartmentCreateRequest req, CancellationToken ct) =>
        StatusCode(201, await _svc.CreateAsync(req, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int id, DepartmentUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/costcenters")]
[Authorize]
public class CostCentersController : ControllerBase
{
    private readonly ICostCenterService _svc;
    public CostCentersController(ICostCenterService svc) { _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _svc.ListAsync(ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(CostCenterCreateRequest req, CancellationToken ct) =>
        StatusCode(201, await _svc.CreateAsync(req, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int id, CostCenterUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/variables")]
[Authorize]
public class VariablesController : ControllerBase
{
    private readonly IVariableService _svc;
    public VariablesController(IVariableService svc) { _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _svc.ListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await _svc.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator,Backoffice")]
    public async Task<IActionResult> Create(VariableCreateRequest req, CancellationToken ct) =>
        StatusCode(201, await _svc.CreateAsync(req, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Backoffice")]
    public async Task<IActionResult> Update(int id, VariableUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}

// Mapeos Celero
[ApiController]
[Route("api/celero-mappings/resources")]
[Authorize(Roles = "Administrator")]
public class CeleroResourceMappingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    public CeleroResourceMappingsController(AppDbContext db, IUserRepository userRepo)
    {
        _db = db;
        _userRepo = userRepo;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.CeleroResourceMappings.AsNoTracking().ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CeleroResourceMappingRequest req, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(req.UserId, ct);
        if (user is null) return NotFound($"Usuario {req.UserId} no encontrado");

        var mapping = new CeleroResourceMapping { CeleroNif = req.CeleroNif, UserId = req.UserId, Descripcion = req.Descripcion, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.CeleroResourceMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return StatusCode(201, mapping);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var mapping = await _db.CeleroResourceMappings.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (mapping is null) return NotFound();
        _db.CeleroResourceMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/celero-mappings/services")]
[Authorize(Roles = "Administrator")]
public class CeleroServiceMappingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IServiceRepository _serviceRepo;
    public CeleroServiceMappingsController(AppDbContext db, IServiceRepository serviceRepo)
    {
        _db = db;
        _serviceRepo = serviceRepo;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.CeleroServiceMappings.AsNoTracking().ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CeleroServiceMappingRequest req, CancellationToken ct)
    {
        var service = await _serviceRepo.GetByIdAsync(req.ServiceId, ct);
        if (service is null) return NotFound($"Servicio {req.ServiceId} no encontrado");

        var mapping = new CeleroServiceMapping { CeleroServiceName = req.CeleroServiceName, ServiceId = req.ServiceId, Descripcion = req.Descripcion, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.CeleroServiceMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return StatusCode(201, mapping);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var mapping = await _db.CeleroServiceMappings.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (mapping is null) return NotFound();
        _db.CeleroServiceMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/celero-mappings/missions")]
[Authorize(Roles = "Administrator")]
public class CeleroMissionMappingsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IServiceRepository _serviceRepo;
    public CeleroMissionMappingsController(AppDbContext db, IServiceRepository serviceRepo)
    {
        _db = db;
        _serviceRepo = serviceRepo;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.CeleroMissionMappings.AsNoTracking().ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CeleroMissionMappingRequest req, CancellationToken ct)
    {
        var service = await _serviceRepo.GetByIdAsync(req.ServiceId, ct);
        if (service is null) return NotFound($"Servicio {req.ServiceId} no encontrado");

        var mapping = new CeleroMissionMapping { CeleroMissionName = req.CeleroMissionName, ServiceId = req.ServiceId, Descripcion = req.Descripcion, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.CeleroMissionMappings.Add(mapping);
        await _db.SaveChangesAsync(ct);
        return StatusCode(201, mapping);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var mapping = await _db.CeleroMissionMappings.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (mapping is null) return NotFound();
        _db.CeleroMissionMappings.Remove(mapping);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}

public record CeleroResourceMappingRequest(string CeleroNif, int UserId, string? Descripcion);
public record CeleroServiceMappingRequest(string CeleroServiceName, int ServiceId, string? Descripcion);
public record CeleroMissionMappingRequest(string CeleroMissionName, int ServiceId, string? Descripcion);

// Gestión de valores pendientes de mapeo
[ApiController]
[Route("api/celero-mappings")]
[Authorize(Roles = "Administrator")]
public class CeleroMapeosPendientesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CeleroMapeosPendientesController(AppDbContext db) => _db = db;

    /// <summary>
    /// Obtiene los valores únicos de staging sin mapear y su estado
    /// </summary>
    [HttpGet("pendientes")]
    public async Task<IActionResult> GetPendientes(CancellationToken ct)
    {
        // Recursos (NIF) únicos en staging
        var resourcesInStaging = await _db.StagingCeleroVisitas
            .Where(s => !string.IsNullOrEmpty(s.ResourceNif))
            .GroupBy(s => s.ResourceNif)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var mappedResources = await _db.CeleroResourceMappings
            .Select(m => m.CeleroNif)
            .ToListAsync(ct);

        var pendingResources = resourcesInStaging
            .Select(r => new CeleroMapeosPendientesDto.PendingValue
            {
                Valor = r.Value,
                Cantidad = r.Count,
                EstaMapado = mappedResources.Contains(r.Value)
            })
            .OrderByDescending(r => r.Cantidad)
            .ToList();

        // Servicios únicos en staging
        var servicesInStaging = await _db.StagingCeleroVisitas
            .Where(s => !string.IsNullOrEmpty(s.ServiceName))
            .GroupBy(s => s.ServiceName)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var mappedServices = await _db.CeleroServiceMappings
            .Select(m => m.CeleroServiceName)
            .ToListAsync(ct);

        var pendingServices = servicesInStaging
            .Select(s => new CeleroMapeosPendientesDto.PendingValue
            {
                Valor = s.Value,
                Cantidad = s.Count,
                EstaMapado = mappedServices.Contains(s.Value)
            })
            .OrderByDescending(s => s.Cantidad)
            .ToList();

        // Misiones únicas en staging
        var missionsInStaging = await _db.StagingCeleroVisitas
            .Where(s => !string.IsNullOrEmpty(s.MissionName))
            .GroupBy(s => s.MissionName)
            .Select(g => new { Value = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var mappedMissions = await _db.CeleroMissionMappings
            .Select(m => m.CeleroMissionName)
            .ToListAsync(ct);

        var pendingMissions = missionsInStaging
            .Select(m => new CeleroMapeosPendientesDto.PendingValue
            {
                Valor = m.Value,
                Cantidad = m.Count,
                EstaMapado = mappedMissions.Contains(m.Value)
            })
            .OrderByDescending(m => m.Cantidad)
            .ToList();

        return Ok(new CeleroMapeosPendientesDto
        {
            Recursos = pendingResources,
            Servicios = pendingServices,
            Misiones = pendingMissions,
            TotalVisitasSinMapear = await _db.StagingCeleroVisitas
                .CountAsync(s => s.UserId == null || s.ServiceId == null, cancellationToken: ct)
        });
    }

    /// <summary>
    /// Reprocesa las visitas sin mapear usando los mapeos existentes
    /// </summary>
    [HttpPost("reprocesar")]
    public async Task<IActionResult> Reprocesar(CancellationToken ct)
    {
        // Cargar mapeos actuales en memoria
        var resourceMappings = await _db.CeleroResourceMappings
            .AsNoTracking()
            .ToDictionaryAsync(m => m.CeleroNif, m => m.UserId, ct);

        var serviceMappings = await _db.CeleroServiceMappings
            .AsNoTracking()
            .ToDictionaryAsync(m => m.CeleroServiceName, m => m.ServiceId, ct);

        var missionMappings = await _db.CeleroMissionMappings
            .AsNoTracking()
            .ToDictionaryAsync(m => m.CeleroMissionName, m => m.ServiceId, ct);

        // Obtener visitas sin mapear completo
        var visitasSinMapear = await _db.StagingCeleroVisitas
            .Where(v => v.UserId == null || v.ServiceId == null)
            .ToListAsync(ct);

        int procesados = 0, resueltos = 0;

        foreach (var visita in visitasSinMapear)
        {
            bool cambio = false;

            // Intentar resolver UserId
            if (visita.UserId == null && !string.IsNullOrEmpty(visita.ResourceNif))
            {
                if (resourceMappings.TryGetValue(visita.ResourceNif, out var userId))
                {
                    visita.UserId = userId;
                    cambio = true;
                }
            }

            // Intentar resolver ServiceId: misión (más específico) y, en su defecto, servicio
            if (visita.ServiceId == null && !string.IsNullOrEmpty(visita.MissionName))
            {
                if (missionMappings.TryGetValue(visita.MissionName, out var serviceId))
                {
                    visita.ServiceId = serviceId;
                    cambio = true;
                }
            }
            if (visita.ServiceId == null && !string.IsNullOrEmpty(visita.ServiceName))
            {
                if (serviceMappings.TryGetValue(visita.ServiceName, out var serviceId))
                {
                    visita.ServiceId = serviceId;
                    cambio = true;
                }
            }

            if (cambio && visita.UserId.HasValue && visita.ServiceId.HasValue)
            {
                resueltos++;
            }

            procesados++;
        }

        // Guardar cambios en batch
        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            procesados,
            resueltos,
            sinResolver = procesados - resueltos,
            mensaje = $"Se reprocesaron {procesados} visitas: {resueltos} se resolvieron completamente, {procesados - resueltos} aún requieren mapeo."
        });
    }
}

// DTOs para respuestas
public class CeleroMapeosPendientesDto
{
    public List<PendingValue> Recursos { get; set; } = [];
    public List<PendingValue> Servicios { get; set; } = [];
    public List<PendingValue> Misiones { get; set; } = [];
    public int TotalVisitasSinMapear { get; set; }

    public class PendingValue
    {
        public string Valor { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public bool EstaMapado { get; set; }
    }
}

// Datos de integraciones externas (Bizneo, Intratime, PayHawk)
[ApiController]
[Route("api/bizneo")]
[Authorize(Roles = "Administrator")]
public class BizneoController : ControllerBase
{
    private readonly AppDbContext _db;
    public BizneoController(AppDbContext db) { _db = db; }

    [HttpGet("empleados")]
    public async Task<IActionResult> Empleados([FromQuery] string? search, CancellationToken ct)
    {
        var q = _db.StagingBizneoEmpleados.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(e => e.Nombre.Contains(search));
        return Ok(await q.OrderBy(e => e.Nombre).ToListAsync(ct));
    }

    [HttpGet("ausencias")]
    public async Task<IActionResult> Ausencias([FromQuery] string? search, CancellationToken ct)
    {
        var q = _db.StagingBizneoAbsences.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(a => a.UserId.ToString().Contains(search));
        return Ok(await q.OrderByDescending(a => a.Fecha).ToListAsync(ct));
    }
}

[ApiController]
[Route("api/intratime")]
[Authorize(Roles = "Administrator")]
public class IntratimeController : ControllerBase
{
    private readonly AppDbContext _db;
    public IntratimeController(AppDbContext db) { _db = db; }

    [HttpGet("fichajes")]
    public async Task<IActionResult> Fichajes(
        [FromQuery] string? search,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken ct)
    {
        var q = _db.StagingIntratimeFichajes.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(f => f.UserIdExterno.Contains(search));
        if (desde.HasValue)
            q = q.Where(f => DateOnly.FromDateTime(f.Entrada) >= desde.Value);
        if (hasta.HasValue)
            q = q.Where(f => DateOnly.FromDateTime(f.Entrada) <= hasta.Value);
        return Ok(await q.OrderByDescending(f => f.Entrada).ToListAsync(ct));
    }
}

[ApiController]
[Route("api/payhawk")]
[Authorize(Roles = "Administrator,Fico")]
public class PayHawkController : ControllerBase
{
    private readonly AppDbContext _db;
    public PayHawkController(AppDbContext db) { _db = db; }

    [HttpGet("gastos")]
    public async Task<IActionResult> Gastos(
        [FromQuery] string? search,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        CancellationToken ct)
    {
        var q = _db.StagingPayHawkGastos.AsNoTracking();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(g => g.Categoria.Contains(search));
        if (desde.HasValue)
            q = q.Where(g => g.Fecha >= desde.Value);
        if (hasta.HasValue)
            q = q.Where(g => g.Fecha <= hasta.Value);
        return Ok(await q.OrderByDescending(g => g.Fecha).ToListAsync(ct));
    }
}
