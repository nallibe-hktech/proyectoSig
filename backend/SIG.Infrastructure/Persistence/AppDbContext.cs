using Microsoft.EntityFrameworkCore;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using Action = SIG.Domain.Entities.Action;

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
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectCostCenter> ProjectCostCenters => Set<ProjectCostCenter>();
    public DbSet<ProjectUser> ProjectUsers => Set<ProjectUser>();
    public DbSet<Action> Actions => Set<Action>();
    public DbSet<ActionConcept> ActionConcepts => Set<ActionConcept>();
    public DbSet<ActionUser> ActionUsers => Set<ActionUser>();
    public DbSet<Concept> Concepts => Set<Concept>();
    public DbSet<ConceptUser> ConceptUsers => Set<ConceptUser>();
    public DbSet<TarifaProyecto> TarifasProyecto => Set<TarifaProyecto>();
    public DbSet<PresupuestoProyecto> PresupuestosProyecto => Set<PresupuestoProyecto>();
    public DbSet<Variable> Variables => Set<Variable>();
    public DbSet<Period> Periods => Set<Period>();
    public DbSet<Closure> Closures => Set<Closure>();
    public DbSet<ClosureLine> ClosureLines => Set<ClosureLine>();
    public DbSet<Approval> Approvals => Set<Approval>();
    public DbSet<ApprovalHistory> ApprovalHistory => Set<ApprovalHistory>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CalculationLog> CalculationLogs => Set<CalculationLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StagingCeleroVisita> StagingCeleroVisitas => Set<StagingCeleroVisita>();
    public DbSet<StagingBizneoEmpleado> StagingBizneoEmpleados => Set<StagingBizneoEmpleado>();
    public DbSet<StagingBizneoHora> StagingBizneoHoras => Set<StagingBizneoHora>();
    public DbSet<StagingIntratimeFichaje> StagingIntratimeFichajes => Set<StagingIntratimeFichaje>();
    public DbSet<StagingPayHawkGasto> StagingPayHawkGastos => Set<StagingPayHawkGasto>();
    public DbSet<CeleroResourceMapping> CeleroResourceMappings => Set<CeleroResourceMapping>();
    public DbSet<CeleroServiceMapping> CeleroServiceMappings => Set<CeleroServiceMapping>();
    public DbSet<CeleroMissionMapping> CeleroMissionMappings => Set<CeleroMissionMapping>();
    public DbSet<StagingSgpvVisita> StagingSgpvVisitas => Set<StagingSgpvVisita>();
    public DbSet<StagingSgpvProducto> StagingSgpvProductos => Set<StagingSgpvProducto>();
    public DbSet<StagingA3InnuvaEmpleado> StagingA3InnuvaEmpleados => Set<StagingA3InnuvaEmpleado>();
    public DbSet<StagingTravelPerkViaje> StagingTravelPerkViajes => Set<StagingTravelPerkViaje>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
