using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;
using Action = SIG.Domain.Entities.Action;

namespace SIG.Application.Services;

public class ActionService : IActionService
{
    private readonly IActionRepository _repo;

    public ActionService(IActionRepository repo) { _repo = repo; }

    public async Task<PagedResult<ActionListItemDto>> ListAsync(int usuarioId, int page, int pageSize, int? projectId, string? search, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, page, pageSize, projectId, search, ct);
        var items = result.Items.Select(a => new ActionListItemDto(a.Id, a.Nombre, a.ProjectId, a.Project?.Nombre ?? "", a.ClientId, a.DepartmentId, a.Estado)).ToList();
        return new PagedResult<ActionListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<ActionDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Action", id);
        return Map(a);
    }

    public async Task<ActionDetailDto> CreateAsync(ActionCreateRequest req, int usuarioId, CancellationToken ct)
    {
        var a = new Action
        {
            Nombre = req.Nombre,
            ProjectId = req.ProjectId,
            ClientId = req.ClientId,
            DepartmentId = req.DepartmentId,
            Estado = req.Estado,
            ActionConcepts = req.ConceptIds.Select(cId => new ActionConcept { ConceptId = cId }).ToList(),
            ActionUsers = req.UserIds.Select(uId => new ActionUser { UserId = uId }).ToList()
        };
        await _repo.AddAsync(a, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task<ActionDetailDto> UpdateAsync(int id, ActionUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Action", id);
        a.Nombre = req.Nombre;
        a.ProjectId = req.ProjectId;
        a.ClientId = req.ClientId;
        a.DepartmentId = req.DepartmentId;
        a.Estado = req.Estado;
        a.ActionConcepts.Clear();
        foreach (var cId in req.ConceptIds) a.ActionConcepts.Add(new ActionConcept { ActionId = id, ConceptId = cId });
        a.ActionUsers.Clear();
        foreach (var uId in req.UserIds) a.ActionUsers.Add(new ActionUser { ActionId = id, UserId = uId });
        await _repo.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task DeleteAsync(int id, int usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Action", id);
        if (await _repo.HasClosuresAsync(id, ct))
            throw new DependenciesExistException(1);
        a.IsDeleted = true;
        a.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }

    private static ActionDetailDto Map(Action a) =>
        new(a.Id, a.Nombre, a.ProjectId, a.ClientId, a.DepartmentId, a.Estado,
            a.ActionConcepts.Select(x => x.ConceptId).ToArray(),
            a.ActionUsers.Select(x => x.UserId).ToArray());
}
