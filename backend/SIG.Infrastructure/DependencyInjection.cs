using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SIG.Application.Calculation;
using SIG.Application.Interfaces.Integrations;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Application.Services;
using SIG.Infrastructure.Integrations.Fake;
using SIG.Infrastructure.Integrations.Http;
using SIG.Infrastructure.Integrations.Postgres;
using SIG.Infrastructure.Persistence;
using SIG.Infrastructure.Persistence.Interceptors;
using SIG.Infrastructure.Repositories;
using SIG.Infrastructure.Seed;
using SIG.Infrastructure.Services;

namespace SIG.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connStr = config.GetConnectionString("Default")
                      ?? throw new InvalidOperationException("ConnectionStrings:Default no configurada");

        services.AddScoped<TimestampsInterceptor>();
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connStr,
                    npg => npg.MigrationsAssembly("SIG.Infrastructure"))
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(
                        sp.GetRequiredService<TimestampsInterceptor>(),
                        sp.GetRequiredService<AuditInterceptor>())
                   .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IConceptRepository, ConceptRepository>();
        services.AddScoped<IVariableRepository, VariableRepository>();
        services.AddScoped<IPeriodRepository, PeriodRepository>();
        services.AddScoped<IClosureRepository, ClosureRepository>();
        services.AddScoped<IClosureLineRepository, ClosureLineRepository>();
        services.AddScoped<IApprovalRepository, ApprovalRepository>();
        services.AddScoped<ICalculationLogRepository, CalculationLogRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<ICostCenterRepository, CostCenterRepository>();
        services.AddScoped<ICeleroMappingRepository, CeleroMappingRepository>();
        services.AddScoped<ITarifaServicioRepository, TarifaServicioRepository>();
        services.AddScoped<IPresupuestoServicioRepository, PresupuestoServicioRepository>();
        services.AddScoped<IClosureAlertaRepository, ClosureAlertaRepository>();
        services.AddScoped<IStagingA3InnuvaContratoRepository, StagingA3InnuvaContratoRepository>();
        services.AddScoped(typeof(IStagingRepository<>), typeof(StagingRepository<>));

        // Services
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<IConceptService, ConceptService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ICostCenterService, CostCenterService>();
        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<ITarifaServicioService, TarifaServicioService>();
        services.AddScoped<IPresupuestoServicioService, PresupuestoServicioService>();
        services.AddScoped<IContratoService, ContratoService>();
        services.AddScoped<IClosureService, ClosureService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IClosureValidationService, ClosureValidationService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICalculationService, CalculationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<ICeleroVisitaService, CeleroVisitaService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ISeedService, DataSeeder>();
        services.AddScoped<IDataProcessorService, DataProcessorService>();
        services.AddScoped<IGalanService, GalanService>();
        services.AddScoped<IMediapostService, MediapostService>();
        services.AddScoped<GalanSyncService>();
        services.AddScoped<MediapostSyncService>();

        // Calculation
        services.AddScoped<IFormulaParser, FormulaParser>();
        services.AddScoped<ICalculationDataLoader, CalculationDataLoader>();
        services.AddScoped<IVariableResolver, VariableResolver>();
        services.AddScoped<ICalculationEngine, CalculationEngine>();

        // Integraciones
        var useFake = config.GetValue<bool>("Integrations:UseFake");
        if (useFake)
        {
            services.AddSingleton<ICeleroClient, CeleroFakeClient>();
            services.AddSingleton<IBizneoClient, BizneoFakeClient>();
            services.AddSingleton<IIntratimeClient, IntratimeFakeClient>();
            services.AddSingleton<IPayHawkClient, PayHawkFakeClient>();
            services.AddSingleton<ISgpvClient, SgpvFakeClient>();
            services.AddSingleton<IA3InnuvaClient, A3InnuvaFakeClient>();
            services.AddSingleton<ITravelPerkClient, TravelPerkFakeClient>();
            services.AddSingleton<IGalanClient, GalanCsvClient>();
            services.AddSingleton<IMediapostClient, MediapostExcelClient>();
        }
        else
        {
            // Celero: use PostgreSQL client
            var celeroConnStr = config.GetConnectionString("Celero")
                              ?? throw new InvalidOperationException("ConnectionStrings:Celero no configurada");
            services.AddScoped<ICeleroClient>(sp =>
                new CeleroPostgresClient(celeroConnStr));

            // Bizneo
            var bizneoUrl = config["Integrations:Bizneo:BaseUrl"]
                          ?? throw new InvalidOperationException("Integrations:Bizneo:BaseUrl no configurada");
            var bizneoKey = config["Integrations:Bizneo:ApiKey"]
                          ?? throw new InvalidOperationException("Integrations:Bizneo:ApiKey no configurada");
            services.AddHttpClient("bizneo", client => client.BaseAddress = new Uri(bizneoUrl));
            services.AddScoped<IBizneoClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("bizneo");
                return new BizneoClient(client, bizneoKey);
            });

            // Intratime
            var intratimeUrl = config["Integrations:Intratime:BaseUrl"]
                             ?? throw new InvalidOperationException("Integrations:Intratime:BaseUrl no configurada");
            var intratimeToken = config["Integrations:Intratime:ApiToken"]
                               ?? throw new InvalidOperationException("Integrations:Intratime:ApiToken no configurada");
            var intratimeCompanyId = config.GetValue<int>("Integrations:Intratime:CompanyId");
            var intratimeUserEmail = config["Integrations:Intratime:UserEmail"];
            var intratimeUserPassword = config["Integrations:Intratime:UserPassword"];

            services.AddHttpClient("intratime", client => client.BaseAddress = new Uri(intratimeUrl));
            services.AddScoped<IIntratimeClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("intratime");
                var logger = sp.GetRequiredService<ILogger<IntratimeClient>>();
                return new IntratimeClient(client, intratimeToken, intratimeCompanyId, logger, intratimeUserEmail, intratimeUserPassword);
            });

            // PayHawk
            var payhawkUrl = config["Integrations:PayHawk:BaseUrl"]
                           ?? throw new InvalidOperationException("Integrations:PayHawk:BaseUrl no configurada");
            var payhawkKey = config["Integrations:PayHawk:ApiKey"]
                           ?? throw new InvalidOperationException("Integrations:PayHawk:ApiKey no configurada");
            var payhawkAccountId = config["Integrations:PayHawk:AccountId"]
                                ?? throw new InvalidOperationException("Integrations:PayHawk:AccountId no configurada");
            services.AddHttpClient("payhawk", client =>
            {
                client.BaseAddress = new Uri(payhawkUrl);
                client.DefaultRequestHeaders.Add("X-Payhawk-ApiKey", payhawkKey);
            });
            services.AddScoped<IPayHawkClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient("payhawk");
                var logger = sp.GetRequiredService<ILogger<PayHawkClient>>();
                return new PayHawkClient(httpClient, payhawkAccountId, logger);
            });

            // Sgpv
            var sgpvUrl = config["Integrations:Sgpv:BaseUrl"]
                       ?? throw new InvalidOperationException("Integrations:Sgpv:BaseUrl no configurada");
            var sgpvUsername = config["Integrations:Sgpv:Username"]
                            ?? throw new InvalidOperationException("Integrations:Sgpv:Username no configurada");
            var sgpvPassword = config["Integrations:Sgpv:Password"]
                            ?? throw new InvalidOperationException("Integrations:Sgpv:Password no configurada");
            services.AddHttpClient("sgpv", client => client.BaseAddress = new Uri(sgpvUrl));
            services.AddScoped<ISgpvClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("sgpv");
                return new SgpvClient(client, sgpvUsername, sgpvPassword);
            });

            // A3 Innuva
            var a3InuvaUrl = config["Integrations:A3Innuva:BaseUrl"]
                           ?? throw new InvalidOperationException("Integrations:A3Innuva:BaseUrl no configurada");
            var a3InuvaKey = config["Integrations:A3Innuva:ApiKey"]
                           ?? throw new InvalidOperationException("Integrations:A3Innuva:ApiKey no configurada");
            services.AddHttpClient<IA3InnuvaClient, A3InnuvaClient>(client =>
            {
                client.BaseAddress = new Uri(a3InuvaUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {a3InuvaKey}");
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", a3InuvaKey);
            });

            // Travel Perk
            var travelPerkUrl = config["Integrations:TravelPerk:BaseUrl"]
                              ?? throw new InvalidOperationException("Integrations:TravelPerk:BaseUrl no configurada");
            var travelPerkKey = config["Integrations:TravelPerk:ApiKey"]
                              ?? throw new InvalidOperationException("Integrations:TravelPerk:ApiKey no configurada");
            services.AddHttpClient<ITravelPerkClient, TravelPerkClient>(client =>
            {
                client.BaseAddress = new Uri(travelPerkUrl);
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {travelPerkKey}");
            });

            // Galán (CSV client for local file reading)
            services.AddSingleton<IGalanClient, GalanCsvClient>();

            // Mediapost (Excel client for local file reading)
            services.AddSingleton<IMediapostClient, MediapostExcelClient>();
        }

        return services;
    }
}
