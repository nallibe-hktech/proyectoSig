using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
                        sp.GetRequiredService<AuditInterceptor>());
        });

        // Repositorios
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IActionRepository, ActionRepository>();
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
        services.AddScoped(typeof(IStagingRepository<>), typeof(StagingRepository<>));

        // Services
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IActionService, ActionService>();
        services.AddScoped<IConceptService, ConceptService>();
        services.AddScoped<IVariableService, VariableService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<ICostCenterService, CostCenterService>();
        services.AddScoped<IPeriodService, PeriodService>();
        services.AddScoped<IClosureService, ClosureService>();
        services.AddScoped<IApprovalService, ApprovalService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICalculationService, CalculationService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ISeedService, DataSeeder>();

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
        }
        else
        {
            // Celero: use PostgreSQL client
            var celeroConnStr = config.GetConnectionString("Celero")
                              ?? throw new InvalidOperationException("ConnectionStrings:Celero no configurada");
            services.AddScoped<ICeleroClient>(sp =>
                new CeleroPostgresClient(celeroConnStr,
                    sp.GetRequiredService<IUserRepository>(),
                    sp.GetRequiredService<IProjectRepository>(),
                    sp.GetRequiredService<IActionRepository>()));

            // Other integrations: use HTTP clients
            services.AddHttpClient<IBizneoClient, BizneoClient>();
            services.AddHttpClient<IIntratimeClient, IntratimeClient>();
            services.AddHttpClient<IPayHawkClient, PayHawkClient>();
        }

        return services;
    }
}
