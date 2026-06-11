using System.Globalization;
using ClosedXML.Excel;
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

    public MediapostExcelClient(ILogger<MediapostExcelClient> logger)
    {
        _logger = logger;
        // Ruta de los archivos de ejemplo (en producción sería carpeta SharePoint o API HTTP)
        _basePath = @"C:\Users\NallibeRiveraGrisale\proyecto SIG ES\Mediapost\Mediapost\Documentación";
    }

    public async Task<IReadOnlyList<MediapostPedidoDto>> GetPedidosAsync(DateTime desde, DateTime hasta, CancellationToken ct)
    {
        try
        {
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
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning("No hay worksheets en {File}", files);
                    return dtos;
                }

                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header
                foreach (var row in rows)
                {
                    try
                    {
                        var fechaPedido = DateTime.TryParse(row.Cell(1).Value.ToString(), out var fecha) ? fecha : DateTime.Now;

                        if (fechaPedido >= desde && fechaPedido <= hasta)
                        {
                            dtos.Add(new MediapostPedidoDto(
                                row.Cell(1).Value.ToString() ?? Guid.NewGuid().ToString(),
                                row.Cell(2).Value.ToString() ?? "",
                                row.Cell(3).Value.ToString() ?? "",
                                fechaPedido,
                                int.TryParse(row.Cell(4).Value.ToString(), out var qty) ? qty : 0,
                                row.Cell(5).Value.ToString() ?? "Pendiente",
                                row.Cell(6).Value.ToString(),
                                row.Cell(7).Value.ToString(),
                                row.Cell(8).Value.ToString(),
                                row.Cell(9).Value.ToString(),
                                row.Cell(10).Value.ToString()
                            ));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo fila de pedido en {File}", files);
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
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                {
                    _logger.LogWarning("No hay worksheets en {File}", files);
                    return dtos;
                }

                var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Skip header
                foreach (var row in rows)
                {
                    try
                    {
                        var fechaRecepcion = DateTime.TryParse(row.Cell(1).Value.ToString(), out var fecha) ? fecha : DateTime.Now;

                        if (fechaRecepcion >= desde && fechaRecepcion <= hasta)
                        {
                            dtos.Add(new MediapostRecepcionDto(
                                row.Cell(1).Value.ToString() ?? Guid.NewGuid().ToString(),
                                row.Cell(2).Value.ToString() ?? "",
                                row.Cell(3).Value.ToString() ?? "",
                                fechaRecepcion,
                                int.TryParse(row.Cell(4).Value.ToString(), out var qty) ? qty : 0,
                                int.TryParse(row.Cell(5).Value.ToString(), out var dmg) ? dmg : null,
                                row.Cell(6).Value.ToString() ?? "Recibida",
                                row.Cell(7).Value.ToString(),
                                row.Cell(8).Value.ToString()
                            ));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error leyendo fila de recepción en {File}", files);
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
