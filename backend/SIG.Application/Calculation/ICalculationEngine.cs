using SIG.Domain.Entities;

namespace SIG.Application.Calculation;

public interface ICalculationEngine
{
    Task<CalculationResult> EvaluateAsync(Concept concept, Closure closure, int? recursoId, CancellationToken ct);
}
