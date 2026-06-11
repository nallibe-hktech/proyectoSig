using Microsoft.EntityFrameworkCore;
using SIG.Application.Common;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Common;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Infrastructure.Persistence;
using Action = SIG.Domain.Entities.Action;

namespace SIG.Infrastructure.Repositories;

// Owns visibility logic — administrative roles see everything; ProjectManager sees only owned projects.
internal static class OwnershipHelper
{
    public static readonly string[] PrivilegedRoles = new[] { "Administrator", "Direction", "Fico", "Backoffice", "Auditor", "Reader" };
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

    public async Task<IReadOnlyList<int>> ListProjectIdsForUserAsync(int usuarioId, CancellationToken ct) =>
        await _db.ProjectUsers.AsNoTracking().Where(pu => pu.UserId == usuarioId).Select(pu => pu.ProjectId).ToListAsync(ct);

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
        // Privilegiados ven todos. PM solo si tiene proyecto con ese cliente.
        var c = await _db.Clients.Include(c => c.Projects).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (c is null) return null;
        if (await IsPrivilegedAsync(usuarioId, ct)) return c;
        var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
        var visible = await _db.Projects.AsNoTracking().AnyAsync(p => p.ClientId == id && projectIds.Contains(p.Id), ct);
        return visible ? c : null;
    }

    public Task<Client?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Clients.Include(c => c.Projects).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PagedResult<Client>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, string? search, CancellationToken ct)
    {
        var q = _db.Clients.AsNoTracking().Include(c => c.Projects).AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
            var allowedClientIds = await _db.Projects.AsNoTracking().Where(p => projectIds.Contains(p.Id)).Select(p => p.ClientId).Distinct().ToListAsync(ct);
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
    public Task<bool> HasProjectsAsync(int clientId, CancellationToken ct) => _db.Projects.AsNoTracking().AnyAsync(p => p.ClientId == clientId, ct);
    public Task<bool> ExistsByNifAsync(string nif, int? excludeId, CancellationToken ct) =>
        _db.Clients.AsNoTracking().AnyAsync(c => c.NIF == nif && (excludeId == null || c.Id != excludeId), ct);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    public ProjectRepository(AppDbContext db, IUserRepository userRepo) { _db = db; _userRepo = userRepo; }

    private async Task<bool> IsPrivilegedAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == 0) return true;
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        return roles.Any(r => OwnershipHelper.PrivilegedRoles.Contains(r));
    }

    public async Task<Project?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var p = await _db.Projects
            .Include(p => p.Client)
            .Include(p => p.ProjectCostCenters)
            .Include(p => p.ProjectUsers)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (p is null) return null;
        if (await IsPrivilegedAsync(usuarioId, ct)) return p;
        var assigned = await _db.ProjectUsers.AsNoTracking().AnyAsync(pu => pu.ProjectId == id && pu.UserId == usuarioId, ct);
        return assigned ? p : null;
    }

    public Task<Project?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Projects.Include(p => p.Client).Include(p => p.ProjectCostCenters).Include(p => p.ProjectUsers)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Project>> ListAsync(CancellationToken ct) =>
        await _db.Projects.AsNoTracking().Include(p => p.Client).OrderBy(p => p.Id).ToListAsync(ct);

    public async Task<PagedResult<Project>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, int? clientId, string? search, CancellationToken ct)
    {
        var q = _db.Projects.AsNoTracking().Include(p => p.Client).AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
            q = q.Where(p => projectIds.Contains(p.Id));
        }
        if (clientId.HasValue) q = q.Where(p => p.ClientId == clientId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(p => EF.Functions.ILike(p.Nombre, searchTerm) || EF.Functions.ILike(p.Client.Nombre, searchTerm));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Project>(items, total, page, pageSize);
    }

    public Task AddAsync(Project project, CancellationToken ct) { _db.Projects.Add(project); return Task.CompletedTask; }
    public Task<bool> IsUserAssignedAsync(int projectId, int usuarioId, CancellationToken ct) =>
        _db.ProjectUsers.AsNoTracking().AnyAsync(pu => pu.ProjectId == projectId && pu.UserId == usuarioId, ct);
    public async Task<bool> HasActionsOrClosuresAsync(int projectId, CancellationToken ct) =>
        await _db.Actions.AsNoTracking().AnyAsync(a => a.ProjectId == projectId, ct) ||
        await _db.Closures.AsNoTracking().AnyAsync(c => c.ProjectId == projectId, ct);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ActionRepository : IActionRepository
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    public ActionRepository(AppDbContext db, IUserRepository userRepo) { _db = db; _userRepo = userRepo; }

    private async Task<bool> IsPrivilegedAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == 0) return true;
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        return roles.Any(r => OwnershipHelper.PrivilegedRoles.Contains(r));
    }

    public async Task<Action?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var a = await _db.Actions
            .Include(a => a.Project)
            .Include(a => a.ActionConcepts)
            .Include(a => a.ActionUsers)
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (a is null) return null;
        if (await IsPrivilegedAsync(usuarioId, ct)) return a;
        var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
        return projectIds.Contains(a.ProjectId) ? a : null;
    }

    public Task<Action?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Actions.Include(a => a.Project).Include(a => a.ActionConcepts).Include(a => a.ActionUsers)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<Action>> ListAsync(CancellationToken ct) =>
        await _db.Actions.AsNoTracking().Include(a => a.Project).OrderBy(a => a.Id).ToListAsync(ct);

    public async Task<PagedResult<Action>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, int? projectId, string? search, CancellationToken ct)
    {
        var q = _db.Actions.AsNoTracking().Include(a => a.Project).Include(a => a.Department).AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
            q = q.Where(a => projectIds.Contains(a.ProjectId));
        }
        if (projectId.HasValue) q = q.Where(a => a.ProjectId == projectId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = $"%{search.Trim()}%";
            q = q.Where(a => EF.Functions.ILike(a.Nombre, searchTerm)
                          || EF.Functions.ILike(a.Project.Nombre, searchTerm)
                          || (a.Department != null && EF.Functions.ILike(a.Department.Nombre, searchTerm)));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(a => a.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Action>(items, total, page, pageSize);
    }

    public Task AddAsync(Action action, CancellationToken ct) { _db.Actions.Add(action); return Task.CompletedTask; }
    public Task<bool> HasClosuresAsync(int actionId, CancellationToken ct) => Task.FromResult(false);
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ConceptRepository : IConceptRepository
{
    private readonly AppDbContext _db;
    public ConceptRepository(AppDbContext db) { _db = db; }

    public Task<Concept?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Concepts.Include(c => c.ActionConcepts).Include(c => c.ConceptUsers).FirstOrDefaultAsync(c => c.Id == id, ct);

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
    public Task<bool> HasActionsAsync(int conceptId, CancellationToken ct) =>
        _db.ActionConcepts.AsNoTracking().AnyAsync(ac => ac.ConceptId == conceptId, ct);
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

public class ClosureRepository : IClosureRepository
{
    private readonly AppDbContext _db;
    private readonly IUserRepository _userRepo;
    public ClosureRepository(AppDbContext db, IUserRepository userRepo) { _db = db; _userRepo = userRepo; }

    private async Task<bool> IsPrivilegedAsync(int usuarioId, CancellationToken ct)
    {
        if (usuarioId == 0) return true;
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        return roles.Any(r => OwnershipHelper.PrivilegedRoles.Contains(r));
    }

    public Task<Closure?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.Closures.Include(c => c.Project).ThenInclude(p => p.Client).Include(c => c.Period)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<Closure?> GetByIdWithLinesAsync(int id, CancellationToken ct) =>
        _db.Closures.Include(c => c.Project).ThenInclude(p => p.Client).Include(c => c.Period)
            .Include(c => c.Lines).ThenInclude(l => l.Concept)
            .Include(c => c.Lines).ThenInclude(l => l.User)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Closure?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var c = await _db.Closures.Include(c => c.Project).ThenInclude(p => p.Client).Include(c => c.Period)
            .Include(c => c.Lines).ThenInclude(l => l.Concept)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
        if (c is null) return null;
        if (await IsPrivilegedAsync(usuarioId, ct)) return c;
        var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
        return projectIds.Contains(c.ProjectId) ? c : null;
    }

    public Task<Closure?> GetByProjectAndPeriodAsync(int projectId, int periodId, CancellationToken ct) =>
        _db.Closures.AsNoTracking().FirstOrDefaultAsync(c => c.ProjectId == projectId && c.PeriodId == periodId, ct);

    public async Task<PagedResult<Closure>> ListPaginatedForUserAsync(int usuarioId, ApprovalFilterRequest filter, CancellationToken ct)
    {
        var q = _db.Closures.AsNoTracking()
            .Include(c => c.Project).ThenInclude(p => p.Client)
            .Include(c => c.Period).AsQueryable();
        if (!await IsPrivilegedAsync(usuarioId, ct))
        {
            var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
            q = q.Where(c => projectIds.Contains(c.ProjectId));
        }
        if (filter.PeriodId.HasValue) q = q.Where(c => c.PeriodId == filter.PeriodId.Value);
        if (filter.ClientId.HasValue) q = q.Where(c => c.Project.ClientId == filter.ClientId.Value);
        if (filter.Estado.HasValue) q = q.Where(c => c.Estado == filter.Estado.Value);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.UpdatedAt).Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        return new PagedResult<Closure>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<PagedResult<Closure>> ListPendingForUserAsync(int usuarioId, int page, int pageSize, CancellationToken ct)
    {
        var roles = await _userRepo.ListRoleNamesForUserAsync(usuarioId, ct);
        var q = _db.Closures.AsNoTracking()
            .Include(c => c.Project).ThenInclude(p => p.Client)
            .Include(c => c.Period)
            .Where(c => c.Estado == EstadoClosure.EnAprobacion || c.Estado == EstadoClosure.Borrador);
        // filtra por paso/rol
        var step = roles.Contains("ProjectManager") ? ApprovalStep.ProjectManager :
                   roles.Contains("Backoffice") ? ApprovalStep.Backoffice :
                   roles.Contains("Fico") ? ApprovalStep.Fico :
                   roles.Contains("Direction") ? ApprovalStep.Direction : (ApprovalStep?)null;
        if (step.HasValue) q = q.Where(c => c.PasoActual == step.Value);
        if (roles.Contains("ProjectManager") && !roles.Intersect(OwnershipHelper.PrivilegedRoles).Any())
        {
            var projectIds = await _userRepo.ListProjectIdsForUserAsync(usuarioId, ct);
            q = q.Where(c => projectIds.Contains(c.ProjectId));
        }
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(c => c.UpdatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<Closure>(items, total, page, pageSize);
    }

    public Task AddAsync(Closure closure, CancellationToken ct) { _db.Closures.Add(closure); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ClosureLineRepository : IClosureLineRepository
{
    private readonly AppDbContext _db;
    private readonly IClosureRepository _closureRepo;
    public ClosureLineRepository(AppDbContext db, IClosureRepository closureRepo) { _db = db; _closureRepo = closureRepo; }

    public async Task<ClosureLine?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct)
    {
        var line = await _db.ClosureLines
            .Include(l => l.Closure)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
        if (line is null) return null;
        var closure = await _closureRepo.GetByIdAndUsuarioIdAsync(line.ClosureId, usuarioId, ct);
        return closure is null ? null : line;
    }

    public async Task RemoveAllByClosureAsync(int closureId, CancellationToken ct)
    {
        await _db.ClosureLines.Where(l => l.ClosureId == closureId).ExecuteDeleteAsync(ct);
    }

    public Task AddRangeAsync(IEnumerable<ClosureLine> lines, CancellationToken ct)
    {
        _db.ClosureLines.AddRange(lines);
        return Task.CompletedTask;
    }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class ApprovalRepository : IApprovalRepository
{
    private readonly AppDbContext _db;
    public ApprovalRepository(AppDbContext db) { _db = db; }

    public Task<Approval?> GetCurrentByClosureAsync(int closureId, CancellationToken ct) =>
        _db.Approvals.Where(a => a.ClosureId == closureId && a.Estado == EstadoApproval.Pendiente)
            .OrderByDescending(a => a.Id).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Approval>> ListByClosureAsync(int closureId, CancellationToken ct) =>
        await _db.Approvals.AsNoTracking().Include(a => a.Role).Include(a => a.User)
            .Where(a => a.ClosureId == closureId).OrderBy(a => a.Id).ToListAsync(ct);

    public async Task<IReadOnlyList<ApprovalHistory>> ListHistoryByClosureAsync(int closureId, CancellationToken ct) =>
        await _db.ApprovalHistory.AsNoTracking().Include(a => a.User)
            .Where(a => a.ClosureId == closureId).OrderBy(a => a.Timestamp).ToListAsync(ct);

    public Task AddAsync(Approval approval, CancellationToken ct) { _db.Approvals.Add(approval); return Task.CompletedTask; }
    public Task AddHistoryAsync(ApprovalHistory history, CancellationToken ct) { _db.ApprovalHistory.Add(history); return Task.CompletedTask; }
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
    public async Task RemoveAllByClosureAsync(int closureId, CancellationToken ct)
    {
        var lineIds = await _db.ClosureLines.AsNoTracking().Where(l => l.ClosureId == closureId).Select(l => l.Id).ToListAsync(ct);
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
    public async Task<bool> HasUsersOrActionsAsync(int id, CancellationToken ct) =>
        await _db.Users.AsNoTracking().AnyAsync(u => u.DepartmentId == id, ct) ||
        await _db.Actions.AsNoTracking().AnyAsync(a => a.DepartmentId == id, ct);
    public Task AddAsync(Department dep, CancellationToken ct) { _db.Departments.Add(dep); return Task.CompletedTask; }
    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class CostCenterRepository : ICostCenterRepository
{
    private readonly AppDbContext _db;
    public CostCenterRepository(AppDbContext db) { _db = db; }
    public async Task<IReadOnlyList<CostCenter>> ListAsync(CancellationToken ct) =>
        await _db.CostCenters.AsNoTracking().OrderBy(c => c.Codigo).ToListAsync(ct);
    public Task<CostCenter?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.CostCenters.FirstOrDefaultAsync(c => c.Id == id, ct);
    public Task<bool> ExistsByCodigoAsync(string codigo, int? excludeId, CancellationToken ct) =>
        _db.CostCenters.AsNoTracking().AnyAsync(c => c.Codigo == codigo && (excludeId == null || c.Id != excludeId), ct);
    public Task<bool> HasProjectsAsync(int id, CancellationToken ct) =>
        _db.ProjectCostCenters.AsNoTracking().AnyAsync(pc => pc.CostCenterId == id, ct);
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

public class TarifaProyectoRepository : ITarifaProyectoRepository
{
    private readonly AppDbContext _db;
    public TarifaProyectoRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<TarifaProyecto>> ListByProjectAsync(int projectId, CancellationToken ct) =>
        await _db.TarifasProyecto.AsNoTracking()
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .OrderByDescending(t => t.FechaDesde)
            .ToListAsync(ct);

    public Task<TarifaProyecto?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.TarifasProyecto.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, ct);

    public Task AddAsync(TarifaProyecto entity, CancellationToken ct)
    {
        _db.TarifasProyecto.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class PresupuestoProyectoRepository : IPresupuestoProyectoRepository
{
    private readonly AppDbContext _db;
    public PresupuestoProyectoRepository(AppDbContext db) { _db = db; }

    public async Task<IReadOnlyList<PresupuestoProyecto>> ListByProjectAsync(int projectId, CancellationToken ct) =>
        await _db.PresupuestosProyecto.AsNoTracking()
            .Where(p => p.ProjectId == projectId && !p.IsDeleted)
            .OrderByDescending(p => p.Id)
            .ToListAsync(ct);

    public Task<PresupuestoProyecto?> GetByIdAsync(int id, CancellationToken ct) =>
        _db.PresupuestosProyecto.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);

    public Task AddAsync(PresupuestoProyecto entity, CancellationToken ct)
    {
        _db.PresupuestosProyecto.Add(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
