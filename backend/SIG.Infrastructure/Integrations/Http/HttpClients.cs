using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Http;

public class CeleroClient : ICeleroClient
{
    public Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<CeleroVisitaDto>>(Array.Empty<CeleroVisitaDto>());
}

public class BizneoClient : IBizneoClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<BizneoClient>? _logger;
    private readonly string _apiKey;

    public BizneoClient(HttpClient httpClient, string apiKey, ILogger<BizneoClient>? logger = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        try
        {
            var url = $"users?token={_apiKey}";
            _logger?.LogInformation($"[Bizneo] GET {_httpClient.BaseAddress}{url}");
            var data = await _httpClient.GetFromJsonAsync<BizneoUsersResponse>(url, ct);
            var count = data?.Users?.Count ?? 0;
            _logger?.LogInformation($"[Bizneo] Respuesta: {count} usuarios");
            if (data?.Users == null) return Array.Empty<BizneoEmpleadoDto>();
            return data.Users.Select(u => new BizneoEmpleadoDto(
                u.Id?.ToString() ?? "",
                u.Email ?? "",
                $"{u.FirstName} {u.LastName}",
                u.Department ?? ""
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"[Bizneo] Error: {ex.Message}");
            return Array.Empty<BizneoEmpleadoDto>();
        }
    }

    public async Task<IReadOnlyList<BizneoHoraDto>> GetHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            var url = $"absences?token={_apiKey}";
            _logger?.LogInformation($"[Bizneo] GET {_httpClient.BaseAddress}{url}");
            var data = await _httpClient.GetFromJsonAsync<BizneoAbsencesResponse>(url, ct);
            var count = data?.Absences?.Count ?? 0;
            _logger?.LogInformation($"[Bizneo] Respuesta: {count} ausencias");
            // Mapper simple: convertir absencias a horas (mismo DTO por ahora)
            if (data?.Absences == null) return Array.Empty<BizneoHoraDto>();
            return data.Absences.Select(a => new BizneoHoraDto(
                a.Id?.ToString() ?? "",
                a.UserId,
                0,
                DateOnly.FromDateTime(a.StartAt),
                0
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger?.LogError($"[Bizneo] Error: {ex.Message}");
            return Array.Empty<BizneoHoraDto>();
        }
    }

    private class BizneoEmpleadosResponse
    {
        [JsonPropertyName("employees")]
        public List<BizneoEmpleadoResponse>? Employees { get; set; }
    }

    private class BizneoEmpleadoResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("nif")]
        public string Nif { get; set; } = "";
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = "";
        [JsonPropertyName("department")]
        public string? Department { get; set; }
    }

    private class BizneoHorasResponse
    {
        [JsonPropertyName("timesheets")]
        public List<BizneoHoraResponse>? Timesheets { get; set; }
    }

    private class BizneoHoraResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = "";
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = "";
        [JsonPropertyName("date")]
        public string Date { get; set; } = "";
        [JsonPropertyName("hours")]
        public decimal Hours { get; set; }
    }

    private class BizneoUsersResponse
    {
        [JsonPropertyName("users")]
        public List<BizneoUserResponse>? Users { get; set; }
    }

    private class BizneoUserResponse
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }
        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }
        [JsonPropertyName("department")]
        public string? Department { get; set; }
    }

    private class BizneoAbsencesResponse
    {
        [JsonPropertyName("absences")]
        public List<BizneoAbsenceResponse>? Absences { get; set; }
    }

    private class BizneoAbsenceResponse
    {
        [JsonPropertyName("id")]
        public int? Id { get; set; }
        [JsonPropertyName("user_id")]
        public int UserId { get; set; }
        [JsonPropertyName("start_at")]
        public DateTime StartAt { get; set; }
        [JsonPropertyName("end_at")]
        public DateTime EndAt { get; set; }
        [JsonPropertyName("state")]
        public string? State { get; set; }
    }
}

public class IntratimeClient : IIntratimeClient
{
    private readonly HttpClient _httpClient;
    private readonly string _token;
    private readonly int _companyId;

    public IntratimeClient(HttpClient httpClient, string token, int companyId)
    {
        _httpClient = httpClient;
        _token = token;
        _companyId = companyId;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    }

