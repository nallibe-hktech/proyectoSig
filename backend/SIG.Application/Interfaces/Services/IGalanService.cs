using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Application.Interfaces.Services;

public interface IGalanService
{
    Task<PagedResult<GalanEntradaDto>> GetEntradasAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<PagedResult<GalanSalidaDto>> GetSalidasAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<IReadOnlyList<GalanStockDto>> GetStockAsync(CancellationToken ct);
    Task<GalanEntradaDto?> GetEntradaByIdAsync(int id, CancellationToken ct);
    Task<GalanSalidaDto?> GetSalidaByIdAsync(int id, CancellationToken ct);
    Task<GalanStockDto?> GetStockByIdAsync(int id, CancellationToken ct);

    // Dashboard KPIs
    Task<GalanDashboardDto> GetDashboardAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public class GalanDashboardDto
{
    public decimal StockTotalValue { get; set; }          // Valor total de inventario
    public int EntradasCount { get; set; }                 // Registros de entrada en período
    public int SalidasCount { get; set; }                  // Registros de salida en período
    public decimal CostoLogisticoTotal { get; set; }       // Costo total logístico estimado
    public int ArticulosDiferentes { get; set; }           // Cantidad de SKUs únicos
    public decimal VolumenMovido { get; set; }             // Unidades movidas (E+S)
    public List<GalanStockBajoDto> AlertasStockBajo { get; set; } = new();
}

public class GalanStockBajoDto
{
    public string CodigoArticulo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal StockActual { get; set; }
    public int UmbraloAlerta { get; set; }
}
