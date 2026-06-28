using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// FASE 2: TarifaConcepto Service - tariffs by concept with finer granularity
public class TarifaConceptoService : ITarifaConceptoService
{
    private readonly ITarifaConceptoRepository _repo;
    private readonly IConceptRepository _conceptRepo;
    private readonly IClientRepository _clientRepo;
    private readonly IServiceRepository _serviceRepo;

    public TarifaConceptoService(
        ITarifaConceptoRepository repo,
        IConceptRepository conceptRepo,
        IClientRepository clientRepo,
        IServiceRepository serviceRepo)
    {
        _repo = repo;
        _conceptRepo = conceptRepo;
        _clientRepo = clientRepo;
        _serviceRepo = serviceRepo;
    }

    public async Task<IReadOnlyList<TarifaConceptoDto>> ListByConceptAsync(int conceptId, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var list = await _repo.ListByConceptAsync(conceptId, ct);
        return list.Select(t => MapToDto(t)).ToList();
    }

    public async Task<IReadOnlyList<TarifaConceptoDto>> ListByConceptAndClientAsync(int conceptId, int clientId, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var client = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
                    ?? throw new EntityNotFoundException("Cliente", clientId);

        var list = await _repo.ListByConceptAndClientAsync(conceptId, clientId, ct);
        return list.Select(t => MapToDto(t)).ToList();
    }

    public async Task<PagedResult<TarifaConceptoDto>> ListPaginatedByConceptAsync(int conceptId, int usuarioId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var result = await _repo.ListPaginatedByConceptAsync(conceptId, page, pageSize, search, ct);
        return new PagedResult<TarifaConceptoDto>(
            result.Items.Select(t => MapToDto(t)).ToList(),
            result.Total,
            result.Page,
            result.PageSize);
    }

    public async Task<TarifaConceptoDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var tarifa = await _repo.GetByIdAsync(id, ct)
                    ?? throw new EntityNotFoundException("TarifaConcepto", id);

        return MapToDto(tarifa);
    }

    public async Task<TarifaConceptoDto> CreateAsync(int conceptId, TarifaConceptoCreateRequest req, int usuarioId, CancellationToken ct)
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

        var tarifa = new TarifaConcepto
        {
            ConceptId = conceptId,
            ClientId = req.ClientId,
            ServiceId = req.ServiceId,
            Valor = req.Valor,
            Unidad = req.Unidad,
            FechaDesde = req.FechaDesde,
            FechaHasta = req.FechaHasta
        };

        await _repo.AddAsync(tarifa, ct);
        await _repo.SaveChangesAsync(ct);

        return MapToDto(tarifa);
    }

    public async Task<TarifaConceptoDto> UpdateAsync(int id, int conceptId, TarifaConceptoUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var tarifa = await _repo.GetByIdAsync(id, ct)
                    ?? throw new EntityNotFoundException("TarifaConcepto", id);

        if (tarifa.ConceptId != conceptId)
            throw new InvalidOperationException("Tarifa no pertenece a este concepto");

        // Validate client if provided
        if (req.ClientId.HasValue && req.ClientId != tarifa.ClientId)
        {
            var client = await _clientRepo.GetByIdAsync(req.ClientId.Value, ct)
                        ?? throw new EntityNotFoundException("Cliente", req.ClientId.Value);
        }

        // Validate service if provided
        if (req.ServiceId.HasValue && req.ServiceId != tarifa.ServiceId)
        {
            var service = await _serviceRepo.GetByIdAsync(req.ServiceId.Value, ct)
                         ?? throw new EntityNotFoundException("Servicio", req.ServiceId.Value);
        }

        tarifa.ClientId = req.ClientId;
        tarifa.ServiceId = req.ServiceId;
        tarifa.Valor = req.Valor;
        tarifa.Unidad = req.Unidad;
        tarifa.FechaDesde = req.FechaDesde;
        tarifa.FechaHasta = req.FechaHasta;

        await _repo.SaveChangesAsync(ct);

        return MapToDto(tarifa);
    }

    public async Task DeleteAsync(int id, int conceptId, int usuarioId, CancellationToken ct)
    {
        var concept = await _conceptRepo.GetByIdAsync(conceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", conceptId);

        var tarifa = await _repo.GetByIdAsync(id, ct)
                    ?? throw new EntityNotFoundException("TarifaConcepto", id);

        if (tarifa.ConceptId != conceptId)
            throw new InvalidOperationException("Tarifa no pertenece a este concepto");

        tarifa.IsDeleted = true;
        tarifa.DeletedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
    }

    private static TarifaConceptoDto MapToDto(TarifaConcepto tarifa) =>
        new(tarifa.Id, tarifa.ConceptId, tarifa.Concept.Nombre,
            tarifa.ClientId, tarifa.Client?.Nombre,
            tarifa.ServiceId, tarifa.Service?.Nombre,
            tarifa.Valor, tarifa.Unidad, tarifa.FechaDesde, tarifa.FechaHasta);
}
