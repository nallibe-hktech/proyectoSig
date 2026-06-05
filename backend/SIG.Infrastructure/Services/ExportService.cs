using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ExportService> _logger;

    public ExportService(AppDbContext db, IAuditLogRepository auditRepo, ICurrentUserService currentUser, ILogger<ExportService> logger)
    {
        _db = db;
        _auditRepo = auditRepo;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<(byte[] Content, string FileName)> ExportA3InnuvaAsync(int closureId, int usuarioId, CancellationToken ct)
    {
        var timestamp = DateTime.UtcNow;
        _logger.LogInformation($"Iniciando export A3 Innuva para cierre {closureId} por usuario {usuarioId}");

        var closure = await LoadClosureForExportAsync(closureId, ct);
        EnsureApproved(closure);
        _logger.LogInformation($"Validación completada: Cierre {closureId} en estado {closure.Estado}");

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
        var paymentLines = closure.Lines.Where(l => l.Tipo == TipoConcepto.Pago && l.UserId.HasValue).ToList();
        _logger.LogInformation($"Procesando {paymentLines.Count} líneas de pago para export");

        var employeeData = paymentLines
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

        _logger.LogInformation($"Export A3 Innuva: {employeeData.Count} empleados, {employeeData.Sum(e => e.Aggregated.Count)} conceptos mapeados");

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

        _logger.LogInformation($"Export A3 Innuva completado exitosamente: {fileName} ({content.Length} bytes)");
        return (content, fileName);
    }

    public async Task<(byte[] Content, string FileName)> ExportA3ErpAsync(int closureId, int usuarioId, CancellationToken ct)
    {
        var timestamp = DateTime.UtcNow;
        _logger.LogInformation($"Iniciando export A3 ERP para cierre {closureId} por usuario {usuarioId}");

        var closure = await LoadClosureForExportAsync(closureId, ct);
        EnsureApproved(closure);
        _logger.LogInformation($"Validación completada: Cierre {closureId} en estado {closure.Estado}");

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
        _logger.LogInformation($"Procesando {invoiceLines.Count} líneas de factura para export ERP");

        decimal totalBeforeVat = 0;
        decimal totalVat = 0;

        foreach (var line in invoiceLines)
        {
            var vat = GetVatRate(client?.Pais, _logger);
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

        _logger.LogInformation($"Export A3 ERP: {invoiceLines.Count} líneas, Total antes IVA: {totalBeforeVat:C}, IVA: {totalVat:C}, Total: {totalBeforeVat + totalVat:C}");

        await LogExportAsync(closureId, "A3ERP", ct);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var content = ms.ToArray();
        var fileName = $"A3ERP_{closure.Id}_{closure.Period.Nombre.Replace(" ", "_")}.xlsx";

        _logger.LogInformation($"Export A3 ERP completado exitosamente: {fileName} ({content.Length} bytes)");
        return (content, fileName);
    }

    private static decimal GetVatRate(string? pais, ILogger<ExportService> logger)
    {
        // 21% for Spain (or null), 0% for intra-EU
        if (string.IsNullOrEmpty(pais) ||
            pais.Equals("España", StringComparison.OrdinalIgnoreCase) ||
            pais.Equals("Spain", StringComparison.OrdinalIgnoreCase))
            return 21m;
        return 0m;
    }

    private async Task<Closure> LoadClosureForExportAsync(int id, CancellationToken ct)
    {
        var closure = await _db.Closures
            .Include(c => c.Project)
            .Include(c => c.Period)
            .Include(c => c.Lines)
            .Include(c => c.Lines).ThenInclude(l => l.User).ThenInclude(u => u!.Department)
            .IgnoreQueryFilters()  // Ignore soft-delete filter for Closure and related entities
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new EntityNotFoundException("Closure", id);

        // Explicitly load Client with IgnoreQueryFilters to bypass soft-delete filter
        if (closure.Project?.ClientId > 0)
        {
            var client = await _db.Clients
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Id == closure.Project.ClientId, ct);
            if (closure.Project != null && client != null)
                closure.Project.Client = client;
        }

        // Explicitly load Concepts with IgnoreQueryFilters to bypass soft-delete filter
        var conceptIds = closure.Lines.Select(l => l.ConceptId).Distinct().ToList();
        var concepts = await _db.Concepts
            .IgnoreQueryFilters()
            .Where(c => conceptIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, ct);

        // Attach loaded concepts to the lines
        foreach (var line in closure.Lines)
        {
            if (concepts.TryGetValue(line.ConceptId, out var concept))
                line.Concept = concept;
        }

        return closure;
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
