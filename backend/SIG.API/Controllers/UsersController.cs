using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _svc;
    public UsersController(IUserService svc) { _svc = svc; }

    // Lectura accesible a cualquier usuario autenticado (dropdowns, asignaciones, etc.)
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, CancellationToken ct = default) =>
        Ok(await _svc.ListAsync(page, pageSize, search, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await _svc.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(UserCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(req, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int id, UserUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, ct));

    [HttpPut("{id:int}/password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(int id, UserPasswordChangeRequest req, CancellationToken ct)
    {
        var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));
        if (uid != id && !User.IsInRole("Administrator")) return Forbid();
        await _svc.ChangePasswordAsync(id, req, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, ct);
        return NoContent();
    }
}
