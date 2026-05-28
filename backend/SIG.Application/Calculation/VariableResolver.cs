using System.Text.Json;

namespace SIG.Application.Calculation;

public class VariableResolver : IVariableResolver
{
    public decimal Resolve(int variableId, CalculationContext ctx)
    {
        var v = ctx.Variables.FirstOrDefault(x => x.Id == variableId);
        if (v is null) return 0m;
        // Mapeo simplificado: si la variable es "TarifaHora" se usa el primer "valor" del mapeo,
        // si es PuntoMontado/TipoVisita/ZonaBonus, se mapea a partir de respuestas en visitas.
        try
        {
            using var doc = JsonDocument.Parse(v.MapeoValoresJson);
            // MapeoValoresJson formato: [{"respuesta":"Sí","valor":1}, ...]
            // Buscamos un valor "default" o el primer valor numérico.
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
            {
                var first = doc.RootElement[0];
                if (first.TryGetProperty("valor", out var val) && val.TryGetDecimal(out var d))
                    return d;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                if (doc.RootElement.TryGetProperty("valor", out var val) && val.TryGetDecimal(out var d))
                    return d;
            }
        }
        catch { return 0m; }
        return 0m;
    }
}
