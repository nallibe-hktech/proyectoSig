using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Fake;

/// <summary>
/// Cliente para leer datos de Galán desde archivos CSV locales o SharePoint
/// Por ahora lee desde carpeta local. En producción usaría SFTP/SharePoint SDK.
/// Implementa parseo flexible que acepta múltiples variantes de nombres de columnas sin validación estricta.
/// </summary>
public class GalanCsvClient : IGalanClient
{
    private readonly ILogger<GalanCsvClient> _logger;
    private readonly string _basePath;

    public GalanCsvClient(ILogger<GalanCsvClient> logger, IConfiguration config)
    {
        _logger = logger;
        // Ruta de los archivos de Galán (configurable; en producción sería carpeta SharePoint)
        _basePath = config["Integrations:Galan:BasePath"]
            ?? @"C:\dev\SIG-es\Galán\Galán";
    }

    public async Task<IReadOnlyList<GalanEntradaDto>> GetEntradasAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        try
        {
            // Buscar primero en Excel (Entradas_*.xlsx), luego en CSV — ordenar por fecha de modificación (más reciente primero)
            var excelFiles = Directory.GetFiles(_basePath, "Entradas_*.xlsx")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (excelFiles != null)
            {
                return ReadEntradasFromExcel(excelFiles, desde, hasta);
            }

            // Fallback a CSV si no hay Excel
            var files = Directory.GetFiles(_basePath, "Entradas_*.csv")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos Entradas_*.xlsx ni Entradas_*.csv en {Path}", _basePath);
                return Array.Empty<GalanEntradaDto>();
            }

