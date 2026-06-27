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

            // Si el AccountId no está configurado (placeholder/vacío), no llamar a la API: evita un 400 garantizado
            if (string.IsNullOrWhiteSpace(_accountId) || _accountId.Contains("__SET_VIA_ENVIRONMENT__"))
            {
                _logger.LogWarning("[PayHawk] AccountId no configurado ('{AccountId}'). Se omite la sincronización de gastos.", _accountId);
                return Array.Empty<PayHawkGastoDto>();
            }

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
                e.MonthlySalary,
                DateTime.UtcNow
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

public class A3InnuvaNominasClient : IA3InnuvaNominasClient
{
    private readonly HttpClient _httpClient;
    private readonly IWoltersKluwerOAuthService _oauthService;
    private readonly string _subscriptionKey;
    private readonly ILogger<A3InnuvaNominasClient> _logger;
    private readonly bool _useFakeData;

    public A3InnuvaNominasClient(
        HttpClient httpClient,
        IWoltersKluwerOAuthService oauthService,
        string subscriptionKey,
        ILogger<A3InnuvaNominasClient> logger,
        bool useFakeData = false)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
        _subscriptionKey = subscriptionKey ?? throw new ArgumentNullException(nameof(subscriptionKey));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _useFakeData = useFakeData;
    }

    public async Task<IReadOnlyList<A3InnuvaNominasCompanyDto>> GetCompaniesAsync(
        int pageNumber = 1,
        int pageSize = 25,
        DateTime? lastUpdate = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetCompaniesAsync iniciado - página {Page}, tamaño {Size}", pageNumber, pageSize);
            var token = await _oauthService.GetAccessTokenAsync(ct);
            _logger.LogInformation("[A3InnuvaNominas] Token obtenido: {TokenLength} caracteres", token?.Length ?? 0);

            // Wolters Kluwer solo tiene acceso a empresa con código 1
            // El endpoint GET /companies no está disponible (403 Forbidden)
            // Retornar empresa con código 1 directamente
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Retornando empresa por defecto (código 1)");

            var company = new A3InnuvaNominasCompanyDto(
                Id: "1",
                Code: "1",
                Name: "SERVICE INNOVATIVO GROUP ESPAÑA",
                TaxId: "2Q4YX",
                Address: "Madrid",
                City: "Madrid",
                Country: "España",
                ContactEmail: "plataforma.sig@sigespana.es",
                ContactPhone: ""
            );

            return new[] { company };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetCompanies: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return Array.Empty<A3InnuvaNominasCompanyDto>();
        }
    }

    public async Task<IReadOnlyList<A3InnuvaNominasPayrollDto>> GetPayrollsAsync(
        string companyCode,
        int pageNumber = 1,
        int pageSize = 25,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetPayrollsAsync iniciado - empresa {Company}, página {Page}, tamaño {Size}", companyCode, pageNumber, pageSize);

            if (string.IsNullOrWhiteSpace(companyCode))
                throw new ArgumentNullException(nameof(companyCode));

            var token = await _oauthService.GetAccessTokenAsync(ct);

            // Construir URL con paginación (obligatoria en WK API)
            var queryParams = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            if (fromDate.HasValue)
                queryParams += $"&fromDate={fromDate:yyyy-MM-dd}";
            if (toDate.HasValue)
                queryParams += $"&toDate={toDate:yyyy-MM-dd}";

            var url = $"Laboral/api/companies/{companyCode}/payrolls{queryParams}";
            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[A3InnuvaNominas] ❌ Error {response.StatusCode}: {responseContent}");
                return Array.Empty<A3InnuvaNominasPayrollDto>();
            }

            // WK devuelve array directo de nóminas: [{...}, {...}]
            var responseBody = await response.Content.ReadAsStringAsync(ct);
            var data = JsonSerializer.Deserialize<List<PayrollResponse>>(responseBody);
            if (data == null || data.Count == 0)
            {
                _logger.LogWarning("[A3InnuvaNominas] Respuesta de nóminas es null o vacía");
                return Array.Empty<A3InnuvaNominasPayrollDto>();
            }

            var count = data.Count;
            _logger.LogInformation($"[A3InnuvaNominas] ✅ {count} nóminas obtenidas");

            return data.Select(p => new A3InnuvaNominasPayrollDto(
                p.Id ?? "",
                p.EmployeeId ?? "",
                p.EmployeeName ?? "",
                p.PeriodCode ?? DateTime.UtcNow.Year.ToString(),
                p.BaseSalary ?? 0,
                p.Deductions ?? 0,
                p.NetSalary ?? 0,
                p.ProcessDate ?? DateTime.UtcNow
            )).ToList();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] HttpRequestException en GetPayrolls: {Message}", ex.Message);
            return Array.Empty<A3InnuvaNominasPayrollDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetPayrolls: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return Array.Empty<A3InnuvaNominasPayrollDto>();
        }
    }

    public async Task<IReadOnlyList<EmployeeDto>> GetEmployeesAsync(
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetEmployeesAsync iniciado - página {Page}, tamaño {Size}", pageNumber, pageSize);

            // Retornar datos fake si UseFake está activo
            if (_useFakeData)
            {
                var fakeEmployees = new List<EmployeeDto>
                {
                    new EmployeeDto("EMP001", "0001", "Juan García López", "12345678A", "CTR001", DateTime.Now.AddYears(-5)),
                    new EmployeeDto("EMP002", "0002", "María Rodríguez Martínez", "12345678B", "CTR001", DateTime.Now.AddYears(-3)),
                    new EmployeeDto("EMP003", "0003", "Carlos López Pérez", "12345678C", "CTR001", DateTime.Now.AddYears(-2)),
                    new EmployeeDto("EMP004", "0004", "Ana Fernández Silva", "12345678D", "CTR002", DateTime.Now.AddYears(-4)),
                    new EmployeeDto("EMP005", "0005", "Pedro Jiménez González", "12345678E", "CTR002", DateTime.Now.AddYears(-1)),
                };

                return fakeEmployees.Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
            }

            var token = await _oauthService.GetAccessTokenAsync(ct);

            // WK requiere paginación explícita
            var queryParams = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            var url = $"Laboral/api/companies/1/employees{queryParams}";
            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[A3InnuvaNominas] ❌ Error {response.StatusCode}: {responseContent}");
                return Array.Empty<EmployeeDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<List<EmployeeResponse>>(cancellationToken: ct);
            var count = data?.Count ?? 0;
            _logger.LogInformation($"[A3InnuvaNominas] ✅ {count} empleados obtenidos");

            if (data == null) return Array.Empty<EmployeeDto>();

            return data.Select(e => new EmployeeDto(
                e.EmployeeId ?? "",
                e.EmployeeCode ?? "",
                e.CompleteName ?? "",
                e.IdentifierNumber ?? "",
                e.WorkplaceCode?.ToString(),
                e.EnrolmentDate
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetEmployees: {Message}", ex.Message);
            return Array.Empty<EmployeeDto>();
        }
    }

    public async Task<IReadOnlyList<ConceptoDto>> GetConceptosAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        // Retry logic: hasta 3 intentos con backoff exponencial en caso de timeout
        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("[A3InnuvaNominas] GetConceptosAsync iniciado - empleado {Code}, página {Page}, intento {Attempt}/{MaxRetries}",
                    employeeCode, pageNumber, attempt, maxRetries);

                // MODO FAKE: Retornar conceptos simulados (limitado a 1 página = 5 conceptos)
                if (_useFakeData)
                {
                    _logger.LogInformation("[A3InnuvaNominas] Usando conceptos FAKE para empleado {Code}", employeeCode);

                    var fakeConceptos = new List<ConceptoDto>
                    {
                        new ConceptoDto(001, "Salario Base", 2500m, "E", false, false, "Percepciones"),
                        new ConceptoDto(002, "Complemento Antigüedad", 300m, "E", false, false, "Percepciones"),
                        new ConceptoDto(003, "IRPF", -400m, "D", false, false, "Descuentos"),
                        new ConceptoDto(004, "Seguridad Social", -250m, "D", false, false, "Descuentos"),
                        new ConceptoDto(005, "Bono Desempeño", 500m, "E", false, false, "Percepciones"),
                    };

                    // Paginación finita: solo página 1 tiene datos, página 2+ retorna vacío
                    if (pageNumber > 1)
                    {
                        _logger.LogInformation("[A3InnuvaNominas] Página {Page} > 1, retornando vacío (paginación finita)", pageNumber);
                        return Array.Empty<ConceptoDto>();
                    }

                    return fakeConceptos.Skip((pageNumber - 1) * pageSize)
                        .Take(pageSize)
                        .ToList();
                }

                // MODO REAL: Llamar a la API
                var token = await _oauthService.GetAccessTokenAsync(ct);

                // Endpoint: /concepts?pageNumber=X&pageSize=Y (parámetros OBLIGATORIOS)
                var url = $"Laboral/api/companies/1/employees/{Uri.EscapeDataString(employeeCode)}/concepts?pageNumber={pageNumber}&pageSize={pageSize}";
                _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Authorization", $"Bearer {token}");
                request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
                request.Headers.Add("api-version", "v2");
                request.Headers.Add("Accept", "application/json");

                var response = await _httpClient.SendAsync(request, ct);
                _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(ct);
                    _logger.LogError($"[A3InnuvaNominas] ❌ Error {response.StatusCode}: {responseContent}");
                    return Array.Empty<ConceptoDto>();
                }

                var data = await response.Content.ReadFromJsonAsync<List<ConceptResponse>>(cancellationToken: ct);
                var count = data?.Count ?? 0;
                _logger.LogInformation($"[A3InnuvaNominas] ✅ {count} conceptos obtenidos");

                if (data == null) return Array.Empty<ConceptoDto>();

                return data.Select(c => {
                    // Mapear tipos Wolters Kluwer a tipos esperados por motor
                    // API devuelve: "fijo"/"Fijo" (fixed), "variable"/"Variable", "E", "D", etc.
                    var normalized = c.ConceptType?.ToUpperInvariant() ?? "";
                    var mappedType = normalized switch
                    {
                        // Español (case-insensitive): "FIJO"/"fijo" → percepciones básicas
                        "FIJO" => "CONCEPTS",
                        // Español (case-insensitive): "VARIABLE"/"variable" → incentivos
                        "VARIABLE" => "INCENTIVES",
                        // Códigos: "E" (earnings/percepciones), "D" (descuentos/deductions)
                        "E" when c.Description?.Contains("SALARIO", StringComparison.OrdinalIgnoreCase) == true
                            => "THEORETICAL-GROSS",
                        "E" => "CONCEPTS",
                        "D" => "SANCTIONS",
                        _ => c.ConceptType ?? "CONCEPTS"
                    };

                    return new ConceptoDto(
                        c.ConceptCode,
                        c.Description ?? "",
                        c.Amount ?? 0m,
                        mappedType,
                        c.InKind ?? false,
                        c.Manual ?? false,
                        c.ConceptCollectionTypeDesc ?? ""
                    );
                }).ToList();
            }
            catch (TaskCanceledException ex) when (attempt < maxRetries)
            {
                // Timeout: esperar con backoff exponencial antes de reintentar
                int delayMs = (int)Math.Pow(2, attempt) * 500; // 1000ms, 2000ms, etc.
                _logger.LogWarning($"[A3InnuvaNominas] Timeout en intento {attempt}: esperando {delayMs}ms antes de reintentar...");
                await Task.Delay(delayMs, ct);
                continue; // Reintentar
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[A3InnuvaNominas] Error GetConceptos: {Message}", ex.Message);
                return Array.Empty<ConceptoDto>();
            }
        }

        // Si llegamos aquí, todos los reintentos fallaron por timeout
        _logger.LogError("[A3InnuvaNominas] GetConceptosAsync: Todos los {MaxRetries} intentos fallaron", maxRetries);
        return Array.Empty<ConceptoDto>();
    }

    private class CompaniesResponse
    {
        [JsonPropertyName("companies")]
        public List<CompanyResponse>? Companies { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; }
    }

    private class CompanyResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("taxId")]
        public string? TaxId { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("contactEmail")]
        public string? ContactEmail { get; set; }

        [JsonPropertyName("contactPhone")]
        public string? ContactPhone { get; set; }

        [JsonPropertyName("lastUpdate")]
        public DateTime? LastUpdate { get; set; }
    }

    private class PayrollsResponse
    {
        [JsonPropertyName("payrolls")]
        public List<PayrollResponse>? Payrolls { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; }
    }

    private class PayrollResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("employeeId")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("employeeName")]
        public string? EmployeeName { get; set; }

        [JsonPropertyName("periodCode")]
        public string? PeriodCode { get; set; }

        [JsonPropertyName("baseSalary")]
        public decimal? BaseSalary { get; set; }

        [JsonPropertyName("deductions")]
        public decimal? Deductions { get; set; }

        [JsonPropertyName("netSalary")]
        public decimal? NetSalary { get; set; }

        [JsonPropertyName("processDate")]
        public DateTime? ProcessDate { get; set; }
    }

    private class EmployeesResponse
    {
        [JsonPropertyName("items")]
        public List<EmployeeResponse>? Items { get; set; }

        [JsonPropertyName("pageNumber")]
        public int PageNumber { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("totalRecords")]
        public int TotalRecords { get; set; }
    }

    private class EmployeeResponse
    {
        [JsonPropertyName("employeeId")]
        public string? EmployeeId { get; set; }

        [JsonPropertyName("employeeCode")]
        public string? EmployeeCode { get; set; }

        [JsonPropertyName("completeName")]
        public string? CompleteName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("identifierNumber")]
        public string? IdentifierNumber { get; set; }

        [JsonPropertyName("workplaceCode")]
        public int? WorkplaceCode { get; set; }

        [JsonPropertyName("enrolmentDate")]
        public DateTime? EnrolmentDate { get; set; }

        [JsonPropertyName("dropDate")]
        public DateTime? DropDate { get; set; }

        [JsonPropertyName("tariffGroupID")]
        public int? TariffGroupId { get; set; }

        [JsonPropertyName("extraPayGroupID")]
        public string? ExtraPayGroupId { get; set; }

        [JsonPropertyName("pactedSalary")]
        public decimal? PactedSalary { get; set; }

        [JsonPropertyName("lastUpdate")]
        public DateTime? LastUpdate { get; set; }

        [JsonPropertyName("ssNAF")]
        public string? SsNaf { get; set; }

        [JsonPropertyName("personTypeDescription")]
        public string? PersonTypeDescription { get; set; }

        [JsonPropertyName("personTypeID")]
        public int? PersonTypeId { get; set; }

        [JsonPropertyName("indNonRemuneratedPractices")]
        public bool? IndNonRemuneratedPractices { get; set; }

        [JsonPropertyName("collectionTypeID")]
        public int? CollectionTypeId { get; set; }

        [JsonPropertyName("collectionTypeDescription")]
        public string? CollectionTypeDescription { get; set; }
    }

    private class ConceptResponse
    {
        [JsonPropertyName("conceptCode")]
        public int? ConceptCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("amount")]
        public decimal? Amount { get; set; }

        [JsonPropertyName("conceptType")]
        public string? ConceptType { get; set; }

        [JsonPropertyName("inKind")]
        public bool? InKind { get; set; }

        [JsonPropertyName("manual")]
        public bool? Manual { get; set; }

        [JsonPropertyName("conceptCollectionTypeID")]
        public int? ConceptCollectionTypeId { get; set; }

        [JsonPropertyName("conceptCollectionTypeDesc")]
        public string? ConceptCollectionTypeDesc { get; set; }
    }

    public async Task<string> WritePayrollAsync(
        string companyCode,
        string employeeCode,
        string periodCode,
        decimal percepciones,
        decimal descuentos,
        decimal neto,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation($"[A3InnuvaNominas] WritePayrollAsync iniciado - empleado {employeeCode}, período {periodCode}");
            var token = await _oauthService.GetAccessTokenAsync(ct);

            var url = $"Laboral/api/companies/{Uri.EscapeDataString(companyCode)}/employees/{Uri.EscapeDataString(employeeCode)}/payroll";
            _logger.LogInformation($"[A3InnuvaNominas] POST {_httpClient.BaseAddress}{url}");

            var payload = new PayrollWriteRequest
            {
                PeriodCode = periodCode,
                BaseSalary = percepciones,
                Deductions = descuentos,
                NetSalary = neto,
                ProcessDate = DateTime.UtcNow
            };

            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[A3InnuvaNominas] Error: {response.StatusCode} - {response.ReasonPhrase}");
                _logger.LogError($"[A3InnuvaNominas] Response body: {responseContent}");
                throw new Exception($"Error escribiendo nómina: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] ✅ Nómina escrita exitosamente para {employeeCode}");
            return responseBody;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"[A3InnuvaNominas] Error WritePayroll: {ex.Message}");
            throw;
        }
    }

    private class PayrollWriteRequest
    {
        [JsonPropertyName("periodCode")]
        public string PeriodCode { get; set; } = "";

        [JsonPropertyName("baseSalary")]
        public decimal BaseSalary { get; set; }

        [JsonPropertyName("deductions")]
        public decimal Deductions { get; set; }

        [JsonPropertyName("netSalary")]
        public decimal NetSalary { get; set; }

        [JsonPropertyName("processDate")]
        public DateTime ProcessDate { get; set; }
    }

    // ====== PHASE 1 REDESIGNED: Real Wolters Kluwer Endpoints ======

    /// <summary>
    /// PHASE 1.3a: Get salary data for employee
    /// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/salary
    /// </summary>
    public async Task<IReadOnlyList<SalaryDto>> GetSalaryAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetSalaryAsync iniciado - empleado {Code}", employeeCode);

            if (_useFakeData)
            {
                var fakeSalaries = new List<SalaryDto>
                {
                    new SalaryDto(
                        $"{employeeCode}_salary",
                        employeeCode,
                        "12345678A",
                        2500m,
                        2000m,
                        "EUR",
                        DateTime.Now.AddYears(-2),
                        null
                    )
                };
                return fakeSalaries;
            }

            var token = await _oauthService.GetAccessTokenAsync(ct);
            var url = $"Laboral/api/companies/{Uri.EscapeDataString(companyCode)}/employees/{Uri.EscapeDataString(employeeCode)}/salary";
            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[A3InnuvaNominas] Error GetSalary: {response.StatusCode}");
                return Array.Empty<SalaryDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<List<SalaryResponse>>(cancellationToken: ct);
            if (data == null) return Array.Empty<SalaryDto>();

            return data.Select(s => new SalaryDto(
                $"{employeeCode}_salary",
                employeeCode,
                s.NIF ?? "",
                s.GrossSalary ?? 0,
                s.NetSalary ?? 0,
                s.Currency ?? "EUR",
                s.StartDate,
                s.EndDate
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetSalary: {Message}", ex.Message);
            return Array.Empty<SalaryDto>();
        }
    }

    /// <summary>
    /// PHASE 1.3b: Get IRPF (tax) data for employee
    /// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/irpf
    /// </summary>
    public async Task<IReadOnlyList<IRPFDto>> GetIRPFAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetIRPFAsync iniciado - empleado {Code}", employeeCode);

            if (_useFakeData)
            {
                var fakeIRPF = new List<IRPFDto>
                {
                    new IRPFDto(
                        $"{employeeCode}_irpf",
                        employeeCode,
                        "12345678A",
                        "IRPF",
                        21m,
                        500m,
                        DateTime.Now.AddYears(-1),
                        null
                    )
                };
                return fakeIRPF;
            }

            var token = await _oauthService.GetAccessTokenAsync(ct);
            var formattedCode = employeeCode.PadLeft(6, '0');
            var url = $"Laboral/api/companies/{Uri.EscapeDataString(companyCode)}/employees/{Uri.EscapeDataString(formattedCode)}/irpfdata";
            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[A3InnuvaNominas] Error GetIRPF: {response.StatusCode}");
                return Array.Empty<IRPFDto>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            _logger.LogInformation($"[A3InnuvaNominas] IRPF Response JSON: {json}");

            var irpfData = JsonSerializer.Deserialize<IRPFResponse>(json);
            if (irpfData == null) return Array.Empty<IRPFDto>();

            // WK devuelve un objeto IRPF único (no lista), mapeamos a IRPFDto
            return new List<IRPFDto>
            {
                new IRPFDto(
                    $"{employeeCode}_irpf",
                    employeeCode,
                    employeeCode,  // Usamos employeeCode como identificador (WK no proporciona NIF en irpfdata)
                    irpfData.PerceptionTypeCode ?? "IRPF",
                    irpfData.WithholdingPercentage,
                    irpfData.IrpfBasePercentage,
                    DateTime.Now,
                    null
                )
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetIRPF: {Message}", ex.Message);
            return Array.Empty<IRPFDto>();
        }
    }

    /// <summary>
    /// PHASE 1.3c: Get remuneration data for employee
    /// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/remuneration
    /// </summary>
    public async Task<IReadOnlyList<RemunerationDto>> GetRemunerationAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetRemunerationAsync iniciado - empleado {Code}", employeeCode);

            if (_useFakeData)
            {
                var fakeRemuneration = new List<RemunerationDto>
                {
                    new RemunerationDto(
                        $"{employeeCode}_rem_bonus",
                        employeeCode,
                        "12345678A",
                        "EXTRAPAYMENTS",
                        500m,
                        "Bono por desempeño",
                        DateTime.Now.AddMonths(-3),
                        null
                    )
                };
                return fakeRemuneration;
            }

            var token = await _oauthService.GetAccessTokenAsync(ct);
            var url = $"Laboral/api/companies/{Uri.EscapeDataString(companyCode)}/employees/{Uri.EscapeDataString(employeeCode)}/remuneration";
            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[A3InnuvaNominas] Error GetRemuneration: {response.StatusCode}");
                return Array.Empty<RemunerationDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<List<RemunerationResponse>>(cancellationToken: ct);
            if (data == null) return Array.Empty<RemunerationDto>();

            return data.Select(r =>
            {
                // Mapear conceptType de WK a tipos que espera el motor
                var remType = r.ConceptType switch
                {
                    "Fijo" when r.Description?.Contains("SALARIO", StringComparison.OrdinalIgnoreCase) == true => "THEORETICAL-GROSS",
                    "Fijo" => "CONCEPTS",
                    "Variable" => "EXTRAPAYMENTS",
                    _ => r.ConceptType ?? "CONCEPTS"
                };

                return new RemunerationDto(
                    $"{employeeCode}_rem_{r.ConceptCode}",
                    employeeCode,
                    "",  // WK no proporciona NIF en esta respuesta
                    remType,
                    r.Amount,
                    r.Description ?? "",
                    DateTime.Now,  // WK no proporciona fecha de inicio
                    null
                );
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetRemuneration: {Message}", ex.Message);
            return Array.Empty<RemunerationDto>();
        }
    }

    /// <summary>
    /// PHASE 1.3d: Get bank account data for employee
    /// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/bankaccounts
    /// </summary>
    public async Task<IReadOnlyList<BankAccountDto>> GetBankAccountsAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetBankAccountsAsync iniciado - empleado {Code}", employeeCode);

            if (_useFakeData)
            {
                var fakeBankAccounts = new List<BankAccountDto>
                {
                    new BankAccountDto(
                        $"{employeeCode}_bank",
                        employeeCode,
                        "12345678A",
                        "ES9121000418450200051332",
                        "BBVAESMM",
                        "Juan García López",
                        "Principal",
                        true,
                        DateTime.Now.AddYears(-3),
                        null
                    )
                };
                return fakeBankAccounts;
            }

            var token = await _oauthService.GetAccessTokenAsync(ct);
            var url = $"Laboral/api/companies/{Uri.EscapeDataString(companyCode)}/employees/{Uri.EscapeDataString(employeeCode)}/bankaccounts";
            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[A3InnuvaNominas] Error GetBankAccounts: {response.StatusCode}");
                return Array.Empty<BankAccountDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<List<BankAccountResponse>>(cancellationToken: ct);
            if (data == null) return Array.Empty<BankAccountDto>();

            return data.Select(b => new BankAccountDto(
                $"{employeeCode}_bank",
                employeeCode,
                b.NIF ?? "",
                b.IBAN ?? "",
                b.BIC,
                b.AccountHolderName,
                b.AccountType,
                b.IsPrimary ?? false,
                b.StartDate,
                b.EndDate
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetBankAccounts: {Message}", ex.Message);
            return Array.Empty<BankAccountDto>();
        }
    }

    /// <summary>
    /// PHASE 1.3e: Get agreement data for employee
    /// Endpoint: GET /Laboral/api/companies/{companyId}/employees/{employeeId}/agreements
    /// </summary>
    public async Task<IReadOnlyList<AgreementDto>> GetAgreementsAsync(
        string companyCode,
        string employeeCode,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetAgreementsAsync iniciado - empleado {Code}", employeeCode);

            if (_useFakeData)
            {
                var fakeAgreements = new List<AgreementDto>
                {
                    new AgreementDto(
                        $"{employeeCode}_agree",
                        employeeCode,
                        "12345678A",
                        "COL_2024",
                        "Convenio Sector Servicios 2024",
                        "Colectivo",
                        DateTime.Parse("2024-01-01"),
                        DateTime.Parse("2024-12-31"),
                        "Acuerdo negociación colectiva sectorial"
                    )
                };
                return fakeAgreements;
            }

            var token = await _oauthService.GetAccessTokenAsync(ct);
            var url = $"Laboral/api/companies/{Uri.EscapeDataString(companyCode)}/employees/{Uri.EscapeDataString(employeeCode)}/agreements";
            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"[A3InnuvaNominas] Error GetAgreements: {response.StatusCode}");
                return Array.Empty<AgreementDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<List<AgreementResponse>>(cancellationToken: ct);
            if (data == null) return Array.Empty<AgreementDto>();

            return data.Select(a => new AgreementDto(
                $"{employeeCode}_agree",
                employeeCode,
                a.NIF ?? "",
                a.AgreementCode ?? "",
                a.AgreementName ?? "",
                a.AgreementType ?? "",
                a.StartDate,
                a.EndDate,
                a.Description
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetAgreements: {Message}", ex.Message);
            return Array.Empty<AgreementDto>();
        }
    }

    public async Task<IReadOnlyList<ContractAgreementDto>> GetContractAgreementAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetContractAgreementAsync iniciado - empleado {Code}, página {Page}", employeeCode, pageNumber);

            var token = await _oauthService.GetAccessTokenAsync(ct);
            var queryParams = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            var url = $"Laboral/api/companies/1/employees/{Uri.EscapeDataString(employeeCode)}/contract-agreement{queryParams}";

            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[A3InnuvaNominas] Error: {response.StatusCode} - {responseContent}");
                return Array.Empty<ContractAgreementDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<List<ContractAgreementResponse>>(cancellationToken: ct);
            var count = data?.Count ?? 0;
            _logger.LogInformation($"[A3InnuvaNominas] ✅ {count} acuerdos de contrato obtenidos");

            if (data == null || data.Count == 0) return Array.Empty<ContractAgreementDto>();

            return data.Select(c => new ContractAgreementDto(
                c.Id ?? $"{employeeCode}_contract_{Guid.NewGuid()}",
                employeeCode,
                c.ContractCode,
                c.ContractDescription,
                c.LabourPeriodStartDate,
                c.LabourPeriodEndDate,
                c.ContributionTypeID,
                c.ContributionType,
                c.ContributionModalityType,
                c.CnoOccupationID,
                c.AnnualGrossAmount,
                c.CollectionTypeID,
                c.CollectionType
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetContractAgreement: {Message}", ex.Message);
            return Array.Empty<ContractAgreementDto>();
        }
    }

    public async Task<IReadOnlyList<ContractTimetableDto>> GetContractTimetableAsync(
        string employeeCode,
        int pageNumber = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] GetContractTimetableAsync iniciado - empleado {Code}, página {Page}", employeeCode, pageNumber);

            var token = await _oauthService.GetAccessTokenAsync(ct);
            var queryParams = $"?pageNumber={pageNumber}&pageSize={pageSize}";
            var url = $"Laboral/api/companies/1/employees/{Uri.EscapeDataString(employeeCode)}/contract/timetable{queryParams}";

            _logger.LogInformation($"[A3InnuvaNominas] GET {_httpClient.BaseAddress}{url}");

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Authorization", $"Bearer {token}");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            request.Headers.Add("Accept", "application/json");

            var response = await _httpClient.SendAsync(request, ct);
            _logger.LogInformation($"[A3InnuvaNominas] Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[A3InnuvaNominas] Error: {response.StatusCode} - {responseContent}");
                return Array.Empty<ContractTimetableDto>();
            }

            var data = await response.Content.ReadFromJsonAsync<List<ContractTimetableResponse>>(cancellationToken: ct);
            var count = data?.Count ?? 0;
            _logger.LogInformation($"[A3InnuvaNominas] ✅ {count} horarios de contrato obtenidos");

            if (data == null || data.Count == 0) return Array.Empty<ContractTimetableDto>();

            return data.Select(t => new ContractTimetableDto(
                t.Id ?? $"{employeeCode}_timetable_{Guid.NewGuid()}",
                employeeCode,
                t.WorkDayTypeID,
                t.TotalWeekHours,
                t.CompleteWorkDayStartID,
                t.CompleteWorkDayEndID,
                t.IndComplementaryHours,
                t.PartialPeriodTypeID,
                t.PartialHours
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error GetContractTimetable: {Message}", ex.Message);
            return Array.Empty<ContractTimetableDto>();
        }
    }

    // Response DTOs for Phase 1 Redesigned endpoints

    private class SalaryResponse
    {
        [JsonPropertyName("nif")]
        public string? NIF { get; set; }

        [JsonPropertyName("grossSalary")]
        public decimal? GrossSalary { get; set; }

        [JsonPropertyName("netSalary")]
        public decimal? NetSalary { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }
    }

    private class IRPFResponse
    {
        [JsonPropertyName("pensionPlan")]
        public decimal PensionPlan { get; set; }

        [JsonPropertyName("detractions")]
        public decimal Detractions { get; set; }

        [JsonPropertyName("spousePension")]
        public decimal SpousePension { get; set; }

        [JsonPropertyName("foodAnnualAllowance")]
        public decimal FoodAnnualAllowance { get; set; }

        [JsonPropertyName("perceptionTypeCode")]
        public string? PerceptionTypeCode { get; set; }

        [JsonPropertyName("withholdingPercentage")]
        public decimal WithholdingPercentage { get; set; }

        [JsonPropertyName("disabilityLevel")]
        public decimal DisabilityLevel { get; set; }

        [JsonPropertyName("indHelp")]
        public bool IndHelp { get; set; }

        [JsonPropertyName("familySituationCode")]
        public int FamilySituationCode { get; set; }

        [JsonPropertyName("indAscendants")]
        public bool IndAscendants { get; set; }

        [JsonPropertyName("indDescendants")]
        public bool IndDescendants { get; set; }

        [JsonPropertyName("indNoResident")]
        public int IndNoResident { get; set; }

        [JsonPropertyName("indMobility")]
        public bool IndMobility { get; set; }

        [JsonPropertyName("indAutMan")]
        public int IndAutMan { get; set; }

        [JsonPropertyName("childrenNumber")]
        public int ChildrenNumber { get; set; }

        [JsonPropertyName("irpfBasePercentage")]
        public decimal IrpfBasePercentage { get; set; }

        [JsonPropertyName("indEconomicActivities")]
        public bool IndEconomicActivities { get; set; }

        [JsonPropertyName("indHomeInvestment")]
        public bool IndHomeInvestment { get; set; }

        [JsonPropertyName("homePreviousWithholdingPercentage")]
        public decimal HomePreviousWithholdingPercentage { get; set; }

        [JsonPropertyName("indBusinessAmountLessThan100000")]
        public bool IndBusinessAmountLessThan100000 { get; set; }
    }

    private class RemunerationResponse
    {
        [JsonPropertyName("conceptCode")]
        public int ConceptCode { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("conceptType")]
        public string? ConceptType { get; set; }

        [JsonPropertyName("inKind")]
        public bool InKind { get; set; }

        [JsonPropertyName("manual")]
        public bool Manual { get; set; }

        [JsonPropertyName("conceptCollectionTypeID")]
        public int ConceptCollectionTypeID { get; set; }

        [JsonPropertyName("conceptCollectionTypeDesc")]
        public string? ConceptCollectionTypeDesc { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }
    }

    private class BankAccountResponse
    {
        [JsonPropertyName("nif")]
        public string? NIF { get; set; }

        [JsonPropertyName("iban")]
        public string? IBAN { get; set; }

        [JsonPropertyName("bic")]
        public string? BIC { get; set; }

        [JsonPropertyName("accountHolderName")]
        public string? AccountHolderName { get; set; }

        [JsonPropertyName("accountType")]
        public string? AccountType { get; set; }

        [JsonPropertyName("isPrimary")]
        public bool? IsPrimary { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }
    }

    private class AgreementResponse
    {
        [JsonPropertyName("nif")]
        public string? NIF { get; set; }

        [JsonPropertyName("agreementCode")]
        public string? AgreementCode { get; set; }

        [JsonPropertyName("agreementName")]
        public string? AgreementName { get; set; }

        [JsonPropertyName("agreementType")]
        public string? AgreementType { get; set; }

        [JsonPropertyName("startDate")]
        public DateTime StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public DateTime? EndDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    private class ContractAgreementResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("contractCode")]
        public string? ContractCode { get; set; }
        [JsonPropertyName("contractDescription")]
        public string? ContractDescription { get; set; }
        [JsonPropertyName("labourPeriodStartDate")]
        public DateTime? LabourPeriodStartDate { get; set; }
        [JsonPropertyName("labourPeriodEndDate")]
        public DateTime? LabourPeriodEndDate { get; set; }
        [JsonPropertyName("contributionTypeID")]
        public int? ContributionTypeID { get; set; }
        [JsonPropertyName("contributionType")]
        public string? ContributionType { get; set; }
        [JsonPropertyName("contributionModalityType")]
        public string? ContributionModalityType { get; set; }
        [JsonPropertyName("cnoOccupationID")]
        public string? CnoOccupationID { get; set; }
        [JsonPropertyName("annualGrossAmount")]
        public decimal? AnnualGrossAmount { get; set; }
        [JsonPropertyName("collectionTypeID")]
        public int? CollectionTypeID { get; set; }
        [JsonPropertyName("collectionType")]
        public string? CollectionType { get; set; }
    }

    private class ContractTimetableResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("workDayTypeID")]
        public string? WorkDayTypeID { get; set; }
        [JsonPropertyName("totalWeekHours")]
        public decimal? TotalWeekHours { get; set; }
        [JsonPropertyName("completeWorkDayStartID")]
        public string? CompleteWorkDayStartID { get; set; }
        [JsonPropertyName("completeWorkDayEndID")]
        public string? CompleteWorkDayEndID { get; set; }
        [JsonPropertyName("indComplementaryHours")]
        public bool? IndComplementaryHours { get; set; }
        [JsonPropertyName("partialPeriodTypeID")]
        public string? PartialPeriodTypeID { get; set; }
        [JsonPropertyName("partialHours")]
        public decimal? PartialHours { get; set; }
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

