using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SIG.Application.Interfaces.Integrations;
using SIG.Infrastructure.Persistence;

namespace SIG.Tests.Integration;

/// <summary>
/// Factory para tests de integración: arranca la API con ASPNETCORE_ENVIRONMENT=Testing,
/// usa la base de datos sig_plataforma_tests, aplica migraciones, y permite registrar
/// mocks de los clientes de integraciones externas (Celero/Bizneo/Intratime/PayHawk).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private static readonly object _lock = new();
    private static bool _migrated;
    public static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public Action<IServiceCollection>? OverrideServices { get; set; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // ContentRoot al directorio del proyecto API para que se carguen los appsettings.*.json correctos
        builder.UseContentRoot(AppContext.BaseDirectory);

        builder.ConfigureAppConfiguration((ctx, cb) =>
        {
            // Cargar el appsettings.Testing.json copiado a la carpeta de salida (con sig_plataforma_tests).
            // Se añade DESPUÉS de los appsettings de SIG.API para sobrescribir cualquier override de Default.
            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.Testing.json");
            if (File.Exists(path))
            {
                cb.AddJsonFile(path, optional: false, reloadOnChange: false);
            }
        });

        builder.ConfigureServices(services =>
        {
            // Tests pueden re-mockear integrations
            OverrideServices?.Invoke(services);
        });
    }

    /// <summary>Inicializa la DB de tests una sola vez por proceso. Idempotente.</summary>
    public void EnsureDatabaseInitialized()
    {
        if (_migrated) return;
        lock (_lock)
        {
            if (_migrated) return;
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            _migrated = true;
        }
    }

    public AppDbContext NewDbContext()
    {
        var scope = Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    /// <summary>Limpia tablas transaccionales (no maestros) para tests independientes.</summary>
    public async Task ResetTransactionalDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                audit_logs,
                calculation_logs,
                approval_history,
                approvals,
                closure_alertas,
                closure_lines,
                cierres_costes,
                cierres_facturacion,
                refresh_tokens
            RESTART IDENTITY CASCADE;
        """);
    }
}
