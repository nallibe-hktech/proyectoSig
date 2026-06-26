using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SIG.Infrastructure.Persistence;
using SIG.Domain.Entities;

namespace SIG.Infrastructure.Integrations.Http;

public interface IWoltersKluwerOAuthService
{
    /// <summary>Gets a valid access token from cache, DB, or by refreshing.</summary>
    Task<string> GetAccessTokenAsync(CancellationToken ct = default);

    /// <summary>Constructs the OAuth authorize URL for user to initiate authorization code flow.</summary>
    string GetAuthorizeUrl(string redirectUri, string state, string nonce);

    /// <summary>Exchanges authorization code for tokens and stores in database.</summary>
    Task ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken ct = default);

    /// <summary>Refreshes access token using stored refresh token.</summary>
    Task RefreshAccessTokenAsync(CancellationToken ct = default);

    /// <summary>Invalidates cached token.</summary>
    void InvalidateToken();
}

public class WoltersKluwerOAuthService : IWoltersKluwerOAuthService
{
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _authorizeEndpoint;
    private readonly string _tokenEndpoint;
    private readonly string _scopes = "offline_access openid IDInfo WK.ES.A3EquipoContex";
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WoltersKluwerOAuthService> _logger;
    private readonly IHostEnvironment _env;
    private const string TokenCacheKey = "wk_access_token";
    private const string CodeVerifierCacheKey = "wk_code_verifier";
    private const string AccessTokenSafetyMargin = "60"; // Refresh 60 seconds before expiry

    public WoltersKluwerOAuthService(
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        IMemoryCache cache,
        AppDbContext dbContext,
        ILogger<WoltersKluwerOAuthService> logger,
        IHostEnvironment env)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _clientSecret = clientSecret ?? throw new ArgumentNullException(nameof(clientSecret));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _env = env ?? throw new ArgumentNullException(nameof(env));

        // Usar SIEMPRE los endpoints reales de Wolters Kluwer (no mock)
        _authorizeEndpoint = "https://login.wolterskluwer.eu/auth/core/connect/authorize";
        _tokenEndpoint = "https://login.wolterskluwer.eu/auth/core/connect/token";

