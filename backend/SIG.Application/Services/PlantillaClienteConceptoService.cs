using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class PlantillaClienteConceptoService : IPlantillaClienteConceptoService
{
    private readonly IPlantillaClienteConceptoRepository _repo;
    private readonly IClientRepository _clientRepo;
    private readonly IConceptRepository _conceptRepo;

    public PlantillaClienteConceptoService(
        IPlantillaClienteConceptoRepository repo,
        IClientRepository clientRepo,
        IConceptRepository conceptRepo)
    {
        _repo = repo;
        _clientRepo = clientRepo;
        _conceptRepo = conceptRepo;
    }

    public async Task<IReadOnlyList<PlantillaClienteConceptoDto>> ListByClientAsync(int clientId, int usuarioId, CancellationToken ct)
    {
        // Verify client exists and user has access
        var client = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
                    ?? throw new EntityNotFoundException("Cliente", clientId);

        var list = await _repo.ListByClientAsync(clientId, ct);
        return list.Select(p => MapToDto(p)).ToList();
    }

    public async Task<PagedResult<PlantillaClienteConceptoDto>> ListPaginatedByClientAsync(int clientId, int usuarioId, int page, int pageSize, string? search, CancellationToken ct)
    {
        // Verify client exists and user has access
        var client = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
                    ?? throw new EntityNotFoundException("Cliente", clientId);

        var result = await _repo.ListPaginatedByClientAsync(clientId, page, pageSize, search, ct);
        return new PagedResult<PlantillaClienteConceptoDto>(
            result.Items.Select(p => MapToDto(p)).ToList(),
            result.Total,
            result.Page,
            result.PageSize);
    }

    public async Task<PlantillaClienteConceptoDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var plantilla = await _repo.GetByIdAsync(id, ct)
                       ?? throw new EntityNotFoundException("PlantillaClienteConcepto", id);

        // Verify user has access to this client
        _ = await _clientRepo.GetByIdAndUsuarioIdAsync(plantilla.ClientId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Cliente", plantilla.ClientId);

        return MapToDto(plantilla);
    }

    public async Task<PlantillaClienteConceptoDto> CreateAsync(int clientId, PlantillaClienteConceptoCreateRequest req, int usuarioId, CancellationToken ct)
    {
        // Verify client exists and user has access
        var client = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
                    ?? throw new EntityNotFoundException("Cliente", clientId);

        // Verify concept exists
        var concept = await _conceptRepo.GetByIdAsync(req.ConceptId, ct)
                     ?? throw new EntityNotFoundException("Concepto", req.ConceptId);

        // Check if plantilla already exists for this client+concept
        var existing = await _repo.GetByClientAndConceptAsync(clientId, req.ConceptId, ct);
        if (existing != null)
            throw new InvalidOperationException($"Ya existe una plantilla para el concepto {concept.Nombre} en este cliente");

        var plantilla = new PlantillaClienteConcepto
        {
            ClientId = clientId,
            ConceptId = req.ConceptId,
            FormulaJsonOverride = req.FormulaJsonOverride,
            ConfiguracionJson = req.ConfiguracionJson,
            Activo = req.Activo,
            FechaDesde = req.FechaDesde,
            FechaHasta = req.FechaHasta
        };

        await _repo.AddAsync(plantilla, ct);
        await _repo.SaveChangesAsync(ct);

        return MapToDto(plantilla);
    }

    public async Task<PlantillaClienteConceptoDto> UpdateAsync(int id, int clientId, PlantillaClienteConceptoUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        // Verify user has access to this client
        _ = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Cliente", clientId);

        var plantilla = await _repo.GetByIdAsync(id, ct)
                       ?? throw new EntityNotFoundException("PlantillaClienteConcepto", id);

        if (plantilla.ClientId != clientId)
            throw new InvalidOperationException("Plantilla no pertenece a este cliente");

        plantilla.FormulaJsonOverride = req.FormulaJsonOverride;
        plantilla.ConfiguracionJson = req.ConfiguracionJson;
        plantilla.Activo = req.Activo;
        plantilla.FechaDesde = req.FechaDesde;
        plantilla.FechaHasta = req.FechaHasta;

        await _repo.SaveChangesAsync(ct);

        return MapToDto(plantilla);
    }

    public async Task DeleteAsync(int id, int clientId, int usuarioId, CancellationToken ct)
    {
        // Verify user has access to this client
        _ = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Cliente", clientId);

        var plantilla = await _repo.GetByIdAsync(id, ct)
                       ?? throw new EntityNotFoundException("PlantillaClienteConcepto", id);

        if (plantilla.ClientId != clientId)
            throw new InvalidOperationException("Plantilla no pertenece a este cliente");

        plantilla.IsDeleted = true;
        plantilla.DeletedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
    }

    private static PlantillaClienteConceptoDto MapToDto(PlantillaClienteConcepto plantilla) =>
        new(plantilla.Id, plantilla.ClientId, plantilla.ConceptId, plantilla.Concept.Nombre,
            plantilla.FormulaJsonOverride, plantilla.ConfiguracionJson, plantilla.Activo,
            plantilla.FechaDesde, plantilla.FechaHasta);
}
