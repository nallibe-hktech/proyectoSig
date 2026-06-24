using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Enums;

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

    // Cambio de estado desde el panel de detalle (registra una entrada en el histórico).
    [HttpPost("{id:int}/estado")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CambiarEstado(int clientId, int id, IncidenciaCambioEstadoRequest req, CancellationToken ct) =>
        Ok(await _svc.CambiarEstadoAsync(id, clientId, req, UserId, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int clientId, int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, clientId, UserId, ct);
        return NoContent();
    }
}

// Configuración de Factura (prototipo pantalla 25/28): categorías que agrupan conceptos de facturación
// por cliente. Lectura para autenticados; escritura solo Administrator (mismo criterio que Incidencias).
[ApiController]
[Route("api/clients/{clientId:int}/categorias-factura")]
[Authorize]
public class CategoriasFacturaController : ControllerBase
{
    private readonly ICategoriaFacturaService _svc;
    public CategoriasFacturaController(ICategoriaFacturaService svc) { _svc = svc; }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List(int clientId, CancellationToken ct) =>
        Ok(await _svc.ListByClientAsync(clientId, UserId, ct));

    [HttpGet("resumen")]
    public async Task<IActionResult> Resumen(int clientId, CancellationToken ct) =>
        Ok(await _svc.GetResumenAsync(clientId, UserId, ct));

    // Panel derecho del prototipo: conceptos de facturación del cliente con su estado (asignado / sin asignar).
    [HttpGet("conceptos-disponibles")]
    public async Task<IActionResult> ConceptosDisponibles(int clientId, CancellationToken ct) =>
        Ok(await _svc.ListConceptosDisponiblesAsync(clientId, UserId, ct));

    [HttpPost]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(int clientId, CategoriaFacturaCreateRequest req, CancellationToken ct)
    {
        var r = await _svc.CreateAsync(clientId, req, UserId, ct);
        return CreatedAtAction(nameof(List), new { clientId }, r);
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Update(int clientId, int id, CategoriaFacturaUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, clientId, req, UserId, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Delete(int clientId, int id, CancellationToken ct)
    {
        await _svc.DeleteAsync(id, clientId, UserId, ct);
        return NoContent();
    }
}

// Incidencias — pantalla de 1er nivel (prototipo): listado global de incidencias de todos los clientes
// accesibles por el usuario, con filtros cliente/tipo/estado y búsqueda. El alta/edición sigue siendo
// anidada bajo el cliente (ClienteIncidenciasController).
[ApiController]
[Route("api/incidencias")]
[Authorize]
public class IncidenciasController : ControllerBase
{
    private readonly IClienteIncidenciaService _svc;
    public IncidenciasController(IClienteIncidenciaService svc) { _svc = svc; }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 25, [FromQuery] string? search = null,
        [FromQuery] int? clientId = null, [FromQuery] string? tipo = null, [FromQuery] EstadoIncidencia? estado = null,
        CancellationToken ct = default) =>
        Ok(await _svc.ListAllAsync(UserId, page, pageSize, search, clientId, tipo, estado, ct));
}
