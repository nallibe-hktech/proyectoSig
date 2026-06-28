using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Enums;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/concepts")]
[Authorize]
public class ConceptsController : ControllerBase
{
    private readonly IConceptService _svc;
    private readonly IAuditService _auditSvc;
    public ConceptsController(IConceptService svc, IAuditService auditSvc) { _svc = svc; _auditSvc = auditSvc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] TipoConcepto? tipo = null, [FromQuery] string? search = null, CancellationToken ct = default) =>
        Ok(await _svc.ListAsync(UserId, page, pageSize, tipo, search, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await _svc.GetByIdAsync(id, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(ConceptCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator,Backoffice")]
    public async Task<IActionResult> Update(int id, ConceptUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, UserId, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, UserId, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/validar-formula")]
    [Authorize(Roles = "Administrator,Backoffice")]
    public async Task<IActionResult> ValidarFormula(int id, [FromBody] ValidarFormulaRequest req, CancellationToken ct) =>
        Ok(await _svc.ValidarFormulaAsync(req.FormulaJson, ct));

    [HttpGet("{id:int}/historial")]
    public async Task<IActionResult> GetHistorial(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var filter = new AuditLogFilterRequest(UserId: null, EntityType: "Concept", Action: null, Desde: null, Hasta: null, Page: page, PageSize: pageSize);
        var result = await _auditSvc.ListAsync(filter, ct);
        var conceptIdStr = id.ToString();
        var filtered = new PagedResult<AuditLogDto>(
            result.Items.Where(a => a.EntityId == conceptIdStr).ToList(),
            result.Total, result.Page, result.PageSize);
        return Ok(filtered);
    }
}
