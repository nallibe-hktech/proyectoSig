using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) { _auth = auth; }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var res = await _auth.LoginAsync(req, ip, ct);
        return Ok(res);
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), 200)]
    public async Task<ActionResult<RefreshResponse>> Refresh(RefreshRequest req, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var res = await _auth.RefreshAsync(req, ip, ct);
        return Ok(res);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest req, CancellationToken ct)
    {
        var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));
        await _auth.LogoutAsync(uid, req.RefreshToken, ct);
        return NoContent();
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UsuarioBriefDto), 200)]
    public async Task<ActionResult<UsuarioBriefDto>> Me(CancellationToken ct)
    {
        var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));
        var res = await _auth.GetMeAsync(uid, ct);
        return Ok(res);
    }
}
