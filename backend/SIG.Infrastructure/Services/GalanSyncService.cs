using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;
using SIG.Domain.Entities.Staging;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

/// <summary>
/// Servicio para sincronizar datos de Galán desde archivos Excel cargados.
/// Implementa deduplicación con SHA-256 hash y actualización incremental.
/// </summary>
public class GalanSyncService
{
    private readonly AppDbContext _db;
    private readonly ILogger<GalanSyncService> _logger;
    private readonly string _basePath;

    public GalanSyncService(AppDbContext db, ILogger<GalanSyncService> logger, IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _basePath = config["Integrations:Galan:BasePath"] ?? @"C:\dev\SIG-es\Galán\Galán";
    }

    /// <summary>
    /// Procesa un archivo Excel de Entradas: compara con BD, inserta nuevos, actualiza duplicados.
    /// </summary>
    public async Task<FileSyncResultDto> SyncEntradasFromFileAsync(string filePath, CancellationToken ct)
    {
        var errores = new List<string>();
        int insertados = 0, actualizados = 0, duplicados = 0, erroresCount = 0;

        try
        {
            var nuevasEntradas = ReadEntradasFromExcel(filePath);
            var entradasExistentes = await _db.StagingGalanEntradas.ToListAsync(ct);

            foreach (var nueva in nuevasEntradas)
            {
                try
                {
                    var hash = ComputeHash(nueva);
                    var existe = entradasExistentes.FirstOrDefault(e =>
                        e.CodigoArticulo == nueva.CodigoArticulo &&
                        e.Fecha == nueva.Fecha &&
                        e.Almacen == nueva.Almacen);

                    if (existe == null)
                    {
                        // Nuevo registro
                        _db.StagingGalanEntradas.Add(nueva);
                        insertados++;
                    }
                    else if (existe.Hash != hash)
                    {
                        // Actualizar si cambió
                        existe.Unidades = nueva.Unidades;
                        existe.Descripcion = nueva.Descripcion;
                        existe.Hash = hash;
                        existe.FechaUltimaSincronizacion = DateTime.UtcNow;
                        actualizados++;
                    }
                    else
                    {
                        // Mismo hash = duplicado
                        duplicados++;
                    }
                }
                catch (Exception ex)
                {
                    erroresCount++;
                    errores.Add($"Error procesando {nueva.CodigoArticulo}: {ex.Message}");
                    _logger.LogWarning(ex, "Error procesando entrada {Codigo}", nueva.CodigoArticulo);
                }
            }

            await _db.SaveChangesAsync(ct);

            return new FileSyncResultDto(
                "Entradas",
                true,
                insertados,
                actualizados,
                duplicados,
                erroresCount,
                FechaSincronizacion: DateTime.UtcNow,
                DetallesErrores: errores.Any() ? errores : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando Entradas desde {FilePath}", filePath);
            return new FileSyncResultDto(
                "Entradas",
                false,
                0, 0, 0, 1,
                MensajeError: ex.Message,
                DetallesErrores: new[] { ex.Message }
            );
        }
    }

    /// <summary>
    /// Procesa un archivo Excel de Stock (STOCK_*.xlsx)
    /// </summary>
    public async Task<FileSyncResultDto> SyncStockFromFileAsync(string filePath, CancellationToken ct)
    {
        var errores = new List<string>();
        int insertados = 0, actualizados = 0, duplicados = 0, erroresCount = 0;

        try
        {
            var nuevoStock = ReadStockFromExcel(filePath);
            var stockExistente = await _db.StagingGalanStocks.ToListAsync(ct);

            foreach (var nueva in nuevoStock)
            {
                try
                {
                    var hash = ComputeHash(nueva);
                    var existe = stockExistente.FirstOrDefault(s =>
                        s.CodigoArticulo == nueva.CodigoArticulo);

                    if (existe == null)
                    {
                        _db.StagingGalanStocks.Add(nueva);
                        insertados++;
                    }
                    else if (existe.Hash != hash)
                    {
                        existe.Stock = nueva.Stock;
                        existe.StockA = nueva.StockA;
                        existe.StockB = nueva.StockB;
                        existe.Descripcion = nueva.Descripcion;
                        existe.Hash = hash;
                        existe.FechaUltimaSincronizacion = DateTime.UtcNow;
                        actualizados++;
                    }
                    else
                    {
                        duplicados++;
                    }
                }
                catch (Exception ex)
                {
                    erroresCount++;
                    errores.Add($"Error procesando stock {nueva.CodigoArticulo}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync(ct);

            return new FileSyncResultDto(
                "Stock",
                true,
                insertados,
                actualizados,
                duplicados,
                erroresCount,
                FechaSincronizacion: DateTime.UtcNow,
                DetallesErrores: errores.Any() ? errores : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando Stock desde {FilePath}", filePath);
            return new FileSyncResultDto(
                "Stock",
                false,
                0, 0, 0, 1,
                MensajeError: ex.Message
            );
        }
    }

    /// <summary>
    /// Procesa un archivo Excel de Almacenaje (ALMACENAJE SIG *.xlsx)
    /// </summary>
    public async Task<FileSyncResultDto> SyncAlmacenajeFromFileAsync(string filePath, CancellationToken ct)
    {
        var errores = new List<string>();
        int insertados = 0, actualizados = 0, duplicados = 0, erroresCount = 0;

        try
        {
            var nuevoStock = ReadAlmacenajeFromExcel(filePath);
            var stockExistente = await _db.StagingGalanStocks.ToListAsync(ct);

            foreach (var nueva in nuevoStock)
            {
                try
                {
                    var hash = ComputeHash(nueva);
                    var existe = stockExistente.FirstOrDefault(s =>
                        s.CodigoArticulo == nueva.CodigoArticulo &&
                        s.CodigoCelda == nueva.CodigoCelda);

                    if (existe == null)
                    {
                        _db.StagingGalanStocks.Add(nueva);
                        insertados++;
                    }
                    else if (existe.Hash != hash)
                    {
                        existe.Stock = nueva.Stock;
                        existe.StockA = nueva.StockA;
                        existe.StockB = nueva.StockB;
                        existe.Descripcion = nueva.Descripcion;
                        existe.Hash = hash;
                        existe.FechaUltimaSincronizacion = DateTime.UtcNow;
                        actualizados++;
                    }
                    else
                    {
                        duplicados++;
                    }
                }
                catch (Exception ex)
                {
                    erroresCount++;
                    errores.Add($"Error procesando almacenaje {nueva.CodigoArticulo}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync(ct);

            return new FileSyncResultDto(
                "Almacenaje",
                true,
                insertados,
                actualizados,
                duplicados,
                erroresCount,
                FechaSincronizacion: DateTime.UtcNow,
                DetallesErrores: errores.Any() ? errores : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando Almacenaje desde {FilePath}", filePath);
            return new FileSyncResultDto(
                "Almacenaje",
                false,
                0, 0, 0, 1,
                MensajeError: ex.Message
            );
        }
    }

    /// <summary>
    /// Procesa un archivo Excel de Salidas (FACT_MENSUAL_*.xlsx)
    /// </summary>
    public async Task<FileSyncResultDto> SyncSalidasFromFileAsync(string filePath, CancellationToken ct)
    {
        var errores = new List<string>();
        int insertados = 0, actualizados = 0, duplicados = 0, erroresCount = 0;

        try
        {
            var nuevasSalidas = ReadSalidasFromExcel(filePath);
            var salidasExistentes = await _db.StagingGalanSalidas.ToListAsync(ct);

            foreach (var nueva in nuevasSalidas)
            {
                try
                {
                    var hash = ComputeHash(nueva);
                    var existe = salidasExistentes.FirstOrDefault(s =>
                        s.Albaran == nueva.Albaran &&
                        s.CodigoArticulo == nueva.CodigoArticulo);

                    if (existe == null)
                    {
                        _db.StagingGalanSalidas.Add(nueva);
                        insertados++;
                    }
                    else if (existe.Hash != hash)
                    {
                        existe.Unidades = nueva.Unidades;
                        existe.Descripcion = nueva.Descripcion;
                        existe.Hash = hash;
                        existe.FechaUltimaSincronizacion = DateTime.UtcNow;
                        actualizados++;
                    }
                    else
                    {
                        duplicados++;
                    }
                }
                catch (Exception ex)
                {
                    erroresCount++;
                    errores.Add($"Error procesando {nueva.Albaran}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync(ct);

            return new FileSyncResultDto(
                "Salidas",
                true,
                insertados,
                actualizados,
                duplicados,
                erroresCount,
                FechaSincronizacion: DateTime.UtcNow,
                DetallesErrores: errores.Any() ? errores : null
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sincronizando Salidas desde {FilePath}", filePath);
            return new FileSyncResultDto(
                "Salidas",
                false,
                0, 0, 0, 1,
                MensajeError: ex.Message
            );
        }
    }

    /// <summary>
    /// Lee Entradas desde un archivo Excel subido
    /// Estructura: CodigoDeArticulo | Código Departamento | Código Familia | Descripcion | FECHA | Unidades | Empresa | Almacen | Celda
    /// </summary>
    private List<StagingGalanEntrada> ReadEntradasFromExcel(string filePath)
    {
        var entradas = new List<StagingGalanEntrada>();

        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return entradas;

                var rows = worksheet.Rows().ToList();
                int headerRowIndex = 0;

                // Buscar headers
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("codigo"))
                    {
                        headerRowIndex = i;
                        break;
                    }
                }

                // Leer datos
                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];
                        var codigo = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(codigo)) continue;

                        // Intentar parsear fecha de Cell(5), si falla usar UtcNow
                        DateTime fecha = DateTime.UtcNow;
                        if (DateTime.TryParse(row.Cell(5).Value.ToString(), out var parsedDate))
                        {
                            fecha = parsedDate;
                        }

                        entradas.Add(new StagingGalanEntrada
                        {
                            CodigoArticulo = codigo,
                            CodigoDepartamento = row.Cell(2).Value.ToString().Trim(),
                            CodigoFamilia = row.Cell(3).Value.ToString().Trim(),
                            Descripcion = row.Cell(4).Value.ToString().Trim(),
                            Fecha = fecha,
                            Unidades = int.TryParse(row.Cell(6).Value.ToString(), out int u) ? u : 0,
                            Empresa = row.Cell(7).Value.ToString().Trim(),
                            Almacen = row.Cell(8).Value.ToString().Trim(),
                            Celda = row.Cell(9).Value.ToString().Trim(),
                            Hash = "",
                            PayloadJson = "",
                            FechaUltimaSincronizacion = DateTime.UtcNow
                        });
                    }
                    catch { /* skip malformed rows */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo Entradas Excel");
        }

        return entradas;
    }

    /// <summary>
    /// Lee Stock desde STOCK_*.xlsx
    /// Estructura: CodigoDeArticulo(1) | Código Departamento(2) | Código Familia(3) | Descripcion(4) | Stock(5) | Empresa(6)
    /// </summary>
    private List<StagingGalanStock> ReadStockFromExcel(string filePath)
    {
        var stock = new List<StagingGalanStock>();

        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return stock;

                var rows = worksheet.Rows().ToList();
                int headerRowIndex = 0;

                // Buscar headers
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("codigo"))
                    {
                        headerRowIndex = i;
                        break;
                    }
                }

                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];
                        var codigo = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(codigo)) continue;

                        stock.Add(new StagingGalanStock
                        {
                            CodigoArticulo = codigo,
                            CodigoDepartamento = row.Cell(2).Value.ToString().Trim(),
                            CodigoFamilia = row.Cell(3).Value.ToString().Trim(),
                            Descripcion = row.Cell(4).Value.ToString().Trim(),
                            Stock = decimal.TryParse(row.Cell(5).Value.ToString(), out decimal s) ? s : 0,
                            StockA = 0,
                            StockB = 0,
                            Almacen = row.Cell(6).Value.ToString().Trim(),
                            CodigoCelda = "",
                            Familia = "",
                            SubFamilia = "",
                            Hash = "",
                            PayloadJson = "",
                            FechaUltimaSincronizacion = DateTime.UtcNow
                        });
                    }
                    catch { /* skip malformed rows */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo Stock desde STOCK_*.xlsx");
        }

        return stock;
    }

    /// <summary>
    /// Lee Stock desde STOCK_*.xlsx
    /// Estructura: CodigoDeArticulo(1) | Código Departamento(2) | Código Familia(3) | Descripcion(4) | Stock(5) | Empresa(6)
    /// </summary>
    private List<StagingGalanStock> ReadAlmacenajeFromExcel(string filePath)
    {
        var stock = new List<StagingGalanStock>();

        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return stock;

                var rows = worksheet.Rows().ToList();
                int headerRowIndex = 0;

                // Buscar headers
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("codigo"))
                    {
                        headerRowIndex = i;
                        break;
                    }
                }

                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];
                        var codigo = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(codigo)) continue;

