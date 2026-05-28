using System.Text.Json.Serialization;

namespace SIG.Application.Calculation.Nodes;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(NumberNode), "Number")]
[JsonDerivedType(typeof(VariableNode), "Variable")]
[JsonDerivedType(typeof(SourceNode), "Source")]
[JsonDerivedType(typeof(AggregateNode), "Aggregate")]
[JsonDerivedType(typeof(BinaryOpNode), "BinaryOp")]
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
    public string Entity { get; set; } = null!; // GastosPayHawk, VisitasCelero, HorasBizneo, HorasIntratime
    public string? Field { get; set; }
    public List<FilterSpec> Filters { get; set; } = new();
}

public sealed class AggregateNode : FormulaNode
{
    public string Op { get; set; } = null!; // Sum, Count, Min, Max
    public SourceNode Source { get; set; } = null!;
    public string? Field { get; set; }
}

public sealed class BinaryOpNode : FormulaNode
{
    public string Op { get; set; } = null!; // Add, Sub, Mul, Div, Pct
    public FormulaNode Left { get; set; } = null!;
    public FormulaNode Right { get; set; } = null!;
}
