using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Common;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;

namespace SIG.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId, CancellationToken ct);
    Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct);
    Task<IReadOnlyList<User>> ListAsync(CancellationToken ct);
    Task<PagedResult<User>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task<IReadOnlyList<int>> ListServiceIdsForUserAsync(int usuarioId, CancellationToken ct);
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
    Task<bool> HasServicesAsync(int clientId, CancellationToken ct);
    Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IServiceRepository
{
    Task<Service?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<Service?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<Service>> ListAsync(CancellationToken ct);
    Task<PagedResult<Service>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct);
    Task AddAsync(Service service, CancellationToken ct);
    Task<bool> IsUserAssignedAsync(int serviceId, int usuarioId, CancellationToken ct);
    Task<bool> HasCierresAsync(int serviceId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IConceptRepository
{
    Task<Concept?> GetByIdAsync(int id, CancellationToken ct);
    Task<Concept?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<PagedResult<Concept>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, TipoConcepto? tipo, string? search, CancellationToken ct);
    Task AddAsync(Concept concept, CancellationToken ct);
    Task<bool> HasServicesAsync(int conceptId, CancellationToken ct);
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

// Ola 3b (#10): un repositorio genérico por raíz de cierre (CierreCostes / CierreFacturacion).
public interface ICierreRepository<TCierre> where TCierre : class, ICierre
{
    TipoCierre Tipo { get; }
    Task<TCierre?> GetByIdAsync(int id, CancellationToken ct);
    Task<TCierre?> GetByIdWithLinesAsync(int id, CancellationToken ct);
    Task<TCierre?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<TCierre?> GetByServiceAndPeriodAsync(int serviceId, int periodId, CancellationToken ct);
    Task<PagedResult<TCierre>> ListPaginatedForUserAsync(int usuarioId, ApprovalFilterRequest filter, CancellationToken ct);
    Task<PagedResult<TCierre>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<TCierre>> ListByPeriodForUserAsync(int usuarioId, int periodId, CancellationToken ct);
    Task AddAsync(TCierre cierre, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICierreCostesRepository : ICierreRepository<CierreCostes> { }
public interface ICierreFacturacionRepository : ICierreRepository<CierreFacturacion> { }

public interface IClosureLineRepository
{
    Task<ClosureLine?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<ClosureLine?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ClosureLine>> ListByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
    Task RemoveAllByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
    Task AddAsync(ClosureLine line, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<ClosureLine> lines, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IApprovalRepository
{
    Task<Approval?> GetCurrentByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
    Task<IReadOnlyList<Approval>> ListByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
    Task<IReadOnlyList<ApprovalHistory>> ListHistoryByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
    Task AddAsync(Approval approval, CancellationToken ct);
    Task AddHistoryAsync(ApprovalHistory history, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICalculationLogRepository
{
    Task<CalculationLog?> GetByClosureLineAndUsuarioIdAsync(int closureLineId, int usuarioId, CancellationToken ct);
    Task AddAsync(CalculationLog log, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<CalculationLog> logs, CancellationToken ct);
    Task RemoveAllByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
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
    Task<bool> HasUsersOrServicesAsync(int id, CancellationToken ct);
    Task AddAsync(Department dep, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICostCenterRepository
{
    Task<IReadOnlyList<CostCenter>> ListAsync(CancellationToken ct);
    Task<CostCenter?> GetByIdAsync(int id, CancellationToken ct);
    Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct);
    Task<bool> HasServicesAsync(int id, CancellationToken ct);
    Task AddAsync(CostCenter cc, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ITarifaServicioRepository
{
    Task<IReadOnlyList<TarifaServicio>> ListByServiceAsync(int serviceId, CancellationToken ct);
    Task<TarifaServicio?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(TarifaServicio entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IPresupuestoServicioRepository
{
    Task<IReadOnlyList<PresupuestoServicio>> ListByServiceAsync(int serviceId, CancellationToken ct);
    Task<PresupuestoServicio?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(PresupuestoServicio entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClosureAlertaRepository
{
    Task<IReadOnlyList<ClosureAlerta>> GetByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
    Task<ClosureAlerta?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(ClosureAlerta alerta, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<ClosureAlerta> alertas, CancellationToken ct);
    Task UpdateAsync(ClosureAlerta alerta, CancellationToken ct);
    Task DeleteByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IStagingA3InnuvaContratoRepository
{
    Task<IReadOnlyList<StagingA3InnuvaContrato>> GetByNifAsync(string nif, CancellationToken ct);
    Task<IReadOnlyList<StagingA3InnuvaContrato>> GetAllAsync(CancellationToken ct);
    Task<IReadOnlyList<StagingA3InnuvaContrato>> GetActivosEnPeriodoAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<StagingA3InnuvaContrato>> ListContratosUnDiaAsync(CancellationToken ct);
    Task<StagingA3InnuvaContrato?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(StagingA3InnuvaContrato contrato, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<StagingA3InnuvaContrato> contratos, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
