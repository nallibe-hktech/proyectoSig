using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
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

    public async Task<IReadOnlyList<BizneoAbsenceDto>> GetAbsencesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            var url = $"absences?token={_apiKey}";
            _logger?.LogInformation($"[Bizneo] GET {_httpClient.BaseAddress}{url}");
            var data = await _httpClient.GetFromJsonAsync<BizneoAbsencesResponse>(url, ct);
            var count = data?.Absences?.Count ?? 0;
            _logger?.LogInformation($"[Bizneo] Respuesta: {count} ausencias");
            if (data?.Absences == null) return Array.Empty<BizneoAbsenceDto>();
            return data.Absences.Select(a => new BizneoAbsenceDto(
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
            return Array.Empty<BizneoAbsenceDto>();
        }
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
    private readonly int _companyId;
    private readonly string? _userEmail;
    private readonly string? _userPassword;
    private readonly ILogger<IntratimeClient> _logger;

    public IntratimeClient(HttpClient httpClient, string token, int companyId, ILogger<IntratimeClient> logger, string? userEmail = null, string? userPassword = null)
    {
        _httpClient = httpClient;
        _companyId = companyId;
        _userEmail = userEmail;
        _userPassword = userPassword;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.apiintratime.v1+json");

        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Add("token", token);
            _logger.LogInformation($"[Intratime] ✅ Token empresa configurado: {token.Substring(0, 15)}...");
        }

        if (!string.IsNullOrEmpty(userEmail) && !string.IsNullOrEmpty(userPassword))
        {
            _logger.LogInformation($"[Intratime] ✅ Credenciales usuario configuradas: {userEmail}");
        }
    }

    public async Task<IReadOnlyList<IntratimeEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct)
    {
        try
        {
            // GET /api/user/ — headers ya configurados en constructor
            var data = await _httpClient.GetFromJsonAsync<List<IntratimeUsuarioResponse>>("user/", ct);
            if (data == null) return Array.Empty<IntratimeEmpleadoDto>();
            return data
                .Where(u => !string.IsNullOrEmpty(u.UserNif))
                .Select(u => new IntratimeEmpleadoDto(
                    u.UserId.ToString(),
                    u.UserName ?? "",
                    u.UserEmail ?? "",
                    u.UserNif ?? "",
                    u.UserAffiliation,
                    int.TryParse(u.UserRole, out var role) ? role : 0
                )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Intratime] Error GetEmpleados");
            return Array.Empty<IntratimeEmpleadoDto>();
        }
    }

    public async Task<IReadOnlyList<IntratimeClockingRequestDto>> GetClockingRequestsAsync(int year, CancellationToken ct)
    {
        try
        {
            // Endpoint: /api/user/clocking_requests no disponible en esta cuenta (permisos insuficientes)
            _logger.LogWarning("[Intratime] GetClockingRequests: endpoint no disponible (permisos insuficientes en la cuenta de Intratime)");
            return Array.Empty<IntratimeClockingRequestDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Intratime] Error GetClockingRequests");
            return Array.Empty<IntratimeClockingRequestDto>();
        }
    }

    public async Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrEmpty(_userEmail) || string.IsNullOrEmpty(_userPassword))
            {
                _logger.LogWarning("[Intratime] GetFichajes: credenciales de usuario no configuradas");
                return Array.Empty<IntratimeFichajeDto>();
            }

            _logger.LogInformation("[Intratime] GetFichajes: autenticando usuario...");

            // 1. LOGIN para obtener token de usuario
            var loginUrl = "https://newapi.intratime.es/api/companies/login";
            var loginBody = new StringContent(
                $"user={Uri.EscapeDataString(_userEmail)}&password={Uri.EscapeDataString(_userPassword)}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
            );

            var loginResponse = await _httpClient.PostAsync(loginUrl, loginBody, ct);
            if (!loginResponse.IsSuccessStatusCode)
            {
                _logger.LogError($"[Intratime] Login falló: {loginResponse.StatusCode}");
                return Array.Empty<IntratimeFichajeDto>();
            }

            var loginJson = await loginResponse.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(loginJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("users", out var usersArray) || usersArray.GetArrayLength() == 0)
            {
                _logger.LogWarning("[Intratime] Login exitoso pero sin usuarios en respuesta");
                return Array.Empty<IntratimeFichajeDto>();
            }

            // 2. ITERAR SOBRE TODOS LOS USUARIOS Y OBTENER SUS FICHAJES
            var allClockings = new List<IntratimeClockingEventDto>();
            var clockingsUrl = "https://newapi.intratime.es/api/user/clockings";
            var fromDateTime = desde.ToDateTime(TimeOnly.MinValue);
            var fromEncoded = Uri.EscapeDataString(fromDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
            var clockingsUrlWithParams = $"{clockingsUrl}?from={fromEncoded}&type=0,1,2,3";

            _logger.LogInformation($"[Intratime] Iterando sobre {usersArray.GetArrayLength()} usuarios...");

            for (int i = 0; i < usersArray.GetArrayLength(); i++)
            {
                var user = usersArray[i];
                if (!user.TryGetProperty("USER_TOKEN", out var tokenElem))
                    continue;

                var userToken = tokenElem.GetString();
                if (string.IsNullOrEmpty(userToken))
                    continue;

                try
                {
                    var clockingsRequest = new HttpRequestMessage(HttpMethod.Get, clockingsUrlWithParams);
                    clockingsRequest.Headers.Add("Accept", "application/vnd.apiintratime.v1+json");
                    clockingsRequest.Headers.Add("token", userToken);

                    var clockingsResponse = await _httpClient.SendAsync(clockingsRequest, ct);
                    if (!clockingsResponse.IsSuccessStatusCode)
                        continue;

                    var clockingsJson = await clockingsResponse.Content.ReadAsStringAsync(ct);
                    var clockingsArray = JsonSerializer.Deserialize<List<IntratimeClockingEventDto>>(clockingsJson);

                    if (clockingsArray != null && clockingsArray.Count > 0)
                    {
                        allClockings.AddRange(clockingsArray);
                        _logger.LogInformation($"[Intratime] Usuario {i}: {clockingsArray.Count} eventos obtenidos");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Intratime] Error obteniendo fichajes para usuario {i}: {ex.Message}");
                }
            }

            if (allClockings.Count == 0)
            {
                _logger.LogWarning($"[Intratime] GetFichajes: 0 eventos obtenidos después de iterar {usersArray.GetArrayLength()} usuarios");
                return Array.Empty<IntratimeFichajeDto>();
            }

            var clockingsArray2 = allClockings;
            _logger.LogInformation($"[Intratime] ✅ Total {clockingsArray2.Count} eventos obtenidos de todos los usuarios");

            // DEBUG: Log primer evento para ver estructura
            if (clockingsArray2.Count > 0)
            {
                var first = clockingsArray2[0];
                _logger.LogInformation($"[Intratime] SAMPLE EVENT: ID={first.INOUT_ID}, USER_ID={first.INOUT_USER_ID}, TYPE={first.INOUT_TYPE}, DATE={first.INOUT_DATE}");
            }

            // 3. AGRUPAR POR USUARIO+DÍA Y EMPAREJAR ENTRADA/SALIDA
            var filtered = clockingsArray2
                .Where(c => c.INOUT_USER_ID.HasValue && !string.IsNullOrEmpty(c.INOUT_DATE))
                .ToList();
            _logger.LogInformation($"[Intratime] Después de filtrar: {filtered.Count} eventos válidos");

            var grouped = filtered
                .GroupBy(c => (UserId: c.INOUT_USER_ID!.Value, Dia: DateOnly.FromDateTime(DateTime.Parse(c.INOUT_DATE))))
                .ToList();
            _logger.LogInformation($"[Intratime] Después de agrupar: {grouped.Count} grupos usuario+día");

            var fichajes = new List<IntratimeFichajeDto>();
            foreach (var g in grouped)
            {
                try
                {
                    var ordered = g.OrderBy(c => DateTime.Parse(c.INOUT_DATE)).ToList();
                    if (ordered.Count == 0) continue;

                    var entrada = ordered.FirstOrDefault(c => c.INOUT_TYPE == 0) ?? ordered.First();
                    var salida = ordered.LastOrDefault(c => c.INOUT_TYPE == 1);

                    fichajes.Add(new IntratimeFichajeDto(
                        entrada.INOUT_ID?.ToString() ?? "",
                        entrada.INOUT_USER_ID!.Value.ToString(),
                        DateTime.Parse(entrada.INOUT_DATE),
                        salida != null ? DateTime.Parse(salida.INOUT_DATE) : null
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[Intratime] Error procesando grupo usuario {g.Key.UserId}: {ex.Message}");
                }
            }

            _logger.LogInformation($"[Intratime] ✅ Fichajes finales creados: {fichajes.Count}");
            return fichajes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Intratime] Error GetFichajes: {Message}", ex.Message);
            return Array.Empty<IntratimeFichajeDto>();
        }
    }

    private class IntratimeClockingEventDto
    {
        [JsonPropertyName("INOUT_ID")]
        public int? INOUT_ID { get; set; }

        [JsonPropertyName("INOUT_USER_ID")]
        public int? INOUT_USER_ID { get; set; }

        [JsonPropertyName("INOUT_TYPE")]
        public int INOUT_TYPE { get; set; }

        [JsonPropertyName("INOUT_DATE")]
        public string? INOUT_DATE { get; set; }
    }

    public async Task<IReadOnlyList<IntratimeExpenseDto>> GetExpensesAsync(DateOnly desde, DateOnly hasta, CancellationToken ct)
    {
        try
        {
            // Endpoint: /api/expenses no disponible en esta cuenta (permisos insuficientes)
            _logger.LogWarning("[Intratime] GetExpenses: endpoint no disponible (permisos insuficientes en la cuenta de Intratime)");
            return Array.Empty<IntratimeExpenseDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Intratime] Error GetExpenses");
            return Array.Empty<IntratimeExpenseDto>();
        }
    }

    private class IntratimeClockingDto
    {
        [JsonPropertyName("CLOCKING_ID")]
        public string CLOCKING_ID { get; set; } = "";
        [JsonPropertyName("USER_ID")]
        public int? USER_ID { get; set; }
        [JsonPropertyName("CLOCKING_TIME")]
        public string? CLOCKING_TIME { get; set; }
        [JsonPropertyName("CLOCKING_DEVICE")]
        public string? CLOCKING_DEVICE { get; set; }
        [JsonPropertyName("CLOCKING_IN_OUT")]
        public int? CLOCKING_IN_OUT { get; set; }  // 0=entrada, 1=salida
    }

    private class IntratimeClockingRequestResponse
    {
        [JsonPropertyName("REQUEST_ID")]
        public string REQUEST_ID { get; set; } = "";
        [JsonPropertyName("USER_ID")]
        public int? USER_ID { get; set; }
        [JsonPropertyName("REQUEST_DATE")]
        public string? REQUEST_DATE { get; set; }
        [JsonPropertyName("REQUEST_TYPE")]
        public string? REQUEST_TYPE { get; set; }
        [JsonPropertyName("REQUEST_STATUS")]
        public string? REQUEST_STATUS { get; set; }
        [JsonPropertyName("REQUEST_REASON")]
        public string? REQUEST_REASON { get; set; }
        [JsonPropertyName("REQUESTED_TIME_FROM")]
        public string? REQUESTED_TIME_FROM { get; set; }
        [JsonPropertyName("REQUESTED_TIME_TO")]
        public string? REQUESTED_TIME_TO { get; set; }
    }

    private class IntratimeUsuarioResponse
    {
        [JsonPropertyName("USER_ID")]          public int UserId { get; set; }
        [JsonPropertyName("USER_NAME")]        public string? UserName { get; set; }
        [JsonPropertyName("USER_EMAIL")]       public string? UserEmail { get; set; }
        [JsonPropertyName("USER_NIF")]         public string? UserNif { get; set; }
        [JsonPropertyName("USER_AFFILIATION")] public string? UserAffiliation { get; set; }
        [JsonPropertyName("USER_ROLE")]        public string? UserRole { get; set; }
    }

    private class IntratimeExpenseResponse
    {
        [JsonPropertyName("INOUT_EXPENSE_ID")]   public int? INOUT_EXPENSE_ID { get; set; }
        [JsonPropertyName("INOUT_USER_ID")]      public int? INOUT_USER_ID { get; set; }
        [JsonPropertyName("INOUT_DATE")]         public string? INOUT_DATE { get; set; }
        [JsonPropertyName("INOUT_AMOUNT")]       public int? INOUT_AMOUNT { get; set; }  // En centavos
        [JsonPropertyName("INOUT_EXPENSE_NAME")] public string? INOUT_EXPENSE_NAME { get; set; }
        [JsonPropertyName("INOUT_COMMENTS")]     public string? INOUT_COMMENTS { get; set; }
        [JsonPropertyName("INOUT_PROJECT_NAME")] public string? INOUT_PROJECT_NAME { get; set; }
    }

    public async Task<IReadOnlyList<IntratimeProyectoDto>> GetProyectosAsync(CancellationToken ct)
    {
        try
        {
            // Endpoint: /api/project/ (proyectos con clientes y usuarios asignados)
            var data = await _httpClient.GetFromJsonAsync<List<IntratimeProyectoResponse>>("project/", ct);
            if (data == null) return Array.Empty<IntratimeProyectoDto>();

            return data.Select(p => new IntratimeProyectoDto(
                p.PROJECT_ID?.ToString() ?? "",
                p.PROJECT_NAME ?? "",
                p.client != null ? new IntratimeClienteDto(
                    p.client.CLIENT_ID?.ToString() ?? "",
                    p.client.CLIENT_NAME ?? "",
                    p.client.CLIENT_COUNTRY,
                    p.client.CLIENT_REGION,
                    p.client.CLIENT_CITY,
                    p.client.CLIENT_ADDRESS
                ) : new IntratimeClienteDto("", "", null, null, null, null),
                p.users?.Select(u => u.USER_ID?.ToString() ?? "").Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new List<string>()
            )).Where(p => !string.IsNullOrEmpty(p.ProyectoIdExterno)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Intratime] Error GetProyectos");
            return Array.Empty<IntratimeProyectoDto>();
        }
    }

    private class IntratimeProyectoResponse
    {
        [JsonPropertyName("PROJECT_ID")]      public int? PROJECT_ID { get; set; }
        [JsonPropertyName("PROJECT_NAME")]    public string? PROJECT_NAME { get; set; }
        [JsonPropertyName("PROJECT_COMPANY")] public string? PROJECT_COMPANY { get; set; }
        [JsonPropertyName("client")]          public IntratimeClientResponse? client { get; set; }
        [JsonPropertyName("users")]           public List<IntratimeUsuarioProyectoResponse>? users { get; set; }
    }

    private class IntratimeClientResponse
    {
        [JsonPropertyName("CLIENT_ID")]       public int? CLIENT_ID { get; set; }
        [JsonPropertyName("CLIENT_NAME")]     public string? CLIENT_NAME { get; set; }
        [JsonPropertyName("CLIENT_COUNTRY")]  public string? CLIENT_COUNTRY { get; set; }
        [JsonPropertyName("CLIENT_REGION")]   public string? CLIENT_REGION { get; set; }
        [JsonPropertyName("CLIENT_CITY")]     public string? CLIENT_CITY { get; set; }
        [JsonPropertyName("CLIENT_ADDRESS")]  public string? CLIENT_ADDRESS { get; set; }
    }

    private class IntratimeUsuarioProyectoResponse
    {
        [JsonPropertyName("USER_ID")]   public int? USER_ID { get; set; }
        [JsonPropertyName("USER_NAME")] public string? USER_NAME { get; set; }
        [JsonPropertyName("USER_EMAIL")] public string? USER_EMAIL { get; set; }
        [JsonPropertyName("USER_NIF")]  public string? USER_NIF { get; set; }
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
                .Where(g => g.UserId > 0 && g.ServiceId > 0)
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

        [JsonPropertyName("category")]
        public PayHawkCategoryResponse? Category { get; set; }

        [JsonPropertyName("reconciliation")]
        public PayHawkReconciliationResponse? Reconciliation { get; set; }
    }

    private class PayHawkUserResponse
    {
        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }
    }

    private class PayHawkCategoryResponse
    {
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

            // Timeout de 240 segundos para SGPV (servidor puede tardar 3-4 minutos)
            _httpClient.Timeout = TimeSpan.FromSeconds(240);

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

            // Timeout de 240 segundos para SGPV (servidor puede tardar 3-4 minutos)
            _httpClient.Timeout = TimeSpan.FromSeconds(240);

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
