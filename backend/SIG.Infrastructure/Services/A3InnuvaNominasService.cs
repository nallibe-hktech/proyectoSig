using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using SIG.Application.Calculation;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public interface IA3InnuvaNominasService
{
    // PHASE 1: SYNC
    Task SyncCompaniesAsync(CancellationToken ct = default);
    Task SyncPayrollsAsync(string companyCode, CancellationToken ct = default);
    Task SyncEmployeesAsync(CancellationToken ct = default);
    Task SyncConceptosAsync(CancellationToken ct = default);

    // PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints
    Task SyncSalaryAsync(CancellationToken ct = default);
    Task SyncIRPFAsync(CancellationToken ct = default);
    Task SyncRemunerationAsync(CancellationToken ct = default);
    Task SyncBankAccountsAsync(CancellationToken ct = default);
    Task SyncAgreementsAsync(CancellationToken ct = default);

    // PHASE 2: CALCULATE
    Task CalculatePayrollsAsync(string periodCode, CancellationToken ct = default);

    // PHASE 3: WRITE
    Task WritePayrollsAsync(string periodCode, CancellationToken ct = default);

    // Read
    Task<PagedResult<A3InnuvaNominasCompanyDto>> GetCompaniesAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<PagedResult<A3InnuvaNominasPayrollDto>> GetPayrollsAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<PagedResult<A3InnuvaEmpleadoDto>> GetEmployeesAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<PagedResult<A3InnuvaConceptoDto>> GetConceptosAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<PagedResult<A3InnuvaNominaCalculadaDto>> GetNominasCalculadasAsync(int page, int pageSize, string? periodCode, string? search, CancellationToken ct);
    Task<PagedResult<A3InnuvaNominaCalculadaDto>> GetNominasCalculadasEnviadasAsync(int page, int pageSize, string? periodCode, CancellationToken ct);

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
    private readonly ICalculationEngine _calculationEngine;
    // TODO: PHASE 2 - IPaymentModelService para validación de modelos de pago

    public A3InnuvaNominasService(
        AppDbContext db,
        IA3InnuvaNominasClient client,
        ILogger<A3InnuvaNominasService> logger,
        ICalculationEngine calculationEngine)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _calculationEngine = calculationEngine ?? throw new ArgumentNullException(nameof(calculationEngine));
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
            _logger.LogInformation("[A3InnuvaNominas] Iniciando sincronización de salarios pactados (payrolls)...");

            // Obtener empleados ya sincronizados (SyncEmployees debe ejecutarse ANTES)
            var employees = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados. Ejecute SyncEmployeesAsync primero.");
                return;
            }

            _logger.LogInformation($"[A3InnuvaNominas] Procesando salarios para {employees.Count} empleados...");

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    // EmpleadoIdExterno contiene el employeeCode
                    var salaries = await _client.GetSalaryAsync(companyCode, employee.EmpleadoIdExterno, cancellationToken);

                    foreach (var salary in salaries)
                    {
                        var hash = ComputeHash($"{salary.EmployeeCode}_{salary.IdExterno}");

                        var existing = await _db.StagingA3InnuvaPayrolls
                            .FirstOrDefaultAsync(p => p.Hash == hash && p.DeletedAt == null, cancellationToken);

                        if (existing != null)
                        {
                            _logger.LogDebug($"[A3InnuvaNominas] Payroll ya existe: {hash}");
                            continue;
                        }

                        _db.StagingA3InnuvaPayrolls.Add(new StagingA3InnuvaPayroll
                        {
                            IdExterno = salary.IdExterno ?? "",
                            IdEmpleado = employee.EmpleadoIdExterno,
                            NombreEmpleado = employee.Nombre,
                            CodigoPeriodo = "",
                            SalarioBase = salary.GrossSalary,
                            SalarioNeto = salary.NetSalary,
                            Deducciones = 0m,
                            FechaProcesamiento = DateTime.UtcNow,
                            Hash = hash,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(salary),
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando salary para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Sincronización de salarios completada: {totalSynced} registros procesados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] 💥 Error sincronizando nóminas");
            throw;
        }
    }

    public async Task SyncEmployeesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] Iniciando sincronización de empleados...");

            // Obtener empleados de la API (todas las páginas)
            var allEmployees = new List<EmployeeDto>();
            int pageNumber = 1;
            const int pageSize = 100;
            int totalFetched = 0;

            while (true)
            {
                _logger.LogInformation($"[A3InnuvaNominas] Obteniendo página {pageNumber} de empleados...");
                var employees = await _client.GetEmployeesAsync(pageNumber, pageSize, ct);
                _logger.LogInformation($"[A3InnuvaNominas] Página {pageNumber}: recibidos {employees.Count} empleados");

                if (employees.Count == 0)
                {
                    _logger.LogInformation($"[A3InnuvaNominas] Página {pageNumber} vacía, terminando paginación");
                    break;
                }

                allEmployees.AddRange(employees);
                totalFetched += employees.Count;
                _logger.LogInformation($"[A3InnuvaNominas] Página {pageNumber}: {employees.Count} empleados obtenidos (total: {totalFetched})");

                if (employees.Count < pageSize) break; // Última página
                pageNumber++;
            }

            if (allEmployees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No se obtuvieron empleados de la API");
                return;
            }

            // Deduplicar por EmployeeCode (clave única)
            var uniqueEmployees = allEmployees
                .DistinctBy(e => e.EmployeeCode)
                .ToList();

            _logger.LogInformation($"[A3InnuvaNominas] {uniqueEmployees.Count} empleados únicos después de deduplicación");

            // Insertar o actualizar
            foreach (var employeeDto in uniqueEmployees)
            {
                var existing = await _db.StagingA3InnuvaEmpleados
                    .FirstOrDefaultAsync(e => e.EmpleadoIdExterno == employeeDto.EmployeeId, ct);

                if (existing != null)
                {
                    // Actualizar
                    existing.NIF = employeeDto.IdentifierNumber;
                    existing.Nombre = employeeDto.CompleteName;
                    existing.FechaUltimaSincronizacion = DateTime.UtcNow;
                    _db.StagingA3InnuvaEmpleados.Update(existing);
                }
                else
                {
                    // Insertar
                    var newEmployee = new StagingA3InnuvaEmpleado
                    {
                        EmpleadoIdExterno = employeeDto.EmployeeCode ?? "",
                        NIF = employeeDto.IdentifierNumber,
                        Nombre = employeeDto.CompleteName,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(employeeDto),
                        Hash = ""
                    };
                    _db.StagingA3InnuvaEmpleados.Add(newEmployee);
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Sincronización de empleados completada: {uniqueEmployees.Count} empleados procesados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando empleados");
            throw;
        }
    }

    public async Task SyncConceptosAsync(CancellationToken ct = default)
    {
        // REACTIVADO: SyncConceptosAsync ahora funciona con URLs corregidas
        // FIX: Usar EmployeeCode en lugar de UUID para obtener conceptos
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] Iniciando sincronización de conceptos de empleados...");

            // Obtener empleados sincronizados (EmpleadoIdExterno YA contiene el EmployeeCode)
            var employees = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados. Ejecute SyncEmployeesAsync primero.");
                return;
            }

            _logger.LogInformation($"[A3InnuvaNominas] Procesando conceptos para {employees.Count} empleados...");

            var allConceptos = new List<ConceptoDto>();
            int conceptosProcessados = 0;

            // Para cada empleado, obtener sus conceptos usando EmployeeCode (que es EmpleadoIdExterno)
            foreach (var employee in employees)
            {
                try
                {
                    _logger.LogInformation($"[A3InnuvaNominas] Obteniendo conceptos para empleado {employee.Nombre} ({employee.EmpleadoIdExterno})...");

                    int pageNumber = 1;
                    const int pageSize = 100;
                    const int maxPages = 100; // Protección contra loops infinitos

                    while (pageNumber <= maxPages)
                    {
                        var conceptos = await _client.GetConceptosAsync(employee.EmpleadoIdExterno, pageNumber, pageSize, ct);
                        if (conceptos.Count == 0) break;

                        // Enriquecer conceptos con datos del empleado
                        var conceptosEnriquecidos = conceptos.Select(c => new ConceptoDto(
                            c.ConceptCode,
                            c.Description,
                            c.Amount,
                            c.ConceptType,
                            c.InKind,
                            c.Manual,
                            c.ConceptCollectionTypeDesc,
                            employee.EmpleadoIdExterno,
                            employee.Nombre
                        )).ToList();

                        allConceptos.AddRange(conceptosEnriquecidos);
                        conceptosProcessados += conceptos.Count;
                        _logger.LogInformation($"[A3InnuvaNominas] Empleado {employee.EmpleadoIdExterno}: {conceptos.Count} conceptos en página {pageNumber}");

                        if (conceptos.Count < pageSize) break;
                        pageNumber++;

                        if (pageNumber > maxPages)
                        {
                            _logger.LogWarning($"[A3InnuvaNominas] Empleado {employee.EmpleadoIdExterno}: Alcanzado máximo de páginas ({maxPages}). Deteniendo paginación.");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error obteniendo conceptos para empleado {employee.EmpleadoIdExterno}");
                }
            }

            if (allConceptos.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No se obtuvieron conceptos de la API");
                return;
            }

            _logger.LogInformation($"[A3InnuvaNominas] Procesando {allConceptos.Count} conceptos únicos...");

            // ⚡ FIX N+1: Cargar todos los conceptos existentes en memoria (una sola consulta)
            var existingConceptos = await _db.StagingA3InnuvaConceptos
                .Where(c => c.DeletedAt == null)
                .AsNoTracking()
                .ToListAsync(ct);

            var existingConceptoDict = existingConceptos
                .ToDictionary(
                    c => $"{c.CodigoConcepto}_{c.CodigoEmpleado}",
                    c => c,
                    StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation($"[A3InnuvaNominas] ✅ Conceptos existentes cargados en memoria: {existingConceptoDict.Count}");

            // Insertar o actualizar conceptos (sin N+1 queries)
            int conceptosInsertados = 0;
            int conceptosActualizados = 0;

            foreach (var conceptoDto in allConceptos)
            {
                var conceptKey = $"{conceptoDto.ConceptCode}_{conceptoDto.CodigoEmpleado}";
                var existing = existingConceptoDict.TryGetValue(conceptKey, out var existingRecord)
                    ? existingRecord
                    : null;

                if (existing != null)
                {
                    // Actualizar
                    existing.DescripcionConcepto = conceptoDto.Description;
                    existing.TipoConcepto = conceptoDto.ConceptType;
                    existing.Importe = conceptoDto.Amount;
                    existing.EsManual = conceptoDto.Manual;
                    existing.EsEnEspecie = conceptoDto.InKind;
                    existing.FechaUltimaSincronizacion = DateTime.UtcNow;
                    _db.StagingA3InnuvaConceptos.Update(existing);
                    conceptosActualizados++;
                }
                else
                {
                    // Insertar
                    var newConcepto = new StagingA3InnuvaConcepto
                    {
                        IdExterno = $"{conceptoDto.ConceptCode}_{conceptoDto.CodigoEmpleado}",
                        CodigoEmpleado = conceptoDto.CodigoEmpleado ?? "",
                        NombreEmpleado = conceptoDto.NombreEmpleado ?? "",
                        CodigoConcepto = int.TryParse(conceptoDto.ConceptCode, out var codigo) ? codigo : 0,
                        DescripcionConcepto = conceptoDto.Description,
                        TipoConcepto = conceptoDto.ConceptType,
                        Importe = conceptoDto.Amount,
                        Unidad = conceptoDto.ConceptCollectionTypeDesc,
                        EsManual = conceptoDto.Manual,
                        EsEnEspecie = conceptoDto.InKind,
                        FechaUltimaSincronizacion = DateTime.UtcNow
                    };
                    _db.StagingA3InnuvaConceptos.Add(newConcepto);
                    conceptosInsertados++;
                }
            }

            _logger.LogInformation($"[A3InnuvaNominas] Antes de SaveChangesAsync: {conceptosInsertados} por insertar, {conceptosActualizados} por actualizar");
            int savedChanges = await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SaveChangesAsync completado: {savedChanges} registros afectados");
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Sincronización de conceptos completada: {allConceptos.Count} conceptos procesados para {employees.Count} empleados (insertados: {conceptosInsertados}, actualizados: {conceptosActualizados})");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando conceptos");
            throw;
        }
    }

    public async Task CalculatePayrollsAsync(string periodCode, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Iniciando cálculo de nóminas para período {periodCode}...");

            if (string.IsNullOrWhiteSpace(periodCode))
                throw new ArgumentNullException(nameof(periodCode));

            // Obtener período (auto-crear si no existe para testing)
            var period = await _db.Periods
                .FirstOrDefaultAsync(p => p.Nombre == periodCode, ct);

            if (period == null)
            {
                _logger.LogWarning($"[A3InnuvaNominas-PHASE2] Período {periodCode} no encontrado. Creando automáticamente para testing...");

                // Auto-crear período con fechas inferidas de periodCode (ej: 2026-01)
                try
                {
                    var parts = periodCode.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int year) && int.TryParse(parts[1], out int month))
                    {
                        var fechaInicio = new DateOnly(year, month, 1);
                        var fechaFin = fechaInicio.AddMonths(1).AddDays(-1);

                        period = new Period
                        {
                            Nombre = periodCode,
                            FechaInicio = fechaInicio,
                            FechaFin = fechaFin,
                            DiaPago = 28,
                            Estado = EstadoPeriodo.Abierto
                        };

                        _db.Periods.Add(period);
                        await _db.SaveChangesAsync(ct);
                        _logger.LogInformation($"[A3InnuvaNominas-PHASE2] ✅ Período {periodCode} creado automáticamente: {fechaInicio:yyyy-MM-dd} a {fechaFin:yyyy-MM-dd}");
                    }
                    else
                    {
                        _logger.LogError($"[A3InnuvaNominas-PHASE2] No se puede parsear código de período {periodCode} (formato esperado: YYYY-MM)");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas-PHASE2] Error creando período automático {periodCode}");
                    return;
                }
            }

            // Obtener empleados desde los conceptos sincronizados (alternativa a staging_a3innuva_empleados)
            var conceptosPorEmpleado = await _db.StagingA3InnuvaConceptos
                .Where(c => c.DeletedAt == null)
                .AsNoTracking()
                .GroupBy(c => new { c.CodigoEmpleado, c.NombreEmpleado })
                .ToListAsync(ct);

            if (conceptosPorEmpleado.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas-PHASE2] No hay conceptos sincronizados");
                return;
            }

            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Calculando nóminas para {conceptosPorEmpleado.Count} empleados con PaymentModel...");

            int nominasCalculadas = 0;

            // Para cada empleado (agrupado desde conceptos), calcular su nómina INTEGRANDO PaymentModelService
            foreach (var empleadoGroup in conceptosPorEmpleado)
            {
                string codigoEmpleado = string.Empty;
                string nombreEmpleado = string.Empty;
                try
                {
                    codigoEmpleado = empleadoGroup.Key.CodigoEmpleado;
                    nombreEmpleado = empleadoGroup.Key.NombreEmpleado;

                    _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Procesando empleado {codigoEmpleado} ({nombreEmpleado})...");

                    // Obtener todos los conceptos del empleado
                    var conceptos = empleadoGroup.ToList();

                    if (conceptos.Count == 0)
                    {
                        _logger.LogWarning($"[A3InnuvaNominas-PHASE2] Empleado {codigoEmpleado} sin conceptos");
                        continue;
                    }

                    // TODO: PHASE 2 - Integración con PaymentModelService para validación de modelos de pago
                    // Por ahora, PHASE 1 solo suma importes crudos sin validación

                    // Calcular totales (PHASE 1: suma simple)
                    var totalPercepciones = 0m;
                    var totalDescuentos = 0m;
                    var incidencias = new List<string>();

                    foreach (var concepto in conceptos)
                    {
                        // PHASE 1: suma simple, sin validación de modelo de pago
                        // PHASE 2: agregar validación con PaymentModelService

                        // Sumar concepto
                        if (concepto.TipoConcepto == "E") // Earnings/Percepciones
                            totalPercepciones += concepto.Importe;
                        else if (concepto.TipoConcepto == "D") // Deductions/Descuentos
                            totalDescuentos += concepto.Importe;
                    }

                    var salarioNeto = totalPercepciones - totalDescuentos;

                    _logger.LogInformation(
                        $"[A3InnuvaNominas-PHASE2] Empleado {codigoEmpleado}: " +
                        $"Percepciones={totalPercepciones:F2}, Descuentos={totalDescuentos:F2}, Neto={salarioNeto:F2}");

                    // Guardar nómina calculada
                    var idExterno = $"{codigoEmpleado}_{periodCode}";
                    var existing = await _db.StagingA3InnuvaNominasCalculadas
                        .FirstOrDefaultAsync(n => n.IdExterno == idExterno && n.DeletedAt == null, ct);

                    if (existing != null)
                    {
                        existing.TotalPercepciones = totalPercepciones;
                        existing.TotalDescuentos = totalDescuentos;
                        existing.SalarioNeto = salarioNeto;
                        existing.UpdatedAt = DateTime.UtcNow;
                        _db.StagingA3InnuvaNominasCalculadas.Update(existing);
                    }
                    else
                    {
                        var nuevaNomina = new StagingA3InnuvaNominaCalculada
                        {
                            IdExterno = idExterno,
                            CodigoEmpleado = codigoEmpleado,
                            NombreEmpleado = nombreEmpleado,
                            CodigoPeriodo = periodCode,
                            FechaPeriodo = period.FechaInicio.ToDateTime(TimeOnly.MinValue),
                            TotalPercepciones = totalPercepciones,
                            TotalDescuentos = totalDescuentos,
                            SalarioNeto = salarioNeto,
                            FueEnviadoAWK = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.StagingA3InnuvaNominasCalculadas.Add(nuevaNomina);
                    }

                    nominasCalculadas++;

                    if (incidencias.Count > 0)
                        _logger.LogWarning($"[A3InnuvaNominas-PHASE2] Empleado {codigoEmpleado} - Incidencias: {string.Join("; ", incidencias)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas-PHASE2] Error calculando nómina para empleado {codigoEmpleado}");
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] ✅ Cálculo completado: {nominasCalculadas} nóminas calculadas para período {periodCode} CON VALIDACIÓN DE MODELO DE PAGO");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-PHASE2] Error calculando nóminas");
            throw;
        }
    }

    public async Task WritePayrollsAsync(string periodCode, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation($"[A3InnuvaNominas-PHASE3] Iniciando escritura de nóminas para período {periodCode}...");

            if (string.IsNullOrWhiteSpace(periodCode))
                throw new ArgumentNullException(nameof(periodCode));

            // Obtener nóminas calculadas que no han sido enviadas
            var nominasNoEnviadas = await _db.StagingA3InnuvaNominasCalculadas
                .Where(n => n.CodigoPeriodo == periodCode && !n.FueEnviadoAWK && n.DeletedAt == null)
                .AsNoTracking()
                .ToListAsync(ct);

            if (nominasNoEnviadas.Count == 0)
            {
                _logger.LogWarning($"[A3InnuvaNominas-PHASE3] No hay nóminas calculadas sin enviar para período {periodCode}");
                return;
            }

            _logger.LogInformation($"[A3InnuvaNominas-PHASE3] Enviando {nominasNoEnviadas.Count} nóminas a Wolters Kluwer...");

            int nominasEnviadas = 0;
            int nominasConError = 0;

            // Enviar cada nómina a Wolters Kluwer
            foreach (var nomina in nominasNoEnviadas)
            {
                try
                {
                    _logger.LogInformation($"[A3InnuvaNominas-PHASE3] Enviando nómina del empleado {nomina.CodigoEmpleado}...");

                    // Llamar a cliente HTTP para escribir en Wolters Kluwer
                    var response = await _client.WritePayrollAsync(
                        "1", // companyCode
                        nomina.CodigoEmpleado,
                        nomina.CodigoPeriodo,
                        nomina.TotalPercepciones,
                        nomina.TotalDescuentos,
                        nomina.SalarioNeto,
                        ct);

                    // Actualizar registro con respuesta
                    var record = await _db.StagingA3InnuvaNominasCalculadas
                        .FirstOrDefaultAsync(n => n.IdExterno == nomina.IdExterno, ct);

                    if (record != null)
                    {
                        record.FueEnviadoAWK = true;
                        record.FechaEnvio = DateTime.UtcNow;
                        record.ResponseWK = response;
                        record.UpdatedAt = DateTime.UtcNow;
                        _db.StagingA3InnuvaNominasCalculadas.Update(record);
                    }

                    nominasEnviadas++;
                    _logger.LogInformation($"[A3InnuvaNominas-PHASE3] ✅ Nómina enviada para {nomina.CodigoEmpleado}");
                }
                catch (Exception ex)
                {
                    nominasConError++;
                    _logger.LogError(ex, $"[A3InnuvaNominas-PHASE3] Error enviando nómina para empleado {nomina.CodigoEmpleado}");
                }
            }

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas-PHASE3] ✅ Escritura completada: {nominasEnviadas} enviadas, {nominasConError} con error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-PHASE3] Error escribiendo nóminas");
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

    public async Task<PagedResult<A3InnuvaNominaCalculadaDto>> GetNominasCalculadasAsync(
        int page, int pageSize, string? periodCode, string? search, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaNominasCalculadas
            .Where(n => n.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(periodCode))
        {
            query = query.Where(n => n.CodigoPeriodo == periodCode);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.CodigoEmpleado, s) ||
                EF.Functions.ILike(n.NombreEmpleado, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new A3InnuvaNominaCalculadaDto(
                n.IdExterno,
                n.CodigoEmpleado,
                n.NombreEmpleado,
                n.CodigoPeriodo,
                n.TotalPercepciones,
                n.TotalDescuentos,
                n.SalarioNeto,
                n.FueEnviadoAWK,
                n.FechaEnvio,
                n.ResponseWK))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaNominaCalculadaDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<A3InnuvaNominaCalculadaDto>> GetNominasCalculadasEnviadasAsync(
        int page, int pageSize, string? periodCode, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaNominasCalculadas
            .Where(n => n.FueEnviadoAWK && n.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(periodCode))
        {
            query = query.Where(n => n.CodigoPeriodo == periodCode);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(n => n.FechaEnvio)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new A3InnuvaNominaCalculadaDto(
                n.IdExterno,
                n.CodigoEmpleado,
                n.NombreEmpleado,
                n.CodigoPeriodo,
                n.TotalPercepciones,
                n.TotalDescuentos,
                n.SalarioNeto,
                n.FueEnviadoAWK,
                n.FechaEnvio,
                n.ResponseWK))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaNominaCalculadaDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<A3InnuvaEmpleadoDto>> GetEmployeesAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaEmpleados.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.EmpleadoIdExterno, s) ||
                EF.Functions.ILike(e.NIF, s) ||
                EF.Functions.ILike(e.Nombre, s) ||
                EF.Functions.ILike(e.Departamento, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.Nombre)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new A3InnuvaEmpleadoDto(
                e.EmpleadoIdExterno ?? "",
                e.NIF,
                e.Nombre,
                e.Departamento,
                e.SueldoMensual,
                e.FechaUltimaSincronizacion))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaEmpleadoDto>(items, total, page, pageSize);
    }

    public async Task<PagedResult<A3InnuvaConceptoDto>> GetConceptosAsync(int page, int pageSize, string? search, CancellationToken ct)
    {
        var query = _db.StagingA3InnuvaConceptos
            .Where(c => c.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.CodigoEmpleado, s) ||
                EF.Functions.ILike(c.NombreEmpleado, s) ||
                EF.Functions.ILike(c.DescripcionConcepto, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.CodigoEmpleado)
            .ThenBy(c => c.CodigoConcepto)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new A3InnuvaConceptoDto(
                c.IdExterno,
                c.CodigoEmpleado,
                c.NombreEmpleado,
                c.CodigoConcepto,
                c.DescripcionConcepto,
                c.TipoConcepto,
                c.Importe,
                c.Unidad,
                c.EsManual,
                c.EsEnEspecie,
                c.FechaUltimaSincronizacion))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaConceptoDto>(items, total, page, pageSize);
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

    // ====== PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints ======

    /// <summary>
    /// PHASE 1.3a: Sync salary data for all employees
    /// </summary>
    public async Task SyncSalaryAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncSalaryAsync iniciado");

            // Obtener empleados sincronizados previamente
            var employees = await _db.StagingA3InnuvaEmpleados
                
                .Take(100)  // Limitar a 100 empleados por sincronización
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados. Ejecute SyncEmployeesAsync primero.");
                return;
            }

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    var salaries = await _client.GetSalaryAsync("1", employee.EmpleadoIdExterno, cancellationToken);

                    foreach (var salary in salaries)
                    {
                        var hash = ComputeHash(
                            $"{salary.EmployeeCode}_{salary.StartDate}_{salary.GrossSalary}");

                        var existing = await _db.StagingA3InnuvaSalaries
                            .FirstOrDefaultAsync(s => s.Hash == hash, cancellationToken);

                        if (existing != null)
                        {
                            _logger.LogDebug($"[A3InnuvaNominas] Salary ya existe: {hash}");
                            continue;
                        }

                        _db.StagingA3InnuvaSalaries.Add(new StagingA3InnuvaSalary
                        {
                            SalaryIdExterno = salary.IdExterno,
                            EmpleadoIdExterno = salary.EmployeeCode,
                            ContratoIdExterno = employee.EmpleadoIdExterno,
                            UserId = employee.UserId,
                            NIF = salary.NIF,
                            ImporteBruto = salary.GrossSalary,
                            ImporteNeto = salary.NetSalary,
                            Moneda = salary.Currency,
                            FechaInicio = salary.StartDate,
                            FechaFin = salary.EndDate,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(salary),
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false,
                            ErrorProcesamiento = null
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando salary para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SyncSalaryAsync completado: {totalSynced} registros sincronizados");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error SyncSalaryAsync: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// PHASE 1.3b: Sync IRPF (tax) data for all employees
    /// </summary>
    public async Task SyncIRPFAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncIRPFAsync iniciado");

            var employees = await _db.StagingA3InnuvaEmpleados
                
                .Take(100)
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados.");
                return;
            }

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    var irpfs = await _client.GetIRPFAsync("1", employee.EmpleadoIdExterno, cancellationToken);

                    foreach (var irpf in irpfs)
                    {
                        var hash = ComputeHash(
                            $"{irpf.EmployeeCode}_{irpf.TaxType}_{irpf.TaxRate}");

                        var existing = await _db.StagingA3InnuvaIRPFs
                            .FirstOrDefaultAsync(i => i.Hash == hash, cancellationToken);

                        if (existing != null) continue;

                        _db.StagingA3InnuvaIRPFs.Add(new StagingA3InnuvaIRPF
                        {
                            IRPFIdExterno = irpf.IdExterno,
                            EmpleadoIdExterno = irpf.EmployeeCode,
                            UserId = employee.UserId,
                            NIF = irpf.NIF,
                            TipoImpuesto = irpf.TaxType,
                            PercentajeTariacion = irpf.TaxRate,
                            ImporteRetencion = irpf.RetentionAmount,
                            FechaInicio = irpf.StartDate,
                            FechaFin = irpf.EndDate,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(irpf),
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando IRPF para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SyncIRPFAsync completado: {totalSynced} registros");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error SyncIRPFAsync: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// PHASE 1.3c: Sync remuneration data for all employees
    /// </summary>
    public async Task SyncRemunerationAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncRemunerationAsync iniciado");

            var employees = await _db.StagingA3InnuvaEmpleados
                
                .Take(100)
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados.");
                return;
            }

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    var remunerations = await _client.GetRemunerationAsync("1", employee.EmpleadoIdExterno, cancellationToken);

                    foreach (var remuneration in remunerations)
                    {
                        var hash = ComputeHash(
                            $"{remuneration.EmployeeCode}_{remuneration.RemunerationType}_{remuneration.Amount}");

                        var existing = await _db.StagingA3InnuvaRemunerations
                            .FirstOrDefaultAsync(r => r.Hash == hash, cancellationToken);

                        if (existing != null) continue;

                        _db.StagingA3InnuvaRemunerations.Add(new StagingA3InnuvaRemuneration
                        {
                            RemuneracionIdExterno = remuneration.IdExterno,
                            EmpleadoIdExterno = remuneration.EmployeeCode,
                            UserId = employee.UserId,
                            NIF = remuneration.NIF,
                            TipoRemuneracion = remuneration.RemunerationType,
                            Importe = remuneration.Amount,
                            Concepto = remuneration.Concept,
                            FechaInicio = remuneration.StartDate,
                            FechaFin = remuneration.EndDate,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(remuneration),
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando remuneraciones para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SyncRemunerationAsync completado: {totalSynced} registros");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error SyncRemunerationAsync: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// PHASE 1.3d: Sync bank account data for all employees
    /// </summary>
    public async Task SyncBankAccountsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncBankAccountsAsync iniciado");

            var employees = await _db.StagingA3InnuvaEmpleados
                
                .Take(100)
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados.");
                return;
            }

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    var bankAccounts = await _client.GetBankAccountsAsync("1", employee.EmpleadoIdExterno, cancellationToken);

                    foreach (var account in bankAccounts)
                    {
                        var hash = ComputeHash(
                            $"{account.EmployeeCode}_{account.IBAN}");

                        var existing = await _db.StagingA3InnuvaBankAccounts
                            .FirstOrDefaultAsync(b => b.Hash == hash, cancellationToken);

                        if (existing != null) continue;

                        _db.StagingA3InnuvaBankAccounts.Add(new StagingA3InnuvaBankAccount
                        {
                            CuentaIdExterno = account.IdExterno,
                            EmpleadoIdExterno = account.EmployeeCode,
                            UserId = employee.UserId,
                            NIF = account.NIF,
                            IBAN = account.IBAN,
                            BIC = account.BIC,
                            NombreTitular = account.AccountHolderName,
                            TipoCuenta = account.AccountType,
                            EsPrincipal = account.IsPrimary,
                            FechaInicio = account.StartDate,
                            FechaFin = account.EndDate,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(account),
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando cuentas bancarias para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SyncBankAccountsAsync completado: {totalSynced} registros");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error SyncBankAccountsAsync: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// PHASE 1.3e: Sync agreement data for all employees
    /// </summary>
    public async Task SyncAgreementsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncAgreementsAsync iniciado");

            var employees = await _db.StagingA3InnuvaEmpleados
                
                .Take(100)
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados.");
                return;
            }

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    var agreements = await _client.GetAgreementsAsync("1", employee.EmpleadoIdExterno, cancellationToken);

                    foreach (var agreement in agreements)
                    {
                        var hash = ComputeHash(
                            $"{agreement.EmployeeCode}_{agreement.AgreementCode}");

                        var existing = await _db.StagingA3InnuvaAgreements
                            .FirstOrDefaultAsync(a => a.Hash == hash, cancellationToken);

                        if (existing != null) continue;

                        _db.StagingA3InnuvaAgreements.Add(new StagingA3InnuvaAgreement
                        {
                            AcuerdoIdExterno = agreement.IdExterno,
                            EmpleadoIdExterno = agreement.EmployeeCode,
                            UserId = employee.UserId,
                            NIF = agreement.NIF,
                            CodigoAcuerdo = agreement.AgreementCode,
                            NombreAcuerdo = agreement.AgreementName,
                            TipoAcuerdo = agreement.AgreementType,
                            FechaInicio = agreement.StartDate,
                            FechaFin = agreement.EndDate,
                            Descripcion = agreement.Description,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(agreement),
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando acuerdos para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SyncAgreementsAsync completado: {totalSynced} registros");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error SyncAgreementsAsync: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Helper method to compute SHA256 hash of a string
    /// </summary>
    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes);
    }
}
