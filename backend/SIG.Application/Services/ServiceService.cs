using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Antes ActionService + ProjectService fusionados: el Servicio cuelga directamente del Cliente
// y absorbe CECOs, usuarios, conceptos y metadatos de interlocutor (PPT §1).
public class ServiceService : IServiceService
{
    private readonly IServiceRepository _repo;
    private readonly IClientRepository _clientRepo;

    public ServiceService(IServiceRepository repo, IClientRepository clientRepo)
    {
        _repo = repo;
        _clientRepo = clientRepo;
    }

    public async Task<PagedResult<ServiceListItemDto>> ListAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, page, pageSize, clientId, search, ct);
        var items = result.Items.Select(a => new ServiceListItemDto(a.Id, a.Nombre, a.ClientId, a.Client?.Nombre ?? "", a.DepartmentId, a.Estado)).ToList();
        return new PagedResult<ServiceListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<ServiceDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Service", id);
        return Map(a);
    }

    public async Task<ServiceDetailDto> CreateAsync(ServiceCreateRequest req, int usuarioId, CancellationToken ct)
    {
        var client = await _clientRepo.GetByIdAsync(req.ClientId, ct)
                     ?? throw new EntityNotFoundException("Client", req.ClientId);
        var a = new Service
        {
            Nombre = req.Nombre,
            ClientId = req.ClientId,
            DepartmentId = req.DepartmentId,
            Estado = req.Estado,
            InterlocutorNombre = req.InterlocutorNombre,
            InterlocutorEmail = req.InterlocutorEmail,
            InterlocutorTelefono = req.InterlocutorTelefono,
            FechaAlta = req.FechaAlta,
            ServiceConcepts = req.ConceptIds.Select(cId => new ServiceConcept { ConceptId = cId }).ToList(),
            ServiceUsers = req.UserIds.Select(uId => new ServiceUser { UserId = uId }).ToList(),
            ServiceCostCenters = req.CostCenterIds.Select(ccId => new ServiceCostCenter { CostCenterId = ccId }).ToList()
        };
        await _repo.AddAsync(a, ct);
        await _repo.SaveChangesAsync(ct);
        a.Client = client;
        return Map(a);
    }

    public async Task<ServiceDetailDto> UpdateAsync(int id, ServiceUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Service", id);
        a.Nombre = req.Nombre;
        a.ClientId = req.ClientId;
        a.DepartmentId = req.DepartmentId;
        a.Estado = req.Estado;
        a.InterlocutorNombre = req.InterlocutorNombre;
        a.InterlocutorEmail = req.InterlocutorEmail;
        a.InterlocutorTelefono = req.InterlocutorTelefono;
        a.FechaAlta = req.FechaAlta;
        a.ServiceConcepts.Clear();
        foreach (var cId in req.ConceptIds) a.ServiceConcepts.Add(new ServiceConcept { ServiceId = id, ConceptId = cId });
        a.ServiceUsers.Clear();
        foreach (var uId in req.UserIds) a.ServiceUsers.Add(new ServiceUser { ServiceId = id, UserId = uId });
        a.ServiceCostCenters.Clear();
        foreach (var ccId in req.CostCenterIds) a.ServiceCostCenters.Add(new ServiceCostCenter { ServiceId = id, CostCenterId = ccId });
        await _repo.SaveChangesAsync(ct);
        return Map(a);
    }

    public async Task DeleteAsync(int id, int usuarioId, CancellationToken ct)
    {
        var a = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Service", id);
        if (await _repo.HasClosuresAsync(id, ct))
            throw new DependenciesExistException(1);
        a.IsDeleted = true;
        a.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }

    private static ServiceDetailDto Map(Service a) =>
        new(a.Id, a.Nombre, a.ClientId, a.Client?.Nombre ?? "", a.DepartmentId, a.Estado,
            a.InterlocutorNombre, a.InterlocutorEmail, a.InterlocutorTelefono, a.FechaAlta,
            a.ServiceCostCenters.Select(x => x.CostCenterId).ToArray(),
            a.ServiceUsers.Select(x => x.UserId).ToArray(),
            a.ServiceConcepts.Select(x => x.ConceptId).ToArray());
}
