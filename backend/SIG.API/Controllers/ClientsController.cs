using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientService _svc;
    public ClientsController(IClientService svc) { _svc = svc; }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null, CancellationToken ct = default) =>
        Ok(await _svc.ListAsync(UserId, page, pageSize, search, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) => Ok(await _svc.GetByIdAsync(id, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(ClientCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int id, ClientUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, UserId, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, UserId, ct);
        return NoContent();
    }
}

// Incidencias del cliente (PPT slide 6). Lectura para autenticados; escritura solo Administrator
// (suposición registrada en SUPOSICIONES_CRITICAS.md, ajustable si el cliente pide otro rol).
[ApiController]
[Route("api/clients/{clientId:int}/incidencias")]
[Authorize]
public class ClienteIncidenciasController : ControllerBase
{
    private readonly IClienteIncidenciaService _svc;
    public ClienteIncidenciasController(IClienteIncidenciaService svc) { _svc = svc; }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List(int clientId, CancellationToken ct) =>
        Ok(await _svc.ListByClientAsync(clientId, UserId, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int clientId, int id, CancellationToken ct) =>
        Ok(await _svc.GetByIdAsync(id, clientId, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(int clientId, ClienteIncidenciaCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(clientId, req, UserId, ct);
        return CreatedAtAction(nameof(Get), new { clientId, id = r.Id }, r);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int clientId, int id, ClienteIncidenciaUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, clientId, req, UserId, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int clientId, int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, clientId, UserId, ct);
        return NoContent();
    }
}
