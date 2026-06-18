using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class ContratoService : IContratoService
{
    private readonly IStagingA3InnuvaContratoRepository _repo;

    public ContratoService(IStagingA3InnuvaContratoRepository repo) { _repo = repo; }

    public async Task<IReadOnlyList<ContratoUnDiaDto>> ListContratosUnDiaAsync(CancellationToken ct)
    {
        var contratos = await _repo.ListContratosUnDiaAsync(ct);
        return contratos.Select(Map).ToList();
    }

    public async Task<ContratoUnDiaDto> MarcarIgnorarAsync(int id, ContratoIgnorarRequest req, CancellationToken ct)
    {
        var contrato = await _repo.GetByIdAsync(id, ct)
                       ?? throw new EntityNotFoundException("StagingA3InnuvaContrato", id);
        contrato.IgnoradoEnCierre = req.Ignorar;
        contrato.MotivoIgnorar = req.Ignorar ? req.Motivo : null;
        await _repo.SaveChangesAsync(ct);
        return Map(contrato);
    }

    private static ContratoUnDiaDto Map(StagingA3InnuvaContrato c) => new(
        c.Id, c.ContratoIdExterno, c.NIF, c.FechaInicio, c.FechaFin, c.ImporteBruto,
        c.UserId, c.User != null ? $"{c.User.Nombre} {c.User.Apellidos}" : null,
        c.IgnoradoEnCierre, c.MotivoIgnorar);
}
