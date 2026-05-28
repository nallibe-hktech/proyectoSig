namespace SIG.Application.Calculation;

public record CalculationResult(decimal Resultado, string InputsJson, string FormulaSnapshotJson, string SistemaOrigen, IReadOnlyList<CalculationIncidencia> Incidencias);
public record CalculationIncidencia(string Tipo, string Detalle);
