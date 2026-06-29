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
    Task<Client?> GetByNifAsync(string nif, CancellationToken ct);
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
    Task<Service?> GetByNombreAndClienteAsync(string nombre, int clientId, CancellationToken ct);
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

public interface IPlantillaClienteConceptoRepository
{
    Task<PlantillaClienteConcepto?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<PlantillaClienteConcepto>> ListByClientAsync(int clientId, CancellationToken ct);
    Task<PlantillaClienteConcepto?> GetByClientAndConceptAsync(int clientId, int conceptId, CancellationToken ct);
    Task<PagedResult<PlantillaClienteConcepto>> ListPaginatedByClientAsync(int clientId, int page, int pageSize, string? search, CancellationToken ct);
    Task AddAsync(PlantillaClienteConcepto plantilla, CancellationToken ct);
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
    Task<Department?> GetByNombreAsync(string nombre, CancellationToken ct);
    Task<bool> HasUsersOrServicesAsync(int id, CancellationToken ct);
    Task AddAsync(Department dep, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

// Par CECO→Servicio del join ServiceCostCenter (usado para imputar costes por CECO, p.ej. TravelPerk).
public record CecoServicio(string Codigo, int ServiceId);

public interface ICostCenterRepository
{
    Task<IReadOnlyList<CostCenter>> ListAsync(CancellationToken ct);
    // Mapa de imputación CECO→Servicio (join ServiceCostCenter): cada par (Codigo de CECO, ServiceId).
    Task<IReadOnlyList<CecoServicio>> GetCecoToServiceMapAsync(CancellationToken ct);
    // CECOs del maestro SIN Servicio de cliente asociado = estructura/interno de SIG (departamentos: Dirección,
    // Comercial, Finanzas, RRHH…). Una línea de TravelPerk cuyo prefijo casa con uno de estos es gasto interno
    // de SIG (como el 0423 de la suscripción), NO un CECO no-maestro.
    Task<IReadOnlyList<string>> GetInternalSigCecoCodesAsync(CancellationToken ct);
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

// FASE 2: TarifaConcepto repository interface
public interface ITarifaConceptoRepository
{
    Task<TarifaConcepto?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<TarifaConcepto>> ListByConceptAsync(int conceptId, CancellationToken ct);
    Task<IReadOnlyList<TarifaConcepto>> ListByConceptAndClientAsync(int conceptId, int clientId, CancellationToken ct);
    Task<PagedResult<TarifaConcepto>> ListPaginatedByConceptAsync(int conceptId, int page, int pageSize, string? search, CancellationToken ct);
    Task AddAsync(TarifaConcepto entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IPresupuestoServicioRepository
{
    Task<IReadOnlyList<PresupuestoServicio>> ListByServiceAsync(int serviceId, CancellationToken ct);
    Task<PresupuestoServicio?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(PresupuestoServicio entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

// FASE 2: PresupuestoConcepto repository interface
public interface IPresupuestoConceptoRepository
{
    Task<PresupuestoConcepto?> GetByIdAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<PresupuestoConcepto>> ListByConceptAsync(int conceptId, CancellationToken ct);
    Task<IReadOnlyList<PresupuestoConcepto>> ListByConceptAndClientAsync(int conceptId, int clientId, CancellationToken ct);
    Task<PagedResult<PresupuestoConcepto>> ListPaginatedByConceptAsync(int conceptId, int page, int pageSize, string? search, CancellationToken ct);
    Task AddAsync(PresupuestoConcepto entity, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IClienteIncidenciaRepository
{
    Task<IReadOnlyList<ClienteIncidencia>> ListByClientAsync(int clientId, CancellationToken ct);
    // Listado global paginado, restringido a los clientes accesibles por el usuario, con filtros.
    Task<PagedResult<ClienteIncidencia>> ListAllForUserAsync(int usuarioId, int page, int pageSize, string? search, int? clientId, string? tipo, EstadoIncidencia? estado, CancellationToken ct);
    Task<ClienteIncidencia?> GetByIdAsync(int id, CancellationToken ct);
    Task<ClienteIncidencia?> GetByIdWithDetailAsync(int id, CancellationToken ct);
    Task AddAsync(ClienteIncidencia entity, CancellationToken ct);
    Task AddHistorialAsync(IncidenciaHistorial entry, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IPartidaPresupuestoRepository
{
    Task<IReadOnlyList<PartidaPresupuesto>> ListByServiceAsync(int serviceId, CancellationToken ct);
    Task<PartidaPresupuesto?> GetByIdAsync(int id, CancellationToken ct);
    Task AddAsync(PartidaPresupuesto entity, CancellationToken ct);
    // Margen operativo real de la acción a partir de los cierres: (facturación − coste) / facturación.
    // Null si la acción aún no tiene facturación con la que comparar.
    Task<decimal?> GetMargenRealPctAsync(int serviceId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface ICategoriaFacturaRepository
{
    Task<IReadOnlyList<CategoriaFactura>> ListByClientAsync(int clientId, CancellationToken ct);
    Task<CategoriaFactura?> GetByIdWithConceptosAsync(int id, CancellationToken ct);
    // Conceptos de facturación (Tipo=Factura) disponibles para el cliente: globales (ServiceId null) o
    // vinculados a algún servicio del cliente (vía Concept.ServiceId o ServiceConcept).
    Task<IReadOnlyList<Concept>> ListConceptosFacturacionDelClienteAsync(int clientId, CancellationToken ct);
    // Devuelve, para los conceptos indicados, en qué categoría del cliente están ya asignados (si alguna).
    Task<IReadOnlyDictionary<int, CategoriaFactura>> GetAsignacionesAsync(int clientId, IReadOnlyCollection<int> conceptIds, CancellationToken ct);
    Task AddAsync(CategoriaFactura entity, CancellationToken ct);
    void Remove(CategoriaFactura entity);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IForecastRepository
{
    Task<IReadOnlyList<Forecast>> ListByServiceAndYearAsync(int serviceId, int anio, CancellationToken ct);
    Task<Forecast?> GetByServiceMonthAsync(int serviceId, int anio, int mes, CancellationToken ct);
    // Devuelve forecasts del año con Service+Client+Department incluidos para el resumen pivote.
    Task<IReadOnlyList<Forecast>> ListForResumenAsync(int anio, int? departmentId, int? clientId, int? serviceId, CancellationToken ct);
    Task AddAsync(Forecast entity, CancellationToken ct);
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

public interface INotificationRepository
{
    Task<IReadOnlyList<Notification>> ListForUserAsync(int usuarioId, bool soloNoLeidas, int take, CancellationToken ct);
    Task<int> CountUnreadAsync(int usuarioId, CancellationToken ct);
    Task MarkReadAsync(int id, int usuarioId, CancellationToken ct);
    Task MarkAllReadAsync(int usuarioId, CancellationToken ct);
    Task AddAsync(Notification notification, CancellationToken ct);
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
