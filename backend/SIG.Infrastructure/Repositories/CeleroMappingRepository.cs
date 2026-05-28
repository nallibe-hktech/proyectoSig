using Microsoft.EntityFrameworkCore;
using SIG.Application.Interfaces.Repositories;
using SIG.Domain.Entities.Staging;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Repositories;

public class CeleroMappingRepository : ICeleroMappingRepository
{
    private readonly AppDbContext _db;

    public CeleroMappingRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<CeleroResourceMapping>> GetResourceMappingsAsync(CancellationToken ct) =>
        await _db.CeleroResourceMappings.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<CeleroServiceMapping>> GetServiceMappingsAsync(CancellationToken ct) =>
        await _db.CeleroServiceMappings.AsNoTracking().ToListAsync(ct);

    public async Task<IReadOnlyList<CeleroMissionMapping>> GetMissionMappingsAsync(CancellationToken ct) =>
        await _db.CeleroMissionMappings.AsNoTracking().ToListAsync(ct);
}
