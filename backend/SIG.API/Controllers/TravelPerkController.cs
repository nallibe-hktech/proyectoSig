using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SIG.Application.Interfaces.Services;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/travelperk")]
[Authorize]
public class TravelPerkController : ControllerBase
{
    private readonly ITravelPerkService _service;
    private readonly ISyncService _sync;

    public TravelPerkController(ITravelPerkService service, ISyncService sync)
    {
        _service = service;
        _sync = sync;
    }

    /// <summary>Lista las líneas de TravelPerk sincronizadas (hoja "report" del Excel), paginadas.</summary>
    [HttpGet("lineas")]
    public async Task<IActionResult> GetLineas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        [FromQuery] bool soloNoMaestro = false,
        CancellationToken ct = default) =>
        Ok(await _service.ListAsync(page, pageSize, search, soloNoMaestro, ct));

    /// <summary>KPIs de imputación de TravelPerk para la cabecera del dashboard.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct = default) =>
        Ok(await _service.GetKpisAsync(ct));

    /// <summary>
    /// Sube el Excel de TravelPerk (descarga del portal) y lo deja en la carpeta que vigila el cliente Excel,
    /// disparando la sincronización inmediata (igual que Galán/Mediapost). La carpeta es la misma que en
    /// producción sincroniza SharePoint, por lo que subir aquí o dejarlo en SharePoint son canales equivalentes.
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Administrator,Admin SIG")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromServices] IConfiguration config,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No se proporcionó archivo" });

        var ext = Path.GetExtension(file.FileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Solo se aceptan archivos .xlsx" });

        try
        {
            var baseDir = config["Integrations:TravelPerk:BasePath"] ?? @"C:\dev\SIG-es\TravelPerk";
            Directory.CreateDirectory(baseDir);

            // Nombre con timestamp: el cliente Excel lee siempre el .xlsx más reciente de la carpeta.
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"TravelPerk_{timestamp}{ext}";
            var filePath = Path.Combine(baseDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            // Auto-sync inmediato tras guardar (lee el fichero recién subido → staging con imputación CECO).
            var sync = await _sync.SyncAsync("travelperk", ct);

            return Ok(new
            {
                success = sync.Exito,
                mensaje = sync.Exito
                    ? $"Archivo '{fileName}' cargado y sincronizado: {sync.RegistrosInsertados} líneas nuevas."
                    : $"Archivo '{fileName}' cargado pero la sincronización falló.",
                nombre = fileName,
                tamaño = file.Length,
                sync = new
                {
                    registrosInsertados = sync.RegistrosInsertados,
                    registrosDuplicados = sync.RegistrosActualizados,
                    registrosError = sync.RegistrosError
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error guardando archivo: {ex.Message}" });
        }
    }
}
