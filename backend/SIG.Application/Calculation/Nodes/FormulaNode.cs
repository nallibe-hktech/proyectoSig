namespace SIG.Application.Calculation.Nodes;

// Note: JsonPolymorphic attributes removed in favor of custom FormulaNodeJsonConverter
// which handles both new format (with "type" discriminator) and legacy format (type inference)
public abstract class FormulaNode { }

public sealed class NumberNode : FormulaNode
{
    public double Value { get; set; }
}

public sealed class VariableNode : FormulaNode
{
    public int VariableId { get; set; }
}

public sealed class FilterSpec
{
    public string Field { get; set; } = null!;
    public string Op { get; set; } = null!; // Eq, Neq, Gt, Gte, Lt, Lte, In
    public object? Value { get; set; }
}

public sealed class SourceNode : FormulaNode
{
    public string Entity { get; set; } = null!; // GastosPayHawk, VisitasCelero, HorasBizneo, HorasIntratime, TarifasProyecto, VisitasSgpv
    public string? Field { get; set; }
    public List<FilterSpec> Filters { get; set; } = new();
}

public sealed class AggregateNode : FormulaNode
{
    public string Op { get; set; } = null!; // Sum, Count, Min, Max
    public SourceNode Source { get; set; } = null!;
    public string? Field { get; set; }
    // Excel "Conteo de días con actividad": con Op=Count, cuenta valores DISTINTOS de este campo (p.ej. "Fecha").
    public string? Distinct { get; set; }
}

public sealed class BinaryOpNode : FormulaNode
{
    public string Op { get; set; } = null!; // Add, Sub, Mul, Div, Pct
    public FormulaNode Left { get; set; } = null!;
    public FormulaNode Right { get; set; } = null!;
}

// Excel "FILTROS" (modificadores sobre un resultado):
//   Min        -> si el resultado está por debajo de X, devuelve X (cantidad mínima / suelo)
//   Max        -> si el resultado está por encima de X, devuelve X (cantidad máxima / techo)
//   FloorZero  -> si el resultado está por debajo de X, devuelve 0 (rendimiento/umbral mínimo)
//   Franquicia -> resta X y nunca baja de 0 (los primeros X no contabilizan)
public sealed class ModifierNode : FormulaNode
{
    public string Kind { get; set; } = null!; // Min, Max, FloorZero, Franquicia
    public FormulaNode Inner { get; set; } = null!;
    public decimal Threshold { get; set; }
}

// Excel "Conteo de horas/unidades × cantidad incremental" (tarifa por tramos).
// Tramos acumulativos: Hasta = límite superior de unidades del tramo (null = resto); Precio = precio por unidad.
public sealed class Tramo
{
    public decimal? Hasta { get; set; }
    public decimal Precio { get; set; }
}

public sealed class TramosNode : FormulaNode
{
    public FormulaNode Cantidad { get; set; } = null!;
    public List<Tramo> Tramos { get; set; } = new();
}

// Excel "Fee sobre conceptos" / "% fijo de cantidad variable": referencia el importe de OTROS conceptos del
// mismo cierre. ConceptIds vacío = todos los conceptos base ya calculados. Se evalúa en una 2ª pasada.
public sealed class ConceptRefNode : FormulaNode
{
    public List<int> ConceptIds { get; set; } = new();
}
