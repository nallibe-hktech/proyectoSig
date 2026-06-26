using ClosedXML.Excel;
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
    Task SyncIRPFAsync(CancellationToken ct = default);
    Task SyncRemunerationAsync(CancellationToken ct = default);
    Task SyncBankAccountsAsync(CancellationToken ct = default);
    Task SyncAgreementsAsync(CancellationToken ct = default);

    // PHASE 1.5: Contract Data (per employee)
    Task SyncContractAgreementsAsync(CancellationToken ct = default);
    Task SyncContractTimetablesAsync(CancellationToken ct = default);

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
    Task<PagedResult<A3InnuvaEmpleadoDto>> GetEmployeesTestAsync(int page, int pageSize, string? search, CancellationToken ct);

    // Excel generation for manual upload
    Task<byte[]> GenerateExcelAsync(string periodCode, CancellationToken ct = default);
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

            // ⚡ FIX: Cargar todas las empresas existentes en memoria (una sola query)
            var existingCompanies = await _db.StagingA3InnuvaCompanies
                .Where(c => c.DeletedAt == null)
                .AsNoTracking()
                .ToListAsync(ct);

            var existingCompanyDict = existingCompanies.ToDictionary(c => c.IdExterno, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Empresas existentes en memoria: {existingCompanyDict.Count}");

            // Insertar o actualizar (sin N+1 queries)
            int inserted = 0, updated = 0;
            foreach (var companyDto in uniqueCompanies)
            {
                if (existingCompanyDict.TryGetValue(companyDto.Id, out var existing))
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
                    updated++;
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
                    inserted++;
                }
            }

            _logger.LogInformation($"[A3InnuvaNominas] Empresas: {inserted} insertadas, {updated} actualizadas");

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
            _logger.LogInformation("[A3InnuvaNominas] Iniciando cadena completa de sincronización y cálculo de nóminas...");

            // Determinar período actual (formato: YYYY-MM)
            var now = DateTime.UtcNow;
            var periodCode = $"{now.Year}-{now.Month:D2}";

            _logger.LogInformation($"[A3InnuvaNominas] Período objetivo: {periodCode}");

            // PASO 1: Sincronizar empleados
            _logger.LogInformation("[A3InnuvaNominas] PASO 1: Sincronizando empleados...");
            await SyncEmployeesAsync(ct);

            // PASO 2: Sincronizar conceptos
            _logger.LogInformation("[A3InnuvaNominas] PASO 2: Sincronizando conceptos...");
            await SyncConceptosAsync(ct);

            // PASO 3: Sincronizar IRPF (descuentos fiscales)
            _logger.LogInformation("[A3InnuvaNominas] PASO 3: Sincronizando IRPF...");
            await SyncIRPFAsync(ct);

            // PASO 4: Sincronizar remuneration (salarios, pagas extra)
            _logger.LogInformation("[A3InnuvaNominas] PASO 4: Sincronizando remuneración...");
            await SyncRemunerationAsync(ct);

            // PASO 5: Calcular nóminas usando todos los datos sincronizados
            _logger.LogInformation("[A3InnuvaNominas] PASO 5: Calculando nóminas...");
            await CalculatePayrollsAsync(periodCode, ct);

            _logger.LogInformation($"[A3InnuvaNominas] ✅ Cadena completa completada para período {periodCode}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] 💥 Error en cadena de sincronización");
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

            // ⚡ FIX: Cargar todos los empleados existentes en memoria (una sola query)
            var existingEmployees = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .ToListAsync(ct);

            var existingEmployeeDict = existingEmployees.ToDictionary(e => e.EmpleadoIdExterno, StringComparer.OrdinalIgnoreCase);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Empleados existentes en memoria: {existingEmployeeDict.Count}");

            // Insertar o actualizar (sin N+1 queries)
            int inserted = 0, updated = 0;
            foreach (var employeeDto in uniqueEmployees)
            {
                if (existingEmployeeDict.TryGetValue(employeeDto.EmployeeCode ?? "", out var existing))
                {
                    // Actualizar
                    existing.NIF = employeeDto.IdentifierNumber;
                    existing.Nombre = employeeDto.CompleteName;
                    existing.FechaUltimaSincronizacion = DateTime.UtcNow;
                    existing.PayloadJson = System.Text.Json.JsonSerializer.Serialize(employeeDto);
                    existing.Hash = ComputeHash($"{employeeDto.EmployeeCode}_{employeeDto.IdentifierNumber}");
                    _db.StagingA3InnuvaEmpleados.Update(existing);
                    updated++;
                }
                else
                {
                    // Insertar
                    var hash = ComputeHash($"{employeeDto.EmployeeCode}_{employeeDto.IdentifierNumber}");
                    var newEmployee = new StagingA3InnuvaEmpleado
                    {
                        EmpleadoIdExterno = employeeDto.EmployeeCode ?? "",
                        NIF = employeeDto.IdentifierNumber,
                        Nombre = employeeDto.CompleteName,
                        FechaUltimaSincronizacion = DateTime.UtcNow,
                        PayloadJson = System.Text.Json.JsonSerializer.Serialize(employeeDto),
                        Hash = hash
                    };
                    _db.StagingA3InnuvaEmpleados.Add(newEmployee);
                    inserted++;
                }
            }

            _logger.LogInformation($"[A3InnuvaNominas] Empleados: {inserted} insertados, {updated} actualizados");

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

            // ⚡ FIX: Cargar todos los conceptos existentes en memoria (una sola query)
            var existingConceptos = await _db.StagingA3InnuvaConceptos
                .Where(c => c.DeletedAt == null)
                .AsNoTracking()
                .ToListAsync(ct);

            // Usar ToLookup() para manejar duplicados (permitir múltiples conceptos con misma clave)
            var existingConceptoDict = existingConceptos
                .ToLookup(
                    c => $"{c.CodigoConcepto}_{c.CodigoEmpleado}",
                    c => c,
                    StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation($"[A3InnuvaNominas] ✅ Conceptos existentes cargados en memoria: {existingConceptoDict.Count} claves únicas");

            // Insertar o actualizar conceptos (sin N+1 queries)
            int inserted = 0, updated = 0;

            foreach (var conceptoDto in allConceptos)
            {
                var conceptKey = $"{conceptoDto.ConceptCode}_{conceptoDto.CodigoEmpleado}";

                // ToLookup permite múltiples valores por clave; actualizar el primero encontrado
                if (existingConceptoDict.Contains(conceptKey))
                {
                    var existing = existingConceptoDict[conceptKey].FirstOrDefault();
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
                        updated++;
                    }
                }
                else
                {
                    // Insertar
                    var newConcepto = new StagingA3InnuvaConcepto
                    {
                        IdExterno = $"{conceptoDto.ConceptCode}_{conceptoDto.CodigoEmpleado}",
                        CodigoEmpleado = conceptoDto.CodigoEmpleado ?? "",
                        NombreEmpleado = conceptoDto.NombreEmpleado ?? "",
                        CodigoConcepto = conceptoDto.ConceptCode ?? 0,
                        DescripcionConcepto = conceptoDto.Description,
                        TipoConcepto = conceptoDto.ConceptType,
                        Importe = conceptoDto.Amount,
                        Unidad = conceptoDto.ConceptCollectionTypeDesc,
                        EsManual = conceptoDto.Manual,
                        EsEnEspecie = conceptoDto.InKind,
                        FechaUltimaSincronizacion = DateTime.UtcNow
                    };
                    _db.StagingA3InnuvaConceptos.Add(newConcepto);
                    inserted++;
                }
            }

            int savedChanges = await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SaveChangesAsync completado: {savedChanges} registros afectados");
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Sincronización de conceptos completada: {inserted} insertados, {updated} actualizados");
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

            // 1. Obtener período
            var period = await _db.Periods
                .FirstOrDefaultAsync(p => p.Nombre == periodCode, ct);

            if (period == null)
            {
                _logger.LogWarning($"[A3InnuvaNominas-PHASE2] Período {periodCode} no encontrado. Creando automáticamente para testing...");

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

            // 2. Cargar empleados staging
            var empleados = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .ToListAsync(ct);

            if (empleados.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas-PHASE2] No hay empleados sincronizados en PHASE 1");
                return;
            }

            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Carguados {empleados.Count} empleados");

            // 3. Cargar conceptos staging
            var conceptos = await _db.StagingA3InnuvaConceptos
                .Where(c => c.DeletedAt == null)
                .AsNoTracking()
                .ToListAsync(ct);

            if (conceptos.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas-PHASE2] No hay conceptos sincronizados");
                return;
            }

            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Carguados {conceptos.Count} conceptos");

            // Diagnóstico: tipos de concepto disponibles
            var tiposUnicos = conceptos.Select(c => c.TipoConcepto).Distinct().ToList();
            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Tipos de concepto encontrados: {string.Join(", ", tiposUnicos)}");

            // 4. Cargar gastos PayHawk del período + join con User para obtener NIF
            var payhawkGastos = await _db.StagingPayHawkGastos
                .Where(g => g.Fecha >= period.FechaInicio && g.Fecha <= period.FechaFin)
                .Join(
                    _db.Users.AsNoTracking(),
                    g => g.UserId,
                    u => u.Id,
                    (g, u) => new { Gasto = g, NIF = u.NIF })
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Carguados {payhawkGastos.Count} gastos PayHawk");

            // 5. Preparar gastos PayHawk por NIF
            var gastosByNif = payhawkGastos
                .GroupBy(x => x.NIF ?? "")
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.Gasto.Importe),
                    StringComparer.OrdinalIgnoreCase);

            // 5b. Cargar IRPF (descuentos fiscales)
            var irpfData = await _db.StagingA3InnuvaIRPFs
                .AsNoTracking()
                .ToListAsync(ct);

            var irpfByEmpleado = irpfData
                .GroupBy(i => i.EmpleadoIdExterno ?? "")
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(x => x.ImporteRetencion),
                    StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Carguados {irpfData.Count} registros IRPF para {irpfByEmpleado.Count} empleados");

            // 5c. Cargar datos de remuneration (salario teórico, pagas extra, etc.)
            var remuneracionData = await _db.StagingA3InnuvaRemunerations
                .AsNoTracking()
                .ToListAsync(ct);

            var remuneracionByEmpleado = remuneracionData
                .GroupBy(r => r.EmpleadoIdExterno ?? "")
                .ToDictionary(
                    g => g.Key,
                    g => g.ToList(), // Guardar lista completa para filtrar por tipo
                    StringComparer.OrdinalIgnoreCase);

            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Carguados {remuneracionData.Count} registros de remuneration para {remuneracionByEmpleado.Count} empleados");

            _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Calculando nóminas para {empleados.Count} empleados...");

            int nominasCalculadas = 0;
            int erroresCalculados = 0;

            // 6. Calcular por empleado (JOIN en memoria con conceptos)
            foreach (var empleado in empleados)
            {
                try
                {
                    var codigoEmpleado = empleado.EmpleadoIdExterno ?? "";
                    var nif = empleado.NIF ?? "";

                    // Obtener remuneration de este empleado (CONCEPTS + INCENTIVES)
                    var remuneracionEmpleado = remuneracionByEmpleado.ContainsKey(codigoEmpleado)
                        ? remuneracionByEmpleado[codigoEmpleado]
                        : new List<StagingA3InnuvaRemuneration>();

                    _logger.LogInformation($"[A3InnuvaNominas-PHASE2] Empleado={codigoEmpleado} ({empleado.Nombre}): Remuneración registros={remuneracionEmpleado.Count}");

                    // Percepciones: THEORETICAL-GROSS + EXTRAPAYMENTS + INCENTIVES (desde remuneration)
                    var percepciones = remuneracionEmpleado
                        .Where(r => r.TipoRemuneracion == "THEORETICAL-GROSS" ||
                                   r.TipoRemuneracion == "EXTRAPAYMENTS" ||
                                   r.TipoRemuneracion == "INCENTIVES" ||
                                   r.TipoRemuneracion == "CONCEPTS" ||
                                   r.TipoRemuneracion?.Contains("EXTRA", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(r => r.Importe);

                    // Reembolsos PayHawk (percepciones extrasalariales) - por NIF del empleado
                    var reembolsosPayhawk = !string.IsNullOrEmpty(nif) && gastosByNif.ContainsKey(nif)
                        ? gastosByNif[nif]
                        : 0m;

                    // Deducciones: IRPF + SANCIONES (de remuneration)
                    var irpfDelEmpleado = irpfByEmpleado.ContainsKey(codigoEmpleado)
                        ? irpfByEmpleado[codigoEmpleado]
                        : 0m;

                    var sancionesDelEmpleado = remuneracionEmpleado
                        .Where(r => r.TipoRemuneracion == "SANCTIONS" ||
                                   r.TipoRemuneracion?.Contains("SANCION", StringComparison.OrdinalIgnoreCase) == true)
                        .Sum(r => r.Importe);

                    var descuentos = irpfDelEmpleado + sancionesDelEmpleado;

                    var totalPercepciones = percepciones + reembolsosPayhawk;
                    var salarioNeto = totalPercepciones - descuentos;

                    var descuentoInfo = descuentos > 0 ? $"Descuentos={descuentos:F2}" : "Descuentos=0 (NO HAY EN DATOS)";
                    _logger.LogInformation(
                        $"[A3InnuvaNominas-PHASE2] Empleado {codigoEmpleado} ({empleado.Nombre}): " +
                        $"Percepciones={percepciones:F2}, Reembolsos={reembolsosPayhawk:F2}, {descuentoInfo}, Neto={salarioNeto:F2}");

                    // Guardar nómina calculada
                    var idExterno = $"{codigoEmpleado}_{periodCode}";
                    var existing = await _db.StagingA3InnuvaNominasCalculadas
                        .FirstOrDefaultAsync(n => n.IdExterno == idExterno && n.DeletedAt == null, ct);

                    if (existing != null)
                    {
                        existing.TotalPercepciones = totalPercepciones;
                        existing.TotalDescuentos = descuentos;
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
                            NombreEmpleado = $"{empleado.Nombre} {empleado.Departamento}".Trim(),
                            CodigoPeriodo = periodCode,
                            FechaPeriodo = period.FechaInicio.ToDateTime(TimeOnly.MinValue),
                            TotalPercepciones = totalPercepciones,
                            TotalDescuentos = descuentos,
                            SalarioNeto = salarioNeto,
                            FueEnviadoAWK = false,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };
                        _db.StagingA3InnuvaNominasCalculadas.Add(nuevaNomina);
                    }

                    nominasCalculadas++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"[A3InnuvaNominas-PHASE2] Error calculando empleado {empleado.EmpleadoIdExterno}");
                    erroresCalculados++;
                }
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                $"[A3InnuvaNominas-PHASE2] ✅ Cálculo completado: {nominasCalculadas} nóminas calculadas, " +
                $"{erroresCalculados} errores | Periodo: {periodCode}");
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
        var query = _db.StagingA3InnuvaNominasCalculadas
            .Where(n => n.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.CodigoEmpleado, s) ||
                EF.Functions.ILike(p.NombreEmpleado, s) ||
                EF.Functions.ILike(p.CodigoPeriodo, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new A3InnuvaNominasPayrollDto(
                p.IdExterno,
                p.CodigoEmpleado,
                p.NombreEmpleado,
                p.CodigoPeriodo,
                p.TotalPercepciones,
                p.TotalDescuentos,
                p.SalarioNeto,
                p.CreatedAt))
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
        => await GetEmployeesInternalAsync(_db.StagingA3InnuvaEmpleados, page, pageSize, search, ct);

    public async Task<PagedResult<A3InnuvaEmpleadoDto>> GetEmployeesTestAsync(int page, int pageSize, string? search, CancellationToken ct)
        => await GetEmployeesInternalAsync(_db.StagingA3InnuvaEmpleados, page, pageSize, search, ct);

    private async Task<PagedResult<A3InnuvaEmpleadoDto>> GetEmployeesInternalAsync(IQueryable<StagingA3InnuvaEmpleado> query, int page, int pageSize, string? search, CancellationToken ct)
    {
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
        var query = _db.StagingA3InnuvaNominasCalculadas
            .Where(n => n.DeletedAt == null)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.CodigoEmpleado, s) ||
                EF.Functions.ILike(p.NombreEmpleado, s) ||
                EF.Functions.ILike(p.CodigoPeriodo, s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new A3InnuvaNominasPayrollDto(
                p.IdExterno,
                p.CodigoEmpleado,
                p.NombreEmpleado,
                p.CodigoPeriodo,
                p.TotalPercepciones,
                p.TotalDescuentos,
                p.SalarioNeto,
                p.CreatedAt))
            .ToListAsync(ct);

        return new PagedResult<A3InnuvaNominasPayrollDto>(items, total, page, pageSize);
    }

    // ====== PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints ======

    /// <summary>
    /// PHASE 1.3a: Sync IRPF (tax) data for all employees
    /// </summary>
    public async Task SyncIRPFAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncIRPFAsync iniciado");

            var employees = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .ToListAsync(ct); // Cargar TODOS los empleados para sincronizar IRPF

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
                .AsNoTracking()
                .ToListAsync(ct); // Cargar TODOS los empleados

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

    public async Task SyncContractAgreementsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncContractAgreementsAsync iniciado");

            var employees = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .Take(100)
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados para contratos.");
                return;
            }

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    var contractAgreements = await _client.GetContractAgreementAsync(
                        employee.EmpleadoIdExterno, 1, 25, cancellationToken);

                    foreach (var contract in contractAgreements)
                    {
                        var hash = ComputeHash(
                            $"{contract.EmployeeCode}_{contract.ContractCode}");

                        var existing = await _db.StagingA3InnuvaContractAgreements
                            .FirstOrDefaultAsync(c => c.Hash == hash, cancellationToken);

                        if (existing != null) continue;

                        _db.StagingA3InnuvaContractAgreements.Add(new StagingA3InnuvaContractAgreement
                        {
                            ContratoIdExterno = contract.IdExterno,
                            EmpleadoIdExterno = contract.EmployeeCode,
                            UserId = employee.UserId,
                            CodigoContrato = contract.ContractCode,
                            DescripcionContrato = contract.ContractDescription,
                            FechaInicioPeriodoLaboral = contract.LabourPeriodStartDate,
                            FechaFinPeriodoLaboral = contract.LabourPeriodEndDate,
                            TipoAportacionID = contract.ContributionTypeID,
                            TipoAportacion = contract.ContributionType,
                            ModalidadAportacion = contract.ContributionModalityType,
                            CodigoOcupacionCNO = contract.CnoOccupationID,
                            MontoAñualBruto = contract.AnnualGrossAmount,
                            TipoCobroID = contract.CollectionTypeID,
                            TipoCobro = contract.CollectionType,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(contract),
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando acuerdo de contrato para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SyncContractAgreementsAsync completado: {totalSynced} registros");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error SyncContractAgreementsAsync: {Message}", ex.Message);
        }
    }

    public async Task SyncContractTimetablesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] SyncContractTimetablesAsync iniciado");

            var employees = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .Take(100)
                .ToListAsync(ct);

            if (employees.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] No hay empleados sincronizados para horarios.");
                return;
            }

            int totalSynced = 0;
            var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 3, CancellationToken = ct };

            await Parallel.ForEachAsync(employees, parallelOptions, async (employee, cancellationToken) =>
            {
                try
                {
                    var contractTimetables = await _client.GetContractTimetableAsync(
                        employee.EmpleadoIdExterno, 1, 25, cancellationToken);

                    foreach (var timetable in contractTimetables)
                    {
                        var hash = ComputeHash(
                            $"{timetable.EmployeeCode}_timetable");

                        var existing = await _db.StagingA3InnuvaContractTimetables
                            .FirstOrDefaultAsync(t => t.Hash == hash, cancellationToken);

                        if (existing != null) continue;

                        _db.StagingA3InnuvaContractTimetables.Add(new StagingA3InnuvaContractTimetable
                        {
                            HorarioIdExterno = timetable.IdExterno,
                            EmpleadoIdExterno = timetable.EmployeeCode,
                            UserId = employee.UserId,
                            TipoDiaLaboralID = timetable.WorkDayTypeID,
                            TotalHorasSemanal = timetable.TotalWeekHours,
                            DiaLaboralCompletoInicio = timetable.CompleteWorkDayStartID,
                            DiaLaboralCompletoFin = timetable.CompleteWorkDayEndID,
                            TieneHorasComplementarias = timetable.IndComplementaryHours,
                            TipoPeriodoPartial = timetable.PartialPeriodTypeID,
                            HorasPartial = timetable.PartialHours,
                            PayloadJson = System.Text.Json.JsonSerializer.Serialize(timetable),
                            Hash = hash,
                            FechaUltimaSincronizacion = DateTime.UtcNow,
                            FlagProcesado = false
                        });

                        Interlocked.Increment(ref totalSynced);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[A3InnuvaNominas] Error sincronizando horario de contrato para {employee.EmpleadoIdExterno}");
                }
            });

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ SyncContractTimetablesAsync completado: {totalSynced} registros");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error SyncContractTimetablesAsync: {Message}", ex.Message);
        }
    }

    /// <summary>
    /// Generar plantilla Excel A3 Innuva (ARCHIVO A3NOM.xls) para descarga manual
    /// Estructura:
    /// - Filas 1-7: Metadatos (período, empresa, tipo paga)
    /// - Fila 8: Encabezados de columnas
    /// - Filas 9+: Un empleado por fila con datos y cálculos
    /// </summary>
    public async Task<byte[]> GenerateExcelAsync(string periodCode, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation($"[A3InnuvaNominas] Generando plantilla Excel para período {periodCode}...");

            // 1. Cargar nóminas calculadas para el período
            var nominasCalculadas = await _db.StagingA3InnuvaNominasCalculadas
                .Where(n => n.CodigoPeriodo == periodCode)
                .AsNoTracking()
                .ToListAsync(ct);

            if (nominasCalculadas.Count == 0)
            {
                throw new InvalidOperationException($"No hay nóminas calculadas para el período {periodCode}");
            }

            _logger.LogInformation($"[A3InnuvaNominas] Cargadas {nominasCalculadas.Count} nóminas calculadas");

            // 2. Cargar empleados A3 por código (case-insensitive)
            var empleados = await _db.StagingA3InnuvaEmpleados
                .AsNoTracking()
                .ToDictionaryAsync(e => (e.EmpleadoIdExterno ?? "").ToUpperInvariant(), ct);

            _logger.LogInformation($"[A3InnuvaNominas] Cargados {empleados.Count} empleados A3");

            // 3. Cargar gastos PayHawk del período (KM + SUPLIDOS)
            // Resolver NIFs via Service→ServiceUsers→User (no usar UserId directo)
            var gastos = await _db.StagingPayHawkGastos
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.LogInformation($"[A3InnuvaNominas] Cargados {gastos.Count} gastos PayHawk raw");

            // Cargar Services con sus ServiceUsers para resolver NIFs
            var servicesWithUsers = await _db.Services
                .Include(s => s.ServiceUsers)
                .ThenInclude(su => su.User)
                .AsNoTracking()
                .ToListAsync(ct);

            // Map: NIF → List<gasto> (resolver via Service→ServiceUsers→User)
            var gastosByNif = new Dictionary<string, List<StagingPayHawkGasto>>();

            foreach (var gasto in gastos)
            {
                if (!gasto.ServiceId.HasValue) continue;

                var service = servicesWithUsers.FirstOrDefault(s => s.Id == gasto.ServiceId.Value);
                if (service?.ServiceUsers == null || service.ServiceUsers.Count == 0) continue;

                // Para cada usuario en el servicio, agregar el gasto (puede distribuirse entre múltiples usuarios)
                foreach (var su in service.ServiceUsers.Where(su => su.User != null && !string.IsNullOrEmpty(su.User.NIF)))
                {
                    var nif = su.User.NIF;
                    if (!gastosByNif.TryGetValue(nif, out var list))
                    {
                        list = new List<StagingPayHawkGasto>();
                        gastosByNif[nif] = list;
                    }
                    list.Add(gasto);
                }
            }

            _logger.LogInformation($"[A3InnuvaNominas] Cargados {gastosByNif.Count} NIFs únicos con gastos PayHawk");

            // 4. Crear workbook Excel
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Nómina");

            // Parsear período (formato: "2026-06" o "202606")
            var periodParts = periodCode.Replace("-", "");
            var periodDate = DateTime.TryParseExact(
                periodCode.Contains("-") ? periodCode : $"{periodCode.Substring(0, 4)}-{periodCode.Substring(4)}",
                new[] { "yyyy-MM", "yyyy-MM-dd" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsed) ? parsed : DateTime.Now;

            var diasMes = DateTime.DaysInMonth(periodDate.Year, periodDate.Month);
            var fechaCierre = new DateTime(periodDate.Year, periodDate.Month, diasMes);

            // Metadatos (Filas 1-7)
            ws.Cell(1, 1).Value = ""; // Fila vacía
            ws.Cell(2, 1).Value = "";
            ws.Cell(3, 2).Value = "Paga Mensual";
            ws.Cell(3, 12).Value = "Asiento de nomina PRUEBA"; // Col L (12)

            ws.Cell(4, 2).Value = $"Del 01/{periodDate.Month:D2}/{periodDate.Year} al {diasMes:D2}/{periodDate.Month:D2}/{periodDate.Year}";
            ws.Cell(4, 29).Value = fechaCierre.ToString("yyyy-MM-dd"); // Col AC (29)

            ws.Cell(5, 2).Value = "Empresa: 1 - SERVICE INNOVATION GROUP ESPANA SERVICIO";

            ws.Cell(6, 1).Value = "";
            ws.Cell(7, 1).Value = "";

            // Encabezados (Fila 8)
            var headers = new[] { "NIF", "Apellidos", "Nombre", "Centro", "Categoría", "Empresa", "Almacén", "Departamento",
                "", "", "Importe Bruto", "", "", "", "", "", "", "", "", "km", "SUPLIDOS", "", "", "", "", "", "Descuentos Absentismo" };

            for (int col = 1; col <= headers.Length; col++)
            {
                if (!string.IsNullOrEmpty(headers[col - 1]))
                {
                    ws.Cell(8, col).Value = headers[col - 1];
                }
            }

            // Datos empleados (Filas 9+)
            int rowIndex = 9;
            foreach (var nomina in nominasCalculadas)
            {
                empleados.TryGetValue((nomina.CodigoEmpleado ?? "").ToUpperInvariant(), out var empleado);

                // Cols A-H: Datos del empleado
                ws.Cell(rowIndex, 1).Value = empleado?.NIF ?? "";  // A: NIF
                ws.Cell(rowIndex, 2).Value = empleado?.Nombre ?? ""; // B: Apellidos (usando Nombre como fallback)
                ws.Cell(rowIndex, 3).Value = empleado?.Nombre ?? ""; // C: Nombre
                ws.Cell(rowIndex, 4).Value = ""; // D: Centro
                ws.Cell(rowIndex, 5).Value = ""; // E: Categoría
                ws.Cell(rowIndex, 6).Value = "1"; // F: Empresa (hardcoded to 1)
                ws.Cell(rowIndex, 7).Value = ""; // G: Almacén
                ws.Cell(rowIndex, 8).Value = ""; // H: Departamento

                // Col K (11): Importe Bruto (TotalPercepciones)
                ws.Cell(rowIndex, 11).Value = nomina.TotalPercepciones;

                // Col T (20): KM (calculado desde PayHawk - Categoría "Kilometraje")
                var nif = empleado?.NIF ?? "";
                var gastosEmpleado = gastosByNif.TryGetValue(nif, out var listaGastos) ? listaGastos : new List<StagingPayHawkGasto>();

                var kmGastos = gastosEmpleado
                    .Where(x => x.Categoria == "Kilometraje")
                    .Sum(x => x.Importe);
                ws.Cell(rowIndex, 20).Value = kmGastos > 0 ? kmGastos : 0;

                // Col U (21): SUPLIDOS (gastos reembolsables PayHawk - todas las categorías excepto Kilometraje)
                var suplidosGastos = gastosEmpleado
                    .Where(x => x.Categoria != "Kilometraje")
                    .Sum(x => x.Importe);
                ws.Cell(rowIndex, 21).Value = suplidosGastos > 0 ? suplidosGastos : 0;

                // Col Z (26): Descuento Absentismo (TotalDescuentos como fallback)
                ws.Cell(rowIndex, 26).Value = nomina.TotalDescuentos;

                rowIndex++;
            }

            // Ajustar ancho de columnas
            ws.Column(11).Width = 14; // Importe Bruto
            ws.Column(20).Width = 10; // KM
            ws.Column(21).Width = 14; // SUPLIDOS
            ws.Column(26).Width = 14; // Descuentos

            // Convertir a bytes y retornar
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            _logger.LogInformation($"[A3InnuvaNominas] ✅ Plantilla Excel generada correctamente para {nominasCalculadas.Count} empleados");

            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error generando plantilla Excel");
            throw;
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
