using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Infrastructure.Persistence;

namespace SIG.API.Controllers;

[ApiController]
[Route("api/sgpv")]
[Authorize]
public class SgpvController : ControllerBase
{
    private readonly AppDbContext _db;

    public SgpvController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Obtiene visitas de SGPV paginadas</summary>
    [HttpGet("visitas/paginated")]
    public async Task<IActionResult> GetVisitasPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = _db.StagingSgpvVisitas.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(v =>
                EF.Functions.ILike(v.VisitaIdExterno, $"%{searchLower}%") ||
                EF.Functions.ILike(v.ResourceNif, $"%{searchLower}%") ||
                EF.Functions.ILike(v.CentroNombre, $"%{searchLower}%") ||
                EF.Functions.ILike(v.ServiceName, $"%{searchLower}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(v => v.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new SgpvVisitaDashboardDto
            {
                VisitaIdExterno = v.VisitaIdExterno,
                ResourceNif = v.ResourceNif ?? "",
                CentroId = v.IdCentro, // Use new field name
                CentroNombre = v.Cliente, // Use new field name (Cliente instead of CentroNombre)
                ServiceName = v.TipoVisita, // Use new field name (TipoVisita instead of ServiceName)
                Fecha = v.Fecha,
                HorasDuracion = v.HorasDuracion
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<SgpvVisitaDashboardDto>(items, total, page, pageSize));
    }

    /// <summary>Obtiene centros de SGPV paginados</summary>
    [HttpGet("centros/paginated")]
    public async Task<IActionResult> GetCentrosPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = _db.StagingSgpvCentros.AsNoTracking();

        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                EF.Functions.ILike(c.CentroId, $"%{searchLower}%") ||
                EF.Functions.ILike(c.CentroNombre, $"%{searchLower}%") ||
                EF.Functions.ILike(c.Provincia, $"%{searchLower}%") ||
                EF.Functions.ILike(c.Ciudad, $"%{searchLower}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.CentroNombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new SgpvCentroDashboardDto
            {
                CentroId = c.CentroId,
                CentroNombre = c.CentroNombre,
                Provincia = c.Provincia,
                Ciudad = c.Ciudad
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<SgpvCentroDashboardDto>(items, total, page, pageSize));
    }

    /// <summary>Obtiene productos de SGPV paginados</summary>
    [HttpGet("productos/paginated")]
    public async Task<IActionResult> GetProductosPaginated(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var query = _db.StagingSgpvProductos.AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                EF.Functions.ILike(p.IdProducto, $"%{searchLower}%") ||
                EF.Functions.ILike(p.Cliente, $"%{searchLower}%") ||
                EF.Functions.ILike(p.Referencia, $"%{searchLower}%") ||
                EF.Functions.ILike(p.Categoria, $"%{searchLower}%"));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new SgpvProductoDto
            {
                Id = p.Id,
                IdProducto = p.IdProducto,
                IdCliente = p.IdCliente,
                Cliente = p.Cliente,
                Categoria = p.Categoria,
                Subcategoria = p.Subcategoria,
                CodigoReferencia = p.CodigoReferencia,
                Referencia = p.Referencia,
                EAN = p.EAN,
                Marca = p.Marca,
                PVPRecomendado = p.PVPRecomendado,
                Competencia = p.Competencia,
                Activo = p.Activo
            })
            .ToListAsync(ct);

        return Ok(new PagedResult<SgpvProductoDto>(items, total, page, pageSize));
    }
}

/// <summary>DTO for SGPV Visita Dashboard</summary>
public class SgpvVisitaDashboardDto
{
    public string VisitaIdExterno { get; set; } = null!;
    public string ResourceNif { get; set; } = "";
    public string CentroId { get; set; } = null!;
    public string? CentroNombre { get; set; }
    public string? ServiceName { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal? HorasDuracion { get; set; }
}

/// <summary>DTO for SGPV Centro Dashboard</summary>
public class SgpvCentroDashboardDto
{
    public string CentroId { get; set; } = null!;
    public string? CentroNombre { get; set; }
    public string? Provincia { get; set; }
    public string? Ciudad { get; set; }
}

/// <summary>DTO for SGPV Producto</summary>
public class SgpvProductoDto
{
    public int Id { get; set; }
    public string IdProducto { get; set; } = null!;
    public string IdCliente { get; set; } = null!;
    public string Cliente { get; set; } = null!;
    public string Categoria { get; set; } = null!;
    public string? Subcategoria { get; set; }
    public string? CodigoReferencia { get; set; }
    public string? Referencia { get; set; }
    public string? EAN { get; set; }
    public string? Marca { get; set; }
    public string PVPRecomendado { get; set; } = "0";
    public string Competencia { get; set; } = "No";
    public bool Activo { get; set; }
}
