using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Application.Interfaces.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest req, string? ip, CancellationToken ct);
    Task<RefreshResponse> RefreshAsync(RefreshRequest req, string? ip, CancellationToken ct);
    Task LogoutAsync(int userId, string? refreshToken, CancellationToken ct);
    Task<UsuarioBriefDto> GetMeAsync(int userId, CancellationToken ct);
}

public interface ICurrentUserService
{
    int UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    string? Ip { get; }
    bool IsInRole(string role);
    bool IsInAnyRole(params string[] roles);
}

public interface IClientService
{
    Task<PagedResult<ClientListItemDto>> ListAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task<ClientDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<ClientDetailDto> CreateAsync(ClientCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ClientDetailDto> UpdateAsync(int id, ClientUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int usuarioId, CancellationToken ct);
}

public interface IServiceService
{
    Task<PagedResult<ServiceListItemDto>> ListAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct);
    Task<ServiceDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<ServiceDetailDto> CreateAsync(ServiceCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ServiceDetailDto> UpdateAsync(int id, ServiceUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int usuarioId, CancellationToken ct);
    // Ola 2 (#8): gestionar conceptos del catálogo por servicio (no crea conceptos nuevos).
    Task<ServiceDetailDto> AddConceptAsync(int serviceId, int conceptId, int usuarioId, CancellationToken ct);
    Task<ServiceDetailDto> RemoveConceptAsync(int serviceId, int conceptId, int usuarioId, CancellationToken ct);
}

public interface IConceptService
{
    Task<PagedResult<ConceptListItemDto>> ListAsync(int usuarioId, int page, int pageSize, SIG.Domain.Enums.TipoConcepto? tipo, string? search, CancellationToken ct);
    Task<ConceptDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<ConceptDetailDto> CreateAsync(ConceptCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ConceptDetailDto> UpdateAsync(int id, ConceptUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int usuarioId, CancellationToken ct);
    Task<ValidarFormulaResponse> ValidarFormulaAsync(string formulaJson, CancellationToken ct);
}

public interface IVariableService
{
    Task<IReadOnlyList<VariableDto>> ListAsync(CancellationToken ct);
    Task<PagedResult<VariableDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct);
    Task<VariableDto> GetByIdAsync(int id, CancellationToken ct);
    Task<VariableDto> CreateAsync(VariableCreateRequest req, CancellationToken ct);
    Task<VariableDto> UpdateAsync(int id, VariableUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface IUserService
{
    Task<PagedResult<UserListItemDto>> ListAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<UserDetailDto> GetByIdAsync(int id, CancellationToken ct);
    Task<UserDetailDto> CreateAsync(UserCreateRequest req, CancellationToken ct);
    Task<UserDetailDto> UpdateAsync(int id, UserUpdateRequest req, CancellationToken ct);
    Task ChangePasswordAsync(int id, UserPasswordChangeRequest req, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListAsync(CancellationToken ct);
    Task<PagedResult<RoleDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct);
}

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListAsync(CancellationToken ct);
    Task<PagedResult<DepartmentDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct);
    Task<DepartmentDto> CreateAsync(DepartmentCreateRequest req, CancellationToken ct);
    Task<DepartmentDto> UpdateAsync(int id, DepartmentUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface ICostCenterService
{
    Task<IReadOnlyList<CostCenterDto>> ListAsync(CancellationToken ct);
    Task<PagedResult<CostCenterDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct);
    Task<CostCenterDto> CreateAsync(CostCenterCreateRequest req, CancellationToken ct);
    Task<CostCenterDto> UpdateAsync(int id, CostCenterUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface IPeriodService
{
    Task<IReadOnlyList<PeriodDto>> ListAsync(CancellationToken ct);
    Task<PagedResult<PeriodDto>> ListPaginatedAsync(int page, int pageSize, CancellationToken ct);
    Task<PeriodDto> GetActivoAsync(CancellationToken ct);
    Task<PeriodDto> GetByIdAsync(int id, CancellationToken ct);
    Task<PeriodDto> CreateAsync(PeriodCreateRequest req, CancellationToken ct);
    Task<PeriodDto> UpdateAsync(int id, PeriodUpdateRequest req, CancellationToken ct);
    Task<PeriodDto> CerrarAsync(int id, CancellationToken ct);
    Task<PeriodDto> ReabrirAsync(int id, CancellationToken ct);
}

public interface ITarifaServicioService
{
    Task<IReadOnlyList<TarifaServicioDto>> ListByServiceAsync(int serviceId, CancellationToken ct);
    Task<TarifaServicioDto> GetByIdAsync(int id, int serviceId, CancellationToken ct);
    Task<TarifaServicioDto> CreateAsync(int serviceId, TarifaServicioCreateRequest req, CancellationToken ct);
    Task<TarifaServicioDto> UpdateAsync(int id, int serviceId, TarifaServicioUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, int serviceId, CancellationToken ct);
}

public interface IPresupuestoServicioService
{
    Task<IReadOnlyList<PresupuestoServicioDto>> ListByServiceAsync(int serviceId, CancellationToken ct);
    Task<PresupuestoServicioDto> GetByIdAsync(int id, int serviceId, CancellationToken ct);
    Task<PresupuestoServicioDto> CreateAsync(int serviceId, PresupuestoServicioCreateRequest req, CancellationToken ct);
    Task<PresupuestoServicioDto> UpdateAsync(int id, int serviceId, PresupuestoServicioUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, int serviceId, CancellationToken ct);
}

public interface IContratoService
{
    Task<IReadOnlyList<ContratoUnDiaDto>> ListContratosUnDiaAsync(CancellationToken ct);
    Task<ContratoUnDiaDto> MarcarIgnorarAsync(int id, ContratoIgnorarRequest req, CancellationToken ct);
}

public interface IClosureService
{
    Task<PagedResult<ClosureListItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> GetByIdForUserAsync(int id, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> CreateAsync(ClosureCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> RecalcAsync(int closureId, ClosureRecalcRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> ApproveAsync(int closureId, ClosureApproveRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> RejectAsync(int closureId, ClosureRejectRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> OverrideLineAsync(int closureId, int lineId, ClosureLineOverrideRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> AddIncentivoAsync(int closureId, ClosureLineIncentivoRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
}

public interface IApprovalService
{
    Task<PagedResult<ApprovalPanelItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct);
    Task<PagedResult<ApprovalPanelItemDto>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<ApprovalHistoryDto>> GetHistoryAsync(int closureId, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<ClosureDetailDto>> BatchApproveAsync(BatchApproveRequest req, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<ClosureDetailDto>> BatchRejectAsync(BatchRejectRequest req, int usuarioId, CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardKpisDto> GetKpisAsync(int? periodId, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<DashboardAvisoDto>> GetAvisosAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<MiServicioDto>> GetMisServiciosAsync(int? periodId, int usuarioId, CancellationToken ct);
}

public interface ICalculationService
{
    Task<CalculationDetailDto> GetByClosureLineForUserAsync(int closureLineId, int usuarioId, CancellationToken ct);
}

public interface IAuditService
{
    Task<PagedResult<AuditLogDto>> ListAsync(AuditLogFilterRequest filter, CancellationToken ct);
}

public interface ISyncService
{
    Task<SyncResultDto> SyncAsync(string sistema, CancellationToken ct);
}

public interface IDataProcessorService
{
    Task<ProcessingResultDto> ProcessAllPendingAsync(CancellationToken ct);
    Task<IReadOnlyList<DiscrepanciaIntratimeDto>> ValidarDiscrepanciasIntratimeAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IExportService
{
    Task<(byte[] Content, string FileName)> ExportA3InnuvaAsync(int closureId, int usuarioId, CancellationToken ct);
    Task<(byte[] Content, string FileName)> ExportA3ErpAsync(int closureId, int usuarioId, CancellationToken ct);
}

public interface ICeleroVisitaService
{
    Task<PagedCeleroVisitasDto> ListAsync(int page, int pageSize, string? searchNif = null, string? searchService = null, CancellationToken ct = default);
    Task<CeleroVisitaDetailDto> GetByIdAsync(int id, CancellationToken ct);
    Task<CeleroVisitaDetailDto> UpdateAsync(int id, CeleroVisitaUpdateRequest req, int usuarioId, CancellationToken ct);
}

public interface ISeedService
{
    Task RunIfEmptyAsync(CancellationToken ct);
    Task RegenerateAsync(CancellationToken ct);
}

public interface IClosureValidationService
{
    /// <summary>
    /// Valida el cierre sobre datos sincronizados y crea alertas en BD.
    /// Se ejecuta automáticamente al crear o recalcular un cierre.
    /// </summary>
    Task<IReadOnlyList<ClosureAlertaDto>> ValidarYPersistirAsync(int closureId, int serviceId, int periodId, CancellationToken ct);

    /// <summary>
    /// Obtiene todas las alertas asociadas a un cierre.
    /// </summary>
    Task<IReadOnlyList<ClosureAlertaDto>> GetAlertasAsync(int closureId, CancellationToken ct);

    /// <summary>
    /// Confirma una advertencia, permitiendo el cierre a pesar del riesgo.
    /// Solo usuarios con rol apropiado pueden confirmar.
    /// </summary>
    Task ConfirmarAdvertenciaAsync(int alertaId, int usuarioId, CancellationToken ct);
}
