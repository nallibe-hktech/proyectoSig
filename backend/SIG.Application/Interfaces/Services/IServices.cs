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

public interface IProjectService
{
    Task<PagedResult<ProjectListItemDto>> ListAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct);
    Task<ProjectDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<ProjectDetailDto> CreateAsync(ProjectCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ProjectDetailDto> UpdateAsync(int id, ProjectUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int usuarioId, CancellationToken ct);
}

public interface IActionService
{
    Task<PagedResult<ActionListItemDto>> ListAsync(int usuarioId, int page, int pageSize, int? projectId, string? search, CancellationToken ct);
    Task<ActionDetailDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<ActionDetailDto> CreateAsync(ActionCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ActionDetailDto> UpdateAsync(int id, ActionUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int usuarioId, CancellationToken ct);
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
}

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListAsync(CancellationToken ct);
    Task<DepartmentDto> CreateAsync(DepartmentCreateRequest req, CancellationToken ct);
    Task<DepartmentDto> UpdateAsync(int id, DepartmentUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface ICostCenterService
{
    Task<IReadOnlyList<CostCenterDto>> ListAsync(CancellationToken ct);
    Task<CostCenterDto> CreateAsync(CostCenterCreateRequest req, CancellationToken ct);
    Task<CostCenterDto> UpdateAsync(int id, CostCenterUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}

public interface IPeriodService
{
    Task<IReadOnlyList<PeriodDto>> ListAsync(CancellationToken ct);
    Task<PeriodDto> GetActivoAsync(CancellationToken ct);
    Task<PeriodDto> GetByIdAsync(int id, CancellationToken ct);
    Task<PeriodDto> CreateAsync(PeriodCreateRequest req, CancellationToken ct);
    Task<PeriodDto> UpdateAsync(int id, PeriodUpdateRequest req, CancellationToken ct);
    Task<PeriodDto> CerrarAsync(int id, CancellationToken ct);
    Task<PeriodDto> ReabrirAsync(int id, CancellationToken ct);
}

public interface ITarifaProyectoService
{
    Task<IReadOnlyList<TarifaProyectoDto>> ListByProjectAsync(int projectId, CancellationToken ct);
    Task<TarifaProyectoDto> GetByIdAsync(int id, int projectId, CancellationToken ct);
    Task<TarifaProyectoDto> CreateAsync(int projectId, TarifaProyectoCreateRequest req, CancellationToken ct);
    Task<TarifaProyectoDto> UpdateAsync(int id, int projectId, TarifaProyectoUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, int projectId, CancellationToken ct);
}

public interface IPresupuestoProyectoService
{
    Task<IReadOnlyList<PresupuestoProyectoDto>> ListByProjectAsync(int projectId, CancellationToken ct);
    Task<PresupuestoProyectoDto> GetByIdAsync(int id, int projectId, CancellationToken ct);
    Task<PresupuestoProyectoDto> CreateAsync(int projectId, PresupuestoProyectoCreateRequest req, CancellationToken ct);
    Task<PresupuestoProyectoDto> UpdateAsync(int id, int projectId, PresupuestoProyectoUpdateRequest req, CancellationToken ct);
    Task DeleteAsync(int id, int projectId, CancellationToken ct);
}

public interface IClosureService
{
    Task<PagedResult<ClosureListItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> GetByIdForUserAsync(int id, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> CreateAsync(ClosureCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> RecalcAsync(int closureId, ClosureRecalcRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> ApproveAsync(int closureId, ClosureApproveRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<ClosureDetailDto> RejectAsync(int closureId, ClosureRejectRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
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
    Task<IReadOnlyList<MiProyectoDto>> GetMisProyectosAsync(int? periodId, int usuarioId, CancellationToken ct);
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
