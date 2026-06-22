using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Domain.Entities.Staging;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public interface IA3InnuvaNominasService
{
    Task SyncCompaniesAsync(CancellationToken ct = default);
    Task SyncPayrollsAsync(string companyCode, CancellationToken ct = default);
    Task<PagedResult<A3InnuvaNominasCompanyDto>> GetCompaniesAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<PagedResult<A3InnuvaNominasPayrollDto>> GetPayrollsAsync(int page, int pageSize, string? search, CancellationToken ct);

    // Test methods (write to test tables only)
    Task SyncCompaniesTestAsync(CancellationToken ct = default);
    Task SyncPayrollsTestAsync(string companyCode, CancellationToken ct = default);
    Task<PagedResult<A3InnuvaNominasCompanyDto>> GetCompaniesTestAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<PagedResult<A3InnuvaNominasPayrollDto>> GetPayrollsTestAsync(int page, int pageSize, string? search, CancellationToken ct);
}

public class A3InnuvaNominasService : IA3InnuvaNominasService
{
    private readonly AppDbContext _db;
    private readonly IA3InnuvaNominasClient _client;
    private readonly ILogger<A3InnuvaNominasService> _logger;

    public A3InnuvaNominasService(
        AppDbContext db,
        IA3InnuvaNominasClient client,
        ILogger<A3InnuvaNominasService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SyncCompaniesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] Iniciando sincronización de empresas...");
            _logger.LogInformation("[A3InnuvaNominas] Cliente inyectado: {ClientType}", _client?.GetType().Name ?? "null");

            // Obtener empresas de la API (todas las páginas)
            var allCompanies = new List<A3InnuvaNominasCompanyDto>();
            int pageNumber = 1;
            const int pageSize = 100;
            int totalFetched = 0;

            while (true)
            {
                _logger.LogInformation($"[A3InnuvaNominas] Obteniendo página {pageNumber}...");
                var companies = await _client.GetCompaniesAsync(pageNumber, pageSize, null, ct);
                _logger.LogInformation($"[A3InnuvaNominas] Página {pageNumber}: recibidas {companies.Count} empresas");

                if (companies.Count == 0)
                {
                    _logger.LogInformation($"[A3InnuvaNominas] Página {pageNumber} vacía, terminando paginación");
                    break;
                }

                allCompanies.AddRange(companies);
                totalFetched += companies.Count;
                _logger.LogInformation($"[A3InnuvaNominas] Página {pageNumber}: {companies.Count} empresas obtenidas (total: {totalFetched})");

                if (companies.Count < pageSize) break; // Última página
                pageNumber++;
            }

            if (allCompanies.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No se obtuvieron empresas de la API");
                return;
            }

            // Deduplicar por ID externo (usando hash)
            var uniqueCompanies = allCompanies
                .DistinctBy(c => c.Id)
                .ToList();

            _logger.LogInformation($"[A3InnuvaNominas] {uniqueCompanies.Count} empresas únicas después de deduplicación");

            // Obtener empresas existentes para actualización
            var existingIds = await _db.StagingA3InnuvaCompanies
                .Where(c => c.DeletedAt == null)
                .Select(c => c.IdExterno)
                .ToListAsync(ct);