                        stock.Add(new StagingGalanStock
                        {
                            CodigoArticulo = codigo,
                            CodigoDepartamento = row.Cell(2).Value.ToString().Trim(),
                            CodigoFamilia = row.Cell(3).Value.ToString().Trim(),
                            Descripcion = row.Cell(4).Value.ToString().Trim(),
                            Stock = decimal.TryParse(row.Cell(5).Value.ToString(), out decimal s) ? s : 0,
                            StockA = 0,
                            StockB = 0,
                            Almacen = row.Cell(6).Value.ToString().Trim(),
                            CodigoCelda = "",
                            Familia = "",
                            SubFamilia = "",
                            Hash = "",
                            PayloadJson = "",
                            FechaUltimaSincronizacion = DateTime.UtcNow
                        });
                    }
                    catch { /* skip malformed rows */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo Stock desde STOCK_*.xlsx");
        }

        return stock;
    }

    /// <summary>
    /// Lee Salidas desde FACT_MENSUAL_*.xlsx
    /// Estructura: Albaran(1) | NumeroPedidoTercero(2) | CodigoArticulo(3) | CodDepartamento(4) | CodFamilia(5) | Descripcion1(6) | Descripcion2(7) | Unidades(8) | ... | Fecha(17) | Destinatario(18) | Almacen(19) | Celda(20)
    /// </summary>
    private List<StagingGalanSalida> ReadSalidasFromExcel(string filePath)
    {
        var salidas = new List<StagingGalanSalida>();

        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return salidas;

                var rows = worksheet.Rows().ToList();
                int headerRowIndex = 0;

                // Buscar headers (busca "Albaran" o "Referencia")
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("albaran") || cellValue.Contains("referencia"))
                    {
                        headerRowIndex = i;
                        break;
                    }
                }

                for (int i = headerRowIndex + 1; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];
                        var albaran = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(albaran)) continue;

                        // Intentar parsear fecha de Cell(17)
                        DateTime fecha = DateTime.UtcNow;
                        if (DateTime.TryParse(row.Cell(17).Value.ToString(), out var parsedDate))
                        {
                            fecha = parsedDate;
                        }

                        salidas.Add(new StagingGalanSalida
                        {
                            Albaran = albaran,
                            NumeroPedidoTercero = row.Cell(2).Value.ToString().Trim(),
                            CodigoArticulo = row.Cell(3).Value.ToString().Trim(),
                            Descripcion = row.Cell(6).Value.ToString().Trim(),
                            Unidades = int.TryParse(row.Cell(8).Value.ToString(), out int u) ? u : 0,
                            Fecha = fecha,
                            Almacen = row.Cell(19).Value.ToString().Trim(),
                            Celda = row.Cell(20).Value.ToString().Trim(),
                            Hash = "",
                            PayloadJson = "",
                            FechaUltimaSincronizacion = DateTime.UtcNow
                        });
                    }
                    catch { /* skip malformed rows */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo Salidas Excel");
        }

        return salidas;
    }

    /// <summary>
    /// Genera SHA-256 hash de un objeto para deduplicación
    /// </summary>
    private string ComputeHash(object obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes);
    }
}