    public async Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<IntratimeFichajesResponse>(
                $"v1/companies/{_companyId}/fiches?start_date={desde:yyyy-MM-dd}&end_date={hasta:yyyy-MM-dd}", ct);
            if (data?.Fiches == null) return Array.Empty<IntratimeFichajeDto>();
            return data.Fiches.Select(f => new IntratimeFichajeDto(
                f.Id,
                int.TryParse(f.UserId, out var uid) ? uid : 0,
                DateTime.Parse(f.StartTime),
                f.EndTime != null ? DateTime.Parse(f.EndTime) : null
            )).Where(f => f.UserId > 0).ToList();
        }
        catch
        {
            return Array.Empty<IntratimeFichajeDto>();
        }
    }

    private class IntratimeFichajesResponse
    {
        [JsonPropertyName("fiches")]
        public List<IntratimeFichajeResponse>? Fiches { get; set; }
    }

    private class IntratimeFichajeResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = "";
        [JsonPropertyName("start_time")]
        public string StartTime { get; set; } = "";
        [JsonPropertyName("end_time")]
        public string? EndTime { get; set; }
    }
}

public class PayHawkClient : IPayHawkClient
{
    private readonly HttpClient _httpClient;
    private readonly string _accountId;
    private readonly ILogger<PayHawkClient> _logger;

    public PayHawkClient(HttpClient httpClient, string accountId, ILogger<PayHawkClient> logger)
    {
        _httpClient = httpClient;
        _accountId = accountId;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation($"[PayHawk] Sincronizando gastos desde {desde:yyyy-MM-dd} hasta {hasta:yyyy-MM-dd}");

            // PayHawk API v3: /accounts/{accountId}/expenses (sin filtros, filtramos después en memoria)
            var url = $"accounts/{_accountId}/expenses";
            _logger.LogInformation($"[PayHawk] GET {_httpClient.BaseAddress}{url}");
            var data = await _httpClient.GetFromJsonAsync<PayHawkGastosResponse>(url, ct);
            var count = data?.Items?.Count ?? 0;
            _logger.LogInformation($"[PayHawk] Respuesta: {count} gastos (total en API: {data?.Total})");
            if (data?.Items == null) return Array.Empty<PayHawkGastoDto>();

            // DEBUG: Mostrar primeros gastos sin filtrar
            var allGastos = data.Items.Take(3).ToList();
            foreach (var g in allGastos)
            {
                var uid = g.CreatedBy?.ExternalId ?? "EMPTY";
                var projectField = g.Reconciliation?.CustomFields?.FirstOrDefault(cf => cf.Id == "proyecto_37e79a");
                var pid = projectField?.SelectedValues?.FirstOrDefault()?.ExternalId ?? "EMPTY";
                var gastoDate = DateOnly.FromDateTime(DateTime.Parse(g.CreatedAt));
                var isInRange = gastoDate >= desde && gastoDate <= hasta;
                _logger.LogWarning($"[PayHawk DEBUG] Gasto {g.Id}: Fecha={gastoDate} (en rango={isInRange}), Empleado={g.CreatedBy?.Email} ExternalId={uid}, Proyecto={pid}, Importe={g.Reconciliation?.TotalAmount}");
            }

            var gastos = data.Items
                // .Where(g =>
                // {
                //     var gastoDate = DateOnly.FromDateTime(DateTime.Parse(g.CreatedAt));
                //     return gastoDate >= desde && gastoDate <= hasta;
                // })
                .Select(g =>
                {
                    // ExternalId puede ser "44175805G" (NIF) — extraer solo números
                    var externalIdStr = g.CreatedBy?.ExternalId ?? "";
                    var numericExternalId = new string(externalIdStr.Where(char.IsDigit).ToArray());
                    var uid = int.TryParse(numericExternalId, out var uidVal) ? uidVal : 0;

                    var pid = int.TryParse(g.Reconciliation?.CustomFields
                        ?.FirstOrDefault(cf => cf.Id == "proyecto_37e79a")
                        ?.SelectedValues?.FirstOrDefault()?.ExternalId ?? "", out var pidVal) ? pidVal : 0;

                    return new PayHawkGastoDto(
                        g.Id,
                        uid,
                        pid,
                        DateOnly.FromDateTime(DateTime.Parse(g.CreatedAt)),
                        g.Reconciliation?.TotalAmount ?? 0,
                        g.Category?.Name ?? "Otros"
                    );
                })
                .Where(g => g.UserId > 0 && g.ProjectId > 0)
                .ToList();

            _logger.LogInformation($"[PayHawk] Después de filtrar: {gastos.Count} gastos válidos (de {count})");
            return gastos;
        }
        catch (Exception ex)
        {
            _logger.LogError($"[PayHawk] Error: {ex.Message}");
            return Array.Empty<PayHawkGastoDto>();
        }
    }

    private class PayHawkGastosResponse
    {
        [JsonPropertyName("items")]
        public List<PayHawkGastoResponse>? Items { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    private class PayHawkGastoResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("createdBy")]
        public PayHawkUserResponse? CreatedBy { get; set; }

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = "";

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("isPaid")]
        public bool IsPaid { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("category")]
        public PayHawkCategoryResponse? Category { get; set; }

        [JsonPropertyName("reconciliation")]
        public PayHawkReconciliationResponse? Reconciliation { get; set; }
    }

    private class PayHawkUserResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
    }

    private class PayHawkCategoryResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class PayHawkReconciliationResponse
    {
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("customFields")]
        public List<PayHawkCustomFieldResponse>? CustomFields { get; set; }
    }

    private class PayHawkCustomFieldResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("selectedValues")]
        public List<PayHawkSelectedValueResponse>? SelectedValues { get; set; }
    }

    private class PayHawkSelectedValueResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
    }
}

