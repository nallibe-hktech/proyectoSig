using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

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
