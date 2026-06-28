using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// FASE 2: PresupuestoConcepto Service - budgets by concept with finer granularity
public class PresupuestoConceptoService : IPresupuestoConceptoService
{
    private readonly IPresupuestoConceptoRepository _repo;
    private readonly IConceptRepository _conceptRepo;
    private readonly IClientRepository _clientRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IPeriodRepository _periodRepo;

    public PresupuestoConceptoService(
        IPresupuestoConceptoRepository repo,
        IConceptRepository conceptRepo,
        IClientRepository clientRepo,
        IServiceRepository serviceRepo,
        IPeriodRepository periodRepo)
    {
        _repo = repo;
        _conceptRepo = conceptRepo;
        _clientRepo = clientRepo;
        _serviceRepo = serviceRepo;
        _periodRepo = periodRepo;
    }

    public async Task<IReadOnlyList<PresupuestoConceptoDto>> ListByConceptAsync(int conceptId, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var list = await _repo.ListByConceptAsync(conceptId, ct);
        return list.Select(p => MapToDto(p)).ToList();
    }

    public async Task<IReadOnlyList<PresupuestoConceptoDto>> ListByConceptAndClientAsync(int conceptId, int clientId, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var client = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
                    ?? throw new EntityNotFoundException("Cliente", clientId);

        var list = await _repo.ListByConceptAndClientAsync(conceptId, clientId, ct);
        return list.Select(p => MapToDto(p)).ToList();
    }

    public async Task<PagedResult<PresupuestoConceptoDto>> ListPaginatedByConceptAsync(int conceptId, int usuarioId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var result = await _repo.ListPaginatedByConceptAsync(conceptId, page, pageSize, search, ct);
        return new PagedResult<PresupuestoConceptoDto>(
            result.Items.Select(p => MapToDto(p)).ToList(),
            result.Total,
            result.Page,
            result.PageSize);
    }

    public async Task<PresupuestoConceptoDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var presupuesto = await _repo.GetByIdAsync(id, ct)
                         ?? throw new EntityNotFoundException("PresupuestoConcepto", id);

        return MapToDto(presupuesto);
    }

    public async Task<PresupuestoConceptoDto> CreateAsync(int conceptId, PresupuestoConceptoCreateRequest req, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        // Validate client if provided
        if (req.ClientId.HasValue)
        {
            var client = await _clientRepo.GetByIdAsync(req.ClientId.Value, ct)
                        ?? throw new EntityNotFoundException("Cliente", req.ClientId.Value);
        }

        // Validate service if provided
        if (req.ServiceId.HasValue)
        {
            var service = await _serviceRepo.GetByIdAsync(req.ServiceId.Value, ct)
                         ?? throw new EntityNotFoundException("Servicio", req.ServiceId.Value);
        }

        // Validate period if provided
        if (req.PeriodId.HasValue)
        {
            var period = await _periodRepo.GetByIdAsync(req.PeriodId.Value, ct)
                        ?? throw new EntityNotFoundException("Período", req.PeriodId.Value);
        }

        var presupuesto = new PresupuestoConcepto
        {
            ConceptId = conceptId,
            ClientId = req.ClientId,
            ServiceId = req.ServiceId,
            PeriodId = req.PeriodId,
            Tipo = req.Tipo,
            Importe = req.Importe,
            Descripcion = req.Descripcion
        };

        await _repo.AddAsync(presupuesto, ct);
        await _repo.SaveChangesAsync(ct);

        return MapToDto(presupuesto);
    }

    public async Task<PresupuestoConceptoDto> UpdateAsync(int id, int conceptId, PresupuestoConceptoUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var presupuesto = await _repo.GetByIdAsync(id, ct)
                         ?? throw new EntityNotFoundException("PresupuestoConcepto", id);

        if (presupuesto.ConceptId != conceptId)
            throw new InvalidOperationException("Presupuesto no pertenece a este concepto");

        // Validate client if provided
        if (req.ClientId.HasValue && req.ClientId != presupuesto.ClientId)
        {
            var client = await _clientRepo.GetByIdAsync(req.ClientId.Value, ct)
                        ?? throw new EntityNotFoundException("Cliente", req.ClientId.Value);
        }

        // Validate service if provided
        if (req.ServiceId.HasValue && req.ServiceId != presupuesto.ServiceId)
        {
            var service = await _serviceRepo.GetByIdAsync(req.ServiceId.Value, ct)
                         ?? throw new EntityNotFoundException("Servicio", req.ServiceId.Value);
        }

        // Validate period if provided
        if (req.PeriodId.HasValue && req.PeriodId != presupuesto.PeriodId)
        {
            var period = await _periodRepo.GetByIdAsync(req.PeriodId.Value, ct)
                        ?? throw new EntityNotFoundException("Período", req.PeriodId.Value);
        }

        presupuesto.ClientId = req.ClientId;
        presupuesto.ServiceId = req.ServiceId;
        presupuesto.PeriodId = req.PeriodId;
        presupuesto.Tipo = req.Tipo;
        presupuesto.Importe = req.Importe;
        presupuesto.Descripcion = req.Descripcion;

        await _repo.SaveChangesAsync(ct);

        return MapToDto(presupuesto);
    }

    public async Task DeleteAsync(int id, int conceptId, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var presupuesto = await _repo.GetByIdAsync(id, ct)
                         ?? throw new EntityNotFoundException("PresupuestoConcepto", id);

        if (presupuesto.ConceptId != conceptId)
            throw new InvalidOperationException("Presupuesto no pertenece a este concepto");

        presupuesto.IsDeleted = true;
        presupuesto.DeletedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
    }

    private static PresupuestoConceptoDto MapToDto(PresupuestoConcepto presupuesto) =>
        new(presupuesto.Id, presupuesto.ConceptId, presupuesto.Concept.Nombre,
            presupuesto.ClientId, presupuesto.Client?.Nombre,
            presupuesto.ServiceId, presupuesto.Service?.Nombre,
            presupuesto.PeriodId, presupuesto.Period?.Nombre,
            presupuesto.Tipo, presupuesto.Importe, presupuesto.Descripcion);
}