/// <summary>
/// A3 INNUVA ERP - Wolters Kluwer OINV API (Facturación)
/// OAuth-authenticated client for invoice and client data retrieval
/// </summary>
public class A3InnuvaERPClient : IA3InnuvaERPClient
{
    private readonly HttpClient _httpClient;
    private readonly IWoltersKluwerOAuthService _oauthService;
    private readonly string _baseUrl;
    private readonly string _apiPath;
    private readonly string _subscriptionKey;
    private readonly ILogger<A3InnuvaERPClient> _logger;

    public A3InnuvaERPClient(
        HttpClient httpClient,
        IWoltersKluwerOAuthService oauthService,
        string baseUrl,
        string apiPath,
        string subscriptionKey,
        ILogger<A3InnuvaERPClient> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
        _baseUrl = baseUrl ?? throw new ArgumentNullException(nameof(baseUrl));
        _apiPath = apiPath ?? throw new ArgumentNullException(nameof(apiPath));
        _subscriptionKey = subscriptionKey ?? throw new ArgumentNullException(nameof(subscriptionKey));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get facturas (invoices) from Wolters Kluwer OINV API
    /// STUB: Endpoints to be confirmed with Wolters Kluwer
    /// </summary>
    public async Task<IReadOnlyList<A3ERPFacturaDto>> GetFacturasAsync(
        string companyCode,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaERP] GetFacturasAsync iniciado - empresa {Company}", companyCode);

            if (string.IsNullOrWhiteSpace(companyCode))
                throw new ArgumentNullException(nameof(companyCode));

            var token = await _oauthService.GetAccessTokenAsync(ct);
            _logger.LogInformation("[A3InnuvaERP] Token obtenido: {TokenLength} caracteres", token?.Length ?? 0);

            // STUB: Placeholder implementation - endpoints to be confirmed
            _logger.LogInformation("[A3InnuvaERP] ⚠️ GetFacturasAsync - STUB implementation (awaiting endpoint confirmation)");
            return Array.Empty<A3ERPFacturaDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaERP] Error GetFacturas: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return Array.Empty<A3ERPFacturaDto>();
        }
    }

    /// <summary>
    /// Get clientes (clients) from Wolters Kluwer OINV API
    /// STUB: Endpoints to be confirmed with Wolters Kluwer
    /// </summary>
    public async Task<IReadOnlyList<A3ERPClienteDto>> GetClientesAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaERP] GetClientesAsync iniciado");

            var token = await _oauthService.GetAccessTokenAsync(ct);
            _logger.LogInformation("[A3InnuvaERP] Token obtenido: {TokenLength} caracteres", token?.Length ?? 0);

            // STUB: Placeholder implementation - endpoints to be confirmed
            _logger.LogInformation("[A3InnuvaERP] ⚠️ GetClientesAsync - STUB implementation (awaiting endpoint confirmation)");
            return Array.Empty<A3ERPClienteDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaERP] Error GetClientes: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return Array.Empty<A3ERPClienteDto>();
        }
    }

    /// <summary>
    /// Get líneas de factura (invoice lines) from Wolters Kluwer OINV API
    /// STUB: Endpoints to be confirmed with Wolters Kluwer
    /// </summary>
    public async Task<IReadOnlyList<A3ERPLineaFacturaDto>> GetLineasFacturaAsync(
        string facturaId,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaERP] GetLineasFacturaAsync iniciado - factura {FacturaId}", facturaId);

            if (string.IsNullOrWhiteSpace(facturaId))
                throw new ArgumentNullException(nameof(facturaId));

            var token = await _oauthService.GetAccessTokenAsync(ct);
            _logger.LogInformation("[A3InnuvaERP] Token obtenido: {TokenLength} caracteres", token?.Length ?? 0);

            // STUB: Placeholder implementation - endpoints to be confirmed
            _logger.LogInformation("[A3InnuvaERP] ⚠️ GetLineasFacturaAsync - STUB implementation (awaiting endpoint confirmation)");
            return Array.Empty<A3ERPLineaFacturaDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaERP] Error GetLineasFactura: {ExceptionType} - {Message}", ex.GetType().Name, ex.Message);
            return Array.Empty<A3ERPLineaFacturaDto>();
        }
    }
}
