using SIG.Application.Common;
using SIG.Application.DTOs;

namespace SIG.Application.Interfaces.Services;

public interface IMediapostService
{
    Task<PagedResult<MediapostPedidoDto>> GetPedidosAsync(int page, int pageSize, string? search, string? estado, CancellationToken ct);
    Task<PagedResult<MediapostRecepcionDto>> GetRecepcionesAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<MediapostPedidoDto?> GetPedidoByIdAsync(int id, CancellationToken ct);
    Task<MediapostRecepcionDto?> GetRecepcionByIdAsync(int id, CancellationToken ct);

    // Dashboard KPIs
    Task<MediapostDashboardDto> GetDashboardAsync(DateOnly desde, DateOnly hasta, CancellationToken ct);
}

public class MediapostDashboardDto
{
    public int PedidosTotal { get; set; }                  // Total de pedidos en período
    public int PedidosEntregados { get; set; }             // Completados exitosamente
    public int PedidosPendientes { get; set; }             // En tránsito
    public int PedidosRechazados { get; set; }             // Rechazados/devueltos
    public decimal TasaEntrega { get; set; }               // % de entrega exitosa
    public int RecepcionesTotal { get; set; }              // Total de recepciones
    public int UnidadesRecibidas { get; set; }             // Total unidades recibidas
    public int UnidadesDestrozadas { get; set; }           // Unidades con daños
    public decimal CostoDistribucion { get; set; }         // Costo estimado de distribución
    public List<MediapostPedidoPendienteDto> PedidosPendientesDetalle { get; set; } = new();
}

public class MediapostPedidoPendienteDto
{
    public string PedidoId { get; set; } = "";
    public string ReferenciaPedido { get; set; } = "";
    public string DestinatarioNombre { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime FechaPedido { get; set; }
    public int DiasEnTransito { get; set; }
}
