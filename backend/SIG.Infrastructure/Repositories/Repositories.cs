using Microsoft.EntityFrameworkCore;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Common;
using SIG.Domain.Entities;
using SIG.Domain.Entities.Staging;
using SIG.Domain.Enums;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Repositories;

// Owns visibility logic — administrative roles see everything; ProjectManager sees only owned services.
internal static class OwnershipHelper
{
    public static readonly string[] PrivilegedRoles = new[] { "Administrator", "Direction", "Fico", "Backoffice", "Auditor", "Reader" };
    // Ola 3a (#1): roles globales que (junto con la asignación al servicio) constituyen el "grupo" del servicio.
    public static readonly string[] GrupoRoles = new[] { "Facilitador", "Interlocutor", "Gestor" };
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) { _db = db; }

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<User?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct) =>
        // En este contexto, los usuarios solo se exponen por admin (no hay ownership real entre Users)
        GetByIdAsync(id, ct);

    public Task<bool> ExistsByEmailAsync(string email, int? excludeId, CancellationToken ct) =>
        _db.Users.AsNoTracking().AnyAsync(u => u.Email == email && (excludeId == null || u.Id != excludeId), ct);

    public Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct) =>
        _db.Users.AsNoTracking().AnyAsync(u => u.NIF == nif && (excludeId == null || u.Id != excludeId), ct);

    public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct) =>
        await _db.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Id).ToListAsync(ct);

    public async Task<PagedResult<User>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var q = _db.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(u => EF.Functions.ILike(u.Email, searchTerm) || EF.Functions.ILike(u.Nombre, searchTerm) || EF.Functions.ILike(u.Apellidos, searchTerm) || EF.Functions.ILike(u.NIF, searchTerm));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(u => u.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<User>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<int>> ListServiceIdsForUserAsync(int usuarioId, CancellationToken ct) =>
        await _db.ServiceUsers.AsNoTracking().Where(su => su.UserId == usuarioId).Select(su => su.ServiceId).ToListAsync(ct);

    public async Task<IReadOnlyList<string>> ListRoleNamesForUserAsync(int usuarioId, CancellationToken ct) =>
        await _db.UserRoles.AsNoTracking().Where(ur => ur.UserId == usuarioId)
            .Include(ur => ur.Role).Select(ur => ur.Role.Nombre).ToListAsync(ct);

    public Task AddAsync(User user, CancellationToken ct) { _db.Users.Add(user); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly AppDbContext _db;
    public RefreshTokenRepository(AppDbContext db) { _db = db; }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct) =>
        _db.RefreshTokens.Include(r => r.User).FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);

    public Task AddAsync(RefreshToken token, CancellationToken ct) { _db.RefreshTokens.Add(token); return Task.CompletedTask; }

    public async Task RevokeAllByUserAsync(int userId, CancellationToken ct)
    {
        await _db.RefreshTokens.Where(r => r.UserId == userId && r.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.RevokedAt, DateTime.UtcNow), ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    public ClientRepository(AppDbContext db, IUserRepository userRepo) { _db = db; _userRepo = userRepo; }

    private async Task<bool> IsPrivilegedAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == 0) return true;
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        return roles.Any(r => OwnershipHelper.PrivilegedRoles.Contains(r));
    }

    public async Task<Client?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        // Privilegiados ven todos. PM solo si tiene servicio con ese cliente.
        var c = await _db.Clients.Include(c => c.Services).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (c is null) return null;
        if (await IsPrivilegedAsync(usuarioId, ct)) return c;
        var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
        var visible = await _db.Services.AsNoTracking().AnyAsync(s => s.ClientId == id && serviceIds.Contains(s.Id), ct);
        return visible ? c : null;
    }

    public Task<Client?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Clients.Include(c => c.Services).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<Client>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var q = _db.Clients.AsNoTracking().Include(c => c.Services).AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            var allowedClientIds = await _db.Services.AsNoTracking().Where(s => serviceIds.Contains(s.Id)).Select(s => s.ClientId).Distinct().ToListAsync(ct);
            q = q.Where(c => allowedClientIds.Contains(c.Id));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(c => EF.Functions.ILike(c.Nombre, searchTerm) || EF.Functions.ILike(c.NIF, searchTerm) || (c.Ciudad != null && EF.Functions.ILike(c.Ciudad, searchTerm)));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(c => c.Nombre).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Client>(items, total, page, pageSize);
    }

    public Task AddAsync(Client client, CancellationToken ct) { _db.Clients.Add(client); return Task.CompletedTask; }
    public Task<bool> HasServicesAsync(int clientId, CancellationToken ct) => _db.Services.AsNoTracking().AnyAsync(s => s.ClientId == clientId, ct);
    public Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct) =>
        _db.Clients.AsNoTracking().AnyAsync(c => c.NIF == nif && (excludeId == null || c.Id != excludeId), ct);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ServiceRepository : IServiceRepository
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    public ServiceRepository(AppDbContext db, IUserRepository userRepo) { _db = db; _userRepo = userRepo; }

    private async Task<bool> IsPrivilegedAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == 0) return true;
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        return roles.Any(r => OwnershipHelper.PrivilegedRoles.Contains(r));
    }

    public async Task<Service?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var a = await _db.Services
            .Include(a => a.Client)
            .Include(a => a.ServiceConcepts)
            .Include(a => a.ServiceUsers)
            .Include(a => a.ServiceCostCenters)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (a is null) return null;
        if (await IsPrivilegedAsync(usuarioId, ct)) return a;
        var assigned = await _db.ServiceUsers.AsNoTracking().AnyAsync(su => su.ServiceId == id && su.UserId == usuarioId, ct);
        return assigned ? a : null;
    }

    public Task<Service?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Services.Include(a => a.Client).Include(a => a.ServiceConcepts).Include(a => a.ServiceUsers).Include(a => a.ServiceCostCenters)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Service>> ListAsync(CancellationToken ct) =>
        await _db.Services.AsNoTracking().Include(a => a.Client).OrderBy(a => a.Id).ToListAsync(ct);

    public async Task<PagedResult<Service>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct)
    {
        var q = _db.Services.AsNoTracking().Include(a => a.Client).Include(a => a.Department).AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            q = q.Where(a => serviceIds.Contains(a.Id));
        }
        if (clientId.HasValue) q = q.Where(a => a.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(a => EF.Functions.ILike(a.Nombre, searchTerm)
                          || EF.Functions.ILike(a.Client.Nombre, searchTerm)
                          || (a.Department != null && EF.Functions.ILike(a.Department.Nombre, searchTerm)));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(a => a.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Service>(items, total, page, pageSize);
    }

    public Task AddAsync(Service service, CancellationToken ct) { _db.Services.Add(service); return Task.CompletedTask; }
    public Task<bool> IsUserAssignedAsync(int serviceId, int usuarioId, CancellationToken ct) =>
        _db.ServiceUsers.AsNoTracking().AnyAsync(su => su.ServiceId == serviceId && su.UserId == usuarioId, ct);
    public async Task<bool> HasCierresAsync(int serviceId, CancellationToken ct) =>
        await _db.CierresCostes.AsNoTracking().AnyAsync(c => c.ServiceId == serviceId, ct)
        || await _db.CierresFacturacion.AsNoTracking().AnyAsync(c => c.ServiceId == serviceId, ct);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ConceptRepository : IConceptRepository
{
    private readonly AppDbContext _db;
    public ConceptRepository(AppDbContext db) { _db = db; }

    public Task<Concept?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Concepts.Include(c => c.ServiceConcepts).Include(c => c.ConceptUsers).FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Concept?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct) =>
        GetByIdAsync(id, ct);

    public async Task<PagedResult<Concept>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, TipoConcepto? tipo, string? search, CancellationToken ct)
    {
        var q = _db.Concepts.AsNoTracking().AsQueryable();
        if (tipo.HasValue) q = q.Where(c => c.Tipo == tipo.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(c => EF.Functions.ILike(c.Nombre, searchTerm));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(c => c.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Concept>(items, total, page, pageSize);
    }

    public Task AddAsync(Concept concept, CancellationToken ct) { _db.Concepts.Add(concept); return Task.CompletedTask; }
    public Task<bool> HasServicesAsync(int conceptId, CancellationToken ct) =>
        _db.ServiceConcepts.AsNoTracking().AnyAsync(sc => sc.ConceptId == conceptId, ct);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class VariableRepository : IVariableRepository
{
    private readonly AppDbContext _db;
    public VariableRepository(AppDbContext db) { _db = db; }
    public Task<Variable?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Variables.FirstOrDefaultAsync(v => v.Id == id, ct);
    public async Task<IReadOnlyList<Variable>> ListAsync(CancellationToken ct) =>
        await _db.Variables.AsNoTracking().OrderBy(v => v.Id).ToListAsync(ct);
    public Task AddAsync(Variable variable, CancellationToken ct) { _db.Variables.Add(variable); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class PlantillaClienteConceptoRepository : IPlantillaClienteConceptoRepository
{
    private readonly AppDbContext _db;
    public PlantillaClienteConceptoRepository(AppDbContext db) { _db = db; }

    public Task<PlantillaClienteConcepto?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PlantillasClienteConcepto.Include(p => p.Client).Include(p => p.Concept)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<PlantillaClienteConcepto>> ListByClientAsync(int clientId, CancellationToken ct) =>
        await _db.PlantillasClienteConcepto.AsNoTracking().Include(p => p.Concept)
            .Where(p => p.ClientId == clientId && !p.IsDeleted)
            .OrderBy(p => p.Concept.Nombre).ToListAsync(ct);

    public Task<PlantillaClienteConcepto?> GetByClientAndConceptAsync(int clientId, int conceptId, CancellationToken ct) =>
        _db.PlantillasClienteConcepto.Include(p => p.Concept)
            .FirstOrDefaultAsync(p => p.ClientId == clientId && p.ConceptId == conceptId && !p.IsDeleted, ct);

    public async Task<PagedResult<PlantillaClienteConcepto>> ListPaginatedByClientAsync(int clientId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var q = _db.PlantillasClienteConcepto.AsNoTracking().Include(p => p.Concept)
            .Where(p => p.ClientId == clientId && !p.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(p => EF.Functions.ILike(p.Concept.Nombre, searchTerm));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(p => p.Concept.Nombre)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<PlantillaClienteConcepto>(items, total, page, pageSize);
    }

    public Task AddAsync(PlantillaClienteConcepto plantilla, CancellationToken ct)
    {
        _db.PlantillasClienteConcepto.Add(plantilla);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class PeriodRepository : IPeriodRepository
{
    private readonly AppDbContext _db;
    public PeriodRepository(AppDbContext db) { _db = db; }
    public Task<Period?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Periods.FirstOrDefaultAsync(p => p.Id == id, ct);
    public Task<Period?> GetActivoAsync(CancellationToken ct) =>
        _db.Periods.Where(p => p.Estado == EstadoPeriodo.Abierto).OrderByDescending(p => p.FechaInicio).FirstOrDefaultAsync(ct);
    public async Task<IReadOnlyList<Period>> ListAsync(CancellationToken ct) =>
        await _db.Periods.AsNoTracking().OrderByDescending(p => p.FechaInicio).ToListAsync(ct);
    public Task<bool> ExistsByNombreAsync(string nombre, int? excludeId, CancellationToken ct) =>
        _db.Periods.AsNoTracking().AnyAsync(p => p.Nombre == nombre && (excludeId == null || p.Id != excludeId), ct);
    public Task AddAsync(Period period, CancellationToken ct) { _db.Periods.Add(period); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

// Ola 3b (#10): base genérica para ambas raíces de cierre, parametrizada por el DbSet/Tipo.
public abstract class CierreRepositoryBase<TCierre> : ICierreRepository<TCierre> where TCierre : class, ICierre
{
    protected readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    protected CierreRepositoryBase(AppDbContext db, IUserRepository userRepo) { _db = db; _userRepo = userRepo; }

    public abstract TipoCierre Tipo { get; }
    protected DbSet<TCierre> Set => _db.Set<TCierre>();

    private async Task<bool> IsPrivilegedAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == 0) return true;
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        return roles.Any(r => OwnershipHelper.PrivilegedRoles.Contains(r));
    }

    public Task<TCierre?> GetByIdAsync(int id, CancellationToken ct) =>
        Set.Include(c => c.Service).ThenInclude(p => p.Client).Include(c => c.Period)
            .FirstOrDefaultAsync(c => c.Id == id, ct)!;

    public Task<TCierre?> GetByIdWithLinesAsync(int id, CancellationToken ct) =>
        Set.Include(c => c.Service).ThenInclude(p => p.Client).Include(c => c.Period)
            .Include(c => c.Lines).ThenInclude(l => l.Concept)
            .Include(c => c.Lines).ThenInclude(l => l.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct)!;

    public async Task<TCierre?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await Set.Include(c => c.Service).ThenInclude(p => p.Client).Include(c => c.Period)
            .Include(c => c.Lines).ThenInclude(l => l.Concept)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (c is null) return null;
        if (await IsPrivilegedAsync(usuarioId, ct)) return c;
        var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
        return serviceIds.Contains(c.ServiceId) ? c : null;
    }

    public Task<TCierre?> GetByServiceAndPeriodAsync(int serviceId, int periodId, CancellationToken ct) =>
        Set.AsNoTracking().FirstOrDefaultAsync(c => c.ServiceId == serviceId && c.PeriodId == periodId, ct)!;

    public async Task<PagedResult<TCierre>> ListPaginatedForUserAsync(int usuarioId, ApprovalFilterRequest filter, CancellationToken ct)
    {
        var q = Set.AsNoTracking()
            .Include(c => c.Service).ThenInclude(p => p.Client)
            .Include(c => c.Period)
            .Include(c => c.Lines)
            .AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            q = q.Where(c => serviceIds.Contains(c.ServiceId));
        }
        if (filter.PeriodId.HasValue) q = q.Where(c => c.PeriodId == filter.PeriodId.Value);
        if (filter.ClientId.HasValue) q = q.Where(c => c.Service.ClientId == filter.ClientId.Value);
        if (filter.Estado.HasValue) q = q.Where(c => c.Estado == filter.Estado.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.UpdatedAt).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        return new PagedResult<TCierre>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<PagedResult<TCierre>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct)
    {
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        var q = Set.AsNoTracking()
            .Include(c => c.Service).ThenInclude(p => p.Client)
            .Include(c => c.Period)
            .Where(c => c.Estado == EstadoClosure.EnAprobacion || c.Estado == EstadoClosure.Borrador);
        var esGrupo = roles.Any(r => OwnershipHelper.GrupoRoles.Contains(r));
        var step = esGrupo ? ApprovalStep.Grupo :
                   roles.Contains("Fico") ? ApprovalStep.Fico : (ApprovalStep?)null;
        if (step.HasValue) q = q.Where(c => c.PasoActual == step.Value);
        if (esGrupo && !roles.Intersect(OwnershipHelper.PrivilegedRoles).Any())
        {
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            q = q.Where(c => serviceIds.Contains(c.ServiceId));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<TCierre>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<TCierre>> ListByPeriodForUserAsync(int usuarioId, int periodId, CancellationToken ct)
    {
        var q = Set.AsNoTracking()
            .Include(c => c.Service).ThenInclude(p => p.Client)
            .Include(c => c.Period)
            .Include(c => c.Lines)
            .Where(c => c.PeriodId == periodId);
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            q = q.Where(c => serviceIds.Contains(c.ServiceId));
        }
        return await q.ToListAsync(ct);
    }

    public Task AddAsync(TCierre cierre, CancellationToken ct) { Set.Add(cierre); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class CierreCostesRepository : CierreRepositoryBase<CierreCostes>, ICierreCostesRepository
{
    public CierreCostesRepository(AppDbContext db, IUserRepository userRepo) : base(db, userRepo) { }
    public override TipoCierre Tipo => TipoCierre.Costes;
}

public class CierreFacturacionRepository : CierreRepositoryBase<CierreFacturacion>, ICierreFacturacionRepository
{
    public CierreFacturacionRepository(AppDbContext db, IUserRepository userRepo) : base(db, userRepo) { }
    public override TipoCierre Tipo => TipoCierre.Facturacion;
}

public class ClosureLineRepository : IClosureLineRepository
{
    private readonly AppDbContext _db;
    public ClosureLineRepository(AppDbContext db) { _db = db; }

    public async Task<ClosureLine?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        // Ownership se valida en la capa de servicio sobre el cierre dueño; aquí basta con devolver la línea.
        return await _db.ClosureLines.FirstOrDefaultAsync(l => l.Id == id, ct);
    }

    // Ola 2 (#3a): el recálculo no borra líneas manuales/incentivos (EsManual == true).
    public async Task RemoveAllByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct)
    {
        if (tipo == TipoCierre.Costes)
            await _db.ClosureLines.Where(l => l.CierreCostesId == cierreId && !l.EsManual).ExecuteDeleteAsync(ct);
        else
            await _db.ClosureLines.Where(l => l.CierreFacturacionId == cierreId && !l.EsManual).ExecuteDeleteAsync(ct);
    }

    public async Task<ClosureLine?> GetByIdAsync(int id, CancellationToken ct) =>
        await _db.ClosureLines.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<IReadOnlyList<ClosureLine>> ListByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct) =>
        tipo == TipoCierre.Costes
            ? await _db.ClosureLines.AsNoTracking().Where(l => l.CierreCostesId == cierreId).ToListAsync(ct)
            : await _db.ClosureLines.AsNoTracking().Where(l => l.CierreFacturacionId == cierreId).ToListAsync(ct);

    public Task AddAsync(ClosureLine line, CancellationToken ct) { _db.ClosureLines.Add(line); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<ClosureLine> lines, CancellationToken ct) { _db.ClosureLines.AddRange(lines); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ApprovalRepository : IApprovalRepository
{
    private readonly AppDbContext _db;
    public ApprovalRepository(AppDbContext db) { _db = db; }

    public Task<Approval?> GetCurrentByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct) =>
        _db.Approvals.Where(a => (tipo == TipoCierre.Costes ? a.CierreCostesId : a.CierreFacturacionId) == cierreId
                                  && a.Estado == EstadoApproval.Pendiente)
            .OrderByDescending(a => a.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Approval>> ListByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct) =>
        await _db.Approvals.AsNoTracking().Include(a => a.Role).Include(a => a.User)
            .Where(a => (tipo == TipoCierre.Costes ? a.CierreCostesId : a.CierreFacturacionId) == cierreId)
            .OrderBy(a => a.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<ApprovalHistory>> ListHistoryByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct) =>
        await _db.ApprovalHistory.AsNoTracking().Include(a => a.User)
            .Where(a => (tipo == TipoCierre.Costes ? a.CierreCostesId : a.CierreFacturacionId) == cierreId)
            .OrderBy(a => a.Timestamp).ToListAsync(ct);

    public Task AddAsync(Approval approval, CancellationToken ct) { _db.Approvals.Add(approval); return Task.CompletedTask; }
    public Task AddHistoryAsync(ApprovalHistory history, CancellationToken ct) { _db.ApprovalHistory.Add(history); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class NotificationRepository : INotificationRepository
{
    private readonly AppDbContext _db;
    public NotificationRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<Notification>> ListForUserAsync(int usuarioId, bool soloNoLeidas, int take, CancellationToken ct)
    {
        var q = _db.Notifications.AsNoTracking().Where(n => n.UsuarioId == usuarioId);
        if (soloNoLeidas) q = q.Where(n => !n.Leida);
        return await q.OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync(ct);
    }

    public Task<int> CountUnreadAsync(int usuarioId, CancellationToken ct) =>
        _db.Notifications.AsNoTracking().CountAsync(n => n.UsuarioId == usuarioId && !n.Leida, ct);

    public async Task MarkReadAsync(int id, int usuarioId, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UsuarioId == usuarioId, ct);
        if (n is null || n.Leida) return;
        n.Leida = true;
        n.LeidaAt = DateTime.UtcNow;
    }

    public async Task MarkAllReadAsync(int usuarioId, CancellationToken ct)
    {
        var pendientes = await _db.Notifications.Where(n => n.UsuarioId == usuarioId && !n.Leida).ToListAsync(ct);
        var ahora = DateTime.UtcNow;
        foreach (var n in pendientes)
        {
            n.Leida = true;
            n.LeidaAt = ahora;
        }
    }

    public Task AddAsync(Notification notification, CancellationToken ct) { _db.Notifications.Add(notification); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class CalculationLogRepository : ICalculationLogRepository
{
    private readonly AppDbContext _db;
    private readonly IClosureLineRepository _lineRepo;
    public CalculationLogRepository(AppDbContext db, IClosureLineRepository lineRepo) { _db = db; _lineRepo = lineRepo; }

    public async Task<CalculationLog?> GetByClosureLineAndUsuarioIdAsync(int closureLineId, int usuarioId, CancellationToken ct)
    {
        var line = await _lineRepo.GetByIdAndUsuarioIdAsync(closureLineId, usuarioId, ct);
        if (line is null) return null;
        return await _db.CalculationLogs.AsNoTracking().Include(c => c.Concept)
            .FirstOrDefaultAsync(c => c.ClosureLineId == closureLineId, ct);
    }

    public Task AddAsync(CalculationLog log, CancellationToken ct) { _db.CalculationLogs.Add(log); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<CalculationLog> logs, CancellationToken ct) { _db.CalculationLogs.AddRange(logs); return Task.CompletedTask; }
    public async Task RemoveAllByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct)
    {
        var lineIds = tipo == TipoCierre.Costes
            ? await _db.ClosureLines.AsNoTracking().Where(l => l.CierreCostesId == cierreId).Select(l => l.Id).ToListAsync(ct)
            : await _db.ClosureLines.AsNoTracking().Where(l => l.CierreFacturacionId == cierreId).Select(l => l.Id).ToListAsync(ct);
        await _db.CalculationLogs.Where(c => lineIds.Contains(c.ClosureLineId)).ExecuteDeleteAsync(ct);
    }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;
    public AuditLogRepository(AppDbContext db) { _db = db; }

    public async Task<PagedResult<AuditLog>> ListAsync(AuditLogFilterRequest filter, CancellationToken ct)
    {
        var q = _db.AuditLogs.AsNoTracking().Include(a => a.User).AsQueryable();
        if (filter.UserId.HasValue) q = q.Where(a => a.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.EntityType)) q = q.Where(a => a.EntityType == filter.EntityType);
        if (filter.Action.HasValue) q = q.Where(a => a.Action == filter.Action.Value);
        if (filter.Desde.HasValue)
        {
            var d = filter.Desde.Value.ToDateTime(TimeOnly.MinValue);
            d = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            q = q.Where(a => a.Timestamp >= d);
        }
        if (filter.Hasta.HasValue)
        {
            var d = filter.Hasta.Value.ToDateTime(TimeOnly.MaxValue);
            d = DateTime.SpecifyKind(d, DateTimeKind.Utc);
            q = q.Where(a => a.Timestamp <= d);
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.Timestamp).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        return new PagedResult<AuditLog>(items, total, filter.Page, filter.PageSize);
    }

    public Task AddAsync(AuditLog log, CancellationToken ct) { _db.AuditLogs.Add(log); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _db;
    public RoleRepository(AppDbContext db) { _db = db; }
    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken ct) =>
        await _db.Roles.AsNoTracking().OrderBy(r => r.Id).ToListAsync(ct);
    public Task<Role?> GetByNombreAsync(string nombre, CancellationToken ct) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Nombre == nombre, ct);
    public Task<Role?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct);
}

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _db;
    public DepartmentRepository(AppDbContext db) { _db = db; }
    public async Task<IReadOnlyList<Department>> ListAsync(CancellationToken ct) =>
        await _db.Departments.AsNoTracking().OrderBy(d => d.Id).ToListAsync(ct);
    public Task<Department?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
    public async Task<bool> HasUsersOrServicesAsync(int id, CancellationToken ct) =>
        await _db.Users.AsNoTracking().AnyAsync(u => u.DepartmentId == id, ct) ||
        await _db.Services.AsNoTracking().AnyAsync(a => a.DepartmentId == id, ct);
    public Task AddAsync(Department dep, CancellationToken ct) { _db.Departments.Add(dep); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class CostCenterRepository : ICostCenterRepository
{
    private readonly AppDbContext _db;
    public CostCenterRepository(AppDbContext db) { _db = db; }
    public async Task<IReadOnlyList<CostCenter>> ListAsync(CancellationToken ct) =>
        await _db.CostCenters.AsNoTracking().OrderBy(c => c.Codigo).ToListAsync(ct);
    public async Task<IReadOnlyList<CecoServicio>> GetCecoToServiceMapAsync(CancellationToken ct) =>
        await _db.ServiceCostCenters.AsNoTracking()
            .Select(sc => new CecoServicio(sc.CostCenter.Codigo, sc.ServiceId))
            .ToListAsync(ct);
    public async Task<IReadOnlyList<string>> GetInternalSigCecoCodesAsync(CancellationToken ct) =>
        await _db.CostCenters.AsNoTracking()
            .Where(c => !_db.ServiceCostCenters.Any(sc => sc.CostCenterId == c.Id))
            .Select(c => c.Codigo)
            .ToListAsync(ct);
    public Task<CostCenter?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.CostCenters.FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct) =>
        _db.CostCenters.AsNoTracking().AnyAsync(c => c.Codigo == codigo && (excludeId == null || c.Id != excludeId), ct);
    public Task<bool> HasServicesAsync(int id, CancellationToken ct) =>
        _db.ServiceCostCenters.AsNoTracking().AnyAsync(sc => sc.CostCenterId == id, ct);
    public Task AddAsync(CostCenter cc, CancellationToken ct) { _db.CostCenters.Add(cc); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class StagingRepository<TStaging> : IStagingRepository<TStaging> where TStaging : class, IStagingRow
{
    private readonly AppDbContext _db;
    public StagingRepository(AppDbContext db) { _db = db; }
    public Task<bool> ExistsByHashAsync(string hash, CancellationToken ct) =>
        _db.Set<TStaging>().AsNoTracking().AnyAsync(s => s.Hash == hash, ct);
    public async Task<IReadOnlyList<TStaging>> ListAsync(CancellationToken ct) =>
        await _db.Set<TStaging>().AsNoTracking().ToListAsync(ct);
    public Task AddRangeAsync(IEnumerable<TStaging> rows, CancellationToken ct) { _db.Set<TStaging>().AddRange(rows); return Task.CompletedTask; }
    public async Task<int> CountByHashesAsync(IEnumerable<string> hashes, CancellationToken ct)
    {
        var list = hashes.ToList();
        return await _db.Set<TStaging>().AsNoTracking().Where(s => list.Contains(s.Hash)).CountAsync(ct);
    }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class TarifaServicioRepository : ITarifaServicioRepository
{
    private readonly AppDbContext _db;
    public TarifaServicioRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<TarifaServicio>> ListByServiceAsync(int serviceId, CancellationToken ct) =>
        await _db.TarifasServicio.AsNoTracking()
            .Where(t => t.ServiceId == serviceId && !t.IsDeleted)
            .OrderByDescending(t => t.FechaDesde)
            .ToListAsync(ct);

    public Task<TarifaServicio?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.TarifasServicio.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    public Task AddAsync(TarifaServicio entity, CancellationToken ct)
    {
        _db.TarifasServicio.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class PresupuestoServicioRepository : IPresupuestoServicioRepository
{
    private readonly AppDbContext _db;
    public PresupuestoServicioRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PresupuestoServicio>> ListByServiceAsync(int serviceId, CancellationToken ct) =>
        await _db.PresupuestosServicio.AsNoTracking()
            .Where(p => p.ServiceId == serviceId && !p.IsDeleted)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    public Task<PresupuestoServicio?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PresupuestosServicio.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    public Task AddAsync(PresupuestoServicio entity, CancellationToken ct)
    {
        _db.PresupuestosServicio.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

// FASE 2: TarifaConcepto repository
public class TarifaConceptoRepository : ITarifaConceptoRepository
{
    private readonly AppDbContext _db;
    public TarifaConceptoRepository(AppDbContext db) { _db = db; }

    public Task<TarifaConcepto?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.TarifasConcepto.Include(t => t.Concept).Include(t => t.Client).Include(t => t.Service)
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    public async Task<IReadOnlyList<TarifaConcepto>> ListByConceptAsync(int conceptId, CancellationToken ct) =>
        await _db.TarifasConcepto.AsNoTracking().Include(t => t.Client).Include(t => t.Service)
            .Where(t => t.ConceptId == conceptId && !t.IsDeleted)
            .OrderByDescending(t => t.FechaDesde)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<TarifaConcepto>> ListByConceptAndClientAsync(int conceptId, int clientId, CancellationToken ct) =>
        await _db.TarifasConcepto.AsNoTracking().Include(t => t.Service)
            .Where(t => t.ConceptId == conceptId && (t.ClientId == clientId || t.ClientId == null) && !t.IsDeleted)
            .OrderByDescending(t => t.FechaDesde)
            .ToListAsync(ct);

    public async Task<PagedResult<TarifaConcepto>> ListPaginatedByConceptAsync(int conceptId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var q = _db.TarifasConcepto.AsNoTracking().Include(t => t.Client).Include(t => t.Service)
            .Where(t => t.ConceptId == conceptId && !t.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(t => (t.Client != null && EF.Functions.ILike(t.Client.Nombre, searchTerm)) ||
                            (t.Service != null && EF.Functions.ILike(t.Service.Nombre, searchTerm)) ||
                            EF.Functions.ILike(t.Unidad, searchTerm));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(t => t.FechaDesde)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<TarifaConcepto>(items, total, page, pageSize);
    }

    public Task AddAsync(TarifaConcepto entity, CancellationToken ct)
    {
        _db.TarifasConcepto.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

// FASE 2: PresupuestoConcepto repository
public class PresupuestoConceptoRepository : IPresupuestoConceptoRepository
{
    private readonly AppDbContext _db;
    public PresupuestoConceptoRepository(AppDbContext db) { _db = db; }

    public Task<PresupuestoConcepto?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PresupuestosConcepto.Include(p => p.Concept).Include(p => p.Client).Include(p => p.Service).Include(p => p.Period)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    public async Task<IReadOnlyList<PresupuestoConcepto>> ListByConceptAsync(int conceptId, CancellationToken ct) =>
        await _db.PresupuestosConcepto.AsNoTracking().Include(p => p.Client).Include(p => p.Service).Include(p => p.Period)
            .Where(p => p.ConceptId == conceptId && !p.IsDeleted)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PresupuestoConcepto>> ListByConceptAndClientAsync(int conceptId, int clientId, CancellationToken ct) =>
        await _db.PresupuestosConcepto.AsNoTracking().Include(p => p.Service).Include(p => p.Period)
            .Where(p => p.ConceptId == conceptId && (p.ClientId == clientId || p.ClientId == null) && !p.IsDeleted)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    public async Task<PagedResult<PresupuestoConcepto>> ListPaginatedByConceptAsync(int conceptId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var q = _db.PresupuestosConcepto.AsNoTracking().Include(p => p.Client).Include(p => p.Service).Include(p => p.Period)
            .Where(p => p.ConceptId == conceptId && !p.IsDeleted).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(p => (p.Client != null && EF.Functions.ILike(p.Client.Nombre, searchTerm)) ||
                            (p.Service != null && EF.Functions.ILike(p.Service.Nombre, searchTerm)) ||
                            (p.Period != null && EF.Functions.ILike(p.Period.Nombre, searchTerm)) ||
                            (p.Descripcion != null && EF.Functions.ILike(p.Descripcion, searchTerm)));
        }

        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<PresupuestoConcepto>(items, total, page, pageSize);
    }

    public Task AddAsync(PresupuestoConcepto entity, CancellationToken ct)
    {
        _db.PresupuestosConcepto.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ClienteIncidenciaRepository : IClienteIncidenciaRepository
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    public ClienteIncidenciaRepository(AppDbContext db, IUserRepository userRepo) { _db = db; _userRepo = userRepo; }

    private async Task<bool> IsPrivilegedAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == 0) return true;
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        return roles.Any(r => OwnershipHelper.PrivilegedRoles.Contains(r));
    }

    public async Task<IReadOnlyList<ClienteIncidencia>> ListByClientAsync(int clientId, CancellationToken ct) =>
        await _db.ClienteIncidencias.AsNoTracking()
            .Where(i => i.ClientId == clientId)
            .OrderByDescending(i => i.FechaApertura)
            .ToListAsync(ct);

    public async Task<PagedResult<ClienteIncidencia>> ListAllForUserAsync(int usuarioId, int page, int pageSize, string? search, int? clientId, string? tipo, EstadoIncidencia? estado, CancellationToken ct)
    {
        var q = _db.ClienteIncidencias.AsNoTracking().Include(i => i.Client).AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var serviceIds = await _userRepo.ListServiceIdsForUserAsync(usuarioId, ct);
            var allowedClientIds = await _db.Services.AsNoTracking().Where(s => serviceIds.Contains(s.Id)).Select(s => s.ClientId).Distinct().ToListAsync(ct);
            q = q.Where(i => allowedClientIds.Contains(i.ClientId));
        }
        if (clientId.HasValue) q = q.Where(i => i.ClientId == clientId.Value);
        if (estado.HasValue) q = q.Where(i => i.Estado == estado.Value);
        if (!string.IsNullOrWhiteSpace(tipo)) q = q.Where(i => i.Tipo == tipo);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            q = q.Where(i => EF.Functions.ILike(i.Tipo, term) || EF.Functions.ILike(i.Descripcion, term) || EF.Functions.ILike(i.Client.Nombre, term));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(i => i.FechaApertura).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<ClienteIncidencia>(items, total, page, pageSize);
    }

    public Task<ClienteIncidencia?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.ClienteIncidencias.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<ClienteIncidencia?> GetByIdWithDetailAsync(int id, CancellationToken ct) =>
        _db.ClienteIncidencias.AsNoTracking()
            .Include(i => i.Historial)
            .FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task AddAsync(ClienteIncidencia entity, CancellationToken ct)
    {
        _db.ClienteIncidencias.Add(entity);
        return Task.CompletedTask;
    }

    public Task AddHistorialAsync(IncidenciaHistorial entry, CancellationToken ct)
    {
        _db.Set<IncidenciaHistorial>().Add(entry);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class PartidaPresupuestoRepository : IPartidaPresupuestoRepository
{
    private readonly AppDbContext _db;
    public PartidaPresupuestoRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PartidaPresupuesto>> ListByServiceAsync(int serviceId, CancellationToken ct) =>
        await _db.PartidasPresupuesto.AsNoTracking()
            .Where(p => p.ServiceId == serviceId)
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

    public Task<PartidaPresupuesto?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PartidasPresupuesto.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task AddAsync(PartidaPresupuesto entity, CancellationToken ct) { _db.PartidasPresupuesto.Add(entity); return Task.CompletedTask; }

    public async Task<decimal?> GetMargenRealPctAsync(int serviceId, CancellationToken ct)
    {
        var factura = await _db.CierresFacturacion.AsNoTracking().Where(c => c.ServiceId == serviceId).SumAsync(c => (decimal?)c.Total, ct) ?? 0m;
        if (factura == 0m) return null;
        var coste = await _db.CierresCostes.AsNoTracking().Where(c => c.ServiceId == serviceId).SumAsync(c => (decimal?)c.Total, ct) ?? 0m;
        return Math.Round((factura - coste) / factura * 100m, 1);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class CategoriaFacturaRepository : ICategoriaFacturaRepository
{
    private readonly AppDbContext _db;
    public CategoriaFacturaRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<CategoriaFactura>> ListByClientAsync(int clientId, CancellationToken ct) =>
        await _db.CategoriasFactura.AsNoTracking()
            .Include(c => c.Conceptos).ThenInclude(cc => cc.Concept)
            .Where(c => c.ClientId == clientId)
            .OrderBy(c => c.Nombre)
            .ToListAsync(ct);

    public Task<CategoriaFactura?> GetByIdWithConceptosAsync(int id, CancellationToken ct) =>
        _db.CategoriasFactura
            .Include(c => c.Conceptos).ThenInclude(cc => cc.Concept)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Concept>> ListConceptosFacturacionDelClienteAsync(int clientId, CancellationToken ct)
    {
        var serviceIds = await _db.Services.AsNoTracking()
            .Where(s => s.ClientId == clientId).Select(s => s.Id).ToListAsync(ct);
        return await _db.Concepts.AsNoTracking()
            .Where(c => c.Tipo == TipoConcepto.Factura &&
                        (c.ServiceId == null
                         || serviceIds.Contains(c.ServiceId.Value)
                         || c.ServiceConcepts.Any(sc => serviceIds.Contains(sc.ServiceId))))
            .OrderBy(c => c.Nombre)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyDictionary<int, CategoriaFactura>> GetAsignacionesAsync(int clientId, IReadOnlyCollection<int> conceptIds, CancellationToken ct)
    {
        var rows = await _db.CategoriaFacturaConceptos.AsNoTracking()
            .Include(x => x.CategoriaFactura)
            .Where(x => x.CategoriaFactura.ClientId == clientId && conceptIds.Contains(x.ConceptId))
            .ToListAsync(ct);
        return rows.ToDictionary(x => x.ConceptId, x => x.CategoriaFactura);
    }

    public Task AddAsync(CategoriaFactura entity, CancellationToken ct) { _db.CategoriasFactura.Add(entity); return Task.CompletedTask; }
    public void Remove(CategoriaFactura entity) => _db.CategoriasFactura.Remove(entity);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ForecastRepository : IForecastRepository
{
    private readonly AppDbContext _db;
    public ForecastRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<Forecast>> ListByServiceAndYearAsync(int serviceId, int anio, CancellationToken ct) =>
        await _db.Forecasts.AsNoTracking()
            .Where(f => f.ServiceId == serviceId && f.Anio == anio && !f.IsDeleted)
            .OrderBy(f => f.Mes)
            .ToListAsync(ct);

    public Task<Forecast?> GetByServiceMonthAsync(int serviceId, int anio, int mes, CancellationToken ct) =>
        _db.Forecasts.FirstOrDefaultAsync(f => f.ServiceId == serviceId && f.Anio == anio && f.Mes == mes && !f.IsDeleted, ct);

    public async Task<IReadOnlyList<Forecast>> ListForResumenAsync(int anio, int? departmentId, int? clientId, int? serviceId, CancellationToken ct)
    {
        var q = _db.Forecasts.AsNoTracking()
            .Include(f => f.Service).ThenInclude(s => s.Client)
            .Include(f => f.Service).ThenInclude(s => s.Department)
            .Where(f => f.Anio == anio && !f.IsDeleted);

        if (departmentId.HasValue) q = q.Where(f => f.Service.DepartmentId == departmentId.Value);
        if (clientId.HasValue) q = q.Where(f => f.Service.ClientId == clientId.Value);
        if (serviceId.HasValue) q = q.Where(f => f.ServiceId == serviceId.Value);

        return await q.ToListAsync(ct);
    }

    public Task AddAsync(Forecast entity, CancellationToken ct)
    {
        _db.Forecasts.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ClosureAlertaRepository : IClosureAlertaRepository
{
    private readonly AppDbContext _db;
    public ClosureAlertaRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<ClosureAlerta>> GetByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct) =>
        await _db.ClosureAlertas.AsNoTracking()
            .Where(a => (tipo == TipoCierre.Costes ? a.CierreCostesId : a.CierreFacturacionId) == cierreId)
            .Include(a => a.ConfirmadaPor)
            .OrderBy(a => a.Tipo).ThenBy(a => a.Codigo)
            .ToListAsync(ct);

    public Task<ClosureAlerta?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.ClosureAlertas.Include(a => a.ConfirmadaPor)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task AddAsync(ClosureAlerta alerta, CancellationToken ct)
    {
        _db.ClosureAlertas.Add(alerta);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<ClosureAlerta> alertas, CancellationToken ct)
    {
        _db.ClosureAlertas.AddRange(alertas);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ClosureAlerta alerta, CancellationToken ct)
    {
        _db.ClosureAlertas.Update(alerta);
        return Task.CompletedTask;
    }

    public async Task DeleteByCierreAsync(TipoCierre tipo, int cierreId, CancellationToken ct)
    {
        if (tipo == TipoCierre.Costes)
            await _db.ClosureAlertas.Where(a => a.CierreCostesId == cierreId).ExecuteDeleteAsync(ct);
        else
            await _db.ClosureAlertas.Where(a => a.CierreFacturacionId == cierreId).ExecuteDeleteAsync(ct);
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class StagingA3InnuvaContratoRepository : IStagingA3InnuvaContratoRepository
{
    private readonly AppDbContext _db;
    public StagingA3InnuvaContratoRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<StagingA3InnuvaContrato>> GetByNifAsync(string nif, CancellationToken ct) =>
        await _db.StagingA3InnuvaContratos.AsNoTracking()
            .Where(c => c.NIF == nif)
            .Include(c => c.User)
            .ToListAsync(ct);

    // Excluye contratos marcados "a ignorar en cierre" (Ola 2 #2): no participan en las validaciones de cierre.
    public async Task<IReadOnlyList<StagingA3InnuvaContrato>> GetAllAsync(CancellationToken ct) =>
        await _db.StagingA3InnuvaContratos.AsNoTracking()
            .Where(c => !c.IgnoradoEnCierre)
            .Include(c => c.User)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<StagingA3InnuvaContrato>> GetActivosEnPeriodoAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        var desdeUtc = DateTime.SpecifyKind(desde, DateTimeKind.Utc);
        var hastaUtc = DateTime.SpecifyKind(hasta, DateTimeKind.Utc);
        return await _db.StagingA3InnuvaContratos.AsNoTracking()
            .Where(c => !c.IgnoradoEnCierre && c.FechaInicio <= hastaUtc && c.FechaFin >= desdeUtc)
            .Include(c => c.User)
            .ToListAsync(ct);
    }

    // Contratos de un día (FechaInicio == FechaFin), señalados para revisión (Ola 2 #2). Incluye ignorados para poder desmarcarlos.
    public async Task<IReadOnlyList<StagingA3InnuvaContrato>> ListContratosUnDiaAsync(CancellationToken ct) =>
        await _db.StagingA3InnuvaContratos.AsNoTracking()
            .Where(c => c.FechaInicio == c.FechaFin)
            .Include(c => c.User)
            .OrderBy(c => c.FechaInicio)
            .ToListAsync(ct);

    public async Task<StagingA3InnuvaContrato?> GetByIdAsync(int id, CancellationToken ct) =>
        await _db.StagingA3InnuvaContratos
            .Include(c => c.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task AddAsync(StagingA3InnuvaContrato contrato, CancellationToken ct)
    {
        _db.StagingA3InnuvaContratos.Add(contrato);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<StagingA3InnuvaContrato> contratos, CancellationToken ct)
    {
        _db.StagingA3InnuvaContratos.AddRange(contratos);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
