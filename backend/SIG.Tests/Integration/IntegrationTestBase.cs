using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Services;
using SIG.Infrastructure.Persistence;

namespace SIG.Tests.Integration;

/// <summary>
/// Base con factory compartida (collection) que arranca la API real contra
/// la BD de tests, aplica migraciones y siembra usuarios reales con BCrypt.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<IntegrationTestFixture>
{
    protected readonly IntegrationTestFixture Fixture;
    protected CustomWebApplicationFactory Factory => Fixture.Factory;

    protected IntegrationTestBase(IntegrationTestFixture fixture)
    {
        Fixture = fixture;
    }

    protected HttpClient CreateClient() => Factory.CreateClient();

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string? email = null, string? password = null)
    {
        email ??= Fixture.TestUserEmail;
        password ??= Fixture.TestUserPassword;
        var client = Factory.CreateClient();
        var (token, _) = await Fixture.LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage resp)
    {
        var s = await resp.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(s)) return default;
        return JsonSerializer.Deserialize<T>(s, CustomWebApplicationFactory.JsonOpts);
    }

    protected static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value, CustomWebApplicationFactory.JsonOpts), Encoding.UTF8, "application/json");

    protected AppDbContext NewDbContext() => Factory.NewDbContext();
}

public class IntegrationTestFixture : IAsyncLifetime
{
    private static readonly SemaphoreSlim _seedLock = new(1, 1);
    public CustomWebApplicationFactory Factory { get; } = new();
    public string TestUserEmail { get; private set; } = string.Empty;
    public string TestUserPassword { get; private set; } = string.Empty;

    public Task InitializeAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
        TestUserEmail = config["TestUser:Email"] ?? "admin@sig.local";
        TestUserPassword = config["TestUser:Password"] ?? throw new InvalidOperationException("TestUser:Password no configurada en appsettings.Testing.json");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        Factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Asegura que tablas y seed están listos. Idempotente y thread-safe.</summary>
    public async Task EnsureSeedAsync()
    {
        await _seedLock.WaitAsync();
        try
        {
            Factory.EnsureDatabaseInitialized();
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!await db.Users.IgnoreQueryFilters().AnyAsync())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<ISeedService>();
                await seeder.RunIfEmptyAsync(CancellationToken.None);
            }
        }
        finally { _seedLock.Release(); }
    }

    public async Task<(string accessToken, string refreshToken)> LoginAsync(HttpClient client, string email, string password)
    {
        await EnsureSeedAsync();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        resp.EnsureSuccessStatusCode();
        var loginResp = await resp.Content.ReadFromJsonAsync<LoginResponse>(CustomWebApplicationFactory.JsonOpts);
        return (loginResp!.AccessToken, loginResp.RefreshToken);
    }
}

[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<IntegrationTestFixture> { }
