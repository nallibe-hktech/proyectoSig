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

    public async Task<CalculationResult> EvaluateAsync(Concept concept, CalculationTarget target, int? recursoId, CancellationToken ct)
    {
        var ast = _parser.Parse(concept.FormulaJson);
        var ctx = await _loader.LoadAsync(target, ct);
        var incidencias = new List<CalculationIncidencia>();
        var resultado = await EvaluateNodeAsync(ast, ctx, target, concept, recursoId, ct, incidencias);
        ctx.UsedInputs["concepto"] = concept.Nombre;
        ctx.UsedInputs["periodo"] = target.Period?.Nombre ?? string.Empty;
        ctx.UsedInputs["serviceId"] = target.ServiceId;
        if (recursoId.HasValue) ctx.UsedInputs["recursoId"] = recursoId.Value;
        ctx.UsedInputs["filasVisitas"] = ctx.Visitas.Count;
        ctx.UsedInputs["filasGastos"] = ctx.Gastos.Count;
        ctx.UsedInputs["filasHorasBizneo"] = ctx.HorasBizneo.Count;
        ctx.UsedInputs["filasFichajes"] = ctx.Fichajes.Count;

        var inputsJson = JsonSerializer.Serialize(ctx.UsedInputs);
        var sistemaOrigen = ctx.DetectSistemaOrigen();
        return new CalculationResult(Math.Round(resultado, 2), inputsJson, concept.FormulaJson, sistemaOrigen, incidencias);
    }

    private async Task<decimal> EvaluateNodeAsync(FormulaNode node, CalculationContext ctx, CalculationTarget target, Concept concept, int? recursoId, CancellationToken ct, List<CalculationIncidencia> inc) =>
        node switch
        {
            NumberNode n => (decimal)n.Value,
            VariableNode v => _varResolver.Resolve(v.VariableId, ctx),
            AggregateNode a => Aggregate(a, ctx, target, recursoId, inc),
            BinaryOpNode b => ApplyBinary(b.Op,
                await EvaluateNodeAsync(b.Left, ctx, target, concept, recursoId, ct, inc),
                await EvaluateNodeAsync(b.Right, ctx, target, concept, recursoId, ct, inc),
                inc),
            ModifierNode m => ApplyModifier(m.Kind, await EvaluateNodeAsync(m.Inner, ctx, target, concept, recursoId, ct, inc), m.Threshold),
            TramosNode t => ApplyTramos(t, await EvaluateNodeAsync(t.Cantidad, ctx, target, concept, recursoId, ct, inc)),
            ConceptRefNode cr => ResolveConceptRef(cr, target, concept, inc),
            CrossServiceAggregateNode cs => await EvaluateCrossServiceAsync(cs, ctx, target, concept, recursoId, ct, inc),
            SourceNode => throw new FormulaInvalidException("SourceNode no puede evaluarse directamente. Debe envolverse en Aggregate."),
            _ => throw new FormulaInvalidException($"Tipo de nodo desconocido: {node.GetType().Name}")
        };

    private static decimal ApplyModifier(string kind, decimal v, decimal threshold) => kind switch
    {
        "Min" => Math.Max(v, threshold),          // suelo: si v < X -> X
        "Max" => Math.Min(v, threshold),          // techo: si v > X -> X
        "FloorZero" => v < threshold ? 0m : v,    // rendimiento/umbral mínimo: si v < X -> 0
        "Franquicia" => Math.Max(0m, v - threshold), // los primeros X no contabilizan
        _ => throw new FormulaInvalidException($"Modificador desconocido: {kind}")
    };

    private static decimal ApplyTramos(TramosNode t, decimal cantidad)
    {
        decimal restante = cantidad, prevLimite = 0m, total = 0m;
        foreach (var tramo in t.Tramos)
        {
            if (restante <= 0m) break;
            var limiteSup = tramo.Hasta ?? decimal.MaxValue;
            var ancho = limiteSup - prevLimite;
            if (ancho <= 0m) { prevLimite = limiteSup; continue; }
            var unidades = Math.Min(restante, ancho);
            total += unidades * tramo.Precio;
            restante -= unidades;
            prevLimite = limiteSup;
        }
        return total;
    }

    private static decimal ResolveConceptRef(ConceptRefNode cr, CalculationTarget target, Concept self, List<CalculationIncidencia> inc)
    {
        var previos = target.ImportesPrevios;
        if (previos.Count == 0)
        {
            inc.Add(new CalculationIncidencia("SinConceptosPrevios", "Fee sobre conceptos sin conceptos base calculados."));
            return 0m;
        }
        var ids = cr.ConceptIds.Where(id => id != self.Id).ToList();
        if (ids.Count == 0)
            return previos.Where(kv => kv.Key != self.Id).Sum(kv => kv.Value);
        return ids.Where(previos.ContainsKey).Sum(id => previos[id]);
    }

    private decimal Aggregate(AggregateNode a, CalculationContext ctx, CalculationTarget target, int? recursoId, List<CalculationIncidencia> inc)
    {
        var rows = ctx.FilteredRows(a.Source, target, recursoId);
        if (rows.Count == 0)
        {
            inc.Add(new CalculationIncidencia("EmptyDataset", $"Sin datos para {a.Source.Entity} en el período."));
            return 0m;
        }
        return a.Op switch
        {
            "Sum" => a.Field is null ? rows.Count : rows.Sum(r => r.GetDecimal(a.Field)),
            // Excel "conteo de días con actividad": Count con distinct cuenta valores únicos del campo.
            "Count" => string.IsNullOrEmpty(a.Distinct)
                ? rows.Count
                : rows.Select(r => r.GetField(a.Distinct)?.ToString()).Where(x => x != null).Distinct().Count(),
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

    private async Task<decimal> EvaluateCrossServiceAsync(CrossServiceAggregateNode cs, CalculationContext ctx, CalculationTarget target, Concept concept, int? recursoId, CancellationToken ct, List<CalculationIncidencia> inc)
    {
        if (!recursoId.HasValue)
        {
            inc.Add(new CalculationIncidencia("CrossServiceSinRecurso", "CrossServiceAggregate requiere recursoId (empleado)."));
            return 0m;
        }

        var baseSalary = await EvaluateNodeAsync(cs.BaseSalary, ctx, target, concept, recursoId, ct, inc);

        var cacheKey = $"{recursoId.Value}_{cs.Entity}_{target.Period.FechaInicio}_{target.Period.FechaFin}";
        if (!ctx.CrossServiceRows.TryGetValue(cacheKey, out var allRows))
        {
            allRows = await _loader.LoadCrossServiceAsync(recursoId.Value, target.Period.FechaInicio, target.Period.FechaFin, cs.Entity, cs.Field, ct);
            ctx.CrossServiceRows[cacheKey] = allRows;
        }

        if (allRows.Count == 0)
        {
            inc.Add(new CalculationIncidencia("CrossServiceSinDatos", $"Sin datos cross-service para {cs.Entity}."));
            return baseSalary;
        }

        var totalHoras = allRows.Sum(r => r.GetDecimal(cs.Field));
        if (totalHoras == 0m)
        {
            inc.Add(new CalculationIncidencia("CrossServiceHorasCero", "Horas totales = 0, no se puede redistribuir salario."));
            return baseSalary;
        }

        var serviceRows = ctx.FilteredRows(new SourceNode { Entity = cs.Entity, Filters = new List<FilterSpec>() }, target, recursoId);
        var serviceHoras = serviceRows.Sum(r => r.GetDecimal(cs.Field));

        var porcentaje = serviceHoras / totalHoras;
        return Math.Round(baseSalary * porcentaje, 2);
    }
}
