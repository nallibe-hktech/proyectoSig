using SIG.Domain.Entities;

namespace SIG.Application.Calculation;

public interface ICalculationDataLoader
{
    Task<CalculationContext> LoadAsync(Closure closure, CancellationToken ct);
}

public interface IVariableResolver
{
    decimal Resolve(int variableId, CalculationContext ctx);
}
