using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Infrastructure.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/galan")]
[Authorize]
public class GalanController : ControllerBase
{
    private readonly IGalanService _service;
    private readonly GalanSyncService _syncService;

    public GalanController(IGalanService service, GalanSyncService syncService)
    {
        _service = service;
        _syncService = syncService;
    }

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

    /// <summary>Obtiene estado actual del stock paginado</summary>
    [HttpGet("stock")]
    public async Task<IActionResult> GetStock(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _service.GetStockAsync(page, pageSize, ct);
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
    [Authorize(Roles = "Administrator")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string tipo, [FromServices] IConfiguration config, CancellationToken ct)
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
            var baseDir = config["Integrations:Galan:BasePath"] ?? @"C:\dev\SIG-es\Galán\Galán";
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

            // Auto-trigger sync immediately after file save
            var syncResult = tipo switch
            {
                "entradas" => await _syncService.SyncEntradasFromFileAsync(filePath, ct),
                "salidas" => await _syncService.SyncSalidasFromFileAsync(filePath, ct),
                "stock" => await _syncService.SyncStockFromFileAsync(filePath, ct),
                "almacenaje" => await _syncService.SyncAlmacenajeFromFileAsync(filePath, ct),
                "facturas" => new Application.DTOs.FileSyncResultDto(
                    "Facturas",
                    false,
                    0, 0, 0, 0,
                    "Sincronización de facturas mensual aún no implementada"
                ),
                _ => new Application.DTOs.FileSyncResultDto(
                    tipo,
                    false,
                    0, 0, 0, 0,
                    $"Tipo de archivo desconocido: {tipo}"
                )
            };

            return Ok(new
            {
                success = syncResult.Exito,
                mensaje = syncResult.Exito
                    ? $"Archivo '{fileName}' cargado y sincronizado exitosamente."
                    : $"Archivo '{fileName}' cargado pero sincronización falló: {syncResult.MensajeError}",
                nombre = fileName,
                tipo = tipo,
                tamaño = file.Length,
                ruta = filePath,
                sync = new
                {
                    success = syncResult.Exito,
                    registrosInsertados = syncResult.RegistrosInsertados,
                    registrosActualizados = syncResult.RegistrosActualizados,
                    registrosDuplicados = syncResult.RegistrosDuplicados,
                    registrosError = syncResult.RegistrosError,
                    error = syncResult.MensajeError
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error guardando archivo: {ex.Message}" });
        }
    }

    /// <summary>Sincroniza un archivo cargado de Entradas (lee, deduplica, actualiza BD)</summary>
    [HttpPost("sync-file/entradas")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    public async Task<IActionResult> SyncEntradas([FromQuery] string filePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { error = "filePath es requerido" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo no encontrado" });

        var result = await _syncService.SyncEntradasFromFileAsync(filePath, ct);
        return Ok(result);
    }

    /// <summary>Sincroniza un archivo cargado de Salidas/Facturas</summary>
    [HttpPost("sync-file/salidas")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    public async Task<IActionResult> SyncSalidas([FromQuery] string filePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { error = "filePath es requerido" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo no encontrado" });

        var result = await _syncService.SyncSalidasFromFileAsync(filePath, ct);
        return Ok(result);
    }

    /// <summary>Sincroniza un archivo cargado de Almacenaje</summary>
    [HttpPost("sync-file/almacenaje")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    public async Task<IActionResult> SyncAlmacenaje([FromQuery] string filePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { error = "filePath es requerido" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo no encontrado" });

        var result = await _syncService.SyncAlmacenajeFromFileAsync(filePath, ct);
        return Ok(result);
    }

    /// <summary>Sincroniza un archivo cargado de Stock</summary>
    [HttpPost("sync-file/stock")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    public async Task<IActionResult> SyncStock([FromQuery] string filePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { error = "filePath es requerido" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo no encontrado" });

        var result = await _syncService.SyncStockFromFileAsync(filePath, ct);
        return Ok(result);
    }
}
