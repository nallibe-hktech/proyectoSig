using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

// Ola 3b (#10): endpoints equivalentes a los de ClosuresController, uno por raíz de cierre.
// Mismas autorizaciones (3a): Grupo = Facilitador/Interlocutor/Gestor/Administrator; Fico = Fico/Administrator.
[Authorize]
public abstract class CierresControllerBase : ControllerBase
{
    protected abstract ICierreService Svc { get; }
    protected int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] ApprovalFilterRequest filter, CancellationToken ct) =>
        Ok(await Svc.ListAsync(filter, UserId, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) =>
        Ok(await Svc.GetByIdForUserAsync(id, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "Facilitador,Interlocutor,Gestor,ProjectManager,Backoffice,Administrator")]
    public async Task<IActionResult> Create(CierreCreateRequest req, CancellationToken ct)
    {
        var r = await Svc.CreateAsync(req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPost("{id:int}/recalcular")]
    [Authorize(Roles = "Facilitador,Interlocutor,Gestor,ProjectManager,Backoffice,Administrator")]
    public async Task<IActionResult> Recalc(int id, CierreRecalcRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) =>
        Ok(await Svc.RecalcAsync(id, req, ParseIfMatch(ifMatch), UserId, ct));

    [HttpPost("{id:int}/aprobar")]
    [Authorize(Roles = "Facilitador,Interlocutor,Gestor,Fico,Administrator")]
    public async Task<IActionResult> Approve(int id, CierreApproveRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) =>
        Ok(await Svc.ApproveAsync(id, req, ParseIfMatch(ifMatch), UserId, ct));

    [HttpPost("{id:int}/rechazar")]
    [Authorize(Roles = "Facilitador,Interlocutor,Gestor,Fico,Administrator")]
    public async Task<IActionResult> Reject(int id, CierreRejectRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) =>
        Ok(await Svc.RejectAsync(id, req, ParseIfMatch(ifMatch), UserId, ct));

    [HttpPost("{id:int}/lines/{lineId:int}/override")]
    [Authorize(Roles = "Facilitador,Interlocutor,Gestor,ProjectManager,Backoffice,Administrator")]
    public async Task<IActionResult> OverrideLine(int id, int lineId, CierreLineOverrideRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) =>
        Ok(await Svc.OverrideLineAsync(id, lineId, req, ParseIfMatch(ifMatch), UserId, ct));

    [HttpPost("{id:int}/lines/incentivo")]
    [Authorize(Roles = "Facilitador,Interlocutor,Gestor,ProjectManager,Backoffice,Administrator")]
    public async Task<IActionResult> AddIncentivo(int id, CierreLineIncentivoRequest req, [FromHeader(Name = "If-Match")] string? ifMatch, CancellationToken ct) =>
        Ok(await Svc.AddIncentivoAsync(id, req, ParseIfMatch(ifMatch), UserId, ct));

    [HttpGet("{id:int}/alertas")]
    public async Task<IActionResult> GetAlertas(int id, CancellationToken ct) =>
        Ok(await Svc.GetAlertasAsync(id, UserId, ct));

    [HttpPost("{id:int}/alertas/{alertaId:int}/confirmar")]
    [Authorize(Roles = "Facilitador,Interlocutor,Gestor,Fico,Administrator")]
    public async Task<IActionResult> ConfirmarAlerta(int id, int alertaId, CancellationToken ct) =>
        Ok(await Svc.ConfirmarAlertaAsync(id, alertaId, UserId, ct));

    [HttpGet("{id:int}/historial")]
    public async Task<IActionResult> Historial(int id, CancellationToken ct) =>
        Ok(await Svc.GetHistoryAsync(id, UserId, ct));

    protected static uint ParseIfMatch(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return 0;
        var trim = v.Trim('"', ' ');
        return uint.TryParse(trim, out var rv) ? rv : 0;
    }
}

[ApiController]
[Route("api/cierres-costes")]
public class CierresCostesController : CierresControllerBase
{
    private readonly ICierreCostesService _svc;
    public CierresCostesController(ICierreCostesService svc) { _svc = svc; }
    protected override ICierreService Svc => _svc;
}

[ApiController]
[Route("api/cierres-facturacion")]
public class CierresFacturacionController : CierresControllerBase
{
    private readonly ICierreFacturacionService _svc;
    public CierresFacturacionController(ICierreFacturacionService svc) { _svc = svc; }
    protected override ICierreService Svc => _svc;
}
