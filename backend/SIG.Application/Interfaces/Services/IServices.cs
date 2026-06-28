using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Domain.Enums;

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

// Incidencias del cliente (PPT slide 6 + prototipo). Anidado bajo el cliente (alta/edición) y con un
// listado global de 1er nivel; el histórico se registra en cada cambio de estado.
public interface IClienteIncidenciaService
{
    Task<IReadOnlyList<ClienteIncidenciaDto>> ListByClientAsync(int clientId, int usuarioId, CancellationToken ct);
    Task<PagedResult<IncidenciaListItemDto>> ListAllAsync(int usuarioId, int page, int pageSize, string? search, int? clientId, string? tipo, EstadoIncidencia? estado, CancellationToken ct);
    Task<ClienteIncidenciaDto> GetByIdAsync(int id, int clientId, int usuarioId, CancellationToken ct);
    Task<ClienteIncidenciaDto> CreateAsync(int clientId, ClienteIncidenciaCreateRequest req, int usuarioId, CancellationToken ct);
    Task<ClienteIncidenciaDto> UpdateAsync(int id, int clientId, ClienteIncidenciaUpdateRequest req, int usuarioId, CancellationToken ct);
    Task<ClienteIncidenciaDto> CambiarEstadoAsync(int id, int clientId, IncidenciaCambioEstadoRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int clientId, int usuarioId, CancellationToken ct);
}

