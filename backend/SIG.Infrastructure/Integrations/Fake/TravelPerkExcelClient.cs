using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Fake;

/// <summary>
/// Lee la descarga Excel de TravelPerk a nivel LÍNEA (hoja "report").
/// El cliente comparte el fichero por SharePoint (igual que Galán/Mediapost); la API se descartó (fuera de presupuesto).
/// Formato confirmado con fichero de muestra: hoja "report", 1 fila por línea, ~81 columnas. Se mapea por NOMBRE de
/// cabecera (más robusto que por índice fijo dado el ancho del fichero). Columnas clave:
/// "Service", "Cost object" (= proyecto/CECO), "Cost per traveler without tax" (coste sin IVA).
/// </summary>
public class TravelPerkExcelClient : ITravelPerkExcelClient
{
    private readonly ILogger<TravelPerkExcelClient> _logger;
    private readonly string _basePath;

    public TravelPerkExcelClient(ILogger<TravelPerkExcelClient> logger, IConfiguration config)
    {
        _logger = logger;
        // Carpeta de descargas de TravelPerk (en producción, carpeta sincronizada de SharePoint).
        _basePath = config["Integrations:TravelPerk:BasePath"] ?? @"C:\dev\SIG-es\TravelPerk";
    }

    public Task<IReadOnlyList<TravelPerkLineaDto>> GetLineasAsync(CancellationToken ct = default)
    {
        // Si la carpeta aún no existe (nadie ha subido ficheros), degradar a vacío en vez de lanzar 500.
        if (!Directory.Exists(_basePath))
        {
            _logger.LogWarning("Carpeta de TravelPerk no existe: {Path}. No hay líneas que sincronizar.", _basePath);
            return Task.FromResult<IReadOnlyList<TravelPerkLineaDto>>(Array.Empty<TravelPerkLineaDto>());
        }

        var file = Directory.GetFiles(_basePath, "*.xlsx")
            .Where(f => !Path.GetFileName(f).StartsWith("~$")) // descartar locks temporales de Excel
            .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
            .FirstOrDefault();

        if (file == null)
        {
            _logger.LogWarning("No se encontraron archivos .xlsx de TravelPerk en {Path}", _basePath);
            return Task.FromResult<IReadOnlyList<TravelPerkLineaDto>>(Array.Empty<TravelPerkLineaDto>());
        }

        using var workbook = new XLWorkbook(file);
        var lineas = TravelPerkExcelParser.Parse(workbook, _logger);
        _logger.LogInformation("Leídas {Count} líneas de TravelPerk desde {File}", lineas.Count, file);
        return Task.FromResult(lineas);
    }
}

/// <summary>
/// Parser puro del libro TravelPerk, aislado del IO para poder testearlo con un workbook en memoria.
/// </summary>
public static class TravelPerkExcelParser
{
    private const string ColService = "service";
    private const string ColCostObject = "cost object";
    private const string ColCosteSinIva = "cost per traveler without tax";
    private const string ColTripId = "trip id";
    private const string ColEmail = "traveler email";
    private const string ColCurrency = "currency code";
    private const string ColFecha = "expense date";

    public static IReadOnlyList<TravelPerkLineaDto> Parse(IXLWorkbook workbook, ILogger? logger = null)
    {
        // Hoja de datos "report" (igual que Mediapost usa "Report"); fallback a la primera hoja.
        var ws = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("report", StringComparison.OrdinalIgnoreCase))
                 ?? workbook.Worksheets.FirstOrDefault();
        if (ws == null)
        {
            logger?.LogWarning("[TravelPerk] El libro no tiene hojas.");
            return Array.Empty<TravelPerkLineaDto>();
        }

        var rows = ws.RowsUsed().ToList();
        if (rows.Count == 0)
            return Array.Empty<TravelPerkLineaDto>();

        // Localizar la fila de cabecera: la primera (de las 15 primeras) que tenga "Service" y la columna de coste sin IVA.
        int headerIdx = -1;
        Dictionary<string, int>? map = null;
        for (int i = 0; i < Math.Min(15, rows.Count); i++)
        {
            var candidate = BuildHeaderMap(rows[i]);
            if (candidate.ContainsKey(ColService) && candidate.ContainsKey(ColCosteSinIva))
            {
                headerIdx = i;
                map = candidate;
                logger?.LogInformation("[TravelPerk] Cabecera detectada en fila {Row}", rows[i].RowNumber());
                break;
            }
        }

        if (map == null)
        {
            logger?.LogWarning("[TravelPerk] No se encontró fila de cabecera con 'Service' y '{Col}'.", ColCosteSinIva);
            return Array.Empty<TravelPerkLineaDto>();
        }

        var lineas = new List<TravelPerkLineaDto>();
        for (int i = headerIdx + 1; i < rows.Count; i++)
        {
            var row = rows[i];
            var service = GetStr(row, map, ColService);
            var tripId = GetStr(row, map, ColTripId);

            // Saltar filas totalmente vacías; una línea válida tiene al menos Service o Trip ID.
            if (string.IsNullOrWhiteSpace(service) && string.IsNullOrWhiteSpace(tripId))
                continue;

            var costObjectRaw = GetStr(row, map, ColCostObject);
            lineas.Add(new TravelPerkLineaDto(
                TripId: tripId,
                Service: service,
                CostObject: string.IsNullOrWhiteSpace(costObjectRaw) ? null : costObjectRaw.Trim(),
                CosteSinIVA: GetDec(row, map, ColCosteSinIva),
                TravelerEmail: NullIfBlank(GetStr(row, map, ColEmail)),
                Currency: NullIfBlank(GetStr(row, map, ColCurrency)),
                FechaGasto: GetDate(row, map, ColFecha)));
        }

        return lineas;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLRow row)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        var last = row.LastCellUsed();
        if (last == null) return map;
        int lastCol = last.Address.ColumnNumber;
        for (int c = 1; c <= lastCol; c++)
        {
            var name = (row.Cell(c).GetString() ?? "").Trim().ToLowerInvariant();
            if (name.Length > 0 && !map.ContainsKey(name))
                map[name] = c;
        }
        return map;
    }

    private static string GetStr(IXLRow row, Dictionary<string, int> map, string key)
        => map.TryGetValue(key, out var c) ? (row.Cell(c).GetString() ?? "").Trim() : "";

    private static decimal GetDec(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var c)) return 0m;
        var cell = row.Cell(c);
        if (cell.TryGetValue<decimal>(out var d)) return d;
        var s = (cell.GetString() ?? "").Trim();
        if (string.IsNullOrEmpty(s)) return 0m;
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv)) return inv;
        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("es-ES"), out var es)) return es;
        return 0m;
    }

    private static DateOnly? GetDate(IXLRow row, Dictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var c)) return null;
        var cell = row.Cell(c);
        if (cell.TryGetValue<DateTime>(out var dt)) return DateOnly.FromDateTime(dt);
        var s = (cell.GetString() ?? "").Trim();
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return DateOnly.FromDateTime(parsed);
        return null;
    }

    private static string? NullIfBlank(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
