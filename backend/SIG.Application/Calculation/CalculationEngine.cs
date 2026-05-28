using System.Text.Json;
using SIG.Application.Calculation.Nodes;
using SIG.Domain.Entities;
using SIG.Domain.Exceptions;

namespace SIG.Application.Calculation;

public class CalculationEngine : ICalculationEngine
{
    private readonly IFormulaParser _parser;
    private readonly ICalculationDataLoader _loader;
    private readonly IVariableResolver _varResolver;

    public CalculationEngine(IFormulaParser parser, ICalculationDataLoader loader, IVariableResolver varResolver)
    {
        _parser = parser;
        _loader = loader;
        _varResolver = varResolver;
    }

    public async Task<CalculationResult> EvaluateAsync(Concept concept, Closure closure, int? recursoId, CancellationToken ct)
    {
        var ast = _parser.Parse(concept.FormulaJson);
        var ctx = await _loader.LoadAsync(closure, ct);
        var incidencias = new List<CalculationIncidencia>();
        var resultado = EvaluateNode(ast, ctx, closure, concept, recursoId, incidencias);
        ctx.UsedInputs["concepto"] = concept.Nombre;
        ctx.UsedInputs["periodo"] = closure.Period?.Nombre ?? string.Empty;
        ctx.UsedInputs["projectId"] = closure.ProjectId;
        if (recursoId.HasValue) ctx.UsedInputs["recursoId"] = recursoId.Value;
        ctx.UsedInputs["filasVisitas"] = ctx.Visitas.Count;
        ctx.UsedInputs["filasGastos"] = ctx.Gastos.Count;
        ctx.UsedInputs["filasHorasBizneo"] = ctx.HorasBizneo.Count;
        ctx.UsedInputs["filasFichajes"] = ctx.Fichajes.Count;

        var inputsJson = JsonSerializer.Serialize(ctx.UsedInputs);
        var sistemaOrigen = ctx.DetectSistemaOrigen();
        return new CalculationResult(Math.Round(resultado, 2), inputsJson, concept.FormulaJson, sistemaOrigen, incidencias);
    }

    private decimal EvaluateNode(FormulaNode node, CalculationContext ctx, Closure closure, Concept concept, int? recursoId, List<CalculationIncidencia> inc) =>
        node switch
        {
            NumberNode n => (decimal)n.Value,
            VariableNode v => _varResolver.Resolve(v.VariableId, ctx),
            AggregateNode a => Aggregate(a, ctx, closure, recursoId, inc),
            BinaryOpNode b => ApplyBinary(b.Op,
                EvaluateNode(b.Left, ctx, closure, concept, recursoId, inc),
                EvaluateNode(b.Right, ctx, closure, concept, recursoId, inc),
                inc),
            SourceNode => throw new FormulaInvalidException("SourceNode no puede evaluarse directamente. Debe envolverse en Aggregate."),
            _ => throw new FormulaInvalidException($"Tipo de nodo desconocido: {node.GetType().Name}")
        };

    private decimal Aggregate(AggregateNode a, CalculationContext ctx, Closure closure, int? recursoId, List<CalculationIncidencia> inc)
    {
        var rows = ctx.FilteredRows(a.Source, closure, recursoId);
        if (rows.Count == 0)
        {
            inc.Add(new CalculationIncidencia("EmptyDataset", $"Sin datos para {a.Source.Entity} en el período."));
            return 0m;
        }
        return a.Op switch
        {
            "Sum" => a.Field is null ? rows.Count : rows.Sum(r => r.GetDecimal(a.Field)),
            "Count" => rows.Count,
            "Min" => rows.Min(r => r.GetDecimal(a.Field ?? "Importe")),
            "Max" => rows.Max(r => r.GetDecimal(a.Field ?? "Importe")),
            _ => throw new FormulaInvalidException($"Operación de agregación desconocida: {a.Op}")
        };
    }

    private decimal ApplyBinary(string op, decimal l, decimal r, List<CalculationIncidencia> inc) => op switch
    {
        "Add" => l + r,
        "Sub" => l - r,
        "Mul" => l * r,
        "Div" => r == 0 ? Incidente(inc, "DivisionByZero", "División por cero") : l / r,
        "Pct" => l * (1 + r / 100),
        _ => throw new FormulaInvalidException($"Operación binaria desconocida: {op}")
    };

    private static decimal Incidente(List<CalculationIncidencia> inc, string tipo, string detalle)
    {
        inc.Add(new CalculationIncidencia(tipo, detalle));
        return 0;
    }
}
