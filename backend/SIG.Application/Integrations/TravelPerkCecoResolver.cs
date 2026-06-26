using SIG.Application.Interfaces.Repositories;

namespace SIG.Application.Integrations;

/// <summary>
/// Resuelve a qué Servicio se imputa una línea de TravelPerk a partir de su "Cost object" (CECO),
/// usando el mapa CECO→Servicio de la tabla maestra (join ServiceCostCenter).
///
/// La conciliación exacta del código está PENDIENTE de confirmar con el cliente (en el fichero de muestra el
/// Cost object viene como "0139_INPOST" —prefijo de 4 dígitos + nombre— mientras el CECO maestro puede ser de
/// 6 dígitos). Por eso la resolución es defensiva y en cascada; si no hay match único, devuelve null (la línea
/// queda sin imputar y se marca CECO_NO_MAESTRO, en vez de adivinar).
/// </summary>
public static class TravelPerkCecoResolver
{
    /// <summary>CECO normalizado de la línea: el Cost object, o el 0423 interno de SIG si la línea no trae Cost object.</summary>
    public static string NormalizarCeco(string? costObject)
        => string.IsNullOrWhiteSpace(costObject) ? TravelPerkImputador.CecoInternoSig : costObject.Trim();

    /// <summary>
    /// ServiceId al que imputar, o null si la línea no trae CECO (gasto interno SIG) o el CECO no casa de forma
    /// inequívoca con la tabla maestra. Cascada: (1) código completo exacto, (2) prefijo numérico exacto,
    /// (3) el maestro empieza por el prefijo (caso 4→6 dígitos). En cada nivel exige UN único servicio.
    /// </summary>
    public static int? ResolverServiceId(string? costObject, IReadOnlyCollection<CecoServicio> mapa)
    {
        if (string.IsNullOrWhiteSpace(costObject)) return null;
        var raw = costObject.Trim();
        var prefijo = PrefijoNumerico(raw);

        var exacto = ServiciosDe(mapa, c => string.Equals(c, raw, StringComparison.OrdinalIgnoreCase));
        if (exacto.Count == 1) return exacto[0];

        if (prefijo.Length > 0)
        {
            var prefijoExacto = ServiciosDe(mapa, c => PrefijoNumerico(c) == prefijo);
            if (prefijoExacto.Count == 1) return prefijoExacto[0];

            var empiezaPor = ServiciosDe(mapa, c => c.StartsWith(prefijo, StringComparison.Ordinal));
            if (empiezaPor.Count == 1) return empiezaPor[0];
        }

        return null;
    }

    /// <summary>
    /// True si la línea es gasto INTERNO de SIG (no de cliente): no trae Cost object (suscripción → 0423), o su
    /// código/prefijo casa con un CECO estructural del maestro (departamento de SIG, sin Servicio de cliente:
    /// Dirección, Comercial, Finanzas…). Misma cascada de matching que <see cref="ResolverServiceId"/>.
    /// </summary>
    public static bool EsCecoInternoSig(string? costObject, IReadOnlyCollection<string> codigosInternos)
    {
        if (string.IsNullOrWhiteSpace(costObject)) return true;
        if (codigosInternos.Count == 0) return false;
        var raw = costObject.Trim();
        var prefijo = PrefijoNumerico(raw);
        return codigosInternos.Any(c =>
            string.Equals(c, raw, StringComparison.OrdinalIgnoreCase)
            || (prefijo.Length > 0 && (PrefijoNumerico(c) == prefijo || c.StartsWith(prefijo, StringComparison.Ordinal))));
    }

    private static List<int> ServiciosDe(IReadOnlyCollection<CecoServicio> mapa, Func<string, bool> pred)
        => mapa.Where(m => pred(m.Codigo)).Select(m => m.ServiceId).Distinct().ToList();

    private static string PrefijoNumerico(string s)
    {
        int i = 0;
        while (i < s.Length && char.IsDigit(s[i])) i++;
        return s.Substring(0, i);
    }
}
