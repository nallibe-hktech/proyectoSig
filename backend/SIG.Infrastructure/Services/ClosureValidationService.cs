using Microsoft.EntityFrameworkCore;
using SIG.Application.Alerts;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class ClosureValidationService : IClosureValidationService
{
    private readonly IClosureAlertaRepository _alertaRepo;
    private readonly IClosureRepository _closureRepo;
    private readonly IUserRepository _userRepo;
    private readonly IStagingRepository<StagingCeleroVisita> _celeroRepo;
    private readonly IStagingRepository<StagingPayHawkGasto> _gastoRepo;
    private readonly IStagingA3InnuvaContratoRepository _contratoRepo;
    private readonly AppDbContext _db;

    public ClosureValidationService(
        IClosureAlertaRepository alertaRepo,
        IClosureRepository closureRepo,
        IUserRepository userRepo,
        IStagingRepository<StagingCeleroVisita> celeroRepo,
        IStagingRepository<StagingPayHawkGasto> gastoRepo,
        IStagingA3InnuvaContratoRepository contratoRepo,
        AppDbContext db)
    {
        _alertaRepo = alertaRepo;
        _closureRepo = closureRepo;
        _userRepo = userRepo;
        _celeroRepo = celeroRepo;
        _gastoRepo = gastoRepo;
        _contratoRepo = contratoRepo;
        _db = db;
    }

    public async Task<IReadOnlyList<ClosureAlertaDto>> ValidarYPersistirAsync(int closureId, int serviceId, int periodId, CancellationToken ct)
    {
        var closure = await _closureRepo.GetByIdAsync(closureId, ct)
                      ?? throw new InvalidOperationException($"Closure {closureId} not found");
        var period = closure.Period;

        await _alertaRepo.DeleteByClosureIdAsync(closureId, ct);

        var alertas = new List<ClosureAlerta>();

        // BLOQUEANTES
        alertas.AddRange(await CheckNifSinMapeoAsync(closureId, serviceId, period.FechaInicio.ToDateTime(TimeOnly.MinValue), period.FechaFin.ToDateTime(TimeOnly.MaxValue), ct));
        alertas.AddRange(await CheckContratosDuplicadosAsync(closureId, ct));
        alertas.AddRange(await CheckCamposClaveAsync(closureId, serviceId, period.FechaInicio.ToDateTime(TimeOnly.MinValue), period.FechaFin.ToDateTime(TimeOnly.MaxValue), ct));
        alertas.AddRange(await CheckActividadSinContratoAsync(closureId, serviceId, period.FechaInicio.ToDateTime(TimeOnly.MinValue), period.FechaFin.ToDateTime(TimeOnly.MaxValue), ct));
        alertas.AddRange(await CheckCecoNoMaestroAsync(closureId, serviceId, ct));

        // ADVERTENCIAS
        alertas.AddRange(await CheckContratoSinActividadAsync(closureId, serviceId, period.FechaInicio.ToDateTime(TimeOnly.MinValue), period.FechaFin.ToDateTime(TimeOnly.MaxValue), ct));
        alertas.AddRange(await CheckPagoPorKmExcesivoAsync(closureId, serviceId, period.FechaInicio.ToDateTime(TimeOnly.MinValue), period.FechaFin.ToDateTime(TimeOnly.MaxValue), ct));
        alertas.AddRange(await CheckGastoNegativoAsync(closureId, serviceId, period.FechaInicio.ToDateTime(TimeOnly.MinValue), period.FechaFin.ToDateTime(TimeOnly.MaxValue), ct));
        alertas.AddRange(await CheckPagoInferiorContratoAsync(closureId, ct));

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

    // ────────────────────── BLOQUEANTES ──────────────────────

    /// <summary>V1: NIF de Celero/PayHawk no coincide con ningún empleado de A3Innuva.</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckNifSinMapeoAsync(int closureId, int serviceId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();
        var contratos = await _contratoRepo.GetActivosEnPeriodoAsync(desde, hasta, ct);
        var nifsContrato = contratos.Select(c => c.NIF).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Celero: visitas con UserId == null (NIF no mapeado)
        var celeroSinMapeo = await _db.StagingCeleroVisitas.AsNoTracking()
            .Where(v => v.ServiceId == serviceId && v.Fecha >= DateOnly.FromDateTime(desde) && v.Fecha <= DateOnly.FromDateTime(hasta) && v.UserId == null)
            .Select(v => v.ResourceNif)
            .Distinct()
            .ToListAsync(ct);

        foreach (var nif in celeroSinMapeo.Where(n => !string.IsNullOrEmpty(n) && !nifsContrato.Contains(n)))
        {
            alertas.Add(new ClosureAlerta
            {
                ClosureId = closureId,
                Tipo = TipoAlerta.Bloqueante,
                Codigo = AlertaCodigos.NifSinMapeo,
                Descripcion = $"NIF '{nif}' no coincide con ningún empleado de A3Innuva (origen: Celero)",
                Detalle = $"{{\"nif\":\"{nif}\",\"fuente\":\"Celero\"}}"
            });
        }

        // PayHawk: gastos con UserId == null
        var payhawkSinMapeo = await _db.StagingPayHawkGastos.AsNoTracking()
            .Where(g => g.ServiceId == serviceId && g.Fecha >= DateOnly.FromDateTime(desde) && g.Fecha <= DateOnly.FromDateTime(hasta) && g.UserId == null)
            .Select(g => g.UserId) // no hay NIF directo en PayHawk, usamos User lookup
            .ToListAsync(ct);

        return alertas;
    }

    /// <summary>V2: Contratos duplicados o solapados en A3Innuva.</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckContratosDuplicadosAsync(int closureId, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();
        var contratos = await _contratoRepo.GetAllAsync(ct);
        var porNif = contratos.GroupBy(c => c.NIF, StringComparer.OrdinalIgnoreCase);

        foreach (var grupo in porNif)
        {
            var lista = grupo.OrderBy(c => c.FechaInicio).ToList();
            for (int i = 0; i < lista.Count - 1; i++)
            {
                if (lista[i].FechaFin >= lista[i + 1].FechaInicio)
                {
                    alertas.Add(new ClosureAlerta
                    {
                        ClosureId = closureId,
                        Tipo = TipoAlerta.Bloqueante,
                        Codigo = AlertaCodigos.ContratosDuplicados,
                        Descripcion = $"Contratos solapados para NIF '{grupo.Key}': [{lista[i].FechaInicio:yyyy-MM-dd} → {lista[i].FechaFin:yyyy-MM-dd}] y [{lista[i + 1].FechaInicio:yyyy-MM-dd} → {lista[i + 1].FechaFin:yyyy-MM-dd}]",
                        Detalle = $"{{\"nif\":\"{grupo.Key}\",\"contrato1\":\"{lista[i].ContratoIdExterno}\",\"contrato2\":\"{lista[i + 1].ContratoIdExterno}\"}}"
                    });
                }
            }
        }

        return alertas;
    }

    /// <summary>V3: Campos clave en blanco o sin coincidencia en algún origen.</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckCamposClaveAsync(int closureId, int serviceId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();

        // Celero: visitas sin ServiceId y sin UserId
        var sinCampos = await _db.StagingCeleroVisitas.AsNoTracking()
            .Where(v => v.ServiceId == serviceId && v.Fecha >= DateOnly.FromDateTime(desde) && v.Fecha <= DateOnly.FromDateTime(hasta) && v.UserId == null && string.IsNullOrEmpty(v.ResourceNif))
            .CountAsync(ct);

        if (sinCampos > 0)
        {
            alertas.Add(new ClosureAlerta
            {
                ClosureId = closureId,
                Tipo = TipoAlerta.Bloqueante,
                Codigo = AlertaCodigos.CamposClave,
                Descripcion = $"{sinCampos} visita(s) Celero con campos clave en blanco (NIF, fecha, identificador)",
                Detalle = $"{{\"count\":{sinCampos},\"fuente\":\"Celero\"}}"
            });
        }

        return alertas;
    }

    /// <summary>V4: Actividad (visitas Celero) sin contrato vigente en A3Innuva.</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckActividadSinContratoAsync(int closureId, int serviceId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();
        var contratos = await _contratoRepo.GetActivosEnPeriodoAsync(desde, hasta, ct);
        var nifsConContrato = contratos
            .Where(c => !(c.FechaInicio == c.FechaFin))
            .Select(c => c.NIF)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var visitas = await _db.StagingCeleroVisitas.AsNoTracking()
            .Where(v => v.ServiceId == serviceId && v.Fecha >= DateOnly.FromDateTime(desde) && v.Fecha <= DateOnly.FromDateTime(hasta) && v.UserId != null)
            .Select(v => v.UserId.Value)
            .Distinct()
            .ToListAsync(ct);

        var users = await _userRepo.ListAsync(ct);
        var userDict = users.ToDictionary(u => u.Id);

        var nifsSinContrato = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var userId in visitas)
        {
            if (userDict.TryGetValue(userId, out var user) && !nifsConContrato.Contains(user.NIF))
                nifsSinContrato.Add(user.NIF);
        }

        foreach (var nif in nifsSinContrato)
        {
            alertas.Add(new ClosureAlerta
            {
                ClosureId = closureId,
                Tipo = TipoAlerta.Bloqueante,
                Codigo = AlertaCodigos.ActividadSinContrato,
                Descripcion = $"Actividad (visitas Celero) sin contrato vigente para NIF '{nif}'",
                Detalle = $"{{\"nif\":\"{nif}\"}}"
            });
        }

        return alertas;
    }

    /// <summary>V5: Ceco no coincide con tabla maestra.</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckCecoNoMaestroAsync(int closureId, int serviceId, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();

        // Verificar si el servicio tiene CostCenters asignados
        var cecos = await _db.ServiceCostCenters.AsNoTracking()
            .Include(sc => sc.CostCenter)
            .Where(sc => sc.ServiceId == serviceId)
            .ToListAsync(ct);

        if (cecos.Count == 0)
        {
            alertas.Add(new ClosureAlerta
            {
                ClosureId = closureId,
                Tipo = TipoAlerta.Bloqueante,
                Codigo = AlertaCodigos.CecoNoMaestro,
                Descripcion = "El servicio no tiene centros de coste (CECO) asignados",
                Detalle = $"{{\"serviceId\":{serviceId}}}"
            });
        }
        else
        {
            // Verificar que los CECOs no estén soft-deleted
            var cecosEliminados = cecos.Where(sc => sc.CostCenter?.IsDeleted == true).ToList();
            if (cecosEliminados.Count > 0)
            {
                var codigos = string.Join(", ", cecosEliminados.Select(sc => sc.CostCenter!.Codigo));
                alertas.Add(new ClosureAlerta
                {
                    ClosureId = closureId,
                    Tipo = TipoAlerta.Bloqueante,
                    Codigo = AlertaCodigos.CecoNoMaestro,
                    Descripcion = $"CECO(s) eliminados: {codigos}",
                    Detalle = $"{{\"cecosEliminados\":[\"{codigos}\"]}}"
                });
            }
        }

        return alertas;
    }

    // ────────────────────── ADVERTENCIAS ──────────────────────

    /// <summary>V6: Contrato de A3Innuva sin actividad (sin visitas Celero ni fichajes Intratime).</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckContratoSinActividadAsync(int closureId, int serviceId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();
        var contratos = await _contratoRepo.GetActivosEnPeriodoAsync(desde, hasta, ct);

        var fechaDesdeDO = DateOnly.FromDateTime(desde);
        var fechaHastaDO = DateOnly.FromDateTime(hasta);

        var userIdsVisitas = await _db.StagingCeleroVisitas.AsNoTracking()
            .Where(v => v.ServiceId == serviceId && v.Fecha >= fechaDesdeDO && v.Fecha <= fechaHastaDO && v.UserId != null)
            .Select(v => v.UserId.Value)
            .Distinct()
            .ToListAsync(ct);

        var userIdsFichajes = await _db.StagingIntratimeFichajes.AsNoTracking()
            .Where(f => f.Entrada >= desde && f.Entrada <= hasta && f.UserId != null)
            .Select(f => f.UserId.Value)
            .Distinct()
            .ToListAsync(ct);

        var userIdsConActividad = new HashSet<int>(userIdsVisitas.Concat(userIdsFichajes));
        var users = await _userRepo.ListAsync(ct);
        var userDict = users.ToDictionary(u => u.Id);

        foreach (var contrato in contratos.Where(c => c.UserId.HasValue && !userIdsConActividad.Contains(c.UserId.Value) && c.FechaInicio != c.FechaFin))
        {
            var nif = contrato.User != null ? contrato.User.NIF : (userDict.TryGetValue(contrato.UserId.Value, out var u) ? u.NIF : contrato.NIF);
            alertas.Add(new ClosureAlerta
            {
                ClosureId = closureId,
                Tipo = TipoAlerta.Advertencia,
                Codigo = AlertaCodigos.ContratoSinActividad,
                Descripcion = $"Contrato A3Innuva sin actividad para NIF '{nif}' ({contrato.ContratoIdExterno})",
                Detalle = $"{{\"nif\":\"{nif}\",\"contratoId\":\"{contrato.ContratoIdExterno}\",\"fechaInicio\":\"{contrato.FechaInicio:yyyy-MM-dd}\",\"fechaFin\":\"{contrato.FechaFin:yyyy-MM-dd}\"}}"
            });
        }

        return alertas;
    }

    /// <summary>V7: Pago por km superior a 0.25€ en PayHawk.</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckPagoPorKmExcesivoAsync(int closureId, int serviceId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();
        var fechaDesdeDO = DateOnly.FromDateTime(desde);
        var fechaHastaDO = DateOnly.FromDateTime(hasta);

        var gastosKm = await _db.StagingPayHawkGastos.AsNoTracking()
            .Where(g => g.ServiceId == serviceId && g.Fecha >= fechaDesdeDO && g.Fecha <= fechaHastaDO && g.Importe > 0.25m)
            .Where(g => EF.Functions.ILike(g.Categoria, "%km%") || EF.Functions.ILike(g.Categoria, "%kilometr%"))
            .ToListAsync(ct);

        foreach (var gasto in gastosKm)
        {
            alertas.Add(new ClosureAlerta
            {
                ClosureId = closureId,
                Tipo = TipoAlerta.Advertencia,
                Codigo = AlertaCodigos.PagoPorKmExcesivo,
                Descripcion = $"Pago por km excesivo: {gasto.Importe:C2} (categoría: '{gasto.Categoria}', fecha: {gasto.Fecha:yyyy-MM-dd})",
                Detalle = $"{{\"gastoId\":\"{gasto.GastoIdExterno}\",\"importe\":{gasto.Importe},\"categoria\":\"{gasto.Categoria}\",\"fecha\":\"{gasto.Fecha:yyyy-MM-dd}\"}}"
            });
        }

        return alertas;
    }

    /// <summary>V8: Gastos de PayHawk en negativo (individual, no suma total).</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckGastoNegativoAsync(int closureId, int serviceId, DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();
        var fechaDesdeDO = DateOnly.FromDateTime(desde);
        var fechaHastaDO = DateOnly.FromDateTime(hasta);

        var gastosNegativos = await _db.StagingPayHawkGastos.AsNoTracking()
            .Where(g => g.ServiceId == serviceId && g.Fecha >= fechaDesdeDO && g.Fecha <= fechaHastaDO && g.Importe < 0)
            .ToListAsync(ct);

        foreach (var gasto in gastosNegativos)
        {
            alertas.Add(new ClosureAlerta
            {
                ClosureId = closureId,
                Tipo = TipoAlerta.Advertencia,
                Codigo = AlertaCodigos.GastoNegativo,
                Descripcion = $"Gasto negativo en PayHawk: {gasto.Importe:C2} (categoría: '{gasto.Categoria}', fecha: {gasto.Fecha:yyyy-MM-dd})",
                Detalle = $"{{\"gastoId\":\"{gasto.GastoIdExterno}\",\"importe\":{gasto.Importe},\"categoria\":\"{gasto.Categoria}\",\"fecha\":\"{gasto.Fecha:yyyy-MM-dd}\"}}"
            });
        }

        return alertas;
    }

    /// <summary>V9: Pago por actividad (visitas) inferior al importe del contrato en Innuva.</summary>
    private async Task<IEnumerable<ClosureAlerta>> CheckPagoInferiorContratoAsync(int closureId, CancellationToken ct)
    {
        var alertas = new List<ClosureAlerta>();
        var closure = await _db.Closures.AsNoTracking()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == closureId, ct);

        if (closure == null) return alertas;

        var contratos = await _contratoRepo.GetAllAsync(ct);
        var users = await _userRepo.ListAsync(ct);
        var userDict = users.ToDictionary(u => u.Id);

        var pagoLines = closure.Lines.Where(l => l.Tipo == TipoConcepto.Pago && l.UserId.HasValue).ToList();

        foreach (var line in pagoLines)
        {
            var user = userDict.TryGetValue(line.UserId.Value, out var u) ? u : null;
            if (user == null) continue;

            var contrato = contratos.FirstOrDefault(c => c.UserId.HasValue && c.UserId.Value == user.Id);
            if (contrato != null && line.Importe < contrato.ImporteBruto)
            {
                alertas.Add(new ClosureAlerta
                {
                    ClosureId = closureId,
                    Tipo = TipoAlerta.Advertencia,
                    Codigo = AlertaCodigos.PagoInferiorContrato,
                    Descripcion = $"Pago ({line.Importe:C2}) inferior al importe del contrato ({contrato.ImporteBruto:C2}) para NIF '{user.NIF}'",
                    Detalle = $"{{\"closureLineId\":{line.Id},\"importeLinea\":{line.Importe},\"importeContrato\":{contrato.ImporteBruto},\"nif\":\"{user.NIF}\"}}"
                });
            }
        }

        return alertas;
    }
}
