using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class VariableService : IVariableService
{
    private readonly IVariableRepository _repo;
    public VariableService(IVariableRepository repo) { _repo = repo; }

    public async Task<IReadOnlyList<VariableDto>> ListAsync(CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        return list.Select(v => new VariableDto(v.Id, v.Nombre, v.QuestionIdExterno, v.MapeoValoresJson)).ToList();
    }

    public async Task<PagedResult<VariableDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        var total = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new VariableDto(v.Id, v.Nombre, v.QuestionIdExterno, v.MapeoValoresJson))
            .ToList();
        return new PagedResult<VariableDto>(items, total, page, pageSize);
    }

    public async Task<VariableDto> GetByIdAsync(int id, CancellationToken ct)
    {
        var v = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Variable", id);
        return new VariableDto(v.Id, v.Nombre, v.QuestionIdExterno, v.MapeoValoresJson);
    }

    public async Task<VariableDto> CreateAsync(VariableCreateRequest req, CancellationToken ct)
    {
        var v = new Variable { Nombre = req.Nombre, QuestionIdExterno = req.QuestionIdExterno, MapeoValoresJson = req.MapeoValoresJson };
        await _repo.AddAsync(v, ct);
        await _repo.SaveChangesAsync(ct);
        return new VariableDto(v.Id, v.Nombre, v.QuestionIdExterno, v.MapeoValoresJson);
    }

    public async Task<VariableDto> UpdateAsync(int id, VariableUpdateRequest req, CancellationToken ct)
    {
        var v = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Variable", id);
        v.Nombre = req.Nombre;
        v.QuestionIdExterno = req.QuestionIdExterno;
        v.MapeoValoresJson = req.MapeoValoresJson;
        await _repo.SaveChangesAsync(ct);
        return new VariableDto(v.Id, v.Nombre, v.QuestionIdExterno, v.MapeoValoresJson);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var v = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Variable", id);
        v.IsDeleted = true;
        v.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }
}

public class RoleService : IRoleService
{
    private readonly IRoleRepository _repo;
    public RoleService(IRoleRepository repo) { _repo = repo; }

    public async Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        return list.Select(r => new RoleDto(r.Id, r.Nombre, r.Descripcion)).ToList();
    }

    public async Task<PagedResult<RoleDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        var total = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleDto(r.Id, r.Nombre, r.Descripcion))
            .ToList();
        return new PagedResult<RoleDto>(items, total, page, pageSize);
    }
}

public class DepartmentService : IDepartmentService
{
    private readonly IDepartmentRepository _repo;
    public DepartmentService(IDepartmentRepository repo) { _repo = repo; }

    public async Task<IReadOnlyList<DepartmentDto>> ListAsync(CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        return list.Select(d => new DepartmentDto(d.Id, d.Nombre)).ToList();
    }

    public async Task<PagedResult<DepartmentDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        var total = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DepartmentDto(d.Id, d.Nombre))
            .ToList();
        return new PagedResult<DepartmentDto>(items, total, page, pageSize);
    }

    public async Task<DepartmentDto> CreateAsync(DepartmentCreateRequest req, CancellationToken ct)
    {
        var d = new Department { Nombre = req.Nombre };
        await _repo.AddAsync(d, ct);
        await _repo.SaveChangesAsync(ct);
        return new DepartmentDto(d.Id, d.Nombre);
    }

    public async Task<DepartmentDto> UpdateAsync(int id, DepartmentUpdateRequest req, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Department", id);
        d.Nombre = req.Nombre;
        await _repo.SaveChangesAsync(ct);
        return new DepartmentDto(d.Id, d.Nombre);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var d = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("Department", id);
        if (await _repo.HasUsersOrServicesAsync(id, ct))
            throw new DependenciesExistException(1);
        d.IsDeleted = true;
        d.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }
}

public class CostCenterService : ICostCenterService
{
    private readonly ICostCenterRepository _repo;
    public CostCenterService(ICostCenterRepository repo) { _repo = repo; }

    public async Task<IReadOnlyList<CostCenterDto>> ListAsync(CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        return list.Select(c => new CostCenterDto(c.Id, c.Codigo, c.Nombre)).ToList();
    }

    public async Task<PagedResult<CostCenterDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct)
    {
        var list = await _repo.ListAsync(ct);
        var total = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CostCenterDto(c.Id, c.Codigo, c.Nombre))
            .ToList();
        return new PagedResult<CostCenterDto>(items, total, page, pageSize);
    }

    public async Task<CostCenterDto> CreateAsync(CostCenterCreateRequest req, CancellationToken ct)
    {
        if (await _repo.ExistsByCodigoAsync(req.Codigo, null, ct))
            throw new DuplicateException($"Ya existe CECO con código {req.Codigo}.");
        var c = new CostCenter { Codigo = req.Codigo, Nombre = req.Nombre };
        await _repo.AddAsync(c, ct);
        await _repo.SaveChangesAsync(ct);
        return new CostCenterDto(c.Id, c.Codigo, c.Nombre);
    }

    public async Task<CostCenterDto> UpdateAsync(int id, CostCenterUpdateRequest req, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("CostCenter", id);
        if (await _repo.ExistsByCodigoAsync(req.Codigo, id, ct))
            throw new DuplicateException($"Ya existe otro CECO con código {req.Codigo}.");
        c.Codigo = req.Codigo;
        c.Nombre = req.Nombre;
        await _repo.SaveChangesAsync(ct);
        return new CostCenterDto(c.Id, c.Codigo, c.Nombre);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var c = await _repo.GetByIdAsync(id, ct) ?? throw new EntityNotFoundException("CostCenter", id);
        if (await _repo.HasServicesAsync(id, ct))
            throw new DependenciesExistException(1);
        c.IsDeleted = true;
        c.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }
}
