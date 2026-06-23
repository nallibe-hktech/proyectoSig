using System.Text.Json;

namespace SIG.Application.Calculation;

public class VariableResolver : IVariableResolver
{
    public decimal Resolve(int variableId, CalculationContext ctx)
    {
        var v = ctx.Variables.FirstOrDefault(x => x.Id == variableId);
        if (v is null) return 0m;

        var mapeo = ParseMapeo(v.MapeoValoresJson);
        if (mapeo.Count == 0) return 0m;

        // Excel: "Posibilidad de asignar un idQuestion de Celero a una variable".
        // Si la variable está ligada a un idQuestion, su valor se resuelve a partir de la respuesta
        // real registrada en las visitas Celero del contexto: la clave del PayloadJson es el QuestionIdExterno
        // y su contenido (p.ej. "A"/"Premium"/"Sí") se mapea a un número vía MapeoValoresJson.
        if (!string.IsNullOrWhiteSpace(v.QuestionIdExterno))
        {
            var respuestas = ctx.Visitas
                .Select(visita => RowAdapter.FromVisita(visita).GetField(v.QuestionIdExterno)?.ToString())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToList();

            if (respuestas.Count > 0)
            {
                // Colapso a escalar (SUP): respuesta representativa = la más frecuente (mode), desempate alfabético.
                var dominante = respuestas
                    .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
                    .OrderByDescending(g => g.Count())
                    .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
                    .First().Key;
                var match = mapeo.FirstOrDefault(m => string.Equals(m.Key, dominante, StringComparison.OrdinalIgnoreCase));
                if (match.Key is not null) return match.Value;
            }
        }

        // Fallback (sin idQuestion o sin respuesta en el período): valor "Default" si existe; si no, el primero.
        var def = mapeo.FirstOrDefault(m => string.Equals(m.Key, "Default", StringComparison.OrdinalIgnoreCase));
        return def.Key is not null ? def.Value : mapeo[0].Value;
    }

    // Devuelve los pares respuesta→valor en orden de documento (para preservar la semántica "primer valor").
    private static List<KeyValuePair<string, decimal>> ParseMapeo(string json)
    {
        var result = new List<KeyValuePair<string, decimal>>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    if (el.ValueKind != JsonValueKind.Object) continue;
                    if (!el.TryGetProperty("valor", out var val) || !val.TryGetDecimal(out var d)) continue;
                    var resp = el.TryGetProperty("respuesta", out var r) && r.ValueKind == JsonValueKind.String
                        ? r.GetString() ?? string.Empty
                        : string.Empty;
                    result.Add(new(resp, d));
                }
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                     doc.RootElement.TryGetProperty("valor", out var v) && v.TryGetDecimal(out var dd))
            {
                result.Add(new(string.Empty, dd));
            }
        }
        catch { /* mapeo no parseable: se trata como vacío */ }
        return result;
    }
}
