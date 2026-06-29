using SIG.Application.DTOs;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;

namespace SIG.Application.Services;

// Notificaciones in-app (circuito de devolución de cierre): el rechazo de un cierre crea una
// notificación para el aprobador anterior; la campana del shell la lista y la marca como leída.
public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo) { _repo = repo; }

    private static NotificationDto Map(Notification n) =>
        new(n.Id, n.Titulo, n.Mensaje, n.Tipo, n.CierreId, n.TipoCierre, n.Leida, n.CreatedAt);

    public async Task<NotificationDto> CreateAsync(int usuarioId, string titulo, string mensaje, string tipo, int? cierreId, TipoCierre? tipoCierre, CancellationToken ct)
    {
        var notification = new Notification
        {
            UsuarioId = usuarioId,
            Titulo = titulo,
            Mensaje = mensaje,
            Tipo = tipo,
            CierreId = cierreId,
            TipoCierre = tipoCierre,
            Leida = false
        };
        await _repo.AddAsync(notification, ct);
        await _repo.SaveChangesAsync(ct);
        return Map(notification);
    }

    public async Task<IReadOnlyList<NotificationDto>> ListForUserAsync(int usuarioId, bool soloNoLeidas, int take, CancellationToken ct)
    {
        var items = await _repo.ListForUserAsync(usuarioId, soloNoLeidas, take, ct);
        return items.Select(Map).ToList();
    }

    public Task<int> CountUnreadAsync(int usuarioId, CancellationToken ct) =>
        _repo.CountUnreadAsync(usuarioId, ct);

    public async Task MarkReadAsync(int id, int usuarioId, CancellationToken ct)
    {
        await _repo.MarkReadAsync(id, usuarioId, ct);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(int usuarioId, CancellationToken ct)
    {
        await _repo.MarkAllReadAsync(usuarioId, ct);
        await _repo.SaveChangesAsync(ct);
    }
}
