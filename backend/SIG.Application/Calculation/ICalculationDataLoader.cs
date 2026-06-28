using SIG.Application.Calculation.Nodes;
using SIG.Domain.Entities;

namespace SIG.Application.Calculation;

public interface ICalculationDataLoader
{
    Task<CalculationContext> LoadAsync(CalculationTarget target, CancellationToken ct);
    Task<List<RowAdapter>> LoadCrossServiceAsync(int userId, DateOnly desde, DateOnly hasta, string entity, string field, CancellationToken ct);
}

public interface IVariableResolver
{
    decimal Resolve(int variableId, CalculationContext ctx);
}
