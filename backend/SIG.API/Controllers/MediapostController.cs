using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/mediapost")]
[Authorize]
public class MediapostController : ControllerBase
{
    private readonly IMediapostService _service;

    public MediapostController(IMediapostService service) => _service = service;

    /// <summary>Obtiene pedidos de envío paginados</summary>
    [HttpGet("pedidos")]
    public async Task<IActionResult> GetPedidos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? estado = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetPedidosAsync(page, pageSize, search, estado, ct);
        return Ok(result);
    }

    /// <summary>Obtiene recepciones de envío paginadas</summary>
    [HttpGet("recepciones")]
    public async Task<IActionResult> GetRecepciones(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetRecepcionesAsync(page, pageSize, search, ct);
        return Ok(result);
    }

    /// <summary>Obtiene detalles de un pedido específico</summary>
    [HttpGet("pedidos/{id:int}")]
    public async Task<IActionResult> GetPedidoById(int id, CancellationToken ct = default)
    {
        var result = await _service.GetPedidoByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Obtiene detalles de una recepción específica</summary>
    [HttpGet("recepciones/{id:int}")]
    public async Task<IActionResult> GetRecepcionById(int id, CancellationToken ct = default)
    {
        var result = await _service.GetRecepcionByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Obtiene KPIs de distribución para el dashboard</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        CancellationToken ct = default)
    {
        var result = await _service.GetDashboardAsync(desde, hasta, ct);
        return Ok(result);
    }
}
