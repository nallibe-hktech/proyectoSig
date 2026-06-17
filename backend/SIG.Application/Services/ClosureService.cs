using System.Text.Json;
using SIG.Application.Calculation;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;

namespace SIG.Application.Services;

public class ClosureService : IClosureService
{
    private readonly IClosureRepository _repo;
    private readonly IClosureLineRepository _lineRepo;
    private readonly ICalculationLogRepository _calcLogRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IPeriodRepository _periodRepo;
    private readonly IApprovalRepository _approvalRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IConceptRepository _conceptRepo;
    private readonly IUserRepository _userRepo;
    private readonly ICalculationEngine _engine;
    private readonly IClosureValidationService _validationSvc;

    // Ola 3a (#1): roles globales que, junto con la asignación al servicio vía ServiceUser,
    // habilitan a un usuario como miembro del "grupo" del servicio.
    private static readonly string[] GrupoRoles = { "Facilitador", "Interlocutor", "Gestor" };

    public ClosureService(
        IClosureRepository repo,
        IClosureLineRepository lineRepo,
        ICalculationLogRepository calcLogRepo,
        IServiceRepository serviceRepo,
        IPeriodRepository periodRepo,
        IApprovalRepository approvalRepo,
        IRoleRepository roleRepo,
        IConceptRepository conceptRepo,
        IUserRepository userRepo,
        ICalculationEngine engine,
        IClosureValidationService validationSvc)
    {
        _repo = repo;
        _lineRepo = lineRepo;
        _calcLogRepo = calcLogRepo;
        _serviceRepo = serviceRepo;
        _periodRepo = periodRepo;
        _approvalRepo = approvalRepo;
        _roleRepo = roleRepo;
        _conceptRepo = conceptRepo;
        _userRepo = userRepo;
        _engine = engine;
        _validationSvc = validationSvc;
    }

