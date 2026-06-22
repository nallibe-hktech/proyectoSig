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

// Ola 3b (#10) — Diseño A (raíces separadas, hijos por FK) + "Margen al vuelo".
// Lógica común de cierre parametrizada por raíz (CierreCostes / CierreFacturacion).
// Cada cierre evalúa SOLO sus conceptos: costes -> Pago, facturación -> Factura.
public abstract class CierreServiceBase<TCierre> : ICierreService where TCierre : class, ICierre, new()
{
    private readonly ICierreRepository<TCierre> _repo;
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

    private static readonly string[] GrupoRoles = { "Facilitador", "Interlocutor", "Gestor" };

    public TipoCierre Tipo => _repo.Tipo;
    private TipoConcepto ConceptoTipo => Tipo == TipoCierre.Costes ? TipoConcepto.Pago : TipoConcepto.Factura;

    protected CierreServiceBase(
        ICierreRepository<TCierre> repo,
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

    // Asigna el dueño correcto (una de las dos FK nullable) en una entidad hija.
    private void SetOwner<T>(T child, int cierreId) where T : class
    {
        switch (child)
        {
            case ClosureLine l: AssignFk(cierreId, v => l.CierreCostesId = v, v => l.CierreFacturacionId = v); break;
            case Approval a: AssignFk(cierreId, v => a.CierreCostesId = v, v => a.CierreFacturacionId = v); break;
            case ApprovalHistory h: AssignFk(cierreId, v => h.CierreCostesId = v, v => h.CierreFacturacionId = v); break;
        }
    }

    private void AssignFk(int cierreId, Action<int?> setCostes, Action<int?> setFact)
    {
        if (Tipo == TipoCierre.Costes) setCostes(cierreId);
        else setFact(cierreId);
    }

    private CalculationTarget Target(TCierre c) => new() { ServiceId = c.ServiceId, PeriodId = c.PeriodId, Period = c.Period };

    public async Task<PagedResult<CierreListItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct)
    {
        var result = await _repo.ListPaginatedForUserAsync(usuarioId, filter, ct);
        var items = result.Items.Select(c => new CierreListItemDto(
            c.Id, Tipo, c.ServiceId, c.Service?.Nombre ?? "", c.PeriodId, c.Period?.Nombre ?? "",
            c.Total, c.Estado, c.PasoActual)).ToList();
        return new PagedResult<CierreListItemDto>(items, result.Total, result.Page, result.PageSize);
    }

