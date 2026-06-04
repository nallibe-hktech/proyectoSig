using System.Net.Http.Json;
using System.Text.Json.Serialization;
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

    public BizneoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<BizneoEmpleadosResponse>("v1/employees", ct);
            if (data?.Employees == null) return Array.Empty<BizneoEmpleadoDto>();
            return data.Employees.Select(e => new BizneoEmpleadoDto(
                e.Id,
                e.Nif,
                e.FullName,
                e.Department
            )).ToList();
        }
        catch
        {
            return Array.Empty<BizneoEmpleadoDto>();
        }
    }

    public async Task<IReadOnlyList<BizneoHoraDto>> GetHorasAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<BizneoHorasResponse>(
                $"v1/timesheets?from={desde:yyyy-MM-dd}&to={hasta:yyyy-MM-dd}", ct);
            if (data?.Timesheets == null) return Array.Empty<BizneoHoraDto>();
            return data.Timesheets.Select(h => new BizneoHoraDto(
                h.Id,
                int.TryParse(h.UserId, out var uid) ? uid : 0,
                int.TryParse(h.ProjectId, out var pid) ? pid : 0,
                DateOnly.Parse(h.Date),
                h.Hours
            )).Where(h => h.UserId > 0 && h.ProjectId > 0).ToList();
        }
        catch
        {
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

    public PayHawkClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            var data = await _httpClient.GetFromJsonAsync<PayHawkGastosResponse>(
                $"expenses?start_date={desde:yyyy-MM-dd}&end_date={hasta:yyyy-MM-dd}", ct);
            if (data?.Expenses == null) return Array.Empty<PayHawkGastoDto>();
            return data.Expenses.Select(g => new PayHawkGastoDto(
                g.Id,
                int.TryParse(g.UserId, out var uid) ? uid : 0,
                int.TryParse(g.ProjectId, out var pid) ? pid : 0,
                DateOnly.Parse(g.Date),
                g.Amount,
                g.Category ?? "Otros"
            )).Where(g => g.UserId > 0 && g.ProjectId > 0).ToList();
        }
        catch
        {
            return Array.Empty<PayHawkGastoDto>();
        }
    }

    private class PayHawkGastosResponse
    {
        [JsonPropertyName("expenses")]
        public List<PayHawkGastoResponse>? Expenses { get; set; }
    }

    private class PayHawkGastoResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
        [JsonPropertyName("user_id")]
        public string UserId { get; set; } = "";
        [JsonPropertyName("project_id")]
        public string ProjectId { get; set; } = "";
        [JsonPropertyName("date")]
        public string Date { get; set; } = "";
        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }
        [JsonPropertyName("category")]
        public string? Category { get; set; }
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
