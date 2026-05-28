using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/closures")]
[Authorize]
public class ClosuresController : ControllerBase
{
    private readonly IClosureService _svc;
    public ClosuresController(IClosureService svc) { _svc = svc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ApprovalFilterRequest filter, CancellationToken ct) =>
        Ok(await _svc.ListAsync(filter, UserId, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) =>
        Ok(await _svc.GetByIdForUserAsync(id, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "ProjectManager,Backoffice,Administrator")]
    public async Task<IActionResult> Create(ClosureCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPost("{id:int}/recalcular")]
    [Authorize(Roles = "ProjectManager,Backoffice,Administrator")]
    public async Task<IActionResult> Recalc(int id, ClosureRecalcRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        uint rv = ParseIfMatch(ifMatch);
        return Ok(await _svc.RecalcAsync(id, req, rv, UserId, ct));
    }

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = "ProjectManager,Backoffice,Fico,Direction,Administrator")]
    public async Task<IActionResult> Approve(int id, ClosureApproveRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        uint rv = ParseIfMatch(ifMatch);
        return Ok(await _svc.ApproveAsync(id, req, rv, UserId, ct));
    }

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = "Backoffice,Fico,Direction,Administrator")]
    public async Task<IActionResult> Reject(int id, ClosureRejectRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct)
    {
        uint rv = ParseIfMatch(ifMatch);
        return Ok(await _svc.RejectAsync(id, req, rv, UserId, ct));
    }

    private static uint ParseIfMatch(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return 0;
        var trim = v.Trim('"', ' ');
        return uint.TryParse(trim, out var rv) ? rv : 0;
    }
}