public class SgpvClient : ISgpvClient
{
    private readonly HttpClient _httpClient;
    private readonly string _username;
    private readonly string _password;

    public SgpvClient(HttpClient httpClient, string username, string password)
    {
        _httpClient = httpClient;
        _username = username;
        _password = password;
    }

    public async Task<IReadOnlyList<SgpvVisitaDto>> GetVisitasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            var request = new HttpRequestMessage(HttpMethod.Get, $"ExportData.php?start={desde:yyyy-MM-dd}&end={hasta:yyyy-MM-dd}");
            request.Headers.Add("Authorization", $"Basic {auth}");

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<SgpvVisitaDto>();

            var data = await response.Content.ReadFromJsonAsync<SgpvVisitasResponse>(ct);
            if (data?.Visitas == null) return Array.Empty<SgpvVisitaDto>();
            return data.Visitas.Select(v => new SgpvVisitaDto(
                v.Id,
                v.ResourceNif,
                v.CentroId,
                v.CentroNombre,
                v.ServiceName,
                DateOnly.Parse(v.Fecha),
                v.HorasDuracion
            )).ToList();
        }
        catch
        {
            return Array.Empty<SgpvVisitaDto>();
        }
    }

    private class SgpvVisitasResponse
    {
        [JsonPropertyName("visitas")]
        public List<SgpvVisitaResponse>? Visitas { get; set; }
    }

    private class SgpvVisitaResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("resource_nif")]
        public string ResourceNif { get; set; } = "";
        [JsonPropertyName("centro_id")]
        public string CentroId { get; set; } = "";
        [JsonPropertyName("centro_nombre")]
        public string? CentroNombre { get; set; }
        [JsonPropertyName("service_name")]
        public string? ServiceName { get; set; }
        [JsonPropertyName("fecha")]
        public string Fecha { get; set; } = "";
        [JsonPropertyName("horas_duracion")]
        public decimal? HorasDuracion { get; set; }
    }

    public async Task<IReadOnlyList<SgpvProductoDto>> GetProductosAsync(CancellationToken ct)
    {
        try
        {
            var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{_username}:{_password}"));
            var request = new HttpRequestMessage(HttpMethod.Get, "ExportData.php");
            request.Headers.Add("Authorization", $"Basic {auth}");

            // Timeout de 120 segundos para SGPV (servidor lento)
            _httpClient.Timeout = TimeSpan.FromSeconds(120);

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return Array.Empty<SgpvProductoDto>();

            var data = await response.Content.ReadFromJsonAsync<SgpvProductosResponse>(ct);
            if (data?.Export?.Productos == null) return Array.Empty<SgpvProductoDto>();

            return data.Export.Productos.Select(p => new SgpvProductoDto(
                p.IdProducto ?? "",
                p.IdCliente ?? "",
                p.Cliente ?? "",
                p.Categoria ?? "",
                p.Subcategoria ?? "",
                p.CodigoReferencia ?? "",
                p.Referencia ?? "",
                p.EAN ?? "",
                p.Marca ?? "",
                p.PVPRecomendado ?? "0",
                p.Competencia ?? "No",
                p.Activo == "1"
            )).ToList();
        }
        catch
        {
            return Array.Empty<SgpvProductoDto>();
        }
    }

    private class SgpvProductosResponse
    {
        [JsonPropertyName("export")]
        public SgpvExportData? Export { get; set; }
    }

    private class SgpvExportData
    {
        [JsonPropertyName("ET_Referencias")]
        public List<SgpvProductoResponse>? Productos { get; set; }
    }

    private class SgpvProductoResponse
    {
        [JsonPropertyName("idProducto")]
        public string? IdProducto { get; set; }
        [JsonPropertyName("idCliente")]
        public string? IdCliente { get; set; }
        [JsonPropertyName("Cliente")]
        public string? Cliente { get; set; }
        [JsonPropertyName("Categoria")]
        public string? Categoria { get; set; }
        [JsonPropertyName("Subcategoria")]
        public string? Subcategoria { get; set; }
        [JsonPropertyName("CodigoReferencia")]
        public string? CodigoReferencia { get; set; }
        [JsonPropertyName("Referencia")]
        public string? Referencia { get; set; }
        [JsonPropertyName("EAN")]
        public string? EAN { get; set; }
        [JsonPropertyName("Marca")]
        public string? Marca { get; set; }
        [JsonPropertyName("PVPRecomendado")]
        public string? PVPRecomendado { get; set; }
        [JsonPropertyName("Competencia")]
        public string? Competencia { get; set; }
        [JsonPropertyName("activo")]
        public string? Activo { get; set; }
    }
}

