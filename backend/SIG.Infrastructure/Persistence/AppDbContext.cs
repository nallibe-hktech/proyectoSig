using Microsoft.EntityFrameworkCore;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;

namespace SIG.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<CostCenter> CostCenters => Set<CostCenter>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ClienteIncidencia> ClienteIncidencias => Set<ClienteIncidencia>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceConcept> ServiceConcepts => Set<ServiceConcept>();
    public DbSet<ServiceUser> ServiceUsers => Set<ServiceUser>();
    public DbSet<ServiceCostCenter> ServiceCostCenters => Set<ServiceCostCenter>();
    public DbSet<Concept> Concepts => Set<Concept>();
    public DbSet<ConceptUser> ConceptUsers => Set<ConceptUser>();
    public DbSet<TarifaServicio> TarifasServicio => Set<TarifaServicio>();
    public DbSet<PresupuestoServicio> PresupuestosServicio => Set<PresupuestoServicio>();
    public DbSet<Forecast> Forecasts => Set<Forecast>();
    public DbSet<Variable> Variables => Set<Variable>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<CierreCostes> CierresCostes => Set<CierreCostes>();
    public DbSet<CierreFacturacion> CierresFacturacion => Set<CierreFacturacion>();
    public DbSet<ClosureLine> ClosureLines => Set<ClosureLine>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ApprovalHistory> ApprovalHistory => Set<ApprovalHistory>();
    public DbSet<ClosureAlerta> ClosureAlertas => Set<ClosureAlerta>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CalculationLog> CalculationLogs => Set<CalculationLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StagingCeleroVisita> StagingCeleroVisitas => Set<StagingCeleroVisita>();
    public DbSet<StagingBizneoEmpleado> StagingBizneoEmpleados => Set<StagingBizneoEmpleado>();
    public DbSet<StagingBizneoAbsence> StagingBizneoAbsences => Set<StagingBizneoAbsence>();
    public DbSet<StagingIntratimeFichaje> StagingIntratimeFichajes => Set<StagingIntratimeFichaje>();
    public DbSet<StagingIntratimeEmpleado> StagingIntratimeEmpleados => Set<StagingIntratimeEmpleado>();
    public DbSet<StagingIntratimeClockingRequest> StagingIntratimeClockingRequests => Set<StagingIntratimeClockingRequest>();
    public DbSet<StagingIntratimeExpense> StagingIntratimeExpenses => Set<StagingIntratimeExpense>();
    public DbSet<StagingPayHawkGasto> StagingPayHawkGastos => Set<StagingPayHawkGasto>();
    public DbSet<CeleroResourceMapping> CeleroResourceMappings => Set<CeleroResourceMapping>();
    public DbSet<CeleroServiceMapping> CeleroServiceMappings => Set<CeleroServiceMapping>();
    public DbSet<CeleroMissionMapping> CeleroMissionMappings => Set<CeleroMissionMapping>();
    public DbSet<StagingSgpvVisita> StagingSgpvVisitas => Set<StagingSgpvVisita>();
    public DbSet<StagingSgpvProducto> StagingSgpvProductos => Set<StagingSgpvProducto>();
    public DbSet<StagingA3InnuvaEmpleado> StagingA3InnuvaEmpleados => Set<StagingA3InnuvaEmpleado>();
    public DbSet<StagingA3InnuvaContrato> StagingA3InnuvaContratos => Set<StagingA3InnuvaContrato>();
    public DbSet<StagingTravelPerkViaje> StagingTravelPerkViajes => Set<StagingTravelPerkViaje>();

    // GALÁN - Logística
    public DbSet<StagingGalanEntrada> StagingGalanEntradas => Set<StagingGalanEntrada>();
    public DbSet<StagingGalanSalida> StagingGalanSalidas => Set<StagingGalanSalida>();
    public DbSet<StagingGalanStock> StagingGalanStocks => Set<StagingGalanStock>();

    // MEDIAPOST - Distribución
    public DbSet<StagingMediapostPedido> StagingMediapostPedidos => Set<StagingMediapostPedido>();
    public DbSet<StagingMediapostRecepcion> StagingMediapostRecepciones => Set<StagingMediapostRecepcion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Add UTC value converter for all DateTime properties to ensure Npgsql compatibility
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>(
                        v => v.Kind == DateTimeKind.Utc ? v : DateTime.SpecifyKind(v, DateTimeKind.Utc),
                        v => v
                    ));
                }
            }
        }

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
