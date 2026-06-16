using System.Globalization;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Application.Interfaces.Integrations;

namespace SIG.Infrastructure.Integrations.Fake;

/// <summary>
/// Cliente para leer datos de Mediapost desde archivos Excel locales o SharePoint
/// Por ahora lee desde carpeta local. En producción usaría HTTP API o SharePoint SDK.
/// </summary>
public class MediapostExcelClient : IMediapostClient
{
    private readonly ILogger<MediapostExcelClient> _logger;
    private readonly string _basePath;

    public MediapostExcelClient(ILogger<MediapostExcelClient> logger, IConfiguration config)
    {
        _logger = logger;
        // Ruta de los archivos de Mediapost (configurable; en producción sería SharePoint o API HTTP)
        _basePath = config["Integrations:Mediapost:BasePath"]
            ?? @"C:\dev\SIG-es\Mediapost\Mediapost\Documentación";
    }

    public async Task<IReadOnlyList<MediapostPedidoDto>> GetPedidosAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        try
        {
            // Si la carpeta de origen aún no existe (nadie ha subido ficheros), degradar a vacío en vez de lanzar 500
            if (!Directory.Exists(_basePath))
            {
                _logger.LogWarning("Carpeta de Mediapost no existe: {Path}. No hay pedidos que sincronizar.", _basePath);
                return Array.Empty<MediapostPedidoDto>();
            }

            // Buscar archivo de pedidos (infpedsit11_*.xlsx)
            var files = Directory.GetFiles(_basePath, "infpedsit11_*.xlsx")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos infpedsit11_*.xlsx en {Path}", _basePath);
                return Array.Empty<MediapostPedidoDto>();
            }

            var dtos = new List<MediapostPedidoDto>();

            using (var workbook = new XLWorkbook(files))
            {
                // Los archivos Mediapost tienen 2 worksheets: "Parametros" y "Report"
                // Necesitamos leer el worksheet "Report"
                var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name.Equals("Report", StringComparison.OrdinalIgnoreCase));
                if (worksheet == null)
                {
                    // Fallback: tomar el segundo worksheet si no existe "Report"
                    worksheet = workbook.Worksheets.Skip(1).FirstOrDefault();
                }

                if (worksheet == null)
                {
                    _logger.LogWarning("No hay worksheet 'Report' en {File}", files);
                    return dtos;
                }

                var rows = worksheet.Rows().ToList();
                _logger.LogInformation("Excel '{Name}' total filas: {Count}", worksheet.Name, rows.Count);

                // El worksheet "Report" tiene:
                // Fila 1: nombre del reporte (infpedsit11)
                // Fila 2: título
                // Fila 3: empresa
                // Fila 4: headers
                // Fila 5+: datos