public class A3InnuvaClient : IA3InnuvaClient
{
    private readonly HttpClient _httpClient;

    public A3InnuvaClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<IReadOnlyList<A3InnuvaEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<A3InnuvaEmpleadosResponse>("nomina/employees", ct);
            if (data?.Employees == null) return Array.Empty<A3InnuvaEmpleadoDto>();
            return data.Employees.Select(e => new A3InnuvaEmpleadoDto(
                e.Id,
                e.Nif,
                e.FullName,
                e.Department,
                e.MonthlySalary
            )).ToList();
        }
        catch
        {
            return Array.Empty<A3InnuvaEmpleadoDto>();
        }
    }

    private class A3InnuvaEmpleadosResponse
    {
        [JsonPropertyName("employees")]
        public List<A3InnuvaEmpleadoResponse>? Employees { get; set; }
    }

    private class A3InnuvaEmpleadoResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("nif")]
        public string Nif { get; set; } = "";
        [JsonPropertyName("full_name")]
        public string FullName { get; set; } = "";
        [JsonPropertyName("department")]
        public string? Department { get; set; }
        [JsonPropertyName("monthly_salary")]
        public decimal? MonthlySalary { get; set; }
    }
}

public class TravelPerkClient : ITravelPerkClient
{
    private readonly HttpClient _httpClient;

    public TravelPerkClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<IReadOnlyList<TravelPerkViajeDto>> GetViajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<TravelPerkViajesResponse>(
                $"trips?start_date={desde:yyyy-MM-dd}&end_date={hasta:yyyy-MM-dd}", ct);
            if (data?.Trips == null) return Array.Empty<TravelPerkViajeDto>();
            return data.Trips.Select(v => new TravelPerkViajeDto(
                v.Id,
                v.Requester,
                DateOnly.Parse(v.StartDate),
                v.EndDate != null ? DateOnly.Parse(v.EndDate) : null,
                v.Budget,
                v.Status ?? "pending"
            )).ToList();
        }
        catch
        {
            return Array.Empty<TravelPerkViajeDto>();
        }
    }

    private class TravelPerkViajesResponse
    {
        [JsonPropertyName("trips")]
        public List<TravelPerkViajeResponse>? Trips { get; set; }
    }

    private class TravelPerkViajeResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("requester")]
        public string Requester { get; set; } = "";
        [JsonPropertyName("start_date")]
        public string StartDate { get; set; } = "";
        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }
        [JsonPropertyName("budget")]
        public decimal Budget { get; set; }
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }
}
