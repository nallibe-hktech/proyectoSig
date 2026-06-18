using System.Text.Json;
using System.Text.Json.Serialization;
using SIG.Application.Calculation.Nodes;
using SIG.Domain.Exceptions;

namespace SIG.Application.Calculation;

public class FormulaParser : IFormulaParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new FormulaNodeJsonConverter() }
    };

    public FormulaNode Parse(string formulaJson)
    {
        if (string.IsNullOrWhiteSpace(formulaJson))
            throw new FormulaInvalidException("Fórmula vacía.");
        try
        {
            var node = JsonSerializer.Deserialize<FormulaNode>(formulaJson, Options);
            if (node is null)
                throw new FormulaInvalidException("No se pudo deserializar la fórmula.");
            Validate(node);
            return node;
        }
        catch (JsonException ex)
        {
            throw new FormulaInvalidException($"JSON inválido: {ex.Message}");
        }
    }

    public bool TryValidate(string formulaJson, out string[] errores)
    {
        try
        {
            Parse(formulaJson);
            errores = Array.Empty<string>();
            return true;
        }
        catch (FormulaInvalidException ex)
        {
            errores = new[] { ex.Message };
            return false;
        }
    }

    private static void Validate(FormulaNode node)
    {
        switch (node)
        {
            case NumberNode:
            case VariableNode:
                return;
            case SourceNode s:
                if (string.IsNullOrEmpty(s.Entity)) throw new FormulaInvalidException("Source.entity vacío.");
                return;
            case AggregateNode a:
                if (string.IsNullOrEmpty(a.Op)) throw new FormulaInvalidException("Aggregate.op vacío.");
                if (a.Source is null) throw new FormulaInvalidException("Aggregate.source nulo.");
                Validate(a.Source);
                return;
            case BinaryOpNode b:
                if (string.IsNullOrEmpty(b.Op)) throw new FormulaInvalidException("BinaryOp.op vacío.");
                if (b.Left is null || b.Right is null) throw new FormulaInvalidException("BinaryOp con operando nulo.");
                Validate(b.Left);
                Validate(b.Right);
                return;
            case ModifierNode m:
                if (string.IsNullOrEmpty(m.Kind)) throw new FormulaInvalidException("Modifier.kind vacío.");
                if (m.Inner is null) throw new FormulaInvalidException("Modifier.inner nulo.");
                Validate(m.Inner);
                return;
            case TramosNode t:
                if (t.Cantidad is null) throw new FormulaInvalidException("Tramos.cantidad nulo.");
                if (t.Tramos is null || t.Tramos.Count == 0) throw new FormulaInvalidException("Tramos sin tramos definidos.");
                Validate(t.Cantidad);
                return;
            case ConceptRefNode:
                return;
            default:
                throw new FormulaInvalidException($"Tipo de nodo desconocido: {node.GetType().Name}");
        }
    }
}