// Configuración de Presupuesto (prototipo 24/28): partidas manuales por acción/servicio + márgenes.
// Anidado bajo el servicio (escritura sólo Administrator). El "consumido" es manual por ahora.
public interface IConfigPresupuestoService
{
    Task<ConfigPresupuestoDto> GetConfigAsync(int serviceId, int usuarioId, CancellationToken ct);
    Task<PartidaPresupuestoDto> CreatePartidaAsync(int serviceId, PartidaPresupuestoCreateRequest req, int usuarioId, CancellationToken ct);
    Task<PartidaPresupuestoDto> UpdatePartidaAsync(int id, int serviceId, PartidaPresupuestoUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeletePartidaAsync(int id, int serviceId, int usuarioId, CancellationToken ct);
    Task<ConfigPresupuestoDto> SetMargenObjetivoAsync(int serviceId, MargenObjetivoRequest req, int usuarioId, CancellationToken ct);
}

// Configuración de Factura (prototipo 25/28): CRUD de categorías por cliente + panel de conceptos
// disponibles. Anidado bajo el cliente (escritura sólo Administrator, como Incidencias).
public interface ICategoriaFacturaService
{
    Task<IReadOnlyList<CategoriaFacturaDto>> ListByClientAsync(int clientId, int usuarioId, CancellationToken ct);
    Task<ConfigFacturaResumenDto> GetResumenAsync(int clientId, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<ConceptoDisponibleDto>> ListConceptosDisponiblesAsync(int clientId, int usuarioId, CancellationToken ct);
    Task<CategoriaFacturaDto> CreateAsync(int clientId, CategoriaFacturaCreateRequest req, int usuarioId, CancellationToken ct);
    Task<CategoriaFacturaDto> UpdateAsync(int id, int clientId, CategoriaFacturaUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int clientId, int usuarioId, CancellationToken ct);
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

// FASE 1: Client-specific concept customizations (plantillas personalizadas por cliente)
public interface IPlantillaClienteConceptoService
{
    Task<IReadOnlyList<PlantillaClienteConceptoDto>> ListByClientAsync(int clientId, int usuarioId, CancellationToken ct);
    Task<PagedResult<PlantillaClienteConceptoDto>> ListPaginatedByClientAsync(int clientId, int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task<PlantillaClienteConceptoDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<PlantillaClienteConceptoDto> CreateAsync(int clientId, PlantillaClienteConceptoCreateRequest req, int usuarioId, CancellationToken ct);
    Task<PlantillaClienteConceptoDto> UpdateAsync(int id, int clientId, PlantillaClienteConceptoUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int clientId, int usuarioId, CancellationToken ct);
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

// FASE 2: TarifaConcepto service interface (tariffs by concept, finer granularity)
public interface ITarifaConceptoService
{
    Task<IReadOnlyList<TarifaConceptoDto>> ListByConceptAsync(int conceptId, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<TarifaConceptoDto>> ListByConceptAndClientAsync(int conceptId, int clientId, int usuarioId, CancellationToken ct);
    Task<PagedResult<TarifaConceptoDto>> ListPaginatedByConceptAsync(int conceptId, int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task<TarifaConceptoDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<TarifaConceptoDto> CreateAsync(int conceptId, TarifaConceptoCreateRequest req, int usuarioId, CancellationToken ct);
    Task<TarifaConceptoDto> UpdateAsync(int id, int conceptId, TarifaConceptoUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int conceptId, int usuarioId, CancellationToken ct);
}

// FASE 2: PresupuestoConcepto service interface (budget by concept, finer granularity)
public interface IPresupuestoConceptoService
{
    Task<IReadOnlyList<PresupuestoConceptoDto>> ListByConceptAsync(int conceptId, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<PresupuestoConceptoDto>> ListByConceptAndClientAsync(int conceptId, int clientId, int usuarioId, CancellationToken ct);
    Task<PagedResult<PresupuestoConceptoDto>> ListPaginatedByConceptAsync(int conceptId, int usuarioId, int page, int pageSize, string? search, CancellationToken ct);
    Task<PresupuestoConceptoDto> GetByIdAsync(int id, int usuarioId, CancellationToken ct);
    Task<PresupuestoConceptoDto> CreateAsync(int conceptId, PresupuestoConceptoCreateRequest req, int usuarioId, CancellationToken ct);
    Task<PresupuestoConceptoDto> UpdateAsync(int id, int conceptId, PresupuestoConceptoUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int conceptId, int usuarioId, CancellationToken ct);
}

// Forecast (PPT slide 36): previsión mensual por servicio + resumen pivote con filtros.
public interface IForecastService
{
    Task<IReadOnlyList<ForecastDto>> ListByServiceAsync(int serviceId, int anio, CancellationToken ct);
    Task<ForecastDto> UpsertAsync(int serviceId, ForecastUpsertRequest req, CancellationToken ct);
    Task<ForecastResumenDto> GetResumenAsync(int anio, int? departmentId, int? clientId, int? serviceId, CancellationToken ct);
}

public interface IContratoService
{
    Task<IReadOnlyList<ContratoUnDiaDto>> ListContratosUnDiaAsync(CancellationToken ct);
    Task<ContratoUnDiaDto> MarcarIgnorarAsync(int id, ContratoIgnorarRequest req, CancellationToken ct);
}

// Ola 3b (#10): un servicio por raíz de cierre. La lógica común vive en la base genérica
// (CierreServiceBase<TCierre>); estos contratos sólo fijan el tipo concreto.
public interface ICierreService
{
    TipoCierre Tipo { get; }
    Task<PagedResult<CierreListItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> GetByIdForUserAsync(int id, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> CreateAsync(CierreCreateRequest req, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> RecalcAsync(int cierreId, CierreRecalcRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> ApproveAsync(int cierreId, CierreApproveRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> RejectAsync(int cierreId, CierreRejectRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> OverrideLineAsync(int cierreId, int lineId, CierreLineOverrideRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> AddIncentivoAsync(int cierreId, CierreLineIncentivoRequest req, uint rowVersion, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<ClosureAlertaDto>> GetAlertasAsync(int cierreId, int usuarioId, CancellationToken ct);
    Task<CierreDetailDto> ConfirmarAlertaAsync(int cierreId, int alertaId, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<CierreHistoryDto>> GetHistoryAsync(int cierreId, int usuarioId, CancellationToken ct);
}

public interface ICierreCostesService : ICierreService { }
public interface ICierreFacturacionService : ICierreService { }

// Panel/pendientes agregando AMBOS tipos de cierre, indicando el tipo de cada uno.
public interface IApprovalService
{
    Task<PagedResult<CierrePanelItemDto>> ListAsync(ApprovalFilterRequest filter, int usuarioId, CancellationToken ct);
    Task<PagedResult<CierrePanelItemDto>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct);
}

public interface IDashboardService
{
    Task<DashboardKpisDto> GetKpisAsync(int? periodId, int usuarioId, CancellationToken ct, int? serviceId = null);
    Task<IReadOnlyList<DashboardAvisoDto>> GetAvisosAsync(int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<MiServicioDto>> GetMisServiciosAsync(int? periodId, int usuarioId, CancellationToken ct, int? serviceId = null);
}

// Informes nativos (PPT slide 23). Reutiliza el emparejado coste+facturación y el Forecast.
public interface IReportsService
{
    Task<ReporteResultadoDto> GetResultadoAsync(int anio, int? departmentId, int? clientId, int? serviceId, int usuarioId, CancellationToken ct);
    Task<PrevisionRealDto> GetPrevisionVsRealAsync(int anio, int? departmentId, int? clientId, int? serviceId, int usuarioId, CancellationToken ct);
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

public interface ITravelPerkService
{
    Task<PagedResult<TravelPerkLineaListDto>> ListAsync(int page, int pageSize, string? search = null, bool soloNoMaestro = false, CancellationToken ct = default);
    Task<TravelPerkKpisDto> GetKpisAsync(CancellationToken ct = default);
}

public interface ISeedService
{
    Task RunIfEmptyAsync(CancellationToken ct);
    Task RegenerateAsync(CancellationToken ct);
}

public interface IClosureValidationService
{
    /// <summary>
    /// Valida el cierre (de un tipo dado) sobre datos sincronizados y crea alertas en BD.
    /// Se ejecuta automáticamente al crear o recalcular un cierre.
    /// Las validaciones de coste/contratos aplican a CierreCostes; las de facturación a CierreFacturacion.
    /// </summary>
    Task<IReadOnlyList<ClosureAlertaDto>> ValidarYPersistirAsync(SIG.Domain.Enums.TipoCierre tipo, int cierreId, int serviceId, int periodId, CancellationToken ct);

    /// <summary>
    /// Obtiene todas las alertas asociadas a un cierre.
    /// </summary>
    Task<IReadOnlyList<ClosureAlertaDto>> GetAlertasAsync(SIG.Domain.Enums.TipoCierre tipo, int cierreId, CancellationToken ct);

    /// <summary>
    /// Confirma una advertencia, permitiendo el cierre a pesar del riesgo.
    /// Solo usuarios con rol apropiado pueden confirmar.
    /// </summary>
    Task ConfirmarAdvertenciaAsync(int alertaId, int usuarioId, CancellationToken ct);
}

public interface IPaymentModelService
{
    Task<IReadOnlyList<PaymentModelDto>> ListByClientAsync(int clientId, CancellationToken ct);
    Task<PaymentModelDto> GetByIdAsync(int id, CancellationToken ct);
    Task<PaymentModelDto> CreateAsync(PaymentModelCreateRequest req, int usuarioId, CancellationToken ct);
    Task<PaymentModelDto> UpdateAsync(int id, PaymentModelUpdateRequest req, int usuarioId, CancellationToken ct);
    Task DeleteAsync(int id, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<ConceptValidationRuleDto>> GetValidationRulesAsync(int paymentModelId, CancellationToken ct);
    Task<ConceptValidationRuleDto> UpsertValidationRuleAsync(ConceptValidationRuleUpsertRequest req, int usuarioId, CancellationToken ct);
    Task<IReadOnlyList<PaymentRatesConfigurationDto>> GetRatesAsync(int paymentModelId, CancellationToken ct);
    Task<PaymentRatesConfigurationDto> UpsertRateAsync(PaymentRatesConfigurationUpsertRequest req, int usuarioId, CancellationToken ct);
    Task<bool> IsConceptApplicableAsync(int conceptId, string paymentModelType, CancellationToken ct);
}
