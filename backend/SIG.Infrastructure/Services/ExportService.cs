using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using SIG.Application.Interfaces.Repositories;
using SIG.Application.Interfaces.Services;
using SIG.Domain.Entities;
using SIG.Domain.Enums;
using SIG.Domain.Exceptions;
using SIG.Infrastructure.Persistence;

namespace SIG.Infrastructure.Services;

public class ExportService : IExportService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogRepository _auditRepo;
    private readonly ICurrentUserService _currentUser;

    public ExportService(AppDbContext db, IAuditLogRepository auditRepo, ICurrentUserService currentUser)
    {
        _db = db;
        _auditRepo = auditRepo;
        _currentUser = currentUser;
    }

    public async Task<(byte[] Content, string FileName)> ExportA3InnuvaAsync(int closureId, int usuarioId, CancellationToken ct)
    {
        var closure = await LoadClosureForExportAsync(closureId, ct);
        EnsureApproved(closure);

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("A3Innuva",
                new XElement("Cabecera",
                    new XElement("ClosureId", closure.Id),
                    new XElement("Periodo", closure.Period.Nombre),
                    new XElement("FechaExportacion", DateTime.UtcNow.ToString("o"))
                ),
                new XElement("Empleados",
                    closure.Lines.Where(l => l.Tipo == TipoConcepto.Pago && l.UserId.HasValue)
                        .GroupBy(l => l.UserId!.Value)
                        .Select(g =>
                        {
                            var user = g.First().User;
                            return new XElement("Empleado",
                                new XAttribute("Id", user?.Id ?? 0),
                                new XElement("NIF", user?.NIF ?? ""),
                                new XElement("Nombre", user != null ? $"{user.Nombre} {user.Apellidos}" : ""),
                                new XElement("Departamento", user?.Department?.Nombre ?? ""),
                                new XElement("Lineas",
                                    g.Select(l => new XElement("Linea",
                                        new XElement("Concepto", l.Concept?.Nombre ?? ""),
                                        new XElement("Importe", l.Importe.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))))
                                )
                            );
                        })
                )
            )
        );

        await LogExportAsync(closureId, "A3Innuva", ct);

        var content = Encoding.UTF8.GetBytes(doc.ToString());
        var fileName = $"A3Innuva_{closure.Id}_{closure.Period.Nombre.Replace(" ", "_")}.xml";
        return (content, fileName);
    }

    public async Task<(byte[] Content, string FileName)> ExportA3ErpAsync(int closureId, int usuarioId, CancellationToken ct)
    {
        var closure = await LoadClosureForExportAsync(closureId, ct);
        EnsureApproved(closure);

        var client = closure.Project.Client;
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement("A3ERP",
                new XElement("Cabecera",
                    new XElement("ClosureId", closure.Id),
                    new XElement("Periodo", closure.Period.Nombre),
                    new XElement("FechaExportacion", DateTime.UtcNow.ToString("o"))
                ),
                new XElement("Factura",
                    new XAttribute("ClientId", client?.Id ?? 0),
                    new XElement("ClienteNIF", client?.NIF ?? ""),
                    new XElement("ClienteNombre", client?.Nombre ?? ""),
                    new XElement("ProjectId", closure.ProjectId),
                    new XElement("ProjectoNombre", closure.Project.Nombre),
                    new XElement("Lineas",
                        closure.Lines.Where(l => l.Tipo == TipoConcepto.Factura)
                            .Select(l => new XElement("Linea",
                                new XElement("Concepto", l.Concept?.Nombre ?? ""),
                                new XElement("Importe", l.Importe.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))))
                    ),
                    new XElement("Total", closure.FacturacionTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture))
                )
            )
        );

        await LogExportAsync(closureId, "A3ERP", ct);

        var content = Encoding.UTF8.GetBytes(doc.ToString());
        var fileName = $"A3ERP_{closure.Id}_{closure.Period.Nombre.Replace(" ", "_")}.xml";
        return (content, fileName);
    }

    private async Task<Closure> LoadClosureForExportAsync(int id, CancellationToken ct)
    {
        return await _db.Closures
            .Include(c => c.Project).ThenInclude(p => p.Client)
            .Include(c => c.Period)
            .Include(c => c.Lines).ThenInclude(l => l.Concept)
            .Include(c => c.Lines).ThenInclude(l => l.User).ThenInclude(u => u!.Department)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new EntityNotFoundException("Closure", id);
    }

    private static void EnsureApproved(Closure c)
    {
        if (c.Estado != EstadoClosure.Aprobado && c.Estado != EstadoClosure.Exportado)
            throw new ClosureNotApprovedException();
    }

    private async Task LogExportAsync(int closureId, string formato, CancellationToken ct)
    {
        await _auditRepo.AddAsync(new AuditLog
        {
            UserId = _currentUser.UserId,
            EntityType = "Closure",
            EntityId = closureId.ToString(),
            Action = AuditAction.Export,
            NewValueJson = $"{{\"formato\":\"{formato}\"}}",
            Timestamp = DateTime.UtcNow,
            Ip = _currentUser.Ip
        }, ct);
        await _auditRepo.SaveChangesAsync(ct);
    }
}
