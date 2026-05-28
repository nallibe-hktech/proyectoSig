using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public class UserService : IUserService
{
    private readonly IUserRepository _repo;
    private readonly IPasswordHasher _hasher;

    public UserService(IUserRepository repo, IPasswordHasher hasher)
    {
        _repo = repo;
        _hasher = hasher;
    }

    public async Task<PagedResult<UserListItemDto>> ListAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        // Para administradores, no se filtra por ownership. Pasamos 0 como usuarioId neutral.
        var result = await _repo.ListPaginatedForUserAsync(0, page, pageSize, search, ct);
        var items = result.Items.Select(u => new UserListItemDto(u.Id, u.NIF, u.Nombre, u.Apellidos, u.Email, u.Estado,
            u.UserRoles.Select(ur => ur.Role?.Nombre ?? "").ToArray())).ToList();
        return new PagedResult<UserListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<UserDetailDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var u = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("User", id);
        return Map(u);
    }

    public async Task<UserDetailDto> CreateAsync(UserCreateRequest req, CancellationToken ct)
    {
        if (await _repo.ExistsByEmailAsync(req.Email, null, ct))
            throw new DuplicateException($"Ya existe un usuario con email {req.Email}.");
        if (await _repo.ExistsByNifAsync(req.NIF, null, ct))
            throw new DuplicateException($"Ya existe un usuario con NIF {req.NIF}.");
        var u = new User
        {
            NIF = req.NIF,
            Nombre = req.Nombre,
            Apellidos = req.Apellidos,
            Email = req.Email,
            PasswordHash = _hasher.Hash(req.Password),
            Estado = req.Estado,
            DepartmentId = req.DepartmentId,
            UserRoles = req.RoleIds.Select(rId => new UserRole { RoleId = rId }).ToList()
        };
        await _repo.AddAsync(u, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(u);
    }

    public async Task<UserDetailDto> UpdateAsync(int id, UserUpdateRequest req, CancellationToken ct)
    {
        var u = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("User", id);
        if (await _repo.ExistsByEmailAsync(req.Email, id, ct))
            throw new DuplicateException($"Ya existe otro usuario con email {req.Email}.");
        if (await _repo.ExistsByNifAsync(req.NIF, id, ct))
            throw new DuplicateException($"Ya existe otro usuario con NIF {req.NIF}.");
        u.NIF = req.NIF;
        u.Nombre = req.Nombre;
        u.Apellidos = req.Apellidos;
        u.Email = req.Email;
        u.Estado = req.Estado;
        u.DepartmentId = req.DepartmentId;
        u.UserRoles.Clear();
        foreach (var rId in req.RoleIds) u.UserRoles.Add(new UserRole { UserId = id, RoleId = rId });
        await _repo.SaveChangesAsync(ct);
        return Map(u);
    }

    public async Task ChangePasswordAsync(int id, UserPasswordChangeRequest req, CancellationToken ct)
    {
        var u = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("User", id);
        u.PasswordHash = _hasher.Hash(req.NewPassword);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var u = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("User", id);
        u.IsDeleted = true;
        u.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }

    private static UserDetailDto Map(User u) =>
        new(u.Id, u.NIF, u.Nombre, u.Apellidos, u.Email, u.Estado, u.DepartmentId,
            u.UserRoles.Select(ur => ur.RoleId).ToArray());
}
