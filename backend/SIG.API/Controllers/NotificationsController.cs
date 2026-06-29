using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

// Notificaciones in-app del usuario actual (campana del shell). El circuito de devolución de cierre
// genera notificaciones "CierreDevuelto" para el aprobador anterior.
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _svc;
    public NotificationsController(INotificationService svc) { _svc = svc; }
    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool soloNoLeidas = false, [FromQuery] int take = 20, CancellationToken ct = default) =>
        Ok(await _svc.ListForUserAsync(UserId, soloNoLeidas, take, ct));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount(CancellationToken ct) =>
        Ok(await _svc.CountUnreadAsync(UserId, ct));

    [HttpPost("{id:int}/leer")]
    public async Task<IActionResult> MarcarLeida(int id, CancellationToken ct)
    {
        await _svc.MarkReadAsync(id, UserId, ct);
        return NoContent();
    }

    [HttpPost("leer-todas")]
    public async Task<IActionResult> MarcarTodasLeidas(CancellationToken ct)
    {
        await _svc.MarkAllReadAsync(UserId, ct);
        return NoContent();
    }
}