                // Buscar fila de headers
                int headerRowIndex = 3; // Por defecto, fila 4 (índice 3)
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellA = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellA.Contains("nº") && cellA.Contains("documento"))
                    {
                        headerRowIndex = i;
                        _logger.LogInformation("Headers detectados en fila {RowNum}", i + 1);
                        break;
                    }
                }

                int dataStartIndex = headerRowIndex + 1;
                _logger.LogInformation("Leyendo datos desde fila {StartRow} (después de headers)", dataStartIndex + 1);

                // Leer datos desde fila siguiente a headers
                for (int i = dataStartIndex; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var cell1 = row.Cell(1).Value.ToString() ?? "";

                    // Detener si encontramos fila vacía
                    if (string.IsNullOrWhiteSpace(cell1))
                        break;

                    try
                    {
                        // Estructura del Report:
                        // A: Nº documento cliente
                        // B: No. referencia transporte
                        // C: No. expedición transporte
                        // D: Nombre punto entrega
                        // E: Dirección punto entrega
                        // F: C.P. Punto entrega
                        // G: Población punto entrega
                        // H: Provincia punto entrega
                        // I: País Punto entrega
                        // J: Comentarios
                        // K: Fecha preparación
                        // L: Fecha de expedición
                        // M: Estado
                        // N: Fecha última situación
                        // O: Descripción última situación
                        // P: Agencia de transporte
                        // Q: Servicio Trans.
                        // R: URL seguimiento
                        // S: Localizador

                        var fechaStr = row.Cell(12).Value.ToString() ?? ""; // L = columna 12
                        if (!DateTime.TryParse(fechaStr, out var fechaPedido))
                            fechaPedido = DateTime.Now;

                        if (fechaPedido >= desde && fechaPedido <= hasta)
                        {
                            var pedido = new MediapostPedidoDto(
                                cell1,                              // A: Nº documento
                                row.Cell(2).Value.ToString() ?? "",  // B: referencia
                                row.Cell(3).Value.ToString() ?? "",  // C: expedición
                                fechaPedido,                         // L: fecha
                                0, // Mediapost no tiene cantidad, usar 0
                                row.Cell(13).Value.ToString() ?? "Pendiente", // M: estado
                                row.Cell(4).Value.ToString(),        // D: nombre entrega
                                row.Cell(5).Value.ToString(),        // E: dirección
                                row.Cell(6).Value.ToString(),        // F: CP
                                row.Cell(7).Value.ToString(),        // G: población
                                row.Cell(8).Value.ToString()         // H: provincia
                            );
                            dtos.Add(pedido);
                            _logger.LogInformation("Pedido: {NroDocumento} - {Estado} ({Fecha})", cell1, pedido.Estado, fechaPedido.Date);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo fila {RowNum} de pedidos", i + 1);
                        continue;
                    }
                }
            }

            _logger.LogInformation("Leídos {Count} pedidos de Mediapost desde {File}", dtos.Count, files);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo pedidos de Mediapost");
            throw;
        }
    }

    public async Task<IReadOnlyList<MediapostRecepcionDto>> GetRecepcionesAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        try
        {
            // Si la carpeta de origen aún no existe (nadie ha subido ficheros), degradar a vacío en vez de lanzar 500
            if (!Directory.Exists(_basePath))
            {
                _logger.LogWarning("Carpeta de Mediapost no existe: {Path}. No hay recepciones que sincronizar.", _basePath);
                return Array.Empty<MediapostRecepcionDto>();
            }

            // Buscar archivo de recepciones (infrecep07_*.xlsx)
            var files = Directory.GetFiles(_basePath, "infrecep07_*.xlsx")
                .OrderByDescending(f => f)
                .FirstOrDefault();

            if (files == null)
            {
                _logger.LogWarning("No se encontraron archivos infrecep07_*.xlsx en {Path}", _basePath);
                return Array.Empty<MediapostRecepcionDto>();
            }

            var dtos = new List<MediapostRecepcionDto>();

            using (var workbook = new XLWorkbook(files))
            {
                // Los archivos Mediapost tienen múltiples worksheets: "Parametros", "Report", "Hoja1", etc.
                // Necesitamos leer el worksheet "Report"
                var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name.Equals("Report", StringComparison.OrdinalIgnoreCase));
                if (worksheet == null)
                {
                    // Fallback: tomar el segundo worksheet si no existe "Report"
                    worksheet = workbook.Worksheets.Skip(1).FirstOrDefault();
                }

                if (worksheet == null)
                {
                    _logger.LogWarning("No hay worksheet 'Report' en {File}", files);
                    return dtos;
                }

                var rows = worksheet.Rows().ToList();
                _logger.LogInformation("Excel '{Name}' total filas: {Count}", worksheet.Name, rows.Count);

                // El worksheet "Report" tiene:
                // Fila 1: nombre del reporte (infrecep07)
                // Fila 2: título
                // Fila 3: vacía
                // Fila 4: empresa
                // Fila 5: headers
                // Fila 6+: datos

                // Buscar fila de headers
                int headerRowIndex = 4; // Por defecto, fila 5 (índice 4)
                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellA = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellA.Contains("nº") && cellA.Contains("recepción"))
                    {
                        headerRowIndex = i;
                        _logger.LogInformation("Headers detectados en fila {RowNum}", i + 1);
                        break;
                    }
                }

                int dataStartIndex = headerRowIndex + 1;
                _logger.LogInformation("Leyendo datos desde fila {StartRow}", dataStartIndex + 1);

                // Leer datos desde fila siguiente a headers
                for (int i = dataStartIndex; i < rows.Count; i++)
                {
                    var row = rows[i];
                    var cell1 = row.Cell(1).Value.ToString() ?? "";

                    // Detener si encontramos fila vacía
                    if (string.IsNullOrWhiteSpace(cell1))
                        break;

                    try
                    {
                        // Estructura del Report (recepciones):
                        // A: Nº Recepción
                        // B: Nº Documento Cliente
                        // C: Nº Documento Origen
                        // D: Nº Documento Proveedor
                        // E: Tipo de Recepción
                        // F: Código Operativa Recepción
                        // G: Fecha confirmación SGA
                        // H: Nombre Proveedor
                        // I: Observaciones
                        // J: Nº Bultos recibidos
                        // K: Nº de palets recibidos
                        // L: Peso Facturable
                        // M: Volumen
                        // N: Recepción Especial
                        // O: Cód. Producto interno

                        var fechaStr = row.Cell(7).Value.ToString() ?? ""; // G = columna 7
                        if (!DateTime.TryParse(fechaStr, out var fechaRecepcion))
                            fechaRecepcion = DateTime.Now;

                        if (fechaRecepcion >= desde && fechaRecepcion <= hasta)
                        {
                            var recepcion = new MediapostRecepcionDto(
                                cell1,                               // A: Nº Recepción
                                row.Cell(2).Value.ToString() ?? "",  // B: Nº Documento Cliente
                                row.Cell(3).Value.ToString() ?? "",  // C: Nº Documento Origen
                                fechaRecepcion,                      // G: Fecha
                                int.TryParse(row.Cell(10).Value.ToString(), out var bultos) ? bultos : 0, // J: Bultos
                                null, // Mediapost no tiene daño/pérdida en recepciones
                                row.Cell(5).Value.ToString() ?? "Recibida", // E: Tipo de recepción
                                row.Cell(8).Value.ToString(),       // H: Proveedor
                                row.Cell(9).Value.ToString()        // I: Observaciones
                            );
                            dtos.Add(recepcion);
                            _logger.LogInformation("Recepción: {NroRecepcion} - {Estado} ({Fecha})", cell1, recepcion.Estado, fechaRecepcion.Date);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo fila {RowNum} de recepción", i + 1);
                        continue;
                    }
                }
            }

            _logger.LogInformation("Leídas {Count} recepciones de Mediapost desde {File}", dtos.Count, files);
            return dtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leyendo recepciones de Mediapost");
            throw;
        }
    }
}
