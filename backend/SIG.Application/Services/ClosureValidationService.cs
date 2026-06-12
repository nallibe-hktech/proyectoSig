using SIG.Application.Alerts;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;

namespace SIG.Application.Services;

public class ClosureValidationService : IClosureValidationService
{
    private readonly IClosureAlertaRepository _alertaRepo;
    private readonly IClosureRepository _closureRepo;
    private readonly IUserRepository _userRepo;

    public ClosureValidationService(
        IClosureAlertaRepository alertaRepo,
        IClosureRepository closureRepo,
        IUserRepository userRepo)
    {
        _alertaRepo = alertaRepo;
        _closureRepo = closureRepo;
        _userRepo = userRepo;
    }

    public async Task<IReadOnlyList<ClosureAlertaDto>> ValidarYPersistirAsync(int closureId, int serviceId, int periodId, CancellationToken ct)
    {
        var closure = await _closureRepo.GetByIdAsync(closureId, ct)
                      ?? throw new InvalidOperationException($"Closure {closureId} not found");

        await _alertaRepo.DeleteByClosureIdAsync(closureId, ct);

        var alertas = new List<ClosureAlerta>();

        // BLOQUEANTE: NIF sin mapeo
        alertas.AddRange(CheckNifSinMapeo(closureId));

        // BLOQUEANTE: Contratos duplicados
        alertas.AddRange(CheckContratosDuplicados(closureId));

        // BLOQUEANTE: Campos clave inválidos
        alertas.AddRange(CheckCamposClave(closureId));

        // BLOQUEANTE: Actividad sin contrato
        alertas.AddRange(CheckActividadSinContrato(closureId));

        // BLOQUEANTE: Ceco no en maestro
        alertas.AddRange(CheckCecoNoMaestro(closureId));

        // ADVERTENCIA: Contrato sin actividad
        alertas.AddRange(CheckContratoSinActividad(closureId));

        // ADVERTENCIA: Pago por KM excesivo
        alertas.AddRange(CheckPagoPorKmExcesivo(closureId));

        // ADVERTENCIA: Gasto negativo
        alertas.AddRange(CheckGastoNegativo(closureId));

        // ADVERTENCIA: Pago inferior a contrato
        alertas.AddRange(CheckPagoInferiorContrato(closureId));

        if (alertas.Count > 0)
        {
            await _alertaRepo.AddRangeAsync(alertas, ct);
            await _alertaRepo.SaveChangesAsync(ct);
        }

        return await GetAlertasAsync(closureId, ct);
    }

    public async Task<IReadOnlyList<ClosureAlertaDto>> GetAlertasAsync(int closureId, CancellationToken ct)
    {
        var alertas = await _alertaRepo.GetByClosureIdAsync(closureId, ct);
        var result = new List<ClosureAlertaDto>();

        foreach (var a in alertas)
        {
            string? confirmadaPorNombre = null;
            if (a.ConfirmadaPorUserId.HasValue)
            {
                var user = await _userRepo.GetByIdAsync(a.ConfirmadaPorUserId.Value, ct);
                confirmadaPorNombre = user != null ? $"{user.Nombre} {user.Apellidos}" : null;
            }

            result.Add(new ClosureAlertaDto(
                a.Id, a.Tipo, a.Codigo, a.Descripcion, a.Detalle,
                a.Confirmada, confirmadaPorNombre, a.FechaConfirmacion));
        }

        return result;
    }

    public async Task ConfirmarAdvertenciaAsync(int alertaId, int usuarioId, CancellationToken ct)
    {
        var alerta = await _alertaRepo.GetByIdAsync(alertaId, ct)
                     ?? throw new InvalidOperationException($"Alerta {alertaId} not found");

        if (alerta.Tipo != TipoAlerta.Advertencia)
            throw new InvalidOperationException("Solo se pueden confirmar advertencias, no bloqueantes.");

        alerta.Confirmada = true;
        alerta.ConfirmadaPorUserId = usuarioId;
        alerta.FechaConfirmacion = DateTime.UtcNow;
        alerta.UpdatedAt = DateTime.UtcNow;

        await _alertaRepo.UpdateAsync(alerta, ct);
        await _alertaRepo.SaveChangesAsync(ct);
    }

    private static IEnumerable<ClosureAlerta> CheckNifSinMapeo(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckContratosDuplicados(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckCamposClave(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckActividadSinContrato(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckCecoNoMaestro(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckContratoSinActividad(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckPagoPorKmExcesivo(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckGastoNegativo(int closureId) => [];
    private static IEnumerable<ClosureAlerta> CheckPagoInferiorContrato(int closureId) => [];
}