    public async Task<CierreDetailDto> GetByIdForUserAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await _repo.GetByIdAndUsuarioIdAsync(id, usuarioId, ct)
                ?? throw new EntityNotFoundException("Cierre", id);
        return await BuildDetailAsync(c, ct);
    }

    public async Task<CierreDetailDto> CreateAsync(CierreCreateRequest req, int usuarioId, CancellationToken ct)
    {
        var period = await _periodRepo.GetByIdAsync(req.PeriodId, ct)
                     ?? throw new EntityNotFoundException("Period", req.PeriodId);
        if (period.Estado != EstadoPeriodo.Abierto)
            throw new PeriodClosedException(period.Nombre);

        var existing = await _repo.GetByServiceAndPeriodAsync(req.ServiceId, req.PeriodId, ct);
        if (existing is not null)
            throw new DuplicateException($"Ya existe un cierre de {Tipo} para ese Service y Period.");

        var service = await _serviceRepo.GetByIdAsync(req.ServiceId, ct)
                     ?? throw new EntityNotFoundException("Service", req.ServiceId);

        var cierre = new TCierre
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

        await _repo.AddAsync(cierre, ct);
        await _repo.SaveChangesAsync(ct);

        await ComputeLinesAsync(cierre, ct);

        await _validationSvc.ValidarYPersistirAsync(Tipo, cierre.Id, cierre.ServiceId, cierre.PeriodId, ct);

        var approval = new Approval { Paso = ApprovalStep.Grupo, RoleId = null, Estado = EstadoApproval.Pendiente };
        SetOwner(approval, cierre.Id);
        await _approvalRepo.AddAsync(approval, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(cierre, ct);
    }

    public async Task<CierreDetailDto> RecalcAsync(int cierreId, CierreRecalcRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var cierre = await _repo.GetByIdAndUsuarioIdAsync(cierreId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Cierre", cierreId);
        if (cierre.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (cierre.Period.Estado != EstadoPeriodo.Abierto)
            throw new PeriodClosedException(cierre.Period.Nombre);
        if (cierre.Estado != EstadoClosure.Borrador && cierre.Estado != EstadoClosure.EnAprobacion && cierre.Estado != EstadoClosure.Rechazado)
            throw new InvalidApprovalTransitionException("Solo se pueden recalcular cierres en Borrador, En aprobación o Rechazado.");

        await _calcLogRepo.RemoveAllByCierreAsync(Tipo, cierreId, ct);
        await _lineRepo.RemoveAllByCierreAsync(Tipo, cierreId, ct);

        cierre.Comentarios = req.Comentarios ?? cierre.Comentarios;
        await ComputeLinesAsync(cierre, ct);

        await _validationSvc.ValidarYPersistirAsync(Tipo, cierreId, cierre.ServiceId, cierre.PeriodId, ct);

        await AddHistoryAsync(cierreId, usuarioId, cierre.PasoActual, cierre.PasoActual, "Recalcular", null, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(cierre, ct);
    }

    public async Task<CierreDetailDto> ApproveAsync(int cierreId, CierreApproveRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var cierre = await _repo.GetByIdAsync(cierreId, ct)
                      ?? throw new EntityNotFoundException("Cierre", cierreId);
        if (cierre.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (cierre.Estado == EstadoClosure.Aprobado || cierre.Estado == EstadoClosure.Exportado)
            throw new InvalidApprovalTransitionException("Cierre ya aprobado/exportado.");

        var alertas = await _validationSvc.GetAlertasAsync(Tipo, cierreId, ct);
        var alertasPendientes = alertas.Where(a => !a.Confirmada).ToList();
        if (alertasPendientes.Any())
            throw new ClosureAlertasBlockingException(alertasPendientes.Select(a => a.Codigo).ToList());

        var current = await _approvalRepo.GetCurrentByCierreAsync(Tipo, cierreId, ct)
                      ?? throw new InvalidApprovalTransitionException("No hay Approval pendiente para este cierre.");

        var pasoOrigen = current.Paso;
        await EnsureCanActOnStepAsync(cierre, pasoOrigen, usuarioId, ct);

        current.Estado = EstadoApproval.Aprobado;
        current.UserId = usuarioId;
        current.FechaDecision = DateTime.UtcNow;
        current.Motivo = req.Comentarios;

        if (pasoOrigen == ApprovalStep.Grupo)
        {
            cierre.PasoActual = ApprovalStep.Fico;
            cierre.Estado = EstadoClosure.EnAprobacion;
            var ficoRole = await _roleRepo.GetByNombreAsync("Fico", ct);
            var nextApproval = new Approval { Paso = ApprovalStep.Fico, RoleId = ficoRole?.Id, Estado = EstadoApproval.Pendiente };
            SetOwner(nextApproval, cierreId);
            await _approvalRepo.AddAsync(nextApproval, ct);
        }
        else
        {
            cierre.PasoActual = ApprovalStep.SystemExports;
            cierre.Estado = EstadoClosure.Aprobado;
        }

        await AddHistoryAsync(cierreId, usuarioId, pasoOrigen, cierre.PasoActual, "Aprobar", req.Comentarios, ct);
        await _repo.SaveChangesAsync(ct);

        return await BuildDetailAsync(cierre, ct);
    }

    public async Task<CierreDetailDto> RejectAsync(int cierreId, CierreRejectRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var cierre = await _repo.GetByIdAsync(cierreId, ct)
                      ?? throw new EntityNotFoundException("Cierre", cierreId);
        if (cierre.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (cierre.Estado == EstadoClosure.Aprobado || cierre.Estado == EstadoClosure.Exportado)
            throw new InvalidApprovalTransitionException("Cierre ya aprobado/exportado.");

        var current = await _approvalRepo.GetCurrentByCierreAsync(Tipo, cierreId, ct)
                      ?? throw new InvalidApprovalTransitionException("No hay Approval pendiente.");

        var pasoOrigen = current.Paso;
        await EnsureCanActOnStepAsync(cierre, pasoOrigen, usuarioId, ct);

        current.Estado = EstadoApproval.Rechazado;
        current.UserId = usuarioId;
        current.FechaDecision = DateTime.UtcNow;
        current.Motivo = req.Motivo;

        var destino = ApprovalStep.Grupo;
        cierre.PasoActual = destino;
        cierre.Estado = EstadoClosure.Rechazado;

        var newApproval = new Approval { Paso = destino, RoleId = null, Estado = EstadoApproval.Pendiente };
        SetOwner(newApproval, cierreId);
        await _approvalRepo.AddAsync(newApproval, ct);

        await AddHistoryAsync(cierreId, usuarioId, pasoOrigen, destino, "Rechazar", req.Motivo, ct);
        await _repo.SaveChangesAsync(ct);

        return await BuildDetailAsync(cierre, ct);
    }

    public async Task<CierreDetailDto> OverrideLineAsync(int cierreId, int lineId, CierreLineOverrideRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var cierre = await _repo.GetByIdAndUsuarioIdAsync(cierreId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Cierre", cierreId);
        if (cierre.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (cierre.Estado != EstadoClosure.Borrador && cierre.Estado != EstadoClosure.Rechazado)
            throw new InvalidApprovalTransitionException("Solo se pueden ajustar líneas de cierres en Borrador o Rechazado.");

        var line = await _lineRepo.GetByIdAsync(lineId, ct)
                   ?? throw new EntityNotFoundException("ClosureLine", lineId);
        if (!LineBelongsTo(line, cierreId))
            throw new EntityNotFoundException("ClosureLine", lineId);

        if (!line.EsManual)
            line.ImporteOriginal = line.Importe;
        line.Importe = req.Importe;
        line.EsManual = true;
        line.MotivoManual = req.Motivo;
        await _lineRepo.SaveChangesAsync(ct);

        await RecomputeTotalAsync(cierre, ct);

        await AddHistoryAsync(cierreId, usuarioId, cierre.PasoActual, cierre.PasoActual, "OverrideLinea", req.Motivo, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(cierre, ct);
    }

    public async Task<CierreDetailDto> AddIncentivoAsync(int cierreId, CierreLineIncentivoRequest req, uint rowVersion, int usuarioId, CancellationToken ct)
    {
        var cierre = await _repo.GetByIdAndUsuarioIdAsync(cierreId, usuarioId, ct)
                      ?? throw new EntityNotFoundException("Cierre", cierreId);
        if (cierre.RowVersion != rowVersion) throw new ConcurrencyConflictException();
        if (cierre.Estado != EstadoClosure.Borrador && cierre.Estado != EstadoClosure.Rechazado)
            throw new InvalidApprovalTransitionException("Solo se pueden añadir incentivos a cierres en Borrador o Rechazado.");

        _ = await _conceptRepo.GetByIdAsync(req.ConceptId, ct)
                      ?? throw new EntityNotFoundException("Concept", req.ConceptId);

        var line = new ClosureLine
        {
            ConceptId = req.ConceptId,
            UserId = req.UserId,
            Importe = req.Importe,
            DatosEntradaJson = "{}",
            Tipo = ConceptoTipo,
            TieneIncidencia = false,
            EsManual = true,
            MotivoManual = req.Motivo
        };
        SetOwner(line, cierreId);
        await _lineRepo.AddAsync(line, ct);
        await _lineRepo.SaveChangesAsync(ct);

        await RecomputeTotalAsync(cierre, ct);

        await AddHistoryAsync(cierreId, usuarioId, cierre.PasoActual, cierre.PasoActual, "AddIncentivo", req.Motivo, ct);
        await _approvalRepo.SaveChangesAsync(ct);

        return await BuildDetailAsync(cierre, ct);
    }

    public async Task<IReadOnlyList<ClosureAlertaDto>> GetAlertasAsync(int cierreId, int usuarioId, CancellationToken ct)
    {
        _ = await _repo.GetByIdAndUsuarioIdAsync(cierreId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Cierre", cierreId);
        return await _validationSvc.GetAlertasAsync(Tipo, cierreId, ct);
    }

    public async Task<CierreDetailDto> ConfirmarAlertaAsync(int cierreId, int alertaId, int usuarioId, CancellationToken ct)
    {
        var cierre = await _repo.GetByIdAndUsuarioIdAsync(cierreId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Cierre", cierreId);
        await _validationSvc.ConfirmarAdvertenciaAsync(alertaId, usuarioId, ct);
        return await BuildDetailAsync(cierre, ct);
    }

    public async Task<IReadOnlyList<CierreHistoryDto>> GetHistoryAsync(int cierreId, int usuarioId, CancellationToken ct)
    {
        _ = await _repo.GetByIdAndUsuarioIdAsync(cierreId, usuarioId, ct)
            ?? throw new EntityNotFoundException("Cierre", cierreId);
        var history = await _approvalRepo.ListHistoryByCierreAsync(Tipo, cierreId, ct);
        return history.Select(h => new CierreHistoryDto(
            h.Id, cierreId, Tipo, h.UserId,
            h.User != null ? $"{h.User.Nombre} {h.User.Apellidos}" : "",
            h.PasoOrigen, h.PasoDestino, h.Accion, h.Motivo, h.Timestamp)).ToList();
    }

    // ─────────────────── helpers ───────────────────

    private bool LineBelongsTo(ClosureLine line, int cierreId) =>
        Tipo == TipoCierre.Costes ? line.CierreCostesId == cierreId : line.CierreFacturacionId == cierreId;

    private async Task AddHistoryAsync(int cierreId, int usuarioId, ApprovalStep origen, ApprovalStep destino, string accion, string? motivo, CancellationToken ct)
    {
        var h = new ApprovalHistory
        {
            UserId = usuarioId,
            PasoOrigen = origen,
            PasoDestino = destino,
            Accion = accion,
            Motivo = motivo,
            Timestamp = DateTime.UtcNow
        };
        SetOwner(h, cierreId);
        await _approvalRepo.AddHistoryAsync(h, ct);
    }

    private async Task RecomputeTotalAsync(TCierre cierre, CancellationToken ct)
    {
        var lines = await _lineRepo.ListByCierreAsync(Tipo, cierre.Id, ct);
        cierre.Total = Math.Round(lines.Sum(l => l.Importe), 2);
        await _repo.SaveChangesAsync(ct);
    }

    private async Task ComputeLinesAsync(TCierre cierre, CancellationToken ct)
    {
        var conceptList = await _conceptRepo.ListPaginatedForUserAsync(0, 1, int.MaxValue, ConceptoTipo, null, ct);
        var aplicables = conceptList.Items.Where(c =>
            c.FechaDesde <= cierre.Period.FechaFin &&
            (c.FechaHasta == null || c.FechaHasta >= cierre.Period.FechaInicio) &&
            (c.ServiceId == null || c.ServiceId == cierre.ServiceId)).ToList();

        var target = Target(cierre);

        // Dos pasadas: 1) conceptos base; 2) conceptos "fee sobre conceptos" (ConceptRef), que necesitan
        // los importes base ya calculados. Cada concepto se evalúa UNA sola vez (línea + log del mismo resultado).
        var baseConcepts = aplicables.Where(c => !EsFeeSobreConceptos(c)).ToList();
        var feeConcepts = aplicables.Where(EsFeeSobreConceptos).ToList();

        var resultados = new Dictionary<int, CalculationResult>();
        foreach (var concept in baseConcepts)
        {
            var result = await _engine.EvaluateAsync(concept, target, null, ct);
            resultados[concept.Id] = result;
            target.ImportesPrevios[concept.Id] = result.Resultado;
        }
        foreach (var concept in feeConcepts)
        {
            resultados[concept.Id] = await _engine.EvaluateAsync(concept, target, null, ct);
        }

        var lines = new List<ClosureLine>();
        foreach (var concept in aplicables)
        {
            var result = resultados[concept.Id];
            var line = new ClosureLine
            {
                ConceptId = concept.Id,
                Importe = result.Resultado,
                DatosEntradaJson = result.InputsJson,
                Tipo = concept.Tipo,
                TieneIncidencia = result.Incidencias.Any()
            };
            SetOwner(line, cierre.Id);
            lines.Add(line);
        }

        await _lineRepo.AddRangeAsync(lines, ct);
        await _lineRepo.SaveChangesAsync(ct);

        var logs = new List<CalculationLog>();
        foreach (var line in lines)
        {
            var result = resultados[line.ConceptId];
            logs.Add(new CalculationLog
            {
                ClosureLineId = line.Id,
                ConceptId = line.ConceptId,
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

        await RecomputeTotalAsync(cierre, ct);
    }

    // Un concepto es "fee sobre conceptos" si su fórmula contiene un ConceptRefNode (depende de otras líneas).
    private static bool EsFeeSobreConceptos(Concept c) =>
        c.FormulaJson?.Contains("\"ConceptRef\"", StringComparison.Ordinal) == true;

    // Ola 3a (#1): autorización por paso reforzada a nivel de servicio.
    private async Task EnsureCanActOnStepAsync(TCierre cierre, ApprovalStep paso, int usuarioId, CancellationToken ct)
    {
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        if (roles.Contains("Administrator")) return;

        if (paso == ApprovalStep.Grupo)
        {
            var esRolGrupo = roles.Any(r => GrupoRoles.Contains(r));
            if (!esRolGrupo)
                throw new NotOwnerException();
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            if (!serviceIds.Contains(cierre.ServiceId))
                throw new NotOwnerException();
            return;
        }

        if (paso == ApprovalStep.Fico)
        {
            if (!roles.Contains("Fico"))
                throw new NotOwnerException();
            return;
        }

        throw new InvalidApprovalTransitionException("El paso actual no admite aprobación o rechazo manual.");
    }

    private async Task<CierreDetailDto> BuildDetailAsync(TCierre cierre, CancellationToken ct)
    {
        var withLines = await _repo.GetByIdWithLinesAsync(cierre.Id, ct) ?? cierre;
        var approvals = await _approvalRepo.ListByCierreAsync(Tipo, cierre.Id, ct);
        var alertas = await _validationSvc.GetAlertasAsync(Tipo, cierre.Id, ct);

        var lines = withLines.Lines.Select(l => new ClosureLineDto(
            l.Id, l.ConceptId, l.Concept?.Nombre ?? "", l.UserId,
            l.User != null ? $"{l.User.Nombre} {l.User.Apellidos}" : null,
            l.Importe, l.Tipo, l.TieneIncidencia, l.RowVersion,
            l.EsManual, l.ImporteOriginal, l.MotivoManual,
            null, null)).ToArray();

        var aps = approvals.Select(a => new ApprovalDto(
            a.Id, a.Paso, a.RoleId ?? 0, a.Role?.Nombre ?? (a.Paso == ApprovalStep.Grupo ? "Grupo" : ""), a.Estado, a.UserId,
            a.User != null ? $"{a.User.Nombre} {a.User.Apellidos}" : null,
            a.Motivo, a.FechaDecision)).ToArray();

        return new CierreDetailDto(
            withLines.Id, Tipo, withLines.ServiceId, withLines.Service?.Nombre ?? "",
            withLines.PeriodId, withLines.Period?.Nombre ?? "",
            withLines.Total, withLines.Estado, withLines.PasoActual, withLines.Comentarios, withLines.RowVersion,
            lines, aps, alertas.ToArray());
    }
}

public class CierreCostesService : CierreServiceBase<CierreCostes>, ICierreCostesService
{
    public CierreCostesService(
        ICierreCostesRepository repo, IClosureLineRepository lineRepo, ICalculationLogRepository calcLogRepo,
        IServiceRepository serviceRepo, IPeriodRepository periodRepo, IApprovalRepository approvalRepo,
        IRoleRepository roleRepo, IConceptRepository conceptRepo, IUserRepository userRepo,
        ICalculationEngine engine, IClosureValidationService validationSvc)
        : base(repo, lineRepo, calcLogRepo, serviceRepo, periodRepo, approvalRepo, roleRepo, conceptRepo, userRepo, engine, validationSvc) { }
}

public class CierreFacturacionService : CierreServiceBase<CierreFacturacion>, ICierreFacturacionService
{
    public CierreFacturacionService(
        ICierreFacturacionRepository repo, IClosureLineRepository lineRepo, ICalculationLogRepository calcLogRepo,
        IServiceRepository serviceRepo, IPeriodRepository periodRepo, IApprovalRepository approvalRepo,
        IRoleRepository roleRepo, IConceptRepository conceptRepo, IUserRepository userRepo,
        ICalculationEngine engine, IClosureValidationService validationSvc)
        : base(repo, lineRepo, calcLogRepo, serviceRepo, periodRepo, approvalRepo, roleRepo, conceptRepo, userRepo, engine, validationSvc) { }
}
