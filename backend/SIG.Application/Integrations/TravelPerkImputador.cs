using SIG.Application.DTOs;

namespace SIG.Application.Integrations;

/// <summary>
/// Imputación de costes de TravelPerk en el cierre, según el criterio confirmado por Finanzas (2026-06-22):
/// <list type="bullet">
/// <item>Línea con CECO (campo "Cost object") → el coste se imputa al cliente/proyecto de ese CECO. Aplica a TODOS
/// los Service (Hotels, Flights, Premium Service, FlexiTravel, Other Service…), sin excepción.</item>
/// <item>Línea SIN CECO (caso "Subscription fee") → es un gasto interno de SIG: se imputa al CECO <c>0423</c>,
/// nunca a un cliente.</item>
/// <item>"Refund for train" viene en negativo → al sumar, netea (resta) del CECO correspondiente.</item>
/// </list>
/// El coste imputable es el importe SIN IVA (columna "Cost per traveler without tax").
/// Es una función pura sobre DTOs (sin EF): la conciliación del código de CECO contra la tabla maestra y la
/// alerta "CECO no coincide" pertenecen al paso de cierre, no a este clasificador.
/// </summary>
public static class TravelPerkImputador
{
    /// <summary>CECO interno de SIG para gastos propios sin CECO de cliente (p.ej. la suscripción mensual de TravelPerk).</summary>
    public const string CecoInternoSig = "0423";

    public static TravelPerkImputacionResultado Imputar(IEnumerable<TravelPerkLineaDto> lineas)
    {
        var list = lineas as IReadOnlyList<TravelPerkLineaDto> ?? lineas.ToList();

        var porCeco = list
            .GroupBy(l => string.IsNullOrWhiteSpace(l.CostObject) ? CecoInternoSig : l.CostObject!.Trim())
            .Select(g => new TravelPerkImputacionCeco(
                Ceco: g.Key,
                EsGastoInternoSig: g.Key == CecoInternoSig,
                CosteSinIVA: g.Sum(x => x.CosteSinIVA),
                NumLineas: g.Count()))
            .OrderBy(x => x.Ceco, StringComparer.Ordinal)
            .ToList();

        return new TravelPerkImputacionResultado(
            PorCeco: porCeco,
            TotalSinIVA: list.Sum(x => x.CosteSinIVA),
            TotalLineas: list.Count,
            LineasConCeco: list.Count(l => !string.IsNullOrWhiteSpace(l.CostObject)),
            LineasSinCeco: list.Count(l => string.IsNullOrWhiteSpace(l.CostObject)));
    }
}

/// <summary>Coste agregado de TravelPerk imputado a un CECO (cliente/proyecto, o el 0423 interno de SIG).</summary>
public record TravelPerkImputacionCeco(string Ceco, bool EsGastoInternoSig, decimal CosteSinIVA, int NumLineas);

/// <summary>Resultado de imputar un lote de líneas de TravelPerk.</summary>
public record TravelPerkImputacionResultado(
    IReadOnlyList<TravelPerkImputacionCeco> PorCeco,
    decimal TotalSinIVA,
    int TotalLineas,
    int LineasConCeco,
    int LineasSinCeco);
