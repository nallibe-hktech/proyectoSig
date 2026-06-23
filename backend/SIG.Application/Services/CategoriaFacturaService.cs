using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

// Configuración de Factura (prototipo pantalla 25/28, PPT slide 37-38). Las categorías agrupan conceptos
// de facturación del cliente para mostrarlos como una sola línea en su factura. Anidado bajo el cliente:
// se valida acceso al cliente y que los conceptos sean de facturación del cliente y no estén ya asignados.
public class CategoriaFacturaService : ICategoriaFacturaService
{
    private readonly ICategoriaFacturaRepository _repo;
    private readonly IClientRepository _clientRepo;

    public CategoriaFacturaService(ICategoriaFacturaRepository repo, IClientRepository clientRepo)
    {
        _repo = repo;
        _clientRepo = clientRepo;
    }

    public async Task<IReadOnlyList<CategoriaFacturaDto>> ListByClientAsync(int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var categorias = await _repo.ListByClientAsync(clientId, ct);
        return categorias.Select(Map).ToList();
    }

    public async Task<ConfigFacturaResumenDto> GetResumenAsync(int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var categorias = await _repo.ListByClientAsync(clientId, ct);
        var disponibles = await _repo.ListConceptosFacturacionDelClienteAsync(clientId, ct);
        var mapeados = categorias.Sum(c => c.Conceptos.Count);
        var sinAsignar = disponibles.Count - mapeados;
        return new ConfigFacturaResumenDto(categorias.Count, mapeados, sinAsignar < 0 ? 0 : sinAsignar);
    }

    public async Task<IReadOnlyList<ConceptoDisponibleDto>> ListConceptosDisponiblesAsync(int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var conceptos = await _repo.ListConceptosFacturacionDelClienteAsync(clientId, ct);
        var asignaciones = await _repo.GetAsignacionesAsync(clientId, conceptos.Select(c => c.Id).ToList(), ct);
        return conceptos.Select(c =>
        {
            var asignada = asignaciones.TryGetValue(c.Id, out var cat) ? cat : null;
            return new ConceptoDisponibleDto(c.Id, c.Nombre, asignada != null, asignada?.Id, asignada?.Nombre);
        }).ToList();
    }

    public async Task<CategoriaFacturaDto> CreateAsync(int clientId, CategoriaFacturaCreateRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var conceptIds = Distinct(req.ConceptIds);
        await ValidarConceptosAsync(clientId, conceptIds, null, ct);
        var categoria = new CategoriaFactura
        {
            ClientId = clientId,
            Nombre = req.Nombre,
            Conceptos = conceptIds.Select(id => new CategoriaFacturaConcepto { ConceptId = id }).ToList(),
        };
        await _repo.AddAsync(categoria, ct);
        await _repo.SaveChangesAsync(ct);
        var full = await _repo.GetByIdWithConceptosAsync(categoria.Id, ct);
        return Map(full!);
    }

    public async Task<CategoriaFacturaDto> UpdateAsync(int id, int clientId, CategoriaFacturaUpdateRequest req, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var categoria = await _repo.GetByIdWithConceptosAsync(id, ct) ?? throw new EntityNotFoundException("CategoriaFactura", id);
        if (categoria.ClientId != clientId) throw new EntityNotFoundException("CategoriaFactura", id);
        var conceptIds = Distinct(req.ConceptIds);
        await ValidarConceptosAsync(clientId, conceptIds, id, ct);
        categoria.Nombre = req.Nombre;
        categoria.Conceptos.Clear();
        foreach (var cid in conceptIds)
            categoria.Conceptos.Add(new CategoriaFacturaConcepto { CategoriaFacturaId = id, ConceptId = cid });
        await _repo.SaveChangesAsync(ct);
        var full = await _repo.GetByIdWithConceptosAsync(id, ct);
        return Map(full!);
    }

    public async Task DeleteAsync(int id, int clientId, int usuarioId, CancellationToken ct)
    {
        await EnsureClientAsync(clientId, usuarioId, ct);
        var categoria = await _repo.GetByIdWithConceptosAsync(id, ct) ?? throw new EntityNotFoundException("CategoriaFactura", id);
        if (categoria.ClientId != clientId) throw new EntityNotFoundException("CategoriaFactura", id);
        categoria.IsDeleted = true;
        categoria.Conceptos.Clear();   // libera los conceptos para que vuelvan a estar "disponibles"
        await _repo.SaveChangesAsync(ct);
    }

    // Valida que cada concepto sea de facturación del cliente y que no esté ya en OTRA categoría del cliente.
    private async Task ValidarConceptosAsync(int clientId, IReadOnlyList<int> conceptIds, int? categoriaActualId, CancellationToken ct)
    {
        if (conceptIds.Count == 0) return;
        var delCliente = (await _repo.ListConceptosFacturacionDelClienteAsync(clientId, ct)).Select(c => c.Id).ToHashSet();
        var ajenos = conceptIds.Where(id => !delCliente.Contains(id)).ToList();
        if (ajenos.Count > 0)
            throw new DuplicateException($"Conceptos no válidos para este cliente: {string.Join(", ", ajenos)}.");

        var asignaciones = await _repo.GetAsignacionesAsync(clientId, conceptIds, ct);
        var enOtra = asignaciones.Where(kv => kv.Value.Id != categoriaActualId).ToList();
        if (enOtra.Count > 0)
        {
            var detalle = string.Join(", ", enOtra.Select(kv => $"el concepto {kv.Key} ya está en '{kv.Value.Nombre}'"));
            throw new DuplicateException($"Un concepto sólo puede estar en una categoría por cliente: {detalle}.");
        }
    }

    private async Task EnsureClientAsync(int clientId, int usuarioId, CancellationToken ct)
    {
        _ = await _clientRepo.GetByIdAndUsuarioIdAsync(clientId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Client", clientId);
    }

    private static List<int> Distinct(int[]? ids) => (ids ?? Array.Empty<int>()).Distinct().ToList();

    private static CategoriaFacturaDto Map(CategoriaFactura c) =>
        new(c.Id, c.ClientId, c.Nombre,
            c.Conceptos.Select(cc => new CategoriaFacturaConceptoDto(cc.ConceptId, cc.Concept?.Nombre ?? string.Empty))
                .OrderBy(x => x.Nombre).ToList());
}
