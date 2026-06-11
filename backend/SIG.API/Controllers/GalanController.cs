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

    /// <summary>Carga un archivo CSV/Excel de Galán para procesamiento</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string tipo, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No se proporcionó archivo" });

        var validExtensions = new[] { ".xlsx", ".csv" };
        var ext = Path.GetExtension(file.FileName);
        if (!validExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { error = "Solo se aceptan archivos .xlsx o .csv" });

        try
        {
            // Guardar archivo en la carpeta de Galán que monitorea GalanCsvClient
            var baseDir = @"C:\Projects\workspaces\SIG-es\Galán\Galán";
            Directory.CreateDirectory(baseDir);

            // Generar nombre de archivo con timestamp para evitar duplicados
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = tipo switch
            {
                "entradas" => $"Entradas_{timestamp}{ext}",
                "salidas" => $"Salidas_{timestamp}{ext}",
                "stock" => $"STOCK_celda_{timestamp}{ext}",
                "almacenaje" => $"ALMACENAJE SIG {timestamp}{ext}",
                "facturas" => $"FACT_MENSUAL_{timestamp}{ext}",
                _ => $"Upload_{tipo}_{timestamp}{ext}"
            };

            var filePath = Path.Combine(baseDir, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            return Ok(new
            {
                success = true,
                mensaje = $"Archivo '{fileName}' cargado exitosamente. Será procesado en la próxima sincronización.",
                nombre = fileName,
                tipo = tipo,
                tamaño = file.Length,
                ruta = filePath
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error guardando archivo: {ex.Message}" });
        }
    }
}
