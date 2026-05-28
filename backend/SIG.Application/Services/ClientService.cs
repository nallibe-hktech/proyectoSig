using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class ClientService : IClientService
{
    private readonly IClientRepository _repo;

    public ClientService(IClientRepository repo) { _repo = repo; }

    public async Task<PagedResult<ClientListItemDto>> ListAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, page, pageSize, search, ct);
        var items = result.Items.Select(c => new ClientListItemDto(c.Id, c.Nombre, c.NIF, c.Ciudad, c.Projects?.Count ?? 0)).ToList();
        return new PagedResult<ClientListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<ClientDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Client", id);
        return Map(c);
    }

    public async Task<ClientDetailDto> CreateAsync(ClientCreateRequest req, int usuarioId, CancellationToken ct)
    {
        if (await _repo.ExistsByNifAsync(req.NIF, null, ct))
            throw new DuplicateException($"Ya existe un cliente con NIF {req.NIF}.");
        var c = new Client
        {
            Nombre = req.Nombre,
            NIF = req.NIF,
            Direccion = req.Direccion,
            Ciudad = req.Ciudad,
            Provincia = req.Provincia,
            Pais = req.Pais,
            CodigoPostal = req.CodigoPostal,
            ContactoNombre = req.ContactoNombre,
            ContactoEmail = req.ContactoEmail,
            ContactoTelefono = req.ContactoTelefono,
        };
        await _repo.AddAsync(c, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task<ClientDetailDto> UpdateAsync(int id, ClientUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Client", id);
        if (await _repo.ExistsByNifAsync(req.NIF, id, ct))
            throw new DuplicateException($"Ya existe otro cliente con NIF {req.NIF}.");
        c.Nombre = req.Nombre;
        c.NIF = req.NIF;
        c.Direccion = req.Direccion;
        c.Ciudad = req.Ciudad;
        c.Provincia = req.Provincia;
        c.Pais = req.Pais;
        c.CodigoPostal = req.CodigoPostal;
        c.ContactoNombre = req.ContactoNombre;
        c.ContactoEmail = req.ContactoEmail;
        c.ContactoTelefono = req.ContactoTelefono;
        await _repo.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task DeleteAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Client", id);
        if (await _repo.HasProjectsAsync(id, ct))
            throw new DependenciesExistException(1);
        c.IsDeleted = true;
        c.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }

    private static ClientDetailDto Map(Client c) =>
        new(c.Id, c.Nombre, c.NIF, c.Direccion, c.Ciudad, c.Provincia, c.Pais, c.CodigoPostal, c.ContactoNombre, c.ContactoEmail, c.ContactoTelefono);
}
