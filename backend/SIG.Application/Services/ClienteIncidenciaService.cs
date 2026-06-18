using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Incidencias del cliente (PPT slide 6). Anidado bajo el cliente; el acceso al cliente se valida con
// usuarioId (misma norma que ClientService). El histórico de cambios lo aporta el AuditInterceptor.
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

    public async Task<ClienteIncidenciaDto> GetByIdAsync(int id, int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var i = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("ClienteIncidencia", id);
        if (i.ClientId != clientId) throw new EntityNotFoundException("ClienteIncidencia", id);
        return Map(i);
    }

    public async Task<ClienteIncidenciaDto> CreateAsync(int clientId, ClienteIncidenciaCreateRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var i = new ClienteIncidencia
        {
            ClientId = clientId,
            Tipo = req.Tipo,
            Descripcion = req.Descripcion,
            Estado = req.Estado ?? EstadoIncidencia.Abierta
        };
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
        await _repo.SaveChangesAsync(ct);
        return Map(i);
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
        new(i.Id, i.ClientId, i.Tipo, i.Descripcion, i.Estado, i.CreatedAt, i.UpdatedAt);
}
