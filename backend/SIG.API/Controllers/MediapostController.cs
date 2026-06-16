using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Infrastructure.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/mediapost")]
[Authorize]
public class MediapostController : ControllerBase
{
    private readonly IMediapostService _service;
    private readonly MediapostSyncService _syncService;

    public MediapostController(IMediapostService service, MediapostSyncService syncService)
    {
        _service = service;
        _syncService = syncService;
    }

    /// <summary>Obtiene pedidos paginados</summary>
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

    /// <summary>Obtiene recepciones paginadas</summary>
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

    /// <summary>Carga un archivo Excel de Mediapost para procesamiento</summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Administrator,Admin SIG")]
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
            // Guardar archivo en la carpeta de Mediapost que monitorea MediapostExcelClient
            var baseDir = config["Integrations:Mediapost:BasePath"] ?? @"C:\dev\SIG-es\Mediapost\Mediapost\Documentación";
            Directory.CreateDirectory(baseDir);

            // Generar nombre de archivo con timestamp para evitar duplicados
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = tipo switch
            {
                "pedidos" => $"infpedsit11_{timestamp}{ext}",
                "recepciones" => $"infrecep07_{timestamp}{ext}",
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

    /// <summary>Sincroniza un archivo cargado de Pedidos (lee, deduplica, actualiza BD)</summary>
    [HttpPost("sync-file/pedidos")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    public async Task<IActionResult> SyncPedidos([FromQuery] string filePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { error = "filePath es requerido" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo no encontrado" });

        var result = await _syncService.SyncPedidosFromFileAsync(filePath, ct);
        return Ok(result);
    }

    /// <summary>Sincroniza un archivo cargado de Recepciones</summary>
    [HttpPost("sync-file/recepciones")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    public async Task<IActionResult> SyncRecepciones([FromQuery] string filePath, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(filePath))
            return BadRequest(new { error = "filePath es requerido" });

        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = "Archivo no encontrado" });

        var result = await _syncService.SyncRecepcionesFromFileAsync(filePath, ct);
        return Ok(result);
    }
}
