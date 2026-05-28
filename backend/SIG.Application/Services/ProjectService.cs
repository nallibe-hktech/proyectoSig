using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _repo;
    private readonly IClientRepository _clientRepo;

    public ProjectService(IProjectRepository repo, IClientRepository clientRepo)
    {
        _repo = repo;
        _clientRepo = clientRepo;
    }

    public async Task<PagedResult<ProjectListItemDto>> ListAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, page, pageSize, clientId, search, ct);
        var items = result.Items.Select(p => new ProjectListItemDto(p.Id, p.Nombre, p.ClientId, p.Client?.Nombre ?? "", p.Estado, p.FechaAlta)).ToList();
        return new PagedResult<ProjectListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<ProjectDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var p = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Project", id);
        return Map(p);
    }

    public async Task<ProjectDetailDto> CreateAsync(ProjectCreateRequest req, int usuarioId, CancellationToken ct)
    {
        var client = await _clientRepo.GetByIdAsync(req.ClientId, ct)
                     ?? throw new EntityNotFoundException("Client", req.ClientId);
        var p = new Project
        {
            Nombre = req.Nombre,
            ClientId = req.ClientId,
            Estado = req.Estado,
            InterlocutorNombre = req.InterlocutorNombre,
            InterlocutorEmail = req.InterlocutorEmail,
            InterlocutorTelefono = req.InterlocutorTelefono,
            FechaAlta = req.FechaAlta,
            ProjectCostCenters = req.CostCenterIds.Select(id => new ProjectCostCenter { CostCenterId = id }).ToList(),
            ProjectUsers = req.UserIds.Select(id => new ProjectUser { UserId = id }).ToList()
        };
        await _repo.AddAsync(p, ct);
        await _repo.SaveChangesAsync(ct);
        p.Client = client;
        return Map(p);
    }

    public async Task<ProjectDetailDto> UpdateAsync(int id, ProjectUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var p = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Project", id);
        p.Nombre = req.Nombre;
        p.ClientId = req.ClientId;
        p.Estado = req.Estado;
        p.InterlocutorNombre = req.InterlocutorNombre;
        p.InterlocutorEmail = req.InterlocutorEmail;
        p.InterlocutorTelefono = req.InterlocutorTelefono;
        p.FechaAlta = req.FechaAlta;
        p.ProjectCostCenters.Clear();
        foreach (var ccId in req.CostCenterIds)
            p.ProjectCostCenters.Add(new ProjectCostCenter { ProjectId = id, CostCenterId = ccId });
        p.ProjectUsers.Clear();
        foreach (var uId in req.UserIds)
            p.ProjectUsers.Add(new ProjectUser { ProjectId = id, UserId = uId });
        await _repo.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task DeleteAsync(int id, int usuarioId, CancellationToken ct)
    {
        var p = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Project", id);
        if (await _repo.HasActionsOrClosuresAsync(id, ct))
            throw new DependenciesExistException(1);
        p.IsDeleted = true;
        p.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }

    private static ProjectDetailDto Map(Project p) =>
        new(p.Id, p.Nombre, p.ClientId, p.Client?.Nombre ?? "", p.Estado,
            p.InterlocutorNombre, p.InterlocutorEmail, p.InterlocutorTelefono, p.FechaAlta,
            p.ProjectCostCenters.Select(x => x.CostCenterId).ToArray(),
            p.ProjectUsers.Select(x => x.UserId).ToArray());
}
