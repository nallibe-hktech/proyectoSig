using SIG.Domain.Entities.Staging;

namespace SIG.Application.Interfaces.Repositories;

public interface ICeleroMappingRepository
{
    Task<IReadOnlyList<CeleroResourceMapping>> GetResourceMappingsAsync(CancellationToken ct);
    Task<IReadOnlyList<CeleroServiceMapping>> GetServiceMappingsAsync(CancellationToken ct);
    Task<IReadOnlyList<CeleroMissionMapping>> GetMissionMappingsAsync(CancellationToken ct);
}
