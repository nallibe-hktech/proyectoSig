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
    private readonly IProjectRepository _projectRepo;
    public CeleroServiceMappingsController(AppDbContext db, IProjectRepository projectRepo)
    {
        _db = db;
        _projectRepo = projectRepo;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.CeleroServiceMappings.AsNoTracking().ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CeleroServiceMappingRequest req, CancellationToken ct)
    {
        var project = await _projectRepo.GetByIdAsync(req.ProjectId, ct);
        if (project is null) return NotFound($"Proyecto {req.ProjectId} no encontrado");

        var mapping = new CeleroServiceMapping { CeleroServiceName = req.CeleroServiceName, ProjectId = req.ProjectId, Descripcion = req.Descripcion, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
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
    private readonly IActionRepository _actionRepo;
    public CeleroMissionMappingsController(AppDbContext db, IActionRepository actionRepo)
    {
        _db = db;
        _actionRepo = actionRepo;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) =>
        Ok(await _db.CeleroMissionMappings.AsNoTracking().ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(CeleroMissionMappingRequest req, CancellationToken ct)
    {
        var action = await _actionRepo.GetByIdAsync(req.ActionId, ct);
        if (action is null) return NotFound($"Acción {req.ActionId} no encontrada");

        var mapping = new CeleroMissionMapping { CeleroMissionName = req.CeleroMissionName, ActionId = req.ActionId, Descripcion = req.Descripcion, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
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
public record CeleroServiceMappingRequest(string CeleroServiceName, int ProjectId, string? Descripcion);
public record CeleroMissionMappingRequest(string CeleroMissionName, int ActionId, string? Descripcion);
