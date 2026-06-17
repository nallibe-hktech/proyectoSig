using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

// Ola 3b (#10): el panel agrega AMBOS tipos de cierre (CierreCostes + CierreFacturacion).
// Cada item indica su TipoCierre. El historial y las acciones por cierre viven en los
// controladores específicos (api/cierres-costes, api/cierres-facturacion).
[ApiController]
[Route("api/approvals")]
[Authorize]
public class ApprovalsController : ControllerBase
{
    private readonly IApprovalService _svc;
    public ApprovalsController(IApprovalService svc) { _svc = svc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ApprovalFilterRequest filter, CancellationToken ct) =>
        Ok(await _svc.ListAsync(filter, UserId, ct));

    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        Ok(await _svc.ListPendingForUserAsync(UserId, page, pageSize, ct));
}
