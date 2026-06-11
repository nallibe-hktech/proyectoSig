using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/galan")]
[Authorize]
public class GalanController : ControllerBase
{
    private readonly IGalanService _service;

    public GalanController(IGalanService service) => _service = service;

    /// <summary>Obtiene entradas de almacén paginadas</summary>
    [HttpGet("entradas")]
    public async Task<IActionResult> GetEntradas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetEntradasAsync(page, pageSize, search, ct);
        return Ok(result);
    }

    /// <summary>Obtiene salidas/despachos paginados</summary>
    [HttpGet("salidas")]
    public async Task<IActionResult> GetSalidas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var result = await _service.GetSalidasAsync(page, pageSize, search, ct);
        return Ok(result);
    }

    /// <summary>Obtiene estado actual del stock</summary>
    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(CancellationToken ct = default)
    {
        var result = await _service.GetStockAsync(ct);
        return Ok(result);
    }

    /// <summary>Obtiene detalles de una entrada específica</summary>
    [HttpGet("entradas/{id:int}")]
    public async Task<IActionResult> GetEntradaById(int id, CancellationToken ct = default)
    {
        var result = await _service.GetEntradaByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Obtiene detalles de una salida específica</summary>
    [HttpGet("salidas/{id:int}")]
    public async Task<IActionResult> GetSalidaById(int id, CancellationToken ct = default)
    {
        var result = await _service.GetSalidaByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Obtiene detalles de stock por celda</summary>
    [HttpGet("stock/{id:int}")]
    public async Task<IActionResult> GetStockById(int id, CancellationToken ct = default)
    {
        var result = await _service.GetStockByIdAsync(id, ct);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>Obtiene KPIs de logística para el dashboard</summary>
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