            // Insertar o actualizar
            foreach (var companyDto in uniqueCompanies)
            {
                var existing = await _db.StagingA3InnuvaCompanies
                    .FirstOrDefaultAsync(c => c.IdExterno == companyDto.Id && c.DeletedAt == null, ct);

                if (existing != null)
                {
                    // Actualizar
                    existing.Codigo = companyDto.Code;
                    existing.Nombre = companyDto.Name;
                    existing.Nif = companyDto.TaxId;
                    existing.Direccion = companyDto.Address;
                    existing.Ciudad = companyDto.City;
                    existing.Pais = companyDto.Country;
                    existing.EmailContacto = companyDto.ContactEmail;
                    existing.TelefonoContacto = companyDto.ContactPhone;
                    existing.FechaUltimaActualizacion = DateTime.UtcNow;
                    _db.StagingA3InnuvaCompanies.Update(existing);
                }
                else
                {
                    // Insertar
                    var newCompany = new StagingA3InnuvaCompany
                    {
                        IdExterno = companyDto.Id,
                        Codigo = companyDto.Code,
                        Nombre = companyDto.Name,
                        Nif = companyDto.TaxId,
                        Direccion = companyDto.Address,
                        Ciudad = companyDto.City,
                        Pais = companyDto.Country,
                        EmailContacto = companyDto.ContactEmail,
                        TelefonoContacto = companyDto.ContactPhone,
                        FechaUltimaActualizacion = DateTime.UtcNow
                    };
                    _db.StagingA3InnuvaCompanies.Add(newCompany);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Sincronización de empresas completada: {uniqueCompanies.Count} empresas procesadas");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando empresas");
            throw;
        }
    }

    public async Task SyncPayrollsAsync(string companyCode, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(companyCode))
                throw new ArgumentNullException(nameof(companyCode));

            _logger.LogInformation($"[A3InnuvaNominas] Iniciando sincronización de nóminas para empresa {companyCode}...");

            // Obtener nóminas de la API (todas las páginas)
            var allPayrolls = new List<A3InnuvaNominasPayrollDto>();
            int pageNumber = 1;
            const int pageSize = 100;
            int totalFetched = 0;

            while (true)
            {
                var payrolls = await _client.GetPayrollsAsync(companyCode, pageNumber, pageSize, null, null, ct);
                if (payrolls.Count == 0) break;

                allPayrolls.AddRange(payrolls);
                totalFetched += payrolls.Count;
                _logger.LogInformation($"[A3InnuvaNominas] Página {pageNumber}: {payrolls.Count} nóminas obtenidas (total: {totalFetched})");

                if (payrolls.Count < pageSize) break; // Última página
                pageNumber++;
            }

            if (allPayrolls.Count == 0)
            {
                _logger.LogWarning($"[A3InnuvaNominas] No se obtuvieron nóminas de la API para {companyCode}");
                return;
            }

            // Deduplicar por ID externo
            var uniquePayrolls = allPayrolls
                .DistinctBy(p => p.Id)
                .ToList();

            _logger.LogInformation($"[A3InnuvaNominas] {uniquePayrolls.Count} nóminas únicas después de deduplicación");

            // Insertar o actualizar
            foreach (var payrollDto in uniquePayrolls)
            {
                var existing = await _db.StagingA3InnuvaPayrolls
                    .FirstOrDefaultAsync(p => p.IdExterno == payrollDto.Id && p.DeletedAt == null, ct);

                if (existing != null)
                {
                    // Actualizar
                    existing.IdEmpleado = payrollDto.EmployeeId;
                    existing.NombreEmpleado = payrollDto.EmployeeName;
                    existing.CodigoPeriodo = payrollDto.PeriodCode;
                    existing.SalarioBase = payrollDto.BaseSalary;
                    existing.Deducciones = payrollDto.Deductions;
                    existing.SalarioNeto = payrollDto.NetSalary;
                    existing.FechaProcesamiento = payrollDto.ProcessDate;
                    _db.StagingA3InnuvaPayrolls.Update(existing);
                }
                else
                {
                    // Insertar
                    var newPayroll = new StagingA3InnuvaPayroll
                    {
                        IdExterno = payrollDto.Id,
                        IdEmpleado = payrollDto.EmployeeId,
                        NombreEmpleado = payrollDto.EmployeeName,
                        CodigoPeriodo = payrollDto.PeriodCode,
                        SalarioBase = payrollDto.BaseSalary,
                        Deducciones = payrollDto.Deductions,
                        SalarioNeto = payrollDto.NetSalary,
                        FechaProcesamiento = payrollDto.ProcessDate
                    };
                    _db.StagingA3InnuvaPayrolls.Add(newPayroll);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Sincronización de nóminas completada para {companyCode}: {uniquePayrolls.Count} nóminas procesadas");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando nóminas");
            throw;
        }
    }

    public async Task<PagedResult<A3InnuvaNominasCompanyDto>> GetCompaniesAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaCompanies.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Codigo, s) ||
                EF.Functions.ILike(c.Nombre, s) ||
                EF.Functions.ILike(c.Nif, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Codigo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new A3InnuvaNominasCompanyDto(
                c.IdExterno,
                c.Codigo,
                c.Nombre,
                c.Nif,
                c.Direccion,
                c.Ciudad,
                c.Pais,
                c.EmailContacto,
                c.TelefonoContacto))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaNominasCompanyDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<A3InnuvaNominasPayrollDto>> GetPayrollsAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaPayrolls.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.IdEmpleado, s) ||
                EF.Functions.ILike(p.NombreEmpleado, s) ||
                EF.Functions.ILike(p.CodigoPeriodo, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.FechaProcesamiento)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new A3InnuvaNominasPayrollDto(
                p.IdExterno,
                p.IdEmpleado,
                p.NombreEmpleado,
                p.CodigoPeriodo,
                p.SalarioBase,
                p.Deducciones,
                p.SalarioNeto,
                p.FechaProcesamiento))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaNominasPayrollDto>(items, total, page, pageSize);
    }

    // Test methods - read from API but write to test tables
    public async Task SyncCompaniesTestAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas-TEST] Iniciando sincronización de empresas (TEST TABLE)...");

            // Obtener empresas de la API (todas las páginas)
            var allCompanies = new List<A3InnuvaNominasCompanyDto>();
            int pageNumber = 1;
            const int pageSize = 100;
            int totalFetched = 0;

            while (true)
            {
                var companies = await _client.GetCompaniesAsync(pageNumber, pageSize, null, ct);
                if (companies.Count == 0) break;

                allCompanies.AddRange(companies);
                totalFetched += companies.Count;
                _logger.LogInformation($"[A3InnuvaNominas-TEST] Página {pageNumber}: {companies.Count} empresas obtenidas (total: {totalFetched})");

                if (companies.Count < pageSize) break; // Última página
                pageNumber++;
            }

            if (allCompanies.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas-TEST] No se obtuvieron empresas de la API");
                return;
            }

            // Deduplicar por ID externo
            var uniqueCompanies = allCompanies
                .DistinctBy(c => c.Id)
                .ToList();

            _logger.LogInformation($"[A3InnuvaNominas-TEST] {uniqueCompanies.Count} empresas únicas después de deduplicación");

            // Obtener empresas existentes para actualización
            var existingIds = await _db.StagingA3InnuvaCompaniesTest
                .Where(c => c.DeletedAt == null)
                .Select(c => c.IdExterno)
                .ToListAsync(ct);

            // Insertar o actualizar
            foreach (var companyDto in uniqueCompanies)
            {
                var existing = await _db.StagingA3InnuvaCompaniesTest
                    .FirstOrDefaultAsync(c => c.IdExterno == companyDto.Id && c.DeletedAt == null, ct);

                if (existing != null)
                {
                    // Actualizar
                    existing.Codigo = companyDto.Code;
                    existing.Nombre = companyDto.Name;
                    existing.Nif = companyDto.TaxId;
                    existing.Direccion = companyDto.Address;
                    existing.Ciudad = companyDto.City;
                    existing.Pais = companyDto.Country;
                    existing.EmailContacto = companyDto.ContactEmail;
                    existing.TelefonoContacto = companyDto.ContactPhone;
                    existing.FechaUltimaActualizacion = DateTime.UtcNow;
                    _db.StagingA3InnuvaCompaniesTest.Update(existing);
                }
                else
                {
                    // Insertar
                    var newCompany = new StagingA3InnuvaCompanyTest
                    {
                        IdExterno = companyDto.Id,
                        Codigo = companyDto.Code,
                        Nombre = companyDto.Name,
                        Nif = companyDto.TaxId,
                        Direccion = companyDto.Address,
                        Ciudad = companyDto.City,
                        Pais = companyDto.Country,
                        EmailContacto = companyDto.ContactEmail,
                        TelefonoContacto = companyDto.ContactPhone,
                        FechaUltimaActualizacion = DateTime.UtcNow
                    };
                    _db.StagingA3InnuvaCompaniesTest.Add(newCompany);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas-TEST] ✅ Sincronización de empresas completada: {uniqueCompanies.Count} empresas procesadas en tabla TEST");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-TEST] Error sincronizando empresas");
            throw;
        }
    }

    public async Task SyncPayrollsTestAsync(string companyCode, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(companyCode))
                throw new ArgumentNullException(nameof(companyCode));

            _logger.LogInformation($"[A3InnuvaNominas-TEST] Iniciando sincronización de nóminas para empresa {companyCode} (TEST TABLE)...");

            // Obtener nóminas de la API (todas las páginas)
            var allPayrolls = new List<A3InnuvaNominasPayrollDto>();
            int pageNumber = 1;
            const int pageSize = 100;
            int totalFetched = 0;

            while (true)
            {
                var payrolls = await _client.GetPayrollsAsync(companyCode, pageNumber, pageSize, null, null, ct);
                if (payrolls.Count == 0) break;

                allPayrolls.AddRange(payrolls);
                totalFetched += payrolls.Count;
                _logger.LogInformation($"[A3InnuvaNominas-TEST] Página {pageNumber}: {payrolls.Count} nóminas obtenidas (total: {totalFetched})");

                if (payrolls.Count < pageSize) break; // Última página
                pageNumber++;
            }

            if (allPayrolls.Count == 0)
            {
                _logger.LogWarning($"[A3InnuvaNominas-TEST] No se obtuvieron nóminas de la API para {companyCode}");
                return;
            }

            // Deduplicar por ID externo
            var uniquePayrolls = allPayrolls
                .DistinctBy(p => p.Id)
                .ToList();

            _logger.LogInformation($"[A3InnuvaNominas-TEST] {uniquePayrolls.Count} nóminas únicas después de deduplicación");

            // Insertar o actualizar
            foreach (var payrollDto in uniquePayrolls)
            {
                var existing = await _db.StagingA3InnuvaPayrollsTest
                    .FirstOrDefaultAsync(p => p.IdExterno == payrollDto.Id && p.DeletedAt == null, ct);

                if (existing != null)
                {
                    // Actualizar
                    existing.IdEmpleado = payrollDto.EmployeeId;
                    existing.NombreEmpleado = payrollDto.EmployeeName;
                    existing.CodigoPeriodo = payrollDto.PeriodCode;
                    existing.SalarioBase = payrollDto.BaseSalary;
                    existing.Deducciones = payrollDto.Deductions;
                    existing.SalarioNeto = payrollDto.NetSalary;
                    existing.FechaProcesamiento = payrollDto.ProcessDate;
                    _db.StagingA3InnuvaPayrollsTest.Update(existing);
                }
                else
                {
                    // Insertar
                    var newPayroll = new StagingA3InnuvaPayrollTest
                    {
                        IdExterno = payrollDto.Id,
                        IdEmpleado = payrollDto.EmployeeId,
                        NombreEmpleado = payrollDto.EmployeeName,
                        CodigoPeriodo = payrollDto.PeriodCode,
                        SalarioBase = payrollDto.BaseSalary,
                        Deducciones = payrollDto.Deductions,
                        SalarioNeto = payrollDto.NetSalary,
                        FechaProcesamiento = payrollDto.ProcessDate
                    };
                    _db.StagingA3InnuvaPayrollsTest.Add(newPayroll);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas-TEST] ✅ Sincronización de nóminas completada para {companyCode}: {uniquePayrolls.Count} nóminas procesadas en tabla TEST");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-TEST] Error sincronizando nóminas");
            throw;
        }
    }

    public async Task<PagedResult<A3InnuvaNominasCompanyDto>> GetCompaniesTestAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaCompaniesTest.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Codigo, s) ||
                EF.Functions.ILike(c.Nombre, s) ||
                EF.Functions.ILike(c.Nif, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.Codigo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new A3InnuvaNominasCompanyDto(
                c.IdExterno,
                c.Codigo,
                c.Nombre,
                c.Nif,
                c.Direccion,
                c.Ciudad,
                c.Pais,
                c.EmailContacto,
                c.TelefonoContacto))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaNominasCompanyDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<A3InnuvaNominasPayrollDto>> GetPayrollsTestAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaPayrollsTest.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.IdEmpleado, s) ||
                EF.Functions.ILike(p.NombreEmpleado, s) ||
                EF.Functions.ILike(p.CodigoPeriodo, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.FechaProcesamiento)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new A3InnuvaNominasPayrollDto(
                p.IdExterno,
                p.IdEmpleado,
                p.NombreEmpleado,
                p.CodigoPeriodo,
                p.SalarioBase,
                p.Deducciones,
                p.SalarioNeto,
                p.FechaProcesamiento))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaNominasPayrollDto>(items, total, page, pageSize);
    }
}
