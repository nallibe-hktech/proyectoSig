using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Incidencias del cliente (PPT slide 6 + prototipo). Anidado bajo el cliente para alta/edición; además
// expone un listado global (pantalla de 1er nivel) restringido a los clientes accesibles. Cada cambio de
// estado registra una entrada en el histórico (el AuditInterceptor cubre además la traza técnica).
public class ClienteIncidenciaService : IClienteIncidenciaService
{
    private readonly IClienteIncidenciaRepository _repo;
    private readonly IClientRepository _clientRepo;

    public ClienteIncidenciaService(IClienteIncidenciaRepository repo, IClientRepository clientRepo)
    {
        _repo = repo;
        _clientRepo = clientRepo;
    }

    public async Task<IReadOnlyList<ClienteIncidenciaDto>> ListByClientAsync(int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var incidencias = await _repo.ListByClientAsync(clientId, ct);
        return incidencias.Select(Map).ToList();
    }

    public async Task<PagedResult<IncidenciaListItemDto>> ListAllAsync(int usuarioId, int page, int pageSize, string? search, int? clientId, string? tipo, EstadoIncidencia? estado, CancellationToken ct)
    {
        var paged = await _repo.ListAllForUserAsync(usuarioId, page, pageSize, search, clientId, tipo, estado, ct);
        var items = paged.Items.Select(i => new IncidenciaListItemDto(
            i.Id, i.ClientId, i.Client?.Nombre ?? string.Empty, i.Tipo, i.Descripcion, i.Estado, i.Origen, i.FechaApertura)).ToList();
        return new PagedResult<IncidenciaListItemDto>(items, paged.Total, paged.Page, paged.PageSize);
    }

    public async Task<ClienteIncidenciaDto> GetByIdAsync(int id, int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var i = await _repo.GetByIdWithDetailAsync(id, ct) ?? throw new EntityNotFoundException("ClienteIncidencia", id);
        if (i.ClientId != clientId) throw new EntityNotFoundException("ClienteIncidencia", id);
        return Map(i);
    }

    public async Task<ClienteIncidenciaDto> CreateAsync(int clientId, ClienteIncidenciaCreateRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var estado = req.Estado ?? EstadoIncidencia.Abierta;
        var apertura = req.FechaApertura ?? DateTime.UtcNow;
        var i = new ClienteIncidencia
        {
            ClientId = clientId,
            Tipo = req.Tipo,
            Descripcion = req.Descripcion,
            Estado = estado,
            Origen = req.Origen,
            FechaApertura = apertura,
        };
        // Primera entrada del histórico: se inserta en cascada junto con la incidencia.
        i.Historial.Add(new IncidenciaHistorial
        {
            Estado = estado,
            Nota = "Incidencia creada",
            Responsable = req.Origen,
            Fecha = apertura,
        });
        await _repo.AddAsync(i, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(i);
    }

    public async Task<ClienteIncidenciaDto> UpdateAsync(int id, int clientId, ClienteIncidenciaUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var i = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("ClienteIncidencia", id);
        if (i.ClientId != clientId) throw new EntityNotFoundException("ClienteIncidencia", id);
        i.Tipo = req.Tipo;
        i.Descripcion = req.Descripcion;
        i.Estado = req.Estado;
        i.Origen = req.Origen;
        await _repo.SaveChangesAsync(ct);
        return Map(i);
    }

    public async Task<ClienteIncidenciaDto> CambiarEstadoAsync(int id, int clientId, IncidenciaCambioEstadoRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var i = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("ClienteIncidencia", id);
        if (i.ClientId != clientId) throw new EntityNotFoundException("ClienteIncidencia", id);
        i.Estado = req.Estado;
        await _repo.AddHistorialAsync(new IncidenciaHistorial
        {
            IncidenciaId = i.Id,
            Estado = req.Estado,
            Nota = req.Nota,
            Responsable = req.Responsable,
            Fecha = DateTime.UtcNow,
        }, ct);
        await _repo.SaveChangesAsync(ct);
        var full = await _repo.GetByIdWithDetailAsync(id, ct);
        return Map(full!);
    }

    public async Task DeleteAsync(int id, int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var i = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("ClienteIncidencia", id);
        if (i.ClientId != clientId) throw new EntityNotFoundException("ClienteIncidencia", id);
        i.IsDeleted = true;
        await _repo.SaveChangesAsync(ct);
    }

    private async Task EnsureClientAsync(int clientId, int usuarioId, CancellationToken ct)
    {
        _ = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Client", clientId);
    }

    private static ClienteIncidenciaDto Map(ClienteIncidencia i) =>
        new(i.Id, i.ClientId, i.Tipo, i.Descripcion, i.Estado, i.Origen, i.FechaApertura, i.CreatedAt, i.UpdatedAt,
            (i.Historial ?? new List<IncidenciaHistorial>())
                .OrderBy(h => h.Fecha).ThenBy(h => h.Id)
                .Select(h => new IncidenciaHistorialDto(h.Id, h.Estado, h.Nota, h.Responsable, h.Fecha)).ToList());
}