        _logger.LogInformation("[WoltersKluwer] ✅ Usando endpoints reales de Wolters Kluwer");
    }

    public string GetAuthorizeUrl(string redirectUri, string state, string nonce)
    {
        // Generar PKCE: code_verifier y code_challenge (WK lo requiere)
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Guardar code_verifier en caché para usarlo en ExchangeAuthorizationCodeAsync
        _cache.Set(CodeVerifierCacheKey, codeVerifier, TimeSpan.FromMinutes(10));

        // Construir URL de autorización con PKCE
        var authorizeUrl = $"{_authorizeEndpoint}?" +
            $"client_id={Uri.EscapeDataString(_clientId)}&" +
            $"response_type=code&" +
            $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
            $"scope={Uri.EscapeDataString(_scopes)}&" +
            $"state={Uri.EscapeDataString(state)}&" +
            $"nonce={Uri.EscapeDataString(nonce)}&" +
            $"code_challenge={Uri.EscapeDataString(codeChallenge)}&" +
            $"code_challenge_method=S256";

        _logger.LogInformation($"[WoltersKluwer] Authorize URL con PKCE: {authorizeUrl}");
        return authorizeUrl;
    }

    private string GenerateCodeVerifier()
    {
        // PKCE code_verifier: 43-128 caracteres ASCII (números, letras, - . _ ~)
        const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new Random();
        var verifier = new StringBuilder();
        for (int i = 0; i < 128; i++)
        {
            verifier.Append(allowedChars[random.Next(allowedChars.Length)]);
        }
        return verifier.ToString();
    }

    private string GenerateCodeChallenge(string codeVerifier)
    {
        // code_challenge = BASE64URL(SHA256(code_verifier))
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            return Base64UrlEncode(hash);
        }
    }

    private string Base64UrlEncode(byte[] data)
    {
        // Base64url encoding: no padding, replace + with -, / with _
        var base64 = Convert.ToBase64String(data);
        return base64.Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }

    public async Task ExchangeAuthorizationCodeAsync(string code, string redirectUri, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("[WoltersKluwer] Exchanging authorization code for tokens...");

            // Recuperar code_verifier del caché (se guardó en GetAuthorizeUrl)
            if (!_cache.TryGetValue(CodeVerifierCacheKey, out var codeVerifierObj))
            {
                throw new InvalidOperationException("code_verifier not found in cache. PKCE flow was not initiated.");
            }
            var codeVerifier = codeVerifierObj as string;
            if (string.IsNullOrEmpty(codeVerifier))
            {
                throw new InvalidOperationException("code_verifier is empty");
            }

            var tokenParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("code_verifier", codeVerifier),
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(tokenParams)
            };

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[WoltersKluwer] OAuth Error {response.StatusCode}: {errorContent}");
            }
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                throw new InvalidOperationException("No access_token en la respuesta de OAuth");
            }

            // Save tokens to database (update or create, always using the most recent)
            var existingToken = _dbContext.A3InnuvaOAuthTokens
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefault();
            var now = DateTime.UtcNow;

            if (existingToken == null)
            {
                var newToken = new A3InnuvaOAuthToken
                {
                    AccessToken = tokenResponse.AccessToken,
                    RefreshToken = tokenResponse.RefreshToken,
                    TokenType = tokenResponse.TokenType,
                    AccessTokenExpiresAt = now.AddSeconds(tokenResponse.ExpiresIn),
                    RefreshTokenExpiresAt = now.AddDays(30), // Assume 30 days for sliding window
                    LastSyncAt = now,
                    IsValid = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                };
                _dbContext.A3InnuvaOAuthTokens.Add(newToken);
            }
            else
            {
                existingToken.AccessToken = tokenResponse.AccessToken;
                existingToken.RefreshToken = tokenResponse.RefreshToken;
                existingToken.TokenType = tokenResponse.TokenType;
                existingToken.AccessTokenExpiresAt = now.AddSeconds(tokenResponse.ExpiresIn);
                existingToken.RefreshTokenExpiresAt = now.AddDays(30);
                existingToken.LastSyncAt = now;
                existingToken.IsValid = true;
                existingToken.UpdatedAt = now;
            }

            await _dbContext.SaveChangesAsync(ct);

            // Cache the token
            _cache.Set(TokenCacheKey, tokenResponse.AccessToken, TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60));

            // Limpiar code_verifier del caché (ya no se necesita)
            _cache.Remove(CodeVerifierCacheKey);

            _logger.LogInformation($"[WoltersKluwer] ✅ Tokens exchanged and saved");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WoltersKluwer] Error exchanging authorization code");
            throw;
        }
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Try to get from cache first
        if (_cache.TryGetValue(TokenCacheKey, out var cachedToken))
        {
            var cached = cachedToken as string;
            if (!string.IsNullOrEmpty(cached))
            {
                _logger.LogInformation("[WoltersKluwer] Token obtenido del cache");
                return cached;
            }
        }

        try
        {
            // Try to get from database (use the most recent one)
            var storedToken = _dbContext.A3InnuvaOAuthTokens
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefault();
            if (storedToken == null)
            {
                throw new InvalidOperationException(
                    "No OAuth token found in database. Please complete the authorization flow first.");
            }

            // Check if access token is still valid
            var now = DateTime.UtcNow;
            if (storedToken.AccessTokenExpiresAt.HasValue &&
                storedToken.AccessTokenExpiresAt.Value > now.AddSeconds(int.Parse(AccessTokenSafetyMargin)))
            {
                if (!string.IsNullOrEmpty(storedToken.AccessToken))
                {
                    _logger.LogInformation("[WoltersKluwer] Token obtenido de BD (aún válido)");
                    _cache.Set(TokenCacheKey, storedToken.AccessToken,
                        storedToken.AccessTokenExpiresAt.Value - now);
                    return storedToken.AccessToken;
                }
            }

            // If access token expired or invalid, try to refresh
            if (!string.IsNullOrEmpty(storedToken.RefreshToken) &&
                (storedToken.RefreshTokenExpiresAt == null || storedToken.RefreshTokenExpiresAt > now))
            {
                _logger.LogInformation("[WoltersKluwer] Access token expirado, refrescando...");
                await RefreshAccessTokenAsync(ct);

                // Get refreshed token from cache
                if (_cache.TryGetValue(TokenCacheKey, out var refreshedToken))
                {
                    return (refreshedToken as string) ?? throw new InvalidOperationException("Failed to refresh token");
                }
            }

            throw new InvalidOperationException("Access token expired and cannot be refreshed. Please re-authorize.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WoltersKluwer] Error obtaining access token");
            throw;
        }
    }

    public async Task RefreshAccessTokenAsync(CancellationToken ct = default)
    {
        try
        {
            var storedToken = _dbContext.A3InnuvaOAuthTokens
                .OrderByDescending(t => t.UpdatedAt)
                .FirstOrDefault();
            if (storedToken == null || string.IsNullOrEmpty(storedToken.RefreshToken))
            {
                throw new InvalidOperationException("No refresh token available");
            }

            _logger.LogInformation("[WoltersKluwer] Refreshing access token...");

            var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", storedToken.RefreshToken),
                    new KeyValuePair<string, string>("client_id", _clientId),
                    new KeyValuePair<string, string>("client_secret", _clientSecret),
                })
            };

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[WoltersKluwer] Refresh Error {response.StatusCode}: {errorContent}");
                storedToken.IsValid = false;
            }
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            if (string.IsNullOrEmpty(tokenResponse?.AccessToken))
            {
                throw new InvalidOperationException("No access_token en la respuesta de refresh");
            }

            var now = DateTime.UtcNow;
            storedToken.AccessToken = tokenResponse.AccessToken;
            if (!string.IsNullOrEmpty(tokenResponse.RefreshToken))
                storedToken.RefreshToken = tokenResponse.RefreshToken;
            storedToken.TokenType = tokenResponse.TokenType;
            storedToken.AccessTokenExpiresAt = now.AddSeconds(tokenResponse.ExpiresIn);
            storedToken.LastSyncAt = now;
            storedToken.UpdatedAt = now;

            await _dbContext.SaveChangesAsync(ct);

            // Cache the new token
            _cache.Set(TokenCacheKey, tokenResponse.AccessToken, TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 60));

            _logger.LogInformation($"[WoltersKluwer] ✅ Access token refrescado");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WoltersKluwer] Error refreshing access token");
            throw;
        }
    }

    public void InvalidateToken()
    {
        _cache.Remove(TokenCacheKey);
        _logger.LogInformation("[WoltersKluwer] Token invalidado del cache");
    }


    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }
    }
}
