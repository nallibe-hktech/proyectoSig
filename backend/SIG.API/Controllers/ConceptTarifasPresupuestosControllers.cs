using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

// FASE 2: Tarifas por Concepto (nested under /concepts/{conceptId}/tarifas)
[ApiController]
[Route("api/concepts/{conceptId:int}/tarifas")]
[Authorize]
public class ConceptTarifasController : ControllerBase
{
    private readonly ITarifaConceptoService _svc;

    public ConceptTarifasController(ITarifaConceptoService svc) => _svc = svc;

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> ListByConcepto(int conceptId, CancellationToken ct) =>
        Ok(await _svc.ListByConceptAsync(conceptId, UserId, ct));

    [HttpGet("by-client/{clientId:int}")]
    public async Task<IActionResult> ListByConceptAndClient(int conceptId, int clientId, CancellationToken ct) =>
        Ok(await _svc.ListByConceptAndClientAsync(conceptId, clientId, UserId, ct));

    [HttpGet("paginated")]
    public async Task<IActionResult> ListPaginated(int conceptId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, CancellationToken ct = default) =>
        Ok(await _svc.ListPaginatedByConceptAsync(conceptId, UserId, page, pageSize, search, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int conceptId, int id, CancellationToken ct) =>
        Ok(await _svc.GetByIdAsync(id, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(int conceptId, [FromBody] TarifaConceptoCreateRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(conceptId, req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { conceptId, id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Backoffice")]
    public async Task<IActionResult> Update(int conceptId, int id, [FromBody] TarifaConceptoUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, conceptId, req, UserId, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int conceptId, int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, conceptId, UserId, ct);
        return NoContent();
    }
}

// FASE 2: Presupuestos por Concepto (nested under /concepts/{conceptId}/presupuestos)
[ApiController]
[Route("api/concepts/{conceptId:int}/presupuestos")]
[Authorize]
public class ConceptPresupuestosController : ControllerBase
{
    private readonly IPresupuestoConceptoService _svc;

    public ConceptPresupuestosController(IPresupuestoConceptoService svc) => _svc = svc;

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> ListByConcepto(int conceptId, CancellationToken ct) =>
        Ok(await _svc.ListByConceptAsync(conceptId, UserId, ct));

    [HttpGet("by-client/{clientId:int}")]
    public async Task<IActionResult> ListByConceptAndClient(int conceptId, int clientId, CancellationToken ct) =>
        Ok(await _svc.ListByConceptAndClientAsync(conceptId, clientId, UserId, ct));

    [HttpGet("paginated")]
    public async Task<IActionResult> ListPaginated(int conceptId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, CancellationToken ct = default) =>
        Ok(await _svc.ListPaginatedByConceptAsync(conceptId, UserId, page, pageSize, search, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int conceptId, int id, CancellationToken ct) =>
        Ok(await _svc.GetByIdAsync(id, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(int conceptId, [FromBody] PresupuestoConceptoCreateRequest req, CancellationToken ct)
    {
        var result = await _svc.CreateAsync(conceptId, req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { conceptId, id = result.Id }, result);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Backoffice")]
    public async Task<IActionResult> Update(int conceptId, int id, [FromBody] PresupuestoConceptoUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, conceptId, req, UserId, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int conceptId, int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, conceptId, UserId, ct);
        return NoContent();
    }
}
