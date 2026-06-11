using Microsoft.EntityFrameworkCore;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities.Staging;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class MediapostService : IMediapostService
{
    private readonly AppDbContext _db;

    public MediapostService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<MediapostPedidoDto>> GetPedidosAsync(
        int page, int pageSize, string? search, string? estado, CancellationToken ct)
    {
        var query = _db.StagingMediapostPedidos.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.PedidoId, s) ||
                EF.Functions.ILike(p.ReferenciaPedido, s) ||
                EF.Functions.ILike(p.CodigoArticulo, s) ||
                EF.Functions.ILike(p.DestinatarioNombre, s));
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            query = query.Where(p => p.Estado == estado);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.FechaPedido)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new MediapostPedidoDto(
                p.PedidoId,
                p.ReferenciaPedido,
                p.CodigoArticulo,
                p.FechaPedido,
                p.Cantidad,
                p.Estado,
                p.DestinatarioNombre,
                p.DireccionEntrega,
                p.CodigoPostal,
                p.Ciudad,
                p.Provincia))
            .ToListAsync(ct);

        return new PagedResult<MediapostPedidoDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<MediapostRecepcionDto>> GetRecepcionesAsync(
        int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingMediapostRecepciones.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(r =>
                EF.Functions.ILike(r.RecepcionId, s) ||
                EF.Functions.ILike(r.ReferenciaRecepcion, s) ||
                EF.Functions.ILike(r.CodigoArticulo, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.FechaRecepcion)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new MediapostRecepcionDto(
                r.RecepcionId,
                r.ReferenciaRecepcion,
                r.CodigoArticulo,
                r.FechaRecepcion,
                r.Cantidad,
                r.CantidadDañada,
                r.Estado,
                r.Almacen,
                r.Observaciones))
            .ToListAsync(ct);

        return new PagedResult<MediapostRecepcionDto>(items, total, page, pageSize);
    }

    public async Task<MediapostPedidoDto?> GetPedidoByIdAsync(int id, CancellationToken ct)
    {
        var item = await _db.StagingMediapostPedidos.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (item == null) return null;

        return new MediapostPedidoDto(
            item.PedidoId,
            item.ReferenciaPedido,
            item.CodigoArticulo,
            item.FechaPedido,
            item.Cantidad,
            item.Estado,
            item.DestinatarioNombre,
            item.DireccionEntrega,
            item.CodigoPostal,
            item.Ciudad,
            item.Provincia);
    }

    public async Task<MediapostRecepcionDto?> GetRecepcionByIdAsync(int id, CancellationToken ct)
    {
        var item = await _db.StagingMediapostRecepciones.FindAsync(new object?[] { id }, cancellationToken: ct);
        if (item == null) return null;

        return new MediapostRecepcionDto(
            item.RecepcionId,
            item.ReferenciaRecepcion,
            item.CodigoArticulo,
            item.FechaRecepcion,
            item.Cantidad,
            item.CantidadDañada,
            item.Estado,
            item.Almacen,
            item.Observaciones);
    }

    public async Task<MediapostDashboardDto> GetDashboardAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        var desdeDateTime = desde.ToDateTime(TimeOnly.MinValue);
        var hastaDateTime = hasta.ToDateTime(TimeOnly.MaxValue);

        var pedidos = await _db.StagingMediapostPedidos
            .Where(p => p.FechaPedido >= desdeDateTime && p.FechaPedido <= hastaDateTime)
            .ToListAsync(ct);

        var recepciones = await _db.StagingMediapostRecepciones
            .Where(r => r.FechaRecepcion >= desdeDateTime && r.FechaRecepcion <= hastaDateTime)
            .ToListAsync(ct);

        var pedidosEntregados = pedidos.Count(p => p.Estado == "Entregado" || p.Estado == "Completado");
        var pedidosPendientes = pedidos.Count(p => p.Estado == "Pendiente" || p.Estado == "En tránsito");
        var pedidosRechazados = pedidos.Count(p => p.Estado == "Rechazado" || p.Estado == "Devuelto");
        var tasaEntrega = pedidos.Count > 0 ? (pedidosEntregados * 100m) / pedidos.Count : 0;
        var unidadesDestrozadas = recepciones.Sum(r => r.CantidadDañada ?? 0);

        var pedidosPendientesDetalle = pedidos
            .Where(p => p.Estado == "Pendiente" || p.Estado == "En tránsito")
            .Select(p => new MediapostPedidoPendienteDto
            {
                PedidoId = p.PedidoId,
                ReferenciaPedido = p.ReferenciaPedido,
                DestinatarioNombre = p.DestinatarioNombre ?? "",
                Estado = p.Estado,
                FechaPedido = p.FechaPedido,
                DiasEnTransito = (int)(DateTime.UtcNow - p.FechaPedido).TotalDays
            })
            .OrderByDescending(p => p.DiasEnTransito)
            .Take(20)
            .ToList();

        return new MediapostDashboardDto
        {
            PedidosTotal = pedidos.Count,
            PedidosEntregados = pedidosEntregados,
            PedidosPendientes = pedidosPendientes,
            PedidosRechazados = pedidosRechazados,
            TasaEntrega = tasaEntrega,
            RecepcionesTotal = recepciones.Count,
            UnidadesRecibidas = recepciones.Sum(r => r.Cantidad),
            UnidadesDestrozadas = unidadesDestrozadas,
            CostoDistribucion = pedidos.Count * 2.5m, // Estimación: 2.5€ por pedido
            PedidosPendientesDetalle = pedidosPendientesDetalle
        };
    }
}
