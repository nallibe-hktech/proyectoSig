using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using SIG.Application.Interfaces.Services;
using SIG.Infrastructure.Services;
using SIG.Infrastructure.Integrations.Http;
using SIG.Infrastructure.Persistence;

namespace SIG.API.Controllers;

/// <summary>
/// A3 INNUVA Nóminas - Integración con Wolters Kluwer OAuth
/// Endpoints para sincronización de empresas y nóminas desde A3 INNUVA.
/// </summary>
[ApiController]
[Route("api/a3-innuva-nominas")]
[AllowAnonymous]
public class A3InnuvaNominasController : ControllerBase
{
    private readonly IA3InnuvaNominasService _service;
    private readonly IWoltersKluwerOAuthService _oauthService;
    private readonly IConfiguration _config;
    private readonly ILogger<A3InnuvaNominasController> _logger;
    private readonly AppDbContext _db;

    public A3InnuvaNominasController(
        IA3InnuvaNominasService service,
        IWoltersKluwerOAuthService oauthService,
        IConfiguration config,
        ILogger<A3InnuvaNominasController> logger,
        AppDbContext db)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _oauthService = oauthService ?? throw new ArgumentNullException(nameof(oauthService));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    /// <summary>
    /// Sincronizar empresas desde A3 INNUVA (OAuth Wolters Kluwer)
    /// </summary>
    [HttpPost("sync/companies")]
    public async Task<IActionResult> SyncCompanies(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] Iniciando sincronización de empresas...");
            await _service.SyncCompaniesAsync(ct);
            return Ok(new { message = "Sincronización de empresas completada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando empresas");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sincronizar nóminas para una empresa específica
    /// </summary>
    [HttpPost("sync/payrolls")]
    public async Task<IActionResult> SyncPayrolls([FromQuery] string companyCode, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(companyCode))
                return BadRequest(new { error = "companyCode es requerido" });

            _logger.LogInformation($"[A3InnuvaNominas] Iniciando sincronización de nóminas para empresa {companyCode}...");
            await _service.SyncPayrollsAsync(companyCode, ct);
            return Ok(new { message = $"Sincronización de nóminas completada para empresa {companyCode}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando nóminas");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sincronizar empleados desde A3 INNUVA
    /// </summary>
    [HttpPost("sync/employees")]
    public async Task<IActionResult> SyncEmployees(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] Iniciando sincronización de empleados...");
            await _service.SyncEmployeesAsync(ct);
            return Ok(new { message = "Sincronización de empleados completada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando empleados");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sincronizar conceptos (percepciones/descuentos) desde A3 INNUVA
    /// </summary>
    [HttpPost("sync-concepts")]
    public async Task<IActionResult> SyncConceptos(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas] Iniciando sincronización de conceptos...");
            await _service.SyncConceptosAsync(ct);
            return Ok(new { message = "Sincronización de conceptos completada" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error sincronizando conceptos");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener lista paginada de empresas sincronizadas
    /// </summary>
    [HttpGet("companies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetCompaniesAsync(page, pageSize, search, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error obteniendo empresas");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener lista paginada de nóminas sincronizadas
    /// </summary>
    [HttpGet("payrolls")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPayrolls(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetPayrollsAsync(page, pageSize, search, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error obteniendo nóminas");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// OAuth: Get authorize URL for Wolters Kluwer authorization code flow.
    /// ⚠️ IMPORTANTE: El redirect_uri se toma del appsettings.json (https://localhost:43971/Login)
    /// que está registrado en Wolters Kluwer. No se puede cambiar desde el frontend.
    /// </summary>
    [HttpGet("oauth/authorize-url")]
    [AllowAnonymous]
    public IActionResult GetAuthorizeUrl()
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas-OAuth] Generando URL de autorización");

            // Obtener redirect_uri del appsettings.json (que está registrado en WK)
            var redirectUri = _config["Integrations:A3InnuvaNominas:RedirectUri"]
                ?? throw new InvalidOperationException("RedirectUri no configurado en appsettings");

            var state = Guid.NewGuid().ToString();
            var nonce = Guid.NewGuid().ToString();
            var authorizeUrl = _oauthService.GetAuthorizeUrl(redirectUri, state, nonce);

            _logger.LogInformation($"[A3InnuvaNominas-OAuth] ✅ URL de autorización generada con redirect_uri: {redirectUri}");

            return Ok(new
            {
                authorizeUrl,
                redirectUri,
                message = "Abre este URL en tu navegador para autorizar el acceso a Wolters Kluwer"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-OAuth] Error generando URL de autorización");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// OAuth: Exchange authorization code for access token.
    /// Called after user authorizes at Wolters Kluwer and receives code.
    /// </summary>
    [HttpPost("oauth/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleOAuthCallback(
        [FromQuery] string code,
        [FromQuery] string redirectUri = "http://localhost:4200/a3-innuva/oauth-callback",
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest(new { error = "Authorization code is required" });

            _logger.LogInformation("[A3InnuvaNominas-OAuth] Intercambiando código por tokens...");
            await _oauthService.ExchangeAuthorizationCodeAsync(code, redirectUri, ct);
            return Ok(new { message = "✅ Tokens obtenidos. Puedes sincronizar empresas y nóminas ahora." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-OAuth] Error intercambiando código");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// OAuth: Manually refresh access token (called automatically, but exposed for debugging).
    /// </summary>
    [HttpPost("oauth/refresh")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> RefreshAccessToken(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas-OAuth] Refrescando token de acceso...");
            await _oauthService.RefreshAccessTokenAsync(ct);
            return Ok(new { message = "✅ Token refrescado" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-OAuth] Error refrescando token");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ⚠️ TEST ONLY: Sincronizar empresas a tabla TEST (sin afectar datos de producción)
    /// </summary>
    [HttpPost("test/sync/companies")]
    public async Task<IActionResult> SyncCompaniesTest(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("[A3InnuvaNominas-TEST] Iniciando sincronización de empresas a tabla TEST...");
            await _service.SyncCompaniesTestAsync(ct);
            return Ok(new { message = "✅ Sincronización de empresas completada en tabla TEST (sin afectar producción)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-TEST] Error sincronizando empresas a tabla TEST");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ⚠️ TEST ONLY: Sincronizar nóminas a tabla TEST para una empresa específica
    /// </summary>
    [HttpPost("test/sync/payrolls")]
    public async Task<IActionResult> SyncPayrollsTest([FromQuery] string companyCode, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(companyCode))
                return BadRequest(new { error = "companyCode es requerido" });

            _logger.LogInformation($"[A3InnuvaNominas-TEST] Iniciando sincronización de nóminas a tabla TEST para empresa {companyCode}...");
            await _service.SyncPayrollsTestAsync(companyCode, ct);
            return Ok(new { message = $"✅ Sincronización de nóminas completada en tabla TEST para empresa {companyCode} (sin afectar producción)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-TEST] Error sincronizando nóminas a tabla TEST");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ⚠️ TEST ONLY: Obtener lista paginada de empresas de tabla TEST
    /// </summary>
    [HttpGet("test/companies")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompaniesTest(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetCompaniesTestAsync(page, pageSize, search, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-TEST] Error obteniendo empresas de tabla TEST");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// ⚠️ TEST ONLY: Obtener lista paginada de nóminas de tabla TEST
    /// </summary>
    [HttpGet("test/payrolls")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPayrollsTest(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetPayrollsTestAsync(page, pageSize, search, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas-TEST] Error obteniendo nóminas de tabla TEST");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener lista paginada de empleados sincronizados desde Wolters Kluwer
    /// </summary>
    [HttpGet("employees")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEmployees(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetEmployeesAsync(page, pageSize, search, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error obteniendo empleados");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener lista paginada de conceptos sincronizados desde Wolters Kluwer
    /// </summary>
    [HttpGet("concepts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetConceptos(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetConceptosAsync(page, pageSize, search, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error obteniendo conceptos");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PHASE 2: Calcular nóminas a partir de conceptos sincronizados
    /// </summary>
    [HttpPost("calculate")]
    public async Task<IActionResult> CalculatePayrolls(
        [FromQuery] string periodCode,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(periodCode))
                return BadRequest(new { error = "periodCode es requerido" });

            _logger.LogInformation($"[A3InnuvaNominas] Iniciando PHASE 2 (CALCULATE) para período {periodCode}");
            await _service.CalculatePayrollsAsync(periodCode, ct);
            return Ok(new { message = $"✅ PHASE 2 completada: Nóminas calculadas para período {periodCode}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error en PHASE 2");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// PHASE 3: Escribir nóminas calculadas de vuelta a Wolters Kluwer
    /// </summary>
    [HttpPost("write")]
    public async Task<IActionResult> WritePayrolls(
        [FromQuery] string periodCode,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(periodCode))
                return BadRequest(new { error = "periodCode es requerido" });

            _logger.LogInformation($"[A3InnuvaNominas] Iniciando PHASE 3 (WRITE) para período {periodCode}");
            await _service.WritePayrollsAsync(periodCode, ct);
            return Ok(new { message = $"✅ PHASE 3 completada: Nóminas escritas a Wolters Kluwer para período {periodCode}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error en PHASE 3");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// DEBUG: Verificar datos en BD (temporal)
    /// </summary>
    [HttpGet("debug/status")]
    public async Task<IActionResult> DebugStatus(CancellationToken ct = default)
    {
        try
        {
            var nominas = _db.StagingA3InnuvaPayrolls.Count();
            var conceptos = _db.StagingA3InnuvaConceptos.Count();
            var nominasCalculadas = _db.StagingA3InnuvaNominasCalculadas.Count();

            return Ok(new
            {
                nominas,
                conceptos,
                nominasCalculadas,
                message = "✅ Datos sincronizados en BD"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener nóminas calculadas (resultado de PHASE 2)
    /// </summary>
    [HttpGet("calculated")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNominasCalculadas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? periodCode = null,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetNominasCalculadasAsync(page, pageSize, periodCode, search, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error obteniendo nóminas calculadas");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Obtener nóminas enviadas a Wolters Kluwer (PHASE 3 completada)
    /// </summary>
    [HttpGet("sent")]
    [AllowAnonymous]
    public async Task<IActionResult> GetNominasEnviadas(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? periodCode = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _service.GetNominasCalculadasEnviadasAsync(page, pageSize, periodCode, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[A3InnuvaNominas] Error obteniendo nóminas enviadas");
            return BadRequest(new { error = ex.Message });
        }
    }
}
