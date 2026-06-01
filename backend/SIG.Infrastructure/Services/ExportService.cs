using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
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

        var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("A3NOM");

        // Headers: Empresa | Imputación | Tipo Paga | ImporteBruto | SSEmpleado | IRPF | ImporteLiquido | SSEmpresa | Embargo | Anticipo | Prestamo | ProrrataExtras | KM
        var headerRow = sheet.CreateRow(0);
        var headers = new[] { "Empresa", "Imputación", "Tipo Paga", "Importe Bruto", "SS Trabajador", "IRPF", "Importe Líquido", "SS Empresa", "Embargo", "Anticipo", "Préstamo", "Prorrata Extras", "KM" };
        for (int i = 0; i < headers.Length; i++)
        {
            headerRow.CreateCell(i).SetCellValue(headers[i]);
        }

        // Column mapping: ColumnaA3 → column index
        var columnMap = new Dictionary<string, int>
        {
            { "ImporteBruto", 3 },
            { "SSEmpleado", 4 },
            { "IRPF", 5 },
            { "ImporteLiquido", 6 },
            { "SSEmpresa", 7 },
            { "Embargo", 8 },
            { "Anticipo", 9 },
            { "Prestamo", 10 },
            { "ProrrataExtras", 11 },
            { "KM", 12 }
        };

        // Group lines by UserId and aggregate by ColumnaA3
        var employeeData = closure.Lines
            .Where(l => l.Tipo == TipoConcepto.Pago && l.UserId.HasValue)
            .GroupBy(l => l.UserId!.Value)
            .Select(g =>
            {
                var user = g.First().User;
                var aggregated = new Dictionary<string, decimal>();
                foreach (var line in g)
                {
                    var colA3 = line.Concept?.ColumnaA3;
                    if (!string.IsNullOrEmpty(colA3) && columnMap.ContainsKey(colA3))
                    {
                        if (!aggregated.ContainsKey(colA3))
                            aggregated[colA3] = 0;
                        aggregated[colA3] += line.Importe;
                    }
                }
                return new { User = user, Aggregated = aggregated };
            })
            .ToList();

        int rowNum = 1;
        foreach (var emp in employeeData)
        {
            var row = sheet.CreateRow(rowNum++);
            row.CreateCell(0).SetCellValue(closure.Project.Client?.Nombre ?? "");
            row.CreateCell(1).SetCellValue(emp.User?.Department?.Nombre ?? "");
            row.CreateCell(2).SetCellValue("Nómina");

            foreach (var kvp in emp.Aggregated)
            {
                if (columnMap.TryGetValue(kvp.Key, out var colIdx))
                    row.CreateCell(colIdx).SetCellValue((double)kvp.Value);
            }
        }

        // Auto-fit columns
        for (int i = 0; i < headers.Length; i++)
            sheet.AutoSizeColumn(i);

        await LogExportAsync(closureId, "A3Innuva", ct);

        using var ms = new MemoryStream();
        workbook.Write(ms);
        var content = ms.ToArray();
        var fileName = $"A3Innuva_{closure.Id}_{closure.Period.Nombre.Replace(" ", "_")}.xls";
        return (content, fileName);
    }

    public async Task<(byte[] Content, string FileName)> ExportA3ErpAsync(int closureId, int usuarioId, CancellationToken ct)
    {
        var closure = await LoadClosureForExportAsync(closureId, ct);
        EnsureApproved(closure);

        var client = closure.Project.Client;

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Factura");

        // Headers
        ws.Cell("A1").Value = "Cliente NIF";
        ws.Cell("B1").Value = "Cliente Nombre";
        ws.Cell("C1").Value = "Proyecto";
        ws.Cell("D1").Value = "Concepto";
        ws.Cell("E1").Value = "Importe";
        ws.Cell("F1").Value = "IVA %";
        ws.Cell("G1").Value = "Total con IVA";

        // Format header row
        var headerRange = ws.Range("A1:G1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data
        int rowNum = 2;
        var invoiceLines = closure.Lines.Where(l => l.Tipo == TipoConcepto.Factura).ToList();
        decimal totalBeforeVat = 0;
        decimal totalVat = 0;

        foreach (var line in invoiceLines)
        {
            var vat = GetVatRate(client?.Pais);
            var vatAmount = line.Importe * vat / 100;

            ws.Cell(rowNum, 1).Value = client?.NIF ?? "";
            ws.Cell(rowNum, 2).Value = client?.Nombre ?? "";
            ws.Cell(rowNum, 3).Value = closure.Project.Nombre;
            ws.Cell(rowNum, 4).Value = line.Concept?.Nombre ?? "";
            ws.Cell(rowNum, 5).Value = (double)line.Importe;
            ws.Cell(rowNum, 6).Value = (double)vat;
            ws.Cell(rowNum, 7).Value = (double)(line.Importe + vatAmount);

            totalBeforeVat += line.Importe;
            totalVat += vatAmount;
            rowNum++;
        }

        // Total row
        ws.Cell(rowNum, 4).Value = "TOTAL";
        ws.Cell(rowNum, 4).Style.Font.Bold = true;
        ws.Cell(rowNum, 5).Value = (double)totalBeforeVat;
        ws.Cell(rowNum, 5).Style.Font.Bold = true;
        ws.Cell(rowNum, 6).Value = "";
        ws.Cell(rowNum, 7).Value = (double)(totalBeforeVat + totalVat);
        ws.Cell(rowNum, 7).Style.Font.Bold = true;

        // Format currency columns
        ws.Column(5).Style.NumberFormat.Format = "#,##0.00";
        ws.Column(7).Style.NumberFormat.Format = "#,##0.00";

        // Auto-fit columns
        ws.Columns("A", "G").AdjustToContents();

        await LogExportAsync(closureId, "A3ERP", ct);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var content = ms.ToArray();
        var fileName = $"A3ERP_{closure.Id}_{closure.Period.Nombre.Replace(" ", "_")}.xlsx";
        return (content, fileName);
    }

    private static decimal GetVatRate(string? pais)
    {
        // 21% for Spain (or null), 0% for intra-EU
        if (string.IsNullOrEmpty(pais) || pais == "España")
            return 21m;
        return 0m;
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
