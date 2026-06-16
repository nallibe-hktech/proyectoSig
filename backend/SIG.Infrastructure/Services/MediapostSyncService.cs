using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SIG.Application.DTOs;
using SIG.Domain.Entities.Staging;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

/// <summary>
/// Servicio para sincronizar datos de Mediapost desde archivos Excel cargados.
/// Implementa deduplicación con SHA-256 hash para Pedidos y Recepciones.
/// </summary>
public class MediapostSyncService
{
    private readonly AppDbContext _db;
    private readonly ILogger<MediapostSyncService> _logger;
    private readonly string _basePath;

    public MediapostSyncService(AppDbContext db, ILogger<MediapostSyncService> logger, IConfiguration config)
    {
        _db = db;
        _logger = logger;
        _basePath = config["Integrations:Mediapost:BasePath"] ?? @"C:\dev\SIG-es\Mediapost\Mediapost\Documentación";
    }

    /// <summary>
    /// Procesa un archivo Excel de Pedidos (infpedsit11_*.xlsx)
    /// </summary>
    public async Task<FileSyncResultDto> SyncPedidosFromFileAsync(string filePath, CancellationToken ct)
    {
        var errores = new List<string>();
        int insertados = 0, actualizados = 0, duplicados = 0, erroresCount = 0;

        try
        {
            var nuevosPedidos = ReadPedidosFromExcel(filePath);
            var pedidosExistentes = await _db.StagingMediapostPedidos.ToListAsync(ct);

            foreach (var nuevo in nuevosPedidos)
            {
                try
                {
                    var hash = ComputeHash(nuevo);
                    var existe = pedidosExistentes.FirstOrDefault(p =>
                        p.PedidoId == nuevo.PedidoId);

                    if (existe == null)
                    {
                        _db.StagingMediapostPedidos.Add(nuevo);
                        insertados++;
                    }
                    else if (existe.Hash != hash)
                    {
                        existe.ReferenciaPedido = nuevo.ReferenciaPedido;
                        existe.DestinatarioNombre = nuevo.DestinatarioNombre;
                        existe.DireccionEntrega = nuevo.DireccionEntrega;
                        existe.CodigoPostal = nuevo.CodigoPostal;
                        existe.Ciudad = nuevo.Ciudad;
                        existe.Provincia = nuevo.Provincia;
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
                    errores.Add($"Error procesando pedido {nuevo.PedidoId}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync(ct);

            return new FileSyncResultDto(
                "Pedidos",
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
            _logger.LogError(ex, "Error sincronizando Pedidos desde {FilePath}", filePath);
            return new FileSyncResultDto(
                "Pedidos",
                false,
                0, 0, 0, 1,
                MensajeError: ex.Message
            );
        }
    }

    /// <summary>
    /// Procesa un archivo Excel de Recepciones (infrecep07_*.xlsx)
    /// </summary>
    public async Task<FileSyncResultDto> SyncRecepcionesFromFileAsync(string filePath, CancellationToken ct)
    {
        var errores = new List<string>();
        int insertados = 0, actualizados = 0, duplicados = 0, erroresCount = 0;

        try
        {
            var nuevasRecepciones = ReadRecepcionesFromExcel(filePath);
            var recepcionesExistentes = await _db.StagingMediapostRecepciones.ToListAsync(ct);

            foreach (var nueva in nuevasRecepciones)
            {
                try
                {
                    var hash = ComputeHash(nueva);
                    var existe = recepcionesExistentes.FirstOrDefault(r =>
                        r.RecepcionId == nueva.RecepcionId);

                    if (existe == null)
                    {
                        _db.StagingMediapostRecepciones.Add(nueva);
                        insertados++;
                    }
                    else if (existe.Hash != hash)
                    {
                        existe.CodigoArticulo = nueva.CodigoArticulo;
                        existe.FechaRecepcion = nueva.FechaRecepcion;
                        existe.Cantidad = nueva.Cantidad;
                        existe.CantidadDañada = nueva.CantidadDañada;
                        existe.Estado = nueva.Estado;
                        existe.Almacen = nueva.Almacen;
                        existe.Observaciones = nueva.Observaciones;
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
                    errores.Add($"Error procesando recepción {nueva.RecepcionId}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync(ct);

            return new FileSyncResultDto(
                "Recepciones",
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
            _logger.LogError(ex, "Error sincronizando Recepciones desde {FilePath}", filePath);
            return new FileSyncResultDto(
                "Recepciones",
                false,
                0, 0, 0, 1,
                MensajeError: ex.Message
            );
        }
    }

    /// <summary>
    /// Lee Pedidos desde infpedsit11_*.xlsx
    /// </summary>
    private List<StagingMediapostPedido> ReadPedidosFromExcel(string filePath)
    {
        var pedidos = new List<StagingMediapostPedido>();

        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Report", StringComparison.OrdinalIgnoreCase))
                             ?? workbook.Worksheets.Skip(1).FirstOrDefault()
                             ?? workbook.Worksheets.FirstOrDefault();

                if (worksheet == null) return pedidos;

                var rows = worksheet.Rows().ToList();
                int headerRowIndex = 3;

                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("nº") || cellValue.Contains("documento"))
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
                        var pedidoId = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(pedidoId)) continue;

                        pedidos.Add(new StagingMediapostPedido
                        {
                            PedidoId = pedidoId,
                            ReferenciaPedido = row.Cell(2).Value.ToString().Trim(),
                            CodigoArticulo = "",
                            FechaPedido = DateTime.UtcNow,
                            Cantidad = 1,
                            Estado = "Pendiente",
                            DestinatarioNombre = row.Cell(4).Value.ToString().Trim(),
                            DireccionEntrega = row.Cell(5).Value.ToString().Trim(),
                            CodigoPostal = row.Cell(6).Value.ToString().Trim(),
                            Ciudad = row.Cell(7).Value.ToString().Trim(),
                            Provincia = row.Cell(8).Value.ToString().Trim(),
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
            _logger.LogError(ex, "Error leyendo Pedidos Excel");
        }

        return pedidos;
    }

    /// <summary>
    /// Lee Recepciones desde infrecep07_*.xlsx
    /// </summary>
    private List<StagingMediapostRecepcion> ReadRecepcionesFromExcel(string filePath)
    {
        var recepciones = new List<StagingMediapostRecepcion>();

        try
        {
            using (var workbook = new XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals("Report", StringComparison.OrdinalIgnoreCase))
                             ?? workbook.Worksheets.Skip(1).FirstOrDefault()
                             ?? workbook.Worksheets.FirstOrDefault();

                if (worksheet == null) return recepciones;

                var rows = worksheet.Rows().ToList();
                int headerRowIndex = 3;

                for (int i = 0; i < Math.Min(10, rows.Count); i++)
                {
                    var cellValue = rows[i].Cell(1).Value.ToString()?.ToLower() ?? "";
                    if (cellValue.Contains("recepcion") || cellValue.Contains("número"))
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
                        var recepcionId = row.Cell(1).Value.ToString().Trim();
                        if (string.IsNullOrEmpty(recepcionId)) continue;

                        recepciones.Add(new StagingMediapostRecepcion
                        {
                            RecepcionId = recepcionId,
                            ReferenciaRecepcion = row.Cell(2).Value.ToString().Trim(),
                            CodigoArticulo = row.Cell(3).Value.ToString().Trim(),
                            FechaRecepcion = DateTime.UtcNow,
                            Cantidad = int.TryParse(row.Cell(4).Value.ToString(), out int c) ? c : 0,
                            CantidadDañada = int.TryParse(row.Cell(5).Value.ToString(), out int cd) ? cd : 0,
                            Estado = row.Cell(6).Value.ToString().Trim(),
                            Almacen = row.Cell(7).Value.ToString().Trim(),
                            Observaciones = row.Cell(8).Value.ToString().Trim(),
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
            _logger.LogError(ex, "Error leyendo Recepciones Excel");
        }

        return recepciones;
    }

    private string ComputeHash(object obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hashBytes);
    }
}
