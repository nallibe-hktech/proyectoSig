using SIG.Application.DTOs;

namespace SIG.Application.Interfaces.Integrations;

public interface ICeleroClient
{
    Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    /// <summary>Lee la tabla <c>client</c> de Celero y devuelve todos los clientes activos.</summary>
    Task<IReadOnlyList<CeleroClienteDto>> GetClientesAsync(CancellationToken ct);
    /// <summary>Lee la tabla <c>service</c> de Celero y devuelve todos los servicios activos con FK al cliente.</summary>
    Task<IReadOnlyList<CeleroServicioDto>> GetServiciosAsync(CancellationToken ct);
    /// <summary>Lee la tabla <c>department</c> de Celero y devuelve todos los departamentos activos.</summary>
    Task<IReadOnlyList<CeleroDepartmentDto>> GetDepartmentsAsync(CancellationToken ct);
}

public interface IBizneoClient
{
    Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<BizneoAbsenceDto>> GetAbsencesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface IIntratimeClient
{
    Task<IReadOnlyList<IntratimeEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task<IReadOnlyList<IntratimeClockingRequestDto>> GetClockingRequestsAsync(int year, CancellationToken ct);
    Task<IReadOnlyList<IntratimeExpenseDto>> GetExpensesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task<IReadOnlyList<IntratimeProyectoDto>> GetProyectosAsync(CancellationToken ct);
}

public interface IPayHawkClient
{
    Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public interface ISgpvClient
{
    Task<IReadOnlyList<SgpvVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
    Task<IReadOnlyList<SgpvProductoDto>> GetProductosAsync(CancellationToken ct);
}

public interface IA3InnuvaClient
{
    Task<IReadOnlyList<A3InnuvaEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
}

public interface ITravelPerkClient
{
    Task<IReadOnlyList<TravelPerkViajeDto>> GetViajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

// Canal real de TravelPerk: descarga Excel compartida por SharePoint (la API se descartó por presupuesto),
// igual que la logística (Galán/Mediapost). Lee la hoja "report" a nivel línea.
public interface ITravelPerkExcelClient
{
    Task<IReadOnlyList<TravelPerkLineaDto>> GetLineasAsync(CancellationToken ct = default);
}

public interface IA3InnuvaNominasClient
{
    Task<IReadOnlyList<A3InnuvaNominasCompanyDto>> GetCompaniesAsync(
        int pageNumber = 1,
        int pageSize = 25,
        DateTime? lastUpdate = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<A3InnuvaNominasPayrollDto>> GetPayrollsAsync(
        string companyCode,
        int pageNumber = 1,
        int pageSize = 25,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    Task<IReadOnlyList<ConceptoDto>> GetConceptosAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    Task<string> WritePayrollAsync(
        string companyCode,
        string employeeCode,
        string periodCode,
        decimal percepciones,
        decimal descuentos,
        decimal neto,
        CancellationToken ct = default);

    // PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints
    Task<IReadOnlyList<SalaryDto>> GetSalaryAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<IRPFDto>> GetIRPFAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<RemunerationDto>> GetRemunerationAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<BankAccountDto>> GetBankAccountsAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgreementDto>> GetAgreementsAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default);

    // PHASE 1.5: Contract Data (agreement & timetable per employee)
    Task<IReadOnlyList<ContractAgreementDto>> GetContractAgreementAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default);

    Task<IReadOnlyList<ContractTimetableDto>> GetContractTimetableAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default);
}

public interface IGalanClient
{
    Task<IReadOnlyList<GalanEntradaDto>> GetEntradasAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<GalanSalidaDto>> GetSalidasAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<GalanStockDto>> GetStockAsync(CancellationToken ct);
}

public interface IMediapostClient
{
    Task<IReadOnlyList<MediapostPedidoDto>> GetPedidosAsync(DateTime desde, DateTime hasta, CancellationToken ct);
    Task<IReadOnlyList<MediapostRecepcionDto>> GetRecepcionesAsync(DateTime desde, DateTime hasta, CancellationToken ct);
}

// A3 INNUVA ERP - Wolters Kluwer OINV API (Facturación)
public interface IA3InnuvaERPClient
{
    // STUB: Endpoints a confirmar con Wolters Kluwer OINV API
    Task<IReadOnlyList<A3ERPFacturaDto>> GetFacturasAsync(
        string companyCode,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<A3ERPClienteDto>> GetClientesAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<A3ERPLineaFacturaDto>> GetLineasFacturaAsync(
        string facturaId,
        CancellationToken ct = default);
}
