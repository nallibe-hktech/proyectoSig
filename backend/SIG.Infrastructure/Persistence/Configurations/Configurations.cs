using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using Action = SIG.Domain.Entities.Action;

namespace SIG.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.HasIndex(u => u.NIF).IsUnique();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.NIF).HasMaxLength(20).IsRequired();
        b.Property(u => u.Email).HasMaxLength(200).IsRequired();
        b.Property(u => u.Nombre).HasMaxLength(100).IsRequired();
        b.Property(u => u.Apellidos).HasMaxLength(200).IsRequired();
        b.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        b.Property(u => u.Estado).HasConversion<string>().HasMaxLength(20);
        b.HasOne(u => u.Department).WithMany(d => d.Users).HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasQueryFilter(u => !u.IsDeleted);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.HasIndex(r => r.Nombre).IsUnique();
        b.Property(r => r.Nombre).HasMaxLength(50).IsRequired();
        b.Property(r => r.Descripcion).HasMaxLength(500);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.HasKey(ur => new { ur.UserId, ur.RoleId });
        b.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
        b.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.Property(d => d.Nombre).HasMaxLength(100).IsRequired();
        b.HasQueryFilter(d => !d.IsDeleted);
    }
}

public class CostCenterConfiguration : IEntityTypeConfiguration<CostCenter>
{
    public void Configure(EntityTypeBuilder<CostCenter> b)
    {
        b.HasIndex(c => c.Codigo).IsUnique();
        b.Property(c => c.Codigo).HasMaxLength(10).IsRequired();
        b.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
        b.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> b)
    {
        b.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
        b.Property(c => c.NIF).HasMaxLength(20).IsRequired();
        b.Property(c => c.Direccion).HasMaxLength(500);
        b.Property(c => c.Ciudad).HasMaxLength(100);
        b.Property(c => c.Provincia).HasMaxLength(100);
        b.Property(c => c.Pais).HasMaxLength(100);
        b.Property(c => c.CodigoPostal).HasMaxLength(20);
        b.Property(c => c.ContactoEmail).HasMaxLength(200);
        b.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        b.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
        b.HasOne(p => p.Client).WithMany(c => c.Projects).HasForeignKey(p => p.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}

public class ProjectCostCenterConfiguration : IEntityTypeConfiguration<ProjectCostCenter>
{
    public void Configure(EntityTypeBuilder<ProjectCostCenter> b)
    {
        b.HasKey(x => new { x.ProjectId, x.CostCenterId });
        b.HasOne(x => x.Project).WithMany(p => p.ProjectCostCenters).HasForeignKey(x => x.ProjectId);
        b.HasOne(x => x.CostCenter).WithMany(c => c.ProjectCostCenters).HasForeignKey(x => x.CostCenterId);
    }
}

public class ProjectUserConfiguration : IEntityTypeConfiguration<ProjectUser>
{
    public void Configure(EntityTypeBuilder<ProjectUser> b)
    {
        b.HasKey(x => new { x.ProjectId, x.UserId });
        b.HasOne(x => x.Project).WithMany(p => p.ProjectUsers).HasForeignKey(x => x.ProjectId);
        b.HasOne(x => x.User).WithMany(u => u.ProjectUsers).HasForeignKey(x => x.UserId);
    }
}

public class ActionConfiguration : IEntityTypeConfiguration<Action>
{
    public void Configure(EntityTypeBuilder<Action> b)
    {
        b.Property(a => a.Nombre).HasMaxLength(200).IsRequired();
        b.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
        b.HasOne(a => a.Project).WithMany(p => p.Actions).HasForeignKey(a => a.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Client).WithMany().HasForeignKey(a => a.ClientId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.Department).WithMany().HasForeignKey(a => a.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        b.HasQueryFilter(a => !a.IsDeleted);
    }
}

public class ActionConceptConfiguration : IEntityTypeConfiguration<ActionConcept>
{
    public void Configure(EntityTypeBuilder<ActionConcept> b)
    {
        b.HasKey(x => new { x.ActionId, x.ConceptId });
        b.HasOne(x => x.Action).WithMany(a => a.ActionConcepts).HasForeignKey(x => x.ActionId);
        b.HasOne(x => x.Concept).WithMany(c => c.ActionConcepts).HasForeignKey(x => x.ConceptId);
    }
}

public class ActionUserConfiguration : IEntityTypeConfiguration<ActionUser>
{
    public void Configure(EntityTypeBuilder<ActionUser> b)
    {
        b.HasKey(x => new { x.ActionId, x.UserId });
        b.HasOne(x => x.Action).WithMany(a => a.ActionUsers).HasForeignKey(x => x.ActionId);
        b.HasOne(x => x.User).WithMany(u => u.ActionUsers).HasForeignKey(x => x.UserId);
    }
}

public class ConceptConfiguration : IEntityTypeConfiguration<Concept>
{
    public void Configure(EntityTypeBuilder<Concept> b)
    {
        b.Property(c => c.Nombre).HasMaxLength(200).IsRequired();
        b.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.FormulaJson).HasColumnType("jsonb").IsRequired();
        b.Property(c => c.ColumnaA3).HasMaxLength(50);
        b.HasOne(c => c.Project)
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(c => c.ProjectId);
        b.HasQueryFilter(c => !c.IsDeleted);
    }
}

public class ConceptUserConfiguration : IEntityTypeConfiguration<ConceptUser>
{
    public void Configure(EntityTypeBuilder<ConceptUser> b)
    {
        b.HasKey(x => new { x.ConceptId, x.UserId });
        b.HasOne(x => x.Concept).WithMany(c => c.ConceptUsers).HasForeignKey(x => x.ConceptId);
        b.HasOne(x => x.User).WithMany(u => u.ConceptUsers).HasForeignKey(x => x.UserId);
    }
}

public class TarifaProyectoConfiguration : IEntityTypeConfiguration<TarifaProyecto>
{
    public void Configure(EntityTypeBuilder<TarifaProyecto> b)
    {
        b.Property(t => t.Nombre).HasMaxLength(200).IsRequired();
        b.Property(t => t.Valor).HasPrecision(18, 4);
        b.Property(t => t.Unidad).HasMaxLength(50);
        b.HasOne(t => t.Project)
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(t => t.ProjectId);
        b.HasQueryFilter(t => !t.IsDeleted);
    }
}

public class PresupuestoProyectoConfiguration : IEntityTypeConfiguration<PresupuestoProyecto>
{
    public void Configure(EntityTypeBuilder<PresupuestoProyecto> b)
    {
        b.Property(p => p.Importe).HasPrecision(18, 4);
        b.Property(p => p.Descripcion).HasMaxLength(500);
        b.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(20);
        b.HasOne(p => p.Project)
            .WithMany()
            .HasForeignKey(p => p.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(p => p.Period)
            .WithMany()
            .HasForeignKey(p => p.PeriodId)
            .OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(p => new { p.ProjectId, p.PeriodId });
        b.HasQueryFilter(p => !p.IsDeleted);
    }
}

public class VariableConfiguration : IEntityTypeConfiguration<Variable>
{
    public void Configure(EntityTypeBuilder<Variable> b)
    {
        b.Property(v => v.Nombre).HasMaxLength(100).IsRequired();
        b.Property(v => v.QuestionIdExterno).HasMaxLength(100).IsRequired();
        b.Property(v => v.MapeoValoresJson).HasColumnType("jsonb").IsRequired();
        b.HasQueryFilter(v => !v.IsDeleted);
    }
}

public class PeriodConfiguration : IEntityTypeConfiguration<Period>
{
    public void Configure(EntityTypeBuilder<Period> b)
    {
        b.HasIndex(p => p.Nombre).IsUnique();
        b.Property(p => p.Nombre).HasMaxLength(100).IsRequired();
        b.Property(p => p.Estado).HasConversion<string>().HasMaxLength(20);
    }
}

public class ClosureConfiguration : IEntityTypeConfiguration<Closure>
{
    public void Configure(EntityTypeBuilder<Closure> b)
    {
        b.HasIndex(c => new { c.ProjectId, c.PeriodId }).IsUnique();
        b.Property(c => c.CosteTotal).HasPrecision(18, 4);
        b.Property(c => c.FacturacionTotal).HasPrecision(18, 4);
        b.Property(c => c.Margen).HasPrecision(18, 4);
        b.Property(c => c.Estado).HasConversion<string>().HasMaxLength(30);
        b.Property(c => c.PasoActual).HasConversion<int>();
        b.Property(c => c.Comentarios).HasMaxLength(2000);
        b.Property(c => c.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        b.HasOne(c => c.Project).WithMany(p => p.Closures).HasForeignKey(c => c.ProjectId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(c => c.Period).WithMany(p => p.Closures).HasForeignKey(c => c.PeriodId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClosureLineConfiguration : IEntityTypeConfiguration<ClosureLine>
{
    public void Configure(EntityTypeBuilder<ClosureLine> b)
    {
        b.Property(c => c.Importe).HasPrecision(18, 4);
        b.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);
        b.Property(c => c.DatosEntradaJson).HasColumnType("jsonb").IsRequired();
        b.Property(c => c.RowVersion)
            .IsRowVersion()
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
        b.HasOne(c => c.Closure).WithMany(cl => cl.Lines).HasForeignKey(c => c.ClosureId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Concept).WithMany().HasForeignKey(c => c.ConceptId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(c => c.User).WithMany().HasForeignKey(c => c.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ApprovalConfiguration : IEntityTypeConfiguration<Approval>
{
    public void Configure(EntityTypeBuilder<Approval> b)
    {
        b.Property(a => a.Estado).HasConversion<string>().HasMaxLength(20);
        b.Property(a => a.Paso).HasConversion<int>();
        b.Property(a => a.Motivo).HasMaxLength(2000);
        b.HasOne(a => a.Closure).WithMany(c => c.Approvals).HasForeignKey(a => a.ClosureId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.Role).WithMany(r => r.Approvals).HasForeignKey(a => a.RoleId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ApprovalHistoryConfiguration : IEntityTypeConfiguration<ApprovalHistory>
{
    public void Configure(EntityTypeBuilder<ApprovalHistory> b)
    {
        b.Property(a => a.PasoOrigen).HasConversion<int>();
        b.Property(a => a.PasoDestino).HasConversion<int>();
        b.Property(a => a.Accion).HasMaxLength(50).IsRequired();
        b.Property(a => a.Motivo).HasMaxLength(2000);
        b.HasOne(a => a.Closure).WithMany(c => c.ApprovalHistory).HasForeignKey(a => a.ClosureId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.Property(a => a.EntityType).HasMaxLength(100).IsRequired();
        b.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
        b.Property(a => a.Action).HasConversion<string>().HasMaxLength(20);
        b.Property(a => a.OldValueJson).HasColumnType("jsonb");
        b.Property(a => a.NewValueJson).HasColumnType("jsonb");
        b.Property(a => a.Ip).HasMaxLength(64);
        b.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class CalculationLogConfiguration : IEntityTypeConfiguration<CalculationLog>
{
    public void Configure(EntityTypeBuilder<CalculationLog> b)
    {
        b.HasIndex(c => c.ClosureLineId).IsUnique();
        b.Property(c => c.FormulaSnapshotJson).HasColumnType("jsonb").IsRequired();
        b.Property(c => c.InputsJson).HasColumnType("jsonb").IsRequired();
        b.Property(c => c.Resultado).HasPrecision(18, 4);
        b.Property(c => c.SistemaOrigen).HasMaxLength(50).IsRequired();
        b.Property(c => c.Incidencias).HasColumnType("jsonb");
        b.HasOne(c => c.ClosureLine).WithOne(cl => cl.CalculationLog).HasForeignKey<CalculationLog>(c => c.ClosureLineId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.Concept).WithMany().HasForeignKey(c => c.ConceptId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.HasIndex(r => r.TokenHash).IsUnique();
        b.Property(r => r.TokenHash).HasMaxLength(200).IsRequired();
        b.Property(r => r.Ip).HasMaxLength(64);
        b.HasOne(r => r.User).WithMany(u => u.RefreshTokens).HasForeignKey(r => r.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

// Staging
public class StagingCeleroVisitaConfiguration : IEntityTypeConfiguration<StagingCeleroVisita>
{
    public void Configure(EntityTypeBuilder<StagingCeleroVisita> b)
    {
        b.HasIndex(s => s.Hash).IsUnique();
        b.Property(s => s.Hash).HasMaxLength(100).IsRequired();
        b.Property(s => s.VisitaIdExterno).HasMaxLength(100).IsRequired();
        b.Property(s => s.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(s => s.ErrorProcesamiento).HasMaxLength(2000);
    }
}

public class StagingBizneoEmpleadoConfiguration : IEntityTypeConfiguration<StagingBizneoEmpleado>
{
    public void Configure(EntityTypeBuilder<StagingBizneoEmpleado> b)
    {
        b.HasIndex(s => s.Hash).IsUnique();
        b.Property(s => s.Hash).HasMaxLength(100).IsRequired();
        b.Property(s => s.EmpleadoIdExterno).HasMaxLength(100).IsRequired();
        b.Property(s => s.NIF).HasMaxLength(20).IsRequired();
        b.Property(s => s.Nombre).HasMaxLength(200).IsRequired();
        b.Property(s => s.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(s => s.ErrorProcesamiento).HasMaxLength(2000);
    }
}

public class StagingBizneoHoraConfiguration : IEntityTypeConfiguration<StagingBizneoHora>
{
    public void Configure(EntityTypeBuilder<StagingBizneoHora> b)
    {
        b.HasIndex(s => s.Hash).IsUnique();
        b.Property(s => s.Hash).HasMaxLength(100).IsRequired();
        b.Property(s => s.RegistroIdExterno).HasMaxLength(100).IsRequired();
        b.Property(s => s.Horas).HasPrecision(18, 4);
        b.Property(s => s.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(s => s.ErrorProcesamiento).HasMaxLength(2000);
    }
}

public class StagingIntratimeFichajeConfiguration : IEntityTypeConfiguration<StagingIntratimeFichaje>
{
    public void Configure(EntityTypeBuilder<StagingIntratimeFichaje> b)
    {
        b.HasIndex(s => s.Hash).IsUnique();
        b.Property(s => s.Hash).HasMaxLength(100).IsRequired();
        b.Property(s => s.FichajeIdExterno).HasMaxLength(100).IsRequired();
        b.Property(s => s.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(s => s.ErrorProcesamiento).HasMaxLength(2000);
    }
}

public class StagingPayHawkGastoConfiguration : IEntityTypeConfiguration<StagingPayHawkGasto>
{
    public void Configure(EntityTypeBuilder<StagingPayHawkGasto> b)
    {
        b.HasIndex(s => s.Hash).IsUnique();
        b.Property(s => s.Hash).HasMaxLength(100).IsRequired();
        b.Property(s => s.GastoIdExterno).HasMaxLength(100).IsRequired();
        b.Property(s => s.Importe).HasPrecision(18, 4);
        b.Property(s => s.Categoria).HasMaxLength(100).IsRequired();
        b.Property(s => s.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(s => s.ErrorProcesamiento).HasMaxLength(2000);
    }
}

public class CeleroResourceMappingConfiguration : IEntityTypeConfiguration<CeleroResourceMapping>
{
    public void Configure(EntityTypeBuilder<CeleroResourceMapping> b)
    {
        b.HasIndex(m => m.CeleroNif).IsUnique();
        b.Property(m => m.CeleroNif).HasMaxLength(20).IsRequired();
        b.Property(m => m.Descripcion).HasMaxLength(500);
        b.HasOne(m => m.User).WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CeleroServiceMappingConfiguration : IEntityTypeConfiguration<CeleroServiceMapping>
{
    public void Configure(EntityTypeBuilder<CeleroServiceMapping> b)
    {
        b.HasIndex(m => m.CeleroServiceName).IsUnique();
        b.Property(m => m.CeleroServiceName).HasMaxLength(300).IsRequired();
        b.Property(m => m.Descripcion).HasMaxLength(500);
        b.HasOne(m => m.Project).WithMany().HasForeignKey(m => m.ProjectId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CeleroMissionMappingConfiguration : IEntityTypeConfiguration<CeleroMissionMapping>
{
    public void Configure(EntityTypeBuilder<CeleroMissionMapping> b)
    {
        b.HasIndex(m => m.CeleroMissionName).IsUnique();
        b.Property(m => m.CeleroMissionName).HasMaxLength(300).IsRequired();
        b.Property(m => m.Descripcion).HasMaxLength(500);
        b.HasOne(m => m.Action).WithMany().HasForeignKey(m => m.ActionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class StagingSgpvVisitaConfiguration : IEntityTypeConfiguration<StagingSgpvVisita>
{
    public void Configure(EntityTypeBuilder<StagingSgpvVisita> b)
    {
        b.HasIndex(s => s.Hash).IsUnique();
        b.Property(s => s.Hash).HasMaxLength(100).IsRequired();
        b.Property(s => s.VisitaIdExterno).HasMaxLength(100).IsRequired();
        b.Property(s => s.ResourceNif).HasMaxLength(20);
        b.Property(s => s.CentroId).HasMaxLength(100).IsRequired();
        b.Property(s => s.CentroNombre).HasMaxLength(200);
        b.Property(s => s.ServiceName).HasMaxLength(200);
        b.Property(s => s.HorasDuracion).HasPrecision(18, 4);
        b.Property(s => s.PayloadJson).HasColumnType("jsonb").IsRequired();
        b.Property(s => s.ErrorProcesamiento).HasMaxLength(2000);
    }
}
