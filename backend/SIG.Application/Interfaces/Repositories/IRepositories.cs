using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Common;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using Action = SIG.Domain.Entities.Action;

namespace SIG.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId, CancellationToken ct);
    Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct);
    Task<PagedResult<User>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task<IReadOnlyList<int>> ListProjectIdsForUserAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<string>> ListRoleNamesForUserAsync(int usuarioId, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task RevokeAllByUserAsync(int userId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClientRepository
{
    Task<Client?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<Client?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<Client>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task AddAsync(Client client, CancellationToken ct);
    Task<bool> HasProjectsAsync(int clientId, CancellationToken ct);
    Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<Project?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<Project>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct);
    Task AddAsync(Project project, CancellationToken ct);
    Task<bool> IsUserAssignedAsync(int projectId, int usuarioId, CancellationToken ct);
    Task<bool> HasActionsOrClosuresAsync(int projectId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IActionRepository
{
    Task<Action?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<Action?> GetByIdAsync(int id, CancellationToken ct);
    Task<PagedResult<Action>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, int? projectId, string? search, CancellationToken ct);
    Task AddAsync(Action action, CancellationToken ct);
    Task<bool> HasClosuresAsync(int actionId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IConceptRepository
{
    Task<Concept?> GetByIdAsync(int id, CancellationToken ct);
    Task<Concept?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<PagedResult<Concept>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, TipoConcepto? tipo, string? search, CancellationToken ct);
    Task AddAsync(Concept concept, CancellationToken ct);
    Task<bool> HasActionsAsync(int conceptId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IVariableRepository
{
    Task<Variable?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Variable>> ListAsync(CancellationToken ct);
    Task AddAsync(Variable variable, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IPeriodRepository
{
    Task<Period?> GetByIdAsync(int id, CancellationToken ct);
    Task<Period?> GetActivoAsync(CancellationToken ct);
    Task<IReadOnlyList<Period>> ListAsync(CancellationToken ct);
    Task<bool> ExistsByNombreAsync(string nombre, int? excludeId, CancellationToken ct);
    Task AddAsync(Period period, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClosureRepository
{
    Task<Closure?> GetByIdAsync(int id, CancellationToken ct);
    Task<Closure?> GetByIdWithLinesAsync(int id, CancellationToken ct);
    Task<Closure?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<Closure?> GetByProjectAndPeriodAsync(int projectId, int periodId, CancellationToken ct);
    Task<PagedResult<Closure>> ListPaginatedForUserAsync(int usuarioId, ApprovalFilterRequest filter, CancellationToken ct);
    Task<PagedResult<Closure>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct);
    Task AddAsync(Closure closure, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClosureLineRepository
{
    Task<ClosureLine?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task RemoveAllByClosureAsync(int closureId, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<ClosureLine> lines, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IApprovalRepository
{
    Task<Approval?> GetCurrentByClosureAsync(int closureId, CancellationToken ct);
    Task<IReadOnlyList<Approval>> ListByClosureAsync(int closureId, CancellationToken ct);
    Task<IReadOnlyList<ApprovalHistory>> ListHistoryByClosureAsync(int closureId, CancellationToken ct);
    Task AddAsync(Approval approval, CancellationToken ct);
    Task AddHistoryAsync(ApprovalHistory history, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICalculationLogRepository
{
    Task<CalculationLog?> GetByClosureLineAndUsuarioIdAsync(int closureLineId, int usuarioId, CancellationToken ct);
    Task AddAsync(CalculationLog log, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<CalculationLog> logs, CancellationToken ct);
    Task RemoveAllByClosureAsync(int closureId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IAuditLogRepository
{
    Task<PagedResult<AuditLog>> ListAsync(AuditLogFilterRequest filter, CancellationToken ct);
    Task AddAsync(AuditLog log, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IStagingRepository<TStaging> where TStaging : class, IStagingRow
{
    Task<bool> ExistsByHashAsync(string hash, CancellationToken ct);
    Task<IReadOnlyList<TStaging>> ListAsync(CancellationToken ct);
    Task AddRangeAsync(IEnumerable<TStaging> rows, CancellationToken ct);
    Task<int> CountByHashesAsync(IEnumerable<string> hashes, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IRoleRepository
{
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct);
    Task<Role?> GetByNombreAsync(string nombre, CancellationToken ct);
    Task<Role?> GetByIdAsync(int id, CancellationToken ct);
}

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct);
    Task<Department?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> HasUsersOrActionsAsync(int id, CancellationToken ct);
    Task AddAsync(Department dep, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICostCenterRepository
{
    Task<IReadOnlyList<CostCenter>> ListAsync(CancellationToken ct);
    Task<CostCenter?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct);
    Task<bool> HasProjectsAsync(int id, CancellationToken ct);
    Task AddAsync(CostCenter cc, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
