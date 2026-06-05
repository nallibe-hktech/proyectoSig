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
    private readonly IProjectRepository _projectRepo;
    private readonly IPeriodRepository _periodRepo;
    private readonly IApprovalRepository _approvalRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IConceptRepository _conceptRepo;
    private readonly ICalculationEngine _engine;

    public ClosureService(
        IClosureRepository repo,
        IClosureLineRepository lineRepo,
        ICalculationLogRepository calcLogRepo,
        IProjectRepository projectRepo,
        IPeriodRepository periodRepo,
        IApprovalRepository approvalRepo,
        IRoleRepository roleRepo,
        IConceptRepository conceptRepo,
        ICalculationEngine engine)
    {
        _repo = repo;
        _lineRepo = lineRepo;
        _calcLogRepo = calcLogRepo;
        _projectRepo = projectRepo;
        _periodRepo = periodRepo;
        _approvalRepo = approvalRepo;
        _roleRepo = roleRepo;
        _conceptRepo = conceptRepo;
        _engine = engine;
    }

    public async Task<PagedResult<ClosureListItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, filter, ct);
        var items = result.Items.Select(c => new ClosureListItemDto(
            c.Id, c.ProjectId, c.Project?.Nombre ?? "", c.PeriodId, c.Period?.Nombre ?? "",
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

        var existing = await _repo.GetByProjectAndPeriodAsync(req.ProjectId, req.PeriodId, ct);
        if (existing is not null)
            throw new DuplicateException("Ya existe un Closure para ese Project y Period.");

        var project = await _projectRepo.GetByIdAsync(req.ProjectId, ct)
                     ?? throw new EntityNotFoundException("Project", req.ProjectId);

        var closure = new Closure
        {
            ProjectId = req.ProjectId,
            Project = project,
            PeriodId = req.PeriodId,
            Period = period,
            Estado = EstadoClosure.Borrador,
            PasoActual = ApprovalStep.ProjectManager,
            Comentarios = req.Comentarios,
            FechaCreacion = DateTime.UtcNow
        };

        await _repo.AddAsync(closure, ct);
        await _repo.SaveChangesAsync(ct);

        await ComputeLinesAsync(closure, ct);

        // Crear primer Approval pendiente PM
        var pmRole = await _roleRepo.GetByNombreAsync("ProjectManager", ct);
        if (pmRole is not null)
        {
            await _approvalRepo.AddAsync(new Approval
            {
                ClosureId = closure.Id,
                RoleId = pmRole.Id,
                Paso = ApprovalStep.ProjectManager,
                Estado = EstadoApproval.Pendiente
            }, ct);
            await _approvalRepo.SaveChangesAsync(ct);
        }

        return await BuildDetailAsync(closure, ct);
    }

    public async Task<ClosureDetailDto> RecalcAsync(int closureId, ClosureRecalcRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var closure = await _repo.GetByIdAndUsuarioIdAsync(closureId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Closure", closureId);
        if (closure.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (closure.Period.Estado != EstadoPeriodo.Abierto)
            throw new PeriodClosedException(closure.Period.Nombre);
        if (closure.Estado != EstadoClosure.Borrador && closure.Estado != EstadoClosure.Rechazado)
            throw new InvalidApprovalTransitionException("Solo se pueden recalcular closures en Borrador o Rechazado.");

        // Borrar líneas existentes y calc logs
        await _calcLogRepo.RemoveAllByClosureAsync(closureId, ct);
        await _lineRepo.RemoveAllByClosureAsync(closureId, ct);

        closure.Comentarios = req.Comentarios ?? closure.Comentarios;
        await ComputeLinesAsync(closure, ct);

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

        var current = await _approvalRepo.GetCurrentByClosureAsync(closureId, ct)
                      ?? throw new InvalidApprovalTransitionException("No hay Approval pendiente para este Closure.");

        var pasoOrigen = current.Paso;
        current.Estado = EstadoApproval.Aprobado;
        current.UserId = usuarioId;
        current.FechaDecision = DateTime.UtcNow;
        current.Motivo = req.Comentarios;

        if ((int)pasoOrigen >= (int)ApprovalStep.SystemExports)
        {
            // Caso límite, ya en último paso
            closure.Estado = EstadoClosure.Aprobado;
        }
        else
        {
            var siguiente = (ApprovalStep)((int)pasoOrigen + 1);
            closure.PasoActual = siguiente;
            closure.Estado = (siguiente == ApprovalStep.SystemExports) ? EstadoClosure.Aprobado : EstadoClosure.EnAprobacion;
            // Crear Approval siguiente solo si no es SystemExports (último ya marca Aprobado)
            if (siguiente != ApprovalStep.SystemExports)
            {
                var roleName = StepToRole(siguiente);
                var role = await _roleRepo.GetByNombreAsync(roleName, ct);
                if (role is not null)
                {
                    await _approvalRepo.AddAsync(new Approval
                    {
                        ClosureId = closureId,
                        RoleId = role.Id,
                        Paso = siguiente,
                        Estado = EstadoApproval.Pendiente
                    }, ct);
                }
            }
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
        current.Estado = EstadoApproval.Rechazado;
        current.UserId = usuarioId;
        current.FechaDecision = DateTime.UtcNow;
        current.Motivo = req.Motivo;

        var anterior = pasoOrigen == ApprovalStep.ProjectManager ? ApprovalStep.ProjectManager
                                                                  : (ApprovalStep)((int)pasoOrigen - 1);
        closure.PasoActual = anterior;
        closure.Estado = EstadoClosure.Rechazado;

        // Crear nuevo Approval pendiente en paso anterior
        var roleName = StepToRole(anterior);
        var role = await _roleRepo.GetByNombreAsync(roleName, ct);
        if (role is not null)
        {
            await _approvalRepo.AddAsync(new Approval
            {
                ClosureId = closureId,
                RoleId = role.Id,
                Paso = anterior,
                Estado = EstadoApproval.Pendiente
            }, ct);
        }

        await _approvalRepo.AddHistoryAsync(new ApprovalHistory
        {
            ClosureId = closureId,
            UserId = usuarioId,
            PasoOrigen = pasoOrigen,
            PasoDestino = anterior,
            Accion = "Rechazar",
            Motivo = req.Motivo,
            Timestamp = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        return await BuildDetailAsync(closure, ct);
    }

    private async Task ComputeLinesAsync(Closure closure, CancellationToken ct)
    {
        // Identifica conceptos aplicables al período del closure
        var conceptList = await _conceptRepo.ListPaginatedForUserAsync(0, 1, int.MaxValue, null, null, ct);
        var aplicables = conceptList.Items.Where(c =>
            c.FechaDesde <= closure.Period.FechaFin &&
            (c.FechaHasta == null || c.FechaHasta >= closure.Period.FechaInicio) &&
            (c.ProjectId == null || c.ProjectId == closure.ProjectId)).ToList();

        decimal coste = 0, factura = 0;
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
            if (concept.Tipo == TipoConcepto.Pago) coste += result.Resultado;
            else factura += result.Resultado;
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

        closure.CosteTotal = Math.Round(coste, 2);
        closure.FacturacionTotal = Math.Round(factura, 2);
        closure.Margen = Math.Round(factura - coste, 2);
        await _repo.SaveChangesAsync(ct);
    }

    private static string StepToRole(ApprovalStep step) => step switch
    {
        ApprovalStep.ProjectManager => "ProjectManager",
        ApprovalStep.Backoffice => "Backoffice",
        ApprovalStep.Fico => "Fico",
        ApprovalStep.Direction => "Direction",
        _ => "Administrator"
    };

    private async Task<ClosureDetailDto> BuildDetailAsync(Closure closure, CancellationToken ct)
    {
        var withLines = await _repo.GetByIdWithLinesAsync(closure.Id, ct) ?? closure;
        var approvals = await _approvalRepo.ListByClosureAsync(closure.Id, ct);

        // Enriquecer líneas con metadata de cálculo
        // Nota: Los logs de cálculo están disponibles en CalculationLog.InputsJson
        // para futuros enriquecimientos de desglose por empleado
        var lines = withLines.Lines.Select(l => new ClosureLineDto(
            l.Id, l.ConceptId, l.Concept?.Nombre ?? "", l.UserId,
            l.User != null ? $"{l.User.Nombre} {l.User.Apellidos}" : null,
            l.Importe, l.Tipo, l.TieneIncidencia, l.RowVersion,
            null,  // SourceDataSummary - Se puede enriquecer con CalculationLog data
            null)).ToArray();  // InputMetadata - Se puede enriquecer con período info

        var aps = approvals.Select(a => new ApprovalDto(
            a.Id, a.Paso, a.RoleId, a.Role?.Nombre ?? "", a.Estado, a.UserId,
            a.User != null ? $"{a.User.Nombre} {a.User.Apellidos}" : null,
            a.Motivo, a.FechaDecision)).ToArray();
        return new ClosureDetailDto(
            withLines.Id, withLines.ProjectId, withLines.Project?.Nombre ?? "",
            withLines.PeriodId, withLines.Period?.Nombre ?? "",
            withLines.CosteTotal, withLines.FacturacionTotal, withLines.Margen,
            withLines.Estado, withLines.PasoActual, withLines.Comentarios, withLines.RowVersion,
            lines, aps);
    }
}
