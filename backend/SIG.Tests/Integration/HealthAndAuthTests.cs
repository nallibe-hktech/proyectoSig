using System.Net;
using System.Net.Http.Json;
using SIG.Application.DTOs;

namespace SIG.Tests.Integration;

[Collection("Integration")]
public class HealthAndAuthTests : IntegrationTestBase
{
    public HealthAndAuthTests(IntegrationTestFixture fixture) : base(fixture)
    {
        Fixture.EnsureSeedAsync().GetAwaiter().GetResult();
    }

    // === /api/health ===

    [Fact]
    public async Task GetHealth_DevuelveOk()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/health");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("\"status\"").And.Contain("ok");
    }

    [Fact]
    public async Task GetHealth_NoRequiereAuth()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/health");
        resp.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    // === /api/auth/login ===

    [Fact]
    public async Task PostLogin_CredencialesValidas_Devuelve200ConToken()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@sig.local", "Demo#2026!"));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<LoginResponse>(resp);
        body.Should().NotBeNull();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
        body.User.Email.Should().Be("admin@sig.local");
        body.User.Roles.Should().Contain("Administrator");
    }

    [Fact]
    public async Task PostLogin_PasswordIncorrecta_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@sig.local", "wrong-password"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLogin_UsuarioInexistente_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("nobody@nowhere.com", "Demo#2026!"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostLogin_EmailInvalido_Devuelve400()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("not-email", "Demo#2026!"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostLogin_PasswordCorta_Devuelve400()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("admin@sig.local", "123"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // === /api/auth/refresh ===

    [Fact]
    public async Task PostRefresh_RefreshTokenValido_Devuelve200ConNuevoToken()
    {
        var client = CreateClient();
        var (_, refreshToken) = await Fixture.LoginAsync(client, "admin@sig.local", "Demo#2026!");
        var resp = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(refreshToken));
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<RefreshResponse>(resp);
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PostRefresh_RefreshTokenInvalido_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest("token-invalido"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === /api/auth/me ===

    [Fact]
    public async Task GetMe_ConToken_DevuelveDatosDelUsuario()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.GetAsync("/api/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync<UsuarioBriefDto>(resp);
        body!.Email.Should().Be("admin@sig.local");
    }

    [Fact]
    public async Task GetMe_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("/api/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // === /api/auth/logout ===

    [Fact]
    public async Task PostLogout_ConToken_Devuelve204()
    {
        var client = await CreateAuthenticatedClientAsync();
        var resp = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(null));
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PostLogout_SinToken_Devuelve401()
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(null));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