            using var reader = new StreamReader(files);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                HeaderValidated = null
            };
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<dynamic>()
                .Select(r => NormalizarEntrada(r))
                .Where(r => r != null)
                .Cast<GalanEntradaRawCsv>()
                .ToList();

            var dtos = records
                .Where(r => r.Fecha >= desde && r.Fecha <= hasta)
                .Select(r => new GalanEntradaDto(
                    r.CodigoDeArticulo,
                    r.CódigoDeDepartamento,
                    r.CodigoDeFamily,
                    r.Descripcion2 ?? "",
                    r.Fecha,
                    r.Unidades,
                    r.Empresa,
                    r.Almacen,
                    r.Celda
                ))
                .ToList();

            _logger.LogInformation("Leídas {Count} entradas de Galán desde {File}", dtos.Count, files);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo entradas de Galán");
            throw;
        }
    }

    public async Task<IReadOnlyList<GalanSalidaDto>> GetSalidasAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        try
        {
            // Buscar primero en Excel (FACT_MENSUAL_*.xlsx), luego en CSV — ordenar por fecha de modificación (más reciente primero)
            var excelFiles = Directory.GetFiles(_basePath, "FACT_MENSUAL_*.xlsx")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (excelFiles != null)
            {
                return ReadSalidasFromExcel(excelFiles, desde, hasta);
            }

            // También buscar directamente Salidas_*.xlsx como alternativa
            var salidasXlsx = Directory.GetFiles(_basePath, "Salidas_*.xlsx")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (salidasXlsx != null)
            {
                return ReadSalidasFromExcel(salidasXlsx, desde, hasta);
            }

            // Fallback a CSV si no hay Excel
            var files = Directory.GetFiles(_basePath, "Salidas_*.csv")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos FACT_MENSUAL_*.xlsx ni Salidas_*.csv en {Path}", _basePath);
                return Array.Empty<GalanSalidaDto>();
            }

            using var reader = new StreamReader(files);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                HeaderValidated = null
            };
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<dynamic>()
                .Select(r => NormalizarSalida(r))
                .Where(r => r != null)
                .Cast<GalanSalidaRawCsv>()
                .ToList();

            var dtos = records
                .Where(r => r.Fecha >= desde && r.Fecha <= hasta)
                .Select(r => new GalanSalidaDto(
                    r.Albaran,
                    r.NumeroDePedidoDeTercero,
                    r.CodigoDeArticulo,
                    r.CódigoDeDepartamento,
                    r.CódigoDeFamily,
                    r.Descripcion1 ?? "",
                    r.Unidades,
                    r.CodigoDeServicioDeTransporte,
                    r.Matricula,
                    r.Fecha,
                    r.Destinatario,
                    r.Almacen,
                    r.Celda
                ))
                .ToList();

            _logger.LogInformation("Leídas {Count} salidas de Galán desde {File}", dtos.Count, files);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo salidas de Galán");
            throw;
        }
    }

    public async Task<IReadOnlyList<GalanStockDto>> GetStockAsync(CancellationToken ct)
    {
        try
        {
            // Buscar en Excel — preferir STOCK_celda_*.xlsx, luego STOCK_*.xlsx — ordenar por fecha de modificación (más reciente primero)
            var stockCeldaXlsx = Directory.GetFiles(_basePath, "STOCK_celda_*.xlsx")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (stockCeldaXlsx != null)
            {
                return ReadStockFromExcel(stockCeldaXlsx);
            }

            var stockXlsx = Directory.GetFiles(_basePath, "STOCK_*.xlsx")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (stockXlsx != null)
            {
                return ReadStockFromExcel(stockXlsx);
            }

            // Fallback a CSV si no hay Excel
            var files = Directory.GetFiles(_basePath, "STOCK_celda_*.csv")
                .OrderByDescending(f => new FileInfo(f).LastWriteTimeUtc)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos STOCK_*.xlsx ni STOCK_celda_*.csv en {Path}", _basePath);
                return Array.Empty<GalanStockDto>();
            }

            using var reader = new StreamReader(files);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                HeaderValidated = null
            };
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<dynamic>()
                .Select(r => NormalizarStock(r))
                .Where(r => r != null)
                .Cast<GalanStockRawCsv>()
                .ToList();

            var dtos = records
                .Select(r => new GalanStockDto(
                    r.CodigoDeArticulo,
                    r.CódigoDeDepartamento,
                    r.CódigoDeFamily,
                    r.CodigoDeCelda,
                    decimal.Parse(r.StockB ?? "0", CultureInfo.InvariantCulture),
                    decimal.Parse(r.StockA ?? "0", CultureInfo.InvariantCulture),
                    decimal.Parse(r.Stock ?? "0", CultureInfo.InvariantCulture),
                    r.ALM,
                    r.Familia,
                    r.Subfamilia,
                    r.Descripcion
                ))
                .ToList();

            _logger.LogInformation("Leído stock de {Count} artículos desde {File}", dtos.Count, files);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo stock de Galán");
            throw;
        }
    }

    /// <summary>
    /// Lee entradas desde un archivo Excel (Entradas_*.xlsx)
    /// Estructura del Excel: Col1=CodigoArticulo, Col2=CodigoDepartamento, Col3=CodigoFamilia, Col4=Descripcion, Col5=Fecha, Col6=Unidades, Col7=Empresa, Col8=Almacen, Col9=Celda
    /// </summary>
    private IReadOnlyList<GalanEntradaDto> ReadEntradasFromExcel(string filePath, DateTime desde, DateTime hasta)
    {
        var dtos = new List<GalanEntradaDto>();
        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning("No hay worksheets en {File}", filePath);
                    return dtos;
                }

                var rows = worksheet.Rows().ToList();

                // Detectar fila de headers
                int headerRowIndex = 0;
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("codigo"))
                    {
                        headerRowIndex = i;
                        _logger.LogInformation("Headers detectados en fila {RowNum}", i + 1);
                        break;
                    }
                }

                // Leer datos a partir de la fila siguiente a headers
                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];

                        var codigoArticulo = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(codigoArticulo)) continue;

                        var codigoDepartamento = row.Cell(2).Value.ToString().Trim();
                        var codigoFamilia = row.Cell(3).Value.ToString().Trim();
                        var descripcion = row.Cell(4).Value.ToString().Trim();

                        DateTime fecha = DateTime.UtcNow;
                        if (DateTime.TryParse(row.Cell(5).Value.ToString(), out var parsedDate))
                            fecha = parsedDate;

                        int unidades = 0;
                        if (int.TryParse(row.Cell(6).Value.ToString(), out int u))
                            unidades = u;

                        var empresa = row.Cell(7).Value.ToString().Trim();
                        var almacen = row.Cell(8).Value.ToString().Trim();
                        var celda = row.Cell(9).Value.ToString().Trim();

                        // Filtrar por fecha
                        if (fecha < desde || fecha > hasta) continue;

                        var dto = new GalanEntradaDto(
                            codigoArticulo,
                            codigoDepartamento,
                            codigoFamilia,
                            descripcion,
                            fecha,
                            unidades,
                            empresa,
                            almacen,
                            celda
                        );
                        dtos.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo fila {RowNum} de entradas Excel", i + 1);
                    }
                }
            }

            _logger.LogInformation("Leídas {Count} entradas de Galán desde Excel {File}", dtos.Count, filePath);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo entradas de Galán desde Excel");
            throw;
        }
    }

    /// <summary>
    /// Lee salidas desde un archivo Excel (FACT_MENSUAL_*.xlsx)
    /// Estructura del Excel: Col1=ReferenciaCli, Col2=ReferenciaCli2, Col3=Descripcion, Col4=Unidades, Col5=CodLinea, Col6=ImporteUnd, Col7=Albaran, Col8=ImporteSuma
    /// </summary>
    private IReadOnlyList<GalanSalidaDto> ReadSalidasFromExcel(string filePath, DateTime desde, DateTime hasta)
    {
        var dtos = new List<GalanSalidaDto>();
        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning("No hay worksheets en {File}", filePath);
                    return dtos;
                }

                var rows = worksheet.Rows().ToList();

                // La estructura es: Row 1 contiene los headers
                int headerRowIndex = 0;
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("referencia") || cellValue.Contains("albarán") || cellValue.Contains("cliente"))
                    {
                        headerRowIndex = i;
                        _logger.LogInformation("Headers detectados en fila {RowNum}", i + 1);
                        break;
                    }
                }

                // Leer datos a partir de la fila siguiente a headers
                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];

                        // Mapeo de columnas según la estructura real del archivo
                        var albaran = row.Cell(7).Value.ToString().Trim();  // Col 7 = Albarán
                        if (string.IsNullOrEmpty(albaran)) continue;

                        var codigoArticulo = row.Cell(5).Value.ToString().Trim();  // Col 5 = Código de línea
                        var descripcion = row.Cell(3).Value.ToString().Trim();     // Col 3 = Descripción
                        var unidadesStr = row.Cell(4).Value.ToString().Trim();     // Col 4 = Unidades
                        var referencia = row.Cell(2).Value.ToString().Trim();      // Col 2 = Referencia

                        if (!int.TryParse(unidadesStr, out int unidades))
                            unidades = 0;

                        // Usar fecha actual ya que el Excel no tiene columna de fecha
                        var fecha = DateTime.UtcNow;

                        var dto = new GalanSalidaDto(
                            albaran,
                            referencia,
                            codigoArticulo,
                            "",
                            "",
                            descripcion,
                            unidades,
                            null,
                            null,
                            fecha,
                            null,
                            "",
                            ""
                        );
                        dtos.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo fila {RowNum} de salidas Excel", i + 1);
                    }
                }
            }

            _logger.LogInformation("Leídas {Count} salidas de Galán desde Excel {File}", dtos.Count, filePath);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo salidas de Galán desde Excel");
            throw;
        }
    }

    /// <summary>
    /// Lee stock desde un archivo Excel (STOCK_*.xlsx)
    /// Estructura del Excel: Col1=CodigoArticulo, Col2=CodigoDepartamento, Col3=CodigoFamilia, Col4=Descripcion, Col5=Stock, Col6=Empresa
    /// </summary>
    private IReadOnlyList<GalanStockDto> ReadStockFromExcel(string filePath)
    {
        var dtos = new List<GalanStockDto>();
        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning("No hay worksheets en {File}", filePath);
                    return dtos;
                }

                var rows = worksheet.Rows().ToList();

                // Detectar fila de headers
                int headerRowIndex = 0;
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("codigo"))
                    {
                        headerRowIndex = i;
                        _logger.LogInformation("Headers detectados en fila {RowNum}", i + 1);
                        break;
                    }
                }

                // Leer datos a partir de la fila siguiente a headers
                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];

                        var codigoArticulo = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(codigoArticulo)) continue;

                        var codigoDepartamento = row.Cell(2).Value.ToString().Trim();
                        var codigoFamilia = row.Cell(3).Value.ToString().Trim();
                        var descripcion = row.Cell(4).Value.ToString().Trim();
                        var stockStr = row.Cell(5).Value.ToString().Trim();
                        var empresa = row.Cell(6).Value.ToString().Trim();

                        if (!decimal.TryParse(stockStr, out decimal stock))
                            stock = 0;

                        var dto = new GalanStockDto(
                            codigoArticulo,
                            codigoDepartamento,
                            codigoFamilia,
                            "",
                            0,
                            0,
                            stock,
                            empresa,
                            "",
                            "",
                            descripcion
                        );
                        dtos.Add(dto);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo fila {RowNum} de stock Excel", i + 1);
                    }
                }
            }

            _logger.LogInformation("Leído stock de {Count} artículos desde Excel {File}", dtos.Count, filePath);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo stock de Galán desde Excel");
            throw;
        }
    }

    /// <summary>
    /// Normaliza un registro dinámico del CSV a GalanEntradaRawCsv mapeando flexiblemente los nombres de columnas.
    /// Este método es tolerante con variaciones de nombres (prefijos, espacios, mayúsculas, acentos).
    /// </summary>
    private GalanEntradaRawCsv? NormalizarEntrada(dynamic row)
    {
        try
        {
            var dict = (IDictionary<string, object>)row;
            var normalized = NormalizarKeys(dict);

            return new GalanEntradaRawCsv
            {
                CodigoDeArticulo = ObtenerValor(normalized, "codigoDeArticulo", "codigo", "articulo") ?? "",
                CódigoDeDepartamento = ObtenerValor(normalized, "codigodeDepartamento", "departamento") ?? "",
                CodigoDeFamily = ObtenerValor(normalized, "codigodefamily", "familia") ?? "",
                Descripcion2 = ObtenerValor(normalized, "descripcion2", "descripcion"),
                Fecha = DateTime.TryParse(ObtenerValor(normalized, "fecha") ?? "", out var fecha) ? fecha : DateTime.MinValue,
                Unidades = int.TryParse(ObtenerValor(normalized, "unidades") ?? "0", out var unidades) ? unidades : 0,
                Empresa = ObtenerValor(normalized, "empresa") ?? "",
                Almacen = ObtenerValor(normalized, "almacen") ?? "",
                Celda = ObtenerValor(normalized, "celda") ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error normalizando registro de Entradas");
            return null;
        }
    }

    /// <summary>
    /// Normaliza un registro dinámico del CSV a GalanSalidaRawCsv.
    /// </summary>
    private GalanSalidaRawCsv? NormalizarSalida(dynamic row)
    {
        try
        {
            var dict = (IDictionary<string, object>)row;
            var normalized = NormalizarKeys(dict);

            return new GalanSalidaRawCsv
            {
                Albaran = ObtenerValor(normalized, "albaran") ?? "",
                NumeroDePedidoDeTercero = ObtenerValor(normalized, "numerodepedidodetercero", "pedido"),
                CodigoDeArticulo = ObtenerValor(normalized, "codigoDeArticulo") ?? "",
                CódigoDeDepartamento = ObtenerValor(normalized, "codigodeDepartamento") ?? "",
                CódigoDeFamily = ObtenerValor(normalized, "codigodefamily") ?? "",
                Descripcion1 = ObtenerValor(normalized, "descripcion1"),
                Unidades = int.TryParse(ObtenerValor(normalized, "unidades") ?? "0", out var unidades) ? unidades : 0,
                CodigoDeServicioDeTransporte = ObtenerValor(normalized, "codigodeserviciodetransporte", "transporte"),
                Matricula = ObtenerValor(normalized, "matricula"),
                Fecha = DateTime.TryParse(ObtenerValor(normalized, "fecha") ?? "", out var fecha) ? fecha : DateTime.MinValue,
                Destinatario = ObtenerValor(normalized, "destinatario"),
                Almacen = ObtenerValor(normalized, "almacen") ?? "",
                Celda = ObtenerValor(normalized, "celda") ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error normalizando registro de Salidas");
            return null;
        }
    }

    /// <summary>
    /// Normaliza un registro dinámico del CSV a GalanStockRawCsv.
    /// </summary>
    private GalanStockRawCsv? NormalizarStock(dynamic row)
    {
        try
        {
            var dict = (IDictionary<string, object>)row;
            var normalized = NormalizarKeys(dict);

            return new GalanStockRawCsv
            {
                CodigoDeArticulo = ObtenerValor(normalized, "codigoDeArticulo") ?? "",
                CódigoDeDepartamento = ObtenerValor(normalized, "codigodeDepartamento") ?? "",
                CódigoDeFamily = ObtenerValor(normalized, "codigodefamily") ?? "",
                CodigoDeCelda = ObtenerValor(normalized, "codigoDecelda", "celda") ?? "",
                StockB = ObtenerValor(normalized, "stockb"),
                StockA = ObtenerValor(normalized, "stocka"),
                Stock = ObtenerValor(normalized, "stock"),
                ALM = ObtenerValor(normalized, "alm", "almacen") ?? "",
                Familia = ObtenerValor(normalized, "familia") ?? "",
                Subfamilia = ObtenerValor(normalized, "subfamilia") ?? "",
                Descripcion = ObtenerValor(normalized, "descripcion") ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error normalizando registro de Stock");
            return null;
        }
    }

    /// <summary>
    /// Normaliza las claves del diccionario removiendo prefijos numéricos, espacios y acentos.
    /// </summary>
    private Dictionary<string, string> NormalizarKeys(IDictionary<string, object> original)
    {
        var resultado = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in original)
        {
            var key = kvp.Key
                .Trim()
                .ToLower()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("ñ", "n")
                .Replace(" ", "")
                .Replace(".", "");

            // Remover prefijos numéricos SOLO si hay letras después (ej: "2codigoDeArticulo" -> "codigoDeArticulo")
            // Pero no si es solo números (ej: "0029597" se deja como está)
            var withoutPrefix = Regex.Replace(key, @"^(\d+)(?=[a-z])", "");
            key = withoutPrefix.Length > 0 ? withoutPrefix : key;

            var value = kvp.Value?.ToString() ?? "";
            if (!string.IsNullOrEmpty(key))
            {
                resultado[key] = value;
            }
        }

        return resultado;
    }

    /// <summary>
    /// Obtiene un valor del diccionario normalizado intentando múltiples claves alternativas.
    /// </summary>
    private string? ObtenerValor(Dictionary<string, string> normalized, params string[] claves)
    {
        foreach (var clave in claves)
        {
            var keyLower = clave.ToLower()
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace(" ", "")
                .Replace(".", "");

            // Remover prefijos numéricos SOLO si hay letras después (para consistencia con NormalizarKeys)
            var withoutPrefix = Regex.Replace(keyLower, @"^(\d+)(?=[a-z])", "");
            keyLower = withoutPrefix.Length > 0 ? withoutPrefix : keyLower;

            if (normalized.TryGetValue(keyLower, out var valor) && !string.IsNullOrWhiteSpace(valor))
            {
                return valor;
            }
        }

        return null;
    }
}

// Raw CSV models (mapan directamente del CSV con separadores y tipos)
public class GalanEntradaRawCsv
{
    public string CodigoDeArticulo { get; set; } = "";
    public string CódigoDeDepartamento { get; set; } = "";
    public string CodigoDeFamily { get; set; } = "";
    public string? Descripcion2 { get; set; }
    public DateTime Fecha { get; set; }
    public int Unidades { get; set; }
    public string Empresa { get; set; } = "";
    public string Almacen { get; set; } = "";
    public string Celda { get; set; } = "";
}

public class GalanSalidaRawCsv
{
    public string Albaran { get; set; } = "";
    public string? NumeroDePedidoDeTercero { get; set; }
    public string CodigoDeArticulo { get; set; } = "";
    public string CódigoDeDepartamento { get; set; } = "";
    public string CódigoDeFamily { get; set; } = "";
    public string? Descripcion1 { get; set; }
    public int Unidades { get; set; }
    public string? CodigoDeServicioDeTransporte { get; set; }
    public string? Matricula { get; set; }
    public DateTime Fecha { get; set; }
    public string? Destinatario { get; set; }
    public string Almacen { get; set; } = "";
    public string Celda { get; set; } = "";
}

public class GalanStockRawCsv
{
    public string CodigoDeArticulo { get; set; } = "";
    public string CódigoDeDepartamento { get; set; } = "";
    public string CódigoDeFamily { get; set; } = "";
    public string CodigoDeCelda { get; set; } = "";
    public string? StockB { get; set; }
    public string? StockA { get; set; }
    public string? Stock { get; set; }
    public string ALM { get; set; } = "";
    public string Familia { get; set; } = "";
    public string Subfamilia { get; set; } = "";
    public string Descripcion { get; set; } = "";
}

// ===== ClassMaps flexibles (mantienen compatibilidad aunque no se usen) =====

/// <summary>
/// Mapeo flexible para Entradas (heredado, mantiene compatibilidad)
/// </summary>
public sealed class GalanEntradaMap : ClassMap<GalanEntradaRawCsv>
{
    public GalanEntradaMap()
    {
        // Mapeos de compatibilidad - aunque no se usan con el nuevo método dinámico
    }
}

/// <summary>
/// Mapeo flexible para Salidas que acepta variaciones de nombres
/// </summary>
public sealed class GalanSalidaMap : ClassMap<GalanSalidaRawCsv>
{
    public GalanSalidaMap()
    {
        Map(m => m.Albaran).Name("Albaran", "Albarán");
        Map(m => m.NumeroDePedidoDeTercero).Name("NumeroDePedidoDeTercero", "Número de Pedido de Tercero");
        Map(m => m.CodigoDeArticulo).Name("CodigoDeArticulo", "Código de Artículo");
        Map(m => m.CódigoDeDepartamento).Name("CódigoDeDepartamento", "Código del Departamento");
        Map(m => m.CódigoDeFamily).Name("CódigoDeFamily", "Código de Familia");
        Map(m => m.Descripcion1).Name("Descripcion1", "Descripción 1");
        Map(m => m.Unidades).Name("Unidades");
        Map(m => m.CodigoDeServicioDeTransporte).Name("CodigoDeServicioDeTransporte", "Código de Servicio de Transporte");
        Map(m => m.Matricula).Name("Matricula", "Matrícula");
        Map(m => m.Fecha).Name("Fecha", "FECHA");
        Map(m => m.Destinatario).Name("Destinatario");
        Map(m => m.Almacen).Name("Almacen", "Almacén");
        Map(m => m.Celda).Name("Celda");
    }
}

/// <summary>
/// Mapeo flexible para Stock que acepta variaciones de nombres
/// </summary>
public sealed class GalanStockMap : ClassMap<GalanStockRawCsv>
{
    public GalanStockMap()
    {
        Map(m => m.CodigoDeArticulo).Name("CodigoDeArticulo", "Código de Artículo");
        Map(m => m.CódigoDeDepartamento).Name("CódigoDeDepartamento", "Código del Departamento");
        Map(m => m.CódigoDeFamily).Name("CódigoDeFamily", "Código de Familia");
        Map(m => m.CodigoDeCelda).Name("CodigoDeCelda", "Código de Celda");
        Map(m => m.StockB).Name("StockB", "Stock B");
        Map(m => m.StockA).Name("StockA", "Stock A");
        Map(m => m.Stock).Name("Stock");
        Map(m => m.ALM).Name("ALM", "Almacen", "Almacén");
        Map(m => m.Familia).Name("Familia");
        Map(m => m.Subfamilia).Name("Subfamilia");
        Map(m => m.Descripcion).Name("Descripcion", "Descripción");
    }
}
