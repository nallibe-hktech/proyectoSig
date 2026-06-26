using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIG.Infrastructure.Integrations.Http;

namespace SIG.API.Controllers;

/// <summary>
/// Maneja el callback OAuth de Wolters Kluwer
/// WK redirige a: https://localhost:43971/Login?code=...&state=...
/// </summary>
[ApiController]
[AllowAnonymous]
public class OAuthCallbackController : ControllerBase
{
    private readonly IWoltersKluwerOAuthService _oauthService;
    private readonly ILogger<OAuthCallbackController> _logger;

    public OAuthCallbackController(
        IWoltersKluwerOAuthService oauthService,
        ILogger<OAuthCallbackController> logger)
    {
        _oauthService = oauthService;
        _logger = logger;
    }

    /// <summary>
    /// Callback endpoint para Wolters Kluwer OAuth
    /// GET https://localhost:43971/Login?code=AUTH_CODE&state=STATE_GUID
    /// </summary>
    [HttpGet("/Login")]
    [ApiExplorerSettings(IgnoreApi = true)] // No mostrar en Swagger
    public async Task<IActionResult> HandleOAuthCallback(
        [FromQuery] string? code = null,
        [FromQuery] string? state = null,
        [FromQuery] string? error = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation($"[OAuthCallback] Recibido callback de WK - code: {code?.Substring(0, 20)}..., state: {state}");

            // Si hay error de autorización
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogError($"[OAuthCallback] ❌ Error de WK: {error}");
                return Redirect($"http://localhost:4200/a3-innuva/oauth-callback?error={error}");
            }

            // Validar que tenemos el code
            if (string.IsNullOrWhiteSpace(code))
            {
                _logger.LogError("[OAuthCallback] ❌ No se recibió authorization code");
                return Redirect("http://localhost:4200/a3-innuva/oauth-callback?error=no_code");
            }

            // El redirect_uri debe coincidir con el que usamos en la solicitud OAuth
            var redirectUri = "https://localhost:43971/Login";

            // Intercambiar código por tokens
            _logger.LogInformation("[OAuthCallback] Intercambiando authorization code por tokens...");
            await _oauthService.ExchangeAuthorizationCodeAsync(code, redirectUri, ct);

            _logger.LogInformation("[OAuthCallback] ✅ Tokens obtenidos exitosamente");

            // Redirigir al frontend con status de éxito
            return Redirect("http://localhost:4200/a3-innuva?authorized=true");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OAuthCallback] ❌ Error procesando callback OAuth");
            return Redirect($"http://localhost:4200/a3-innuva/oauth-callback?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    /// <summary>
    /// GET https://localhost:43971/Login/CallbackFromWKA?code=...
    /// Alternativa compatible con otro redirect_uri registrado en WK
    /// </summary>
    [HttpGet("/Login/CallbackFromWKA")]
    [ApiExplorerSettings(IgnoreApi = true)] // No mostrar en Swagger
    public async Task<IActionResult> HandleOAuthCallbackAlternative(
        [FromQuery] string? code = null,
        [FromQuery] string? state = null,
        [FromQuery] string? error = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[OAuthCallback] Usando ruta alternativa /Login/CallbackFromWKA");
        return await HandleOAuthCallback(code, state, error, ct);
    }
}
