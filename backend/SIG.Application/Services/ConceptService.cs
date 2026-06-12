using SIG.Application.Calculation;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class ConceptService : IConceptService
{
    private readonly IConceptRepository _repo;
    private readonly IFormulaParser _parser;

    public ConceptService(IConceptRepository repo, IFormulaParser parser)
    {
        _repo = repo;
        _parser = parser;
    }

    public async Task<PagedResult<ConceptListItemDto>> ListAsync(int usuarioId, int page, int pageSize, TipoConcepto? tipo, string? search, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, page, pageSize, tipo, search, ct);
        var items = result.Items.Select(c => new ConceptListItemDto(c.Id, c.Nombre, c.Tipo, c.FechaDesde, c.FechaHasta)).ToList();
        return new PagedResult<ConceptListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<ConceptDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Concept", id);
        return Map(c);
    }

    public async Task<ConceptDetailDto> CreateAsync(ConceptCreateRequest req, int usuarioId, CancellationToken ct)
    {
        if (!_parser.TryValidate(req.FormulaJson, out var errores))
            throw new FormulaInvalidException(string.Join("; ", errores));
        var c = new Concept
        {
            Nombre = req.Nombre,
            Tipo = req.Tipo,
            FechaDesde = req.FechaDesde,
            FechaHasta = req.FechaHasta,
            FormulaJson = req.FormulaJson,
            ConceptUsers = req.UserIds.Select(uId => new ConceptUser { UserId = uId }).ToList()
        };
        await _repo.AddAsync(c, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task<ConceptDetailDto> UpdateAsync(int id, ConceptUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Concept", id);
        if (!_parser.TryValidate(req.FormulaJson, out var errores))
            throw new FormulaInvalidException(string.Join("; ", errores));
        c.Nombre = req.Nombre;
        c.Tipo = req.Tipo;
        c.FechaDesde = req.FechaDesde;
        c.FechaHasta = req.FechaHasta;
        c.FormulaJson = req.FormulaJson;
        c.ConceptUsers.Clear();
        foreach (var uId in req.UserIds) c.ConceptUsers.Add(new ConceptUser { ConceptId = id, UserId = uId });
        await _repo.SaveChangesAsync(ct);
        return Map(c);
    }

    public async Task DeleteAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Concept", id);
        if (await _repo.HasServicesAsync(id, ct))
            throw new DependenciesExistException(1);
        c.IsDeleted = true;
        c.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
    }

    public Task<ValidarFormulaResponse> ValidarFormulaAsync(string formulaJson, CancellationToken ct)
    {
        var ok = _parser.TryValidate(formulaJson, out var errores);
        return Task.FromResult(new ValidarFormulaResponse(ok, errores));
    }

    private static ConceptDetailDto Map(Concept c) =>
        new(c.Id, c.Nombre, c.Tipo, c.FechaDesde, c.FechaHasta, c.FormulaJson,
            c.ServiceConcepts.Select(x => x.ServiceId).ToArray(),
            c.ConceptUsers.Select(x => x.UserId).ToArray());
}
