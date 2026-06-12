using Microsoft.EntityFrameworkCore;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Exceptions;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class CeleroVisitaService : ICeleroVisitaService
{
    private readonly AppDbContext _db;

    public CeleroVisitaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedCeleroVisitasDto> ListAsync(int page, int pageSize, string? searchNif = null, string? searchService = null, CancellationToken ct = default)
    {
        var query = _db.StagingCeleroVisitas.AsNoTracking();

        // Filtrar por NIF si se proporciona
        if (!string.IsNullOrEmpty(searchNif))
            query = query.Where(v => v.ResourceNif.Contains(searchNif));

        // Filtrar por Servicio si se proporciona
        if (!string.IsNullOrEmpty(searchService))
            query = query.Where(v => v.ServiceName.Contains(searchService));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(v => v.Fecha)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new CeleroVisitaListDto(
                v.Id,
                v.VisitaIdExterno,
                v.ResourceNif,
                v.ServiceName,
                v.MissionName,
                v.Fecha,
                v.UserId,
                v.ServiceId,
                v.Notas,
                v.EstadoMapeo
            ))
            .ToListAsync(ct);

        return new PagedCeleroVisitasDto(items, total, page, pageSize);
    }

    public async Task<CeleroVisitaDetailDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var visita = await _db.StagingCeleroVisitas
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new EntityNotFoundException("Visita Celero", id);

        return new CeleroVisitaDetailDto(
            visita.Id,
            visita.VisitaIdExterno,
            visita.ResourceNif,
            visita.ServiceName,
            visita.MissionName,
            visita.Fecha,
            visita.UserId,
            visita.ServiceId,
            visita.Notas,
            visita.MapeadoPor,
            visita.FechaMapeo,
            visita.EstadoMapeo
        );
    }

    public async Task<CeleroVisitaDetailDto> UpdateAsync(int id, CeleroVisitaUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var visita = await _db.StagingCeleroVisitas
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new EntityNotFoundException("Visita Celero", id);

        // Actualizar solo los campos de mapeo y anotaciones (no los datos de Celero)
        visita.UserId = req.UserId ?? visita.UserId;
        visita.ServiceId = req.ServiceId ?? visita.ServiceId;
        visita.Notas = req.Notas ?? visita.Notas;
        visita.EstadoMapeo = req.EstadoMapeo ?? visita.EstadoMapeo;
        visita.MapeadoPor = usuarioId;
        visita.FechaMapeo = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        return new CeleroVisitaDetailDto(
            visita.Id,
            visita.VisitaIdExterno,
            visita.ResourceNif,
            visita.ServiceName,
            visita.MissionName,
            visita.Fecha,
            visita.UserId,
            visita.ServiceId,
            visita.Notas,
            visita.MapeadoPor,
            visita.FechaMapeo,
            visita.EstadoMapeo
        );
    }
}
