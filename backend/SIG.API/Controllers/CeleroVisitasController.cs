using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using System.Security.Claims;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/celero-visitas")]
[Authorize]
public class CeleroVisitasController : ControllerBase
{
    private readonly ICeleroVisitaService _svc;

    public CeleroVisitasController(ICeleroVisitaService svc)
    {
        _svc = svc;
    }

    private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new InvalidOperationException("NameIdentifier claim not found"));

    /// <summary>
    /// Listar visitas sincronizadas de Celero con paginación
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? searchNif = null,
        [FromQuery] string? searchService = null,
        CancellationToken ct = default) =>
        Ok(await _svc.ListAsync(page, pageSize, searchNif, searchService, ct));

    /// <summary>
    /// Obtener detalle de una visita
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id, CancellationToken ct) =>
        Ok(await _svc.GetByIdAsync(id, ct));

    /// <summary>
    /// Actualizar mapeos y anotaciones de una visita
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CeleroVisitaUpdateRequest req, CancellationToken ct) =>
        Ok(await _svc.UpdateAsync(id, req, UserId, ct));


}
