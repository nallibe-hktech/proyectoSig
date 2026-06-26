using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/periods")]
[AllowAnonymous]
public class PeriodsController : ControllerBase
{
    private readonly IPeriodService _svc;
    public PeriodsController(IPeriodService svc) { _svc = svc; }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct = default) =>
        Ok(await _svc.ListAsync(ct));

    [HttpGet("paginated")]
    [Authorize]
    public async Task<IActionResult> ListPaginated([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        Ok(await _svc.ListPaginatedAsync(page, pageSize, ct));

    [HttpGet("activo")]
    [Authorize]
    public async Task<IActionResult> GetActivo(CancellationToken ct) => Ok(await _svc.GetActivoAsync(ct));

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await _svc.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(PeriodCreateRequest req, CancellationToken ct) =>
        StatusCode(201, await _svc.CreateAsync(req, ct));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int id, PeriodUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, ct));

    [HttpPost("{id:int}/cerrar")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Cerrar(int id, CancellationToken ct) => Ok(await _svc.CerrarAsync(id, ct));

    [HttpPost("{id:int}/reabrir")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Reabrir(int id, CancellationToken ct) => Ok(await _svc.ReabrirAsync(id, ct));
}
