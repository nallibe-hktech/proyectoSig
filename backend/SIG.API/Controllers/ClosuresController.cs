using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Enums;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/closures")]
[Authorize]
public class ClosuresController : ControllerBase
{
    private readonly IClosureService _svc;
    private readonly IClosureValidationService _validationSvc;
    public ClosuresController(IClosureService svc, IClosureValidationService validationSvc)
    {
        _svc = svc;
        _validationSvc = validationSvc;
    }
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

    [HttpGet("{id:int}/alertas")]
    public async Task<IActionResult> GetAlertas(int id, CancellationToken ct)
    {
        // Validate user has access to this closure
        var closure = await _svc.GetByIdForUserAsync(id, UserId, ct);
        var alertas = await _validationSvc.GetAlertasAsync(id, ct);
        return Ok(alertas);
    }

    [HttpPost("{id:int}/alertas/{alertaId:int}/confirmar")]
    [Authorize(Roles = "ProjectManager,Backoffice,Fico,Direction,Administrator")]
    public async Task<IActionResult> ConfirmarAlerta(int id, int alertaId, CancellationToken ct)
    {
        // Validate user has access to this closure
        var closure = await _svc.GetByIdForUserAsync(id, UserId, ct);
        await _validationSvc.ConfirmarAdvertenciaAsync(alertaId, UserId, ct);
        // Return updated closure with alerts
        return Ok(await _svc.GetByIdForUserAsync(id, UserId, ct));
    }

    [HttpGet("todas-alertas")]
    public async Task<IActionResult> GetAllAlertas(CancellationToken ct)
    {
        // Get all closures for this user with their alerts
        var result = await _svc.ListAsync(new ApprovalFilterRequest(null, null, null, null, null, null, null, null), UserId, ct);
        var alertas = new List<dynamic>();

        foreach (var closure in result.Items)
        {
            var detail = await _svc.GetByIdForUserAsync(closure.Id, UserId, ct);
            foreach (var alerta in detail.Alertas)
            {
                alertas.Add(new
                {
                    alerta.Id,
                    alerta.Tipo,
                    alerta.Codigo,
                    alerta.Descripcion,
                    alerta.Confirmada,
                    ClosureId = closure.Id,
                    ClosureNombre = closure.ProjectNombre + " — " + closure.PeriodNombre
                });
            }
        }

        return Ok(alertas.OrderByDescending(a => a.Confirmada).ThenByDescending(a => a.Tipo == TipoAlerta.Bloqueante));
    }

    private static uint ParseIfMatch(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return 0;
        var trim = v.Trim('"', ' ');
        return uint.TryParse(trim, out var rv) ? rv : 0;
    }
}
