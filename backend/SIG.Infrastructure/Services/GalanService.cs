using Microsoft.EntityFrameworkCore;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities.Staging;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class GalanService : IGalanService
{
    private readonly AppDbContext _db;

    public GalanService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<GalanEntradaDto>> GetEntradasAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingGalanEntradas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.CodigoArticulo, s) ||
                EF.Functions.ILike(e.Descripcion, s) ||
                EF.Functions.ILike(e.Almacen, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(e => e.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new GalanEntradaDto(
                e.CodigoArticulo,
                e.CodigoDepartamento,
                e.CodigoFamilia,
                e.Descripcion,
                e.Fecha,
                e.Unidades,
                e.Empresa,
                e.Almacen,
                e.Celda))
            .ToListAsync(ct);

        return new PagedResult<GalanEntradaDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<GalanSalidaDto>> GetSalidasAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingGalanSalidas.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(sal =>
                EF.Functions.ILike(sal.CodigoArticulo, s) ||
                EF.Functions.ILike(sal.Descripcion, s) ||
                EF.Functions.ILike(sal.Destinatario, s) ||
                EF.Functions.ILike(sal.Albaran, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(sal => sal.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(sal => new GalanSalidaDto(
                sal.Albaran,
                sal.NumeroPedidoTercero,
                sal.CodigoArticulo,
                sal.CodigoDepartamento,
                sal.CodigoFamilia,
                sal.Descripcion,
                sal.Unidades,
                sal.CodigoTransporte,
                sal.Matricula,
                sal.Fecha,
                sal.Destinatario,
                sal.Almacen,
                sal.Celda))
            .ToListAsync(ct);

        return new PagedResult<GalanSalidaDto>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<GalanStockDto>> GetStockAsync(CancellationToken ct)
    {
        var items = await _db.StagingGalanStocks
            .OrderBy(s => s.Almacen)
            .ThenBy(s => s.CodigoCelda)
            .Select(s => new GalanStockDto(
                s.CodigoArticulo,
                s.CodigoDepartamento,
                s.CodigoFamilia,
                s.CodigoCelda,
                s.StockB,
                s.StockA,
                s.Stock,
                s.Almacen,
                s.Familia,
                s.SubFamilia,
                s.Descripcion))
            .ToListAsync(ct);

        return items;
    }

    public async Task<GalanEntradaDto?> GetEntradaByIdAsync(int id, CancellationToken ct)
    {
        var item = await _db.StagingGalanEntradas.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (item == null) return null;

        return new GalanEntradaDto(
            item.CodigoArticulo,
            item.CodigoDepartamento,
            item.CodigoFamilia,
            item.Descripcion,
            item.Fecha,
            item.Unidades,
            item.Empresa,
            item.Almacen,
            item.Celda);
    }

    public async Task<GalanSalidaDto?> GetSalidaByIdAsync(int id, CancellationToken ct)
    {
        var item = await _db.StagingGalanSalidas.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (item == null) return null;

        return new GalanSalidaDto(
            item.Albaran,
            item.NumeroPedidoTercero,
            item.CodigoArticulo,
            item.CodigoDepartamento,
            item.CodigoFamilia,
            item.Descripcion,
            item.Unidades,
            item.CodigoTransporte,
            item.Matricula,
            item.Fecha,
            item.Destinatario,
            item.Almacen,
            item.Celda);
    }

    public async Task<GalanStockDto?> GetStockByIdAsync(int id, CancellationToken ct)
    {
        var item = await _db.StagingGalanStocks.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (item == null) return null;

        return new GalanStockDto(
            item.CodigoArticulo,
            item.CodigoDepartamento,
            item.CodigoFamilia,
            item.CodigoCelda,
            item.StockB,
            item.StockA,
            item.Stock,
            item.Almacen,
            item.Familia,
            item.SubFamilia,
            item.Descripcion);
    }

    public async Task<GalanDashboardDto> GetDashboardAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var desdeDateTime = desde.ToDateTime(TimeOnly.MinValue);
        var hastaDateTime = hasta.ToDateTime(TimeOnly.MaxValue);

        var entradas = await _db.StagingGalanEntradas
            .Where(e => e.Fecha >= desdeDateTime && e.Fecha <= hastaDateTime)
            .ToListAsync(ct);

        var salidas = await _db.StagingGalanSalidas
            .Where(s => s.Fecha >= desdeDateTime && s.Fecha <= hastaDateTime)
            .ToListAsync(ct);

        var stock = await _db.StagingGalanStocks.ToListAsync(ct);

        var stockTotalValue = stock.Sum(s => s.Stock * 10m); // Estimación: 10€ por unidad
        var articulos = stock.GroupBy(s => s.CodigoArticulo).Count();
        var volumenMovido = entradas.Sum(e => e.Unidades) + salidas.Sum(s => s.Unidades);
        var costoLogisticoEstimado = salidas.Count * 5m; // Estimación: 5€ por salida

        return new GalanDashboardDto
        {
            StockTotalValue = stockTotalValue,
            EntradasCount = entradas.Count,
            SalidasCount = salidas.Count,
            CostoLogisticoTotal = costoLogisticoEstimado,
            ArticulosDiferentes = articulos,
            VolumenMovido = volumenMovido,
            AlertasStockBajo = stock
                .Where(s => s.Stock < 5) // Alerta cuando stock < 5 unidades
                .GroupBy(s => s.CodigoArticulo)
                .Select(g => new GalanStockBajoDto
                {
                    CodigoArticulo = g.Key,
                    Descripcion = g.First().Descripcion,
                    StockActual = g.Sum(s => s.Stock),
                    UmbraloAlerta = 5
                })
                .ToList()
        };
    }
}