    public async Task<PagedResult<ClosureListItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, filter, ct);
        var items = result.Items.Select(c => new ClosureListItemDto(
            c.Id, c.ServiceId, c.Service?.Nombre ?? "", c.PeriodId, c.Period?.Nombre ?? "",
            c.CosteTotal, c.FacturacionTotal, c.Margen, c.Estado, c.PasoActual)).ToList();
        return new PagedResult<ClosureListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<ClosureDetailDto> GetByIdForUserAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Closure", id);
        return await BuildDetailAsync(c, ct);
    }

    public async Task<ClosureDetailDto> CreateAsync(ClosureCreateRequest req, int usuarioId, CancellationToken ct)
    {
        var period = await _periodRepo.GetByIdAsync(req.PeriodId, ct)
                     ?? throw new EntityNotFoundException("Period", req.PeriodId);
        if (period.Estado != EstadoPeriodo.Abierto)
            throw new PeriodClosedException(period.Nombre);

        var existing = await _repo.GetByServiceAndPeriodAsync(req.ServiceId, req.PeriodId, ct);
        if (existing is not null)
            throw new DuplicateException("Ya existe un Closure para ese Service y Period.");

        var service = await _serviceRepo.GetByIdAsync(req.ServiceId, ct)
                     ?? throw new EntityNotFoundException("Service", req.ServiceId);

        var closure = new Closure
        {
            ServiceId = req.ServiceId,
            Service = service,
            PeriodId = req.PeriodId,
            Period = period,
            Estado = EstadoClosure.Borrador,
            PasoActual = ApprovalStep.Grupo,
            Comentarios = req.Comentarios,
            FechaCreacion = DateTime.UtcNow
        };

        await _repo.AddAsync(closure, ct);
        await _repo.SaveChangesAsync(ct);

        await ComputeLinesAsync(closure, ct);

        // Validar y generar alertas
        await _validationSvc.ValidarYPersistirAsync(closure.Id, closure.ServiceId, closure.PeriodId, ct);

        // Crear primer Approval pendiente en el paso Grupo (sin rol único: la pertenencia
        // se resuelve por rol global + asignación al servicio, no por Approval.RoleId).
        await _approvalRepo.AddAsync(new Approval
        {
            ClosureId = closure.Id,
            RoleId = null,
            Paso = ApprovalStep.Grupo,
            Estado = EstadoApproval.Pendiente
        }, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(closure, ct);
    }

    public async Task<ClosureDetailDto> RecalcAsync(int closureId, ClosureRecalcRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var closure = await _repo.GetByIdAndUsuarioIdAsync(closureId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Closure", closureId);
        if (closure.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (closure.Period.Estado != EstadoPeriodo.Abierto)
            throw new PeriodClosedException(closure.Period.Nombre);
        if (closure.Estado != EstadoClosure.Borrador && closure.Estado != EstadoClosure.EnAprobacion && closure.Estado != EstadoClosure.Rechazado)
            throw new InvalidApprovalTransitionException("Solo se pueden recalcular closures en Borrador, En aprobación o Rechazado.");

        // Borrar líneas existentes y calc logs
        await _calcLogRepo.RemoveAllByClosureAsync(closureId, ct);
        await _lineRepo.RemoveAllByClosureAsync(closureId, ct);

        closure.Comentarios = req.Comentarios ?? closure.Comentarios;
        await ComputeLinesAsync(closure, ct);

        // Validar y regenerar alertas
        await _validationSvc.ValidarYPersistirAsync(closureId, closure.ServiceId, closure.PeriodId, ct);

        await _approvalRepo.AddHistoryAsync(new ApprovalHistory
        {
            ClosureId = closureId,
            UserId = usuarioId,
            PasoOrigen = closure.PasoActual,
            PasoDestino = closure.PasoActual,
            Accion = "Recalcular",
            Timestamp = DateTime.UtcNow
        }, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(closure, ct);
    }

    public async Task<ClosureDetailDto> ApproveAsync(int closureId, ClosureApproveRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var closure = await _repo.GetByIdAsync(closureId, ct)
                      ?? throw new EntityNotFoundException("Closure", closureId);
        if (closure.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (closure.Estado == EstadoClosure.Aprobado || closure.Estado == EstadoClosure.Exportado)
            throw new InvalidApprovalTransitionException("Closure ya aprobado/exportado.");

        // Validar alertas: bloquear si hay bloqueantes o advertencias sin confirmar
        var alertas = await _validationSvc.GetAlertasAsync(closureId, ct);
        var alertasPendientes = alertas.Where(a => !a.Confirmada).ToList();
        if (alertasPendientes.Any())
            throw new ClosureAlertasBlockingException(alertasPendientes.Select(a => a.Codigo).ToList());

        var current = await _approvalRepo.GetCurrentByClosureAsync(closureId, ct)
                      ?? throw new InvalidApprovalTransitionException("No hay Approval pendiente para este Closure.");

        var pasoOrigen = current.Paso;

        // Autorización a nivel de servicio (refuerzo, además del [Authorize] del controlador).
        await EnsureCanActOnStepAsync(closure, pasoOrigen, usuarioId, ct);

        current.Estado = EstadoApproval.Aprobado;
        current.UserId = usuarioId;
        current.FechaDecision = DateTime.UtcNow;
        current.Motivo = req.Comentarios;

        if (pasoOrigen == ApprovalStep.Grupo)
        {
            // Grupo aprueba → pasa a FICO.
            closure.PasoActual = ApprovalStep.Fico;
            closure.Estado = EstadoClosure.EnAprobacion;
            var ficoRole = await _roleRepo.GetByNombreAsync("Fico", ct);
            await _approvalRepo.AddAsync(new Approval
            {
                ClosureId = closureId,
                RoleId = ficoRole?.Id,
                Paso = ApprovalStep.Fico,
                Estado = EstadoApproval.Pendiente
            }, ct);
        }
        else
        {
            // FICO aprueba → estado terminal Aprobado en el paso SystemExports (sin nuevo Approval).
            closure.PasoActual = ApprovalStep.SystemExports;
            closure.Estado = EstadoClosure.Aprobado;
        }

        await _approvalRepo.AddHistoryAsync(new ApprovalHistory
        {
            ClosureId = closureId,
            UserId = usuarioId,
            PasoOrigen = pasoOrigen,
            PasoDestino = closure.PasoActual,
            Accion = "Aprobar",
            Motivo = req.Comentarios,
            Timestamp = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        return await BuildDetailAsync(closure, ct);
    }

    public async Task<ClosureDetailDto> RejectAsync(int closureId, ClosureRejectRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var closure = await _repo.GetByIdAsync(closureId, ct)
                      ?? throw new EntityNotFoundException("Closure", closureId);
        if (closure.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (closure.Estado == EstadoClosure.Aprobado || closure.Estado == EstadoClosure.Exportado)
            throw new InvalidApprovalTransitionException("Closure ya aprobado/exportado.");

        var current = await _approvalRepo.GetCurrentByClosureAsync(closureId, ct)
                      ?? throw new InvalidApprovalTransitionException("No hay Approval pendiente.");

        var pasoOrigen = current.Paso;

        // Autorización a nivel de servicio (refuerzo, además del [Authorize] del controlador).
        await EnsureCanActOnStepAsync(closure, pasoOrigen, usuarioId, ct);

        current.Estado = EstadoApproval.Rechazado;
        current.UserId = usuarioId;
        current.FechaDecision = DateTime.UtcNow;
        current.Motivo = req.Motivo;

        // Rechazo en FICO → vuelve a Grupo (re-editable). Rechazo en Grupo → permanece en Grupo.
        var destino = ApprovalStep.Grupo;
        closure.PasoActual = destino;
        closure.Estado = EstadoClosure.Rechazado;

        // Nuevo Approval pendiente en Grupo (sin rol único: pertenencia por rol global + asignación).
        await _approvalRepo.AddAsync(new Approval
        {
            ClosureId = closureId,
            RoleId = null,
            Paso = destino,
            Estado = EstadoApproval.Pendiente
        }, ct);

        await _approvalRepo.AddHistoryAsync(new ApprovalHistory
        {
            ClosureId = closureId,
            UserId = usuarioId,
            PasoOrigen = pasoOrigen,
            PasoDestino = destino,
            Accion = "Rechazar",
            Motivo = req.Motivo,
            Timestamp = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        return await BuildDetailAsync(closure, ct);
    }

    public async Task<ClosureDetailDto> OverrideLineAsync(int closureId, int lineId, ClosureLineOverrideRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var closure = await _repo.GetByIdAndUsuarioIdAsync(closureId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Closure", closureId);
        if (closure.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (closure.Estado != EstadoClosure.Borrador && closure.Estado != EstadoClosure.Rechazado)
            throw new InvalidApprovalTransitionException("Solo se pueden ajustar líneas de closures en Borrador o Rechazado.");

        var line = await _lineRepo.GetByIdAsync(lineId, ct)
                   ?? throw new EntityNotFoundException("ClosureLine", lineId);
        if (line.ClosureId != closureId)
            throw new EntityNotFoundException("ClosureLine", lineId);

        if (!line.EsManual)
            line.ImporteOriginal = line.Importe;
        line.Importe = req.Importe;
        line.EsManual = true;
        line.MotivoManual = req.Motivo;
        await _lineRepo.SaveChangesAsync(ct);

        await RecomputeTotalsAsync(closure, ct);

        await _approvalRepo.AddHistoryAsync(new ApprovalHistory
        {
            ClosureId = closureId,
            UserId = usuarioId,
            PasoOrigen = closure.PasoActual,
            PasoDestino = closure.PasoActual,
            Accion = "OverrideLinea",
            Motivo = req.Motivo,
            Timestamp = DateTime.UtcNow
        }, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(closure, ct);
    }

    public async Task<ClosureDetailDto> AddIncentivoAsync(int closureId, ClosureLineIncentivoRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var closure = await _repo.GetByIdAndUsuarioIdAsync(closureId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Closure", closureId);
        if (closure.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (closure.Estado != EstadoClosure.Borrador && closure.Estado != EstadoClosure.Rechazado)
            throw new InvalidApprovalTransitionException("Solo se pueden añadir incentivos a closures en Borrador o Rechazado.");

        var concept = await _conceptRepo.GetByIdAsync(req.ConceptId, ct)
                      ?? throw new EntityNotFoundException("Concept", req.ConceptId);

        var line = new ClosureLine
        {
            ClosureId = closureId,
            ConceptId = req.ConceptId,
            UserId = req.UserId,
            Importe = req.Importe,
            DatosEntradaJson = "{}",
            Tipo = req.Tipo,
            TieneIncidencia = false,
            EsManual = true,
            MotivoManual = req.Motivo
        };
        await _lineRepo.AddAsync(line, ct);
        await _lineRepo.SaveChangesAsync(ct);

        await RecomputeTotalsAsync(closure, ct);

        await _approvalRepo.AddHistoryAsync(new ApprovalHistory
        {
            ClosureId = closureId,
            UserId = usuarioId,
            PasoOrigen = closure.PasoActual,
            PasoDestino = closure.PasoActual,
            Accion = "AddIncentivo",
            Motivo = req.Motivo,
            Timestamp = DateTime.UtcNow
        }, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(closure, ct);
    }

    private async Task RecomputeTotalsAsync(Closure closure, CancellationToken ct)
    {
        var lines = await _lineRepo.ListByClosureAsync(closure.Id, ct);
        decimal coste = lines.Where(l => l.Tipo == TipoConcepto.Pago).Sum(l => l.Importe);
        decimal factura = lines.Where(l => l.Tipo == TipoConcepto.Factura).Sum(l => l.Importe);
        closure.CosteTotal = Math.Round(coste, 2);
        closure.FacturacionTotal = Math.Round(factura, 2);
        closure.Margen = Math.Round(factura - coste, 2);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task ComputeLinesAsync(Closure closure, CancellationToken ct)
    {
        // Identifica conceptos aplicables al período del closure
        var conceptList = await _conceptRepo.ListPaginatedForUserAsync(0, 1, int.MaxValue, null, null, ct);
        var aplicables = conceptList.Items.Where(c =>
            c.FechaDesde <= closure.Period.FechaFin &&
            (c.FechaHasta == null || c.FechaHasta >= closure.Period.FechaInicio) &&
            (c.ServiceId == null || c.ServiceId == closure.ServiceId)).ToList();

        var lines = new List<ClosureLine>();
        var logs = new List<CalculationLog>();

        foreach (var concept in aplicables)
        {
            var result = await _engine.EvaluateAsync(concept, closure, null, ct);
            var line = new ClosureLine
            {
                ClosureId = closure.Id,
                ConceptId = concept.Id,
                Importe = result.Resultado,
                DatosEntradaJson = result.InputsJson,
                Tipo = concept.Tipo,
                TieneIncidencia = result.Incidencias.Any()
            };
            lines.Add(line);
        }

        await _lineRepo.AddRangeAsync(lines, ct);
        await _lineRepo.SaveChangesAsync(ct);

        foreach (var line in lines)
        {
            var concept = aplicables.First(c => c.Id == line.ConceptId);
            // Re-evaluar para capturar el JSON original
            var result = await _engine.EvaluateAsync(concept, closure, null, ct);
            logs.Add(new CalculationLog
            {
                ClosureLineId = line.Id,
                ConceptId = concept.Id,
                FormulaSnapshotJson = result.FormulaSnapshotJson,
                InputsJson = result.InputsJson,
                Resultado = result.Resultado,
                Incidencias = result.Incidencias.Any() ? JsonSerializer.Serialize(result.Incidencias) : null,
                SistemaOrigen = result.SistemaOrigen,
                Timestamp = DateTime.UtcNow
            });
        }

        await _calcLogRepo.AddRangeAsync(logs, ct);
        await _calcLogRepo.SaveChangesAsync(ct);

        // Incluir en los totales las líneas manuales/incentivos preservadas en el recálculo (Ola 2 #3a).
        await RecomputeTotalsAsync(closure, ct);
    }

    // Ola 3a (#1): autorización por paso reforzada a nivel de servicio.
    //  - Grupo: Administrator, o (rol global Facilitador/Interlocutor/Gestor Y asignado al servicio vía ServiceUser).
    //  - Fico: Administrator, o rol global Fico.
    // La fuente de verdad de qué exige cada paso es Approval.Paso (no Approval.RoleId).
    private async Task EnsureCanActOnStepAsync(Closure closure, ApprovalStep paso, int usuarioId, CancellationToken ct)
    {
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        if (roles.Contains("Administrator")) return;

        if (paso == ApprovalStep.Grupo)
        {
            var esRolGrupo = roles.Any(r => GrupoRoles.Contains(r));
            if (!esRolGrupo)
                throw new NotOwnerException();
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            if (!serviceIds.Contains(closure.ServiceId))
                throw new NotOwnerException();
            return;
        }

        if (paso == ApprovalStep.Fico)
        {
            if (!roles.Contains("Fico"))
                throw new NotOwnerException();
            return;
        }

        // SystemExports es terminal: no se aprueba/rechaza manualmente.
        throw new InvalidApprovalTransitionException("El paso actual no admite aprobación o rechazo manual.");
    }

    private async Task<ClosureDetailDto> BuildDetailAsync(Closure closure, CancellationToken ct)
    {
        var withLines = await _repo.GetByIdWithLinesAsync(closure.Id, ct) ?? closure;
        var approvals = await _approvalRepo.ListByClosureAsync(closure.Id, ct);
        var alertas = await _validationSvc.GetAlertasAsync(closure.Id, ct);

        // Enriquecer líneas con metadata de cálculo
        // Nota: Los logs de cálculo están disponibles en CalculationLog.InputsJson
        // para futuros enriquecimientos de desglose por empleado
        var lines = withLines.Lines.Select(l => new ClosureLineDto(
            l.Id, l.ConceptId, l.Concept?.Nombre ?? "", l.UserId,
            l.User != null ? $"{l.User.Nombre} {l.User.Apellidos}" : null,
            l.Importe, l.Tipo, l.TieneIncidencia, l.RowVersion,
            l.EsManual, l.ImporteOriginal, l.MotivoManual,
            null,  // SourceDataSummary - Se puede enriquecer con CalculationLog data
            null)).ToArray();  // InputMetadata - Se puede enriquecer con período info

        var aps = approvals.Select(a => new ApprovalDto(
            a.Id, a.Paso, a.RoleId ?? 0, a.Role?.Nombre ?? (a.Paso == ApprovalStep.Grupo ? "Grupo" : ""), a.Estado, a.UserId,
            a.User != null ? $"{a.User.Nombre} {a.User.Apellidos}" : null,
            a.Motivo, a.FechaDecision)).ToArray();
        return new ClosureDetailDto(
            withLines.Id, withLines.ServiceId, withLines.Service?.Nombre ?? "",
            withLines.PeriodId, withLines.Period?.Nombre ?? "",
            withLines.CosteTotal, withLines.FacturacionTotal, withLines.Margen,
            withLines.Estado, withLines.PasoActual, withLines.Comentarios, withLines.RowVersion,
            lines, aps, alertas.ToArray());
    }
}
