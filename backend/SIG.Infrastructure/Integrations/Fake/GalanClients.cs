using System.Globalization;
using CsvHelper;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Fake;

/// <summary>
/// Cliente para leer datos de Galán desde archivos CSV locales o SharePoint
/// Por ahora lee desde carpeta local. En producción usaría SFTP/SharePoint SDK.
/// </summary>
public class GalanCsvClient : IGalanClient
{
    private readonly ILogger<GalanCsvClient> _logger;
    private readonly string _basePath;

    public GalanCsvClient(ILogger<GalanCsvClient> logger)
    {
        _logger = logger;
        // Ruta de los archivos de ejemplo (en producción sería carpeta SharePoint)
        _basePath = @"C:\Projects\workspaces\SIG-es\Galán\Galán";
    }

    public async Task<IReadOnlyList<GalanEntradaDto>> GetEntradasAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        try
        {
            var filePath = Path.Combine(_basePath, "Entradas_*.csv");
            var files = Directory.GetFiles(_basePath, "Entradas_*.csv")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos Entradas_*.csv en {Path}", _basePath);
                return Array.Empty<GalanEntradaDto>();
            }

            using var reader = new StreamReader(files);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<GalanEntradaRawCsv>().ToList();

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
            var files = Directory.GetFiles(_basePath, "Salidas_*.csv")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos Salidas_*.csv en {Path}", _basePath);
                return Array.Empty<GalanSalidaDto>();
            }

            using var reader = new StreamReader(files);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<GalanSalidaRawCsv>().ToList();

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
            var files = Directory.GetFiles(_basePath, "STOCK_celda_*.csv")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos STOCK_celda_*.csv en {Path}", _basePath);
                return Array.Empty<GalanStockDto>();
            }

            using var reader = new StreamReader(files);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<GalanStockRawCsv>().ToList();

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
