using SIG.Domain.Entities;

namespace SIG.Application.Calculation;

// Ola 3b (#10): el motor de cálculo ya no depende del antiguo Closure. Recibe el "objetivo de cálculo"
// (servicio + período), que ambas raíces de cierre (CierreCostes / CierreFacturacion) saben construir.
public class CalculationTarget
{
    public int ServiceId { get; set; }
    public int PeriodId { get; set; }
    public Period Period { get; set; } = null!;
    // Ola 4 (Excel): importes de conceptos base ya calculados (conceptId -> importe), para resolver
    // ConceptRefNode ("fee sobre conceptos") en la 2ª pasada del cierre. Vacío en evaluaciones aisladas.
    public Dictionary<int, decimal> ImportesPrevios { get; set; } = new();
}

public interface ICalculationEngine
{
    Task<CalculationResult> EvaluateAsync(Concept concept, CalculationTarget target, int? recursoId, CancellationToken ct);
}
