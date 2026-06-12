using ClosedXML.Excel;

var file = @"C:\Projects\workspaces\SIG-es\Mediapost\Mediapost\Documentación\infpedsit11_20260605_052634.xlsx";
Console.WriteLine($"=== Explorando {Path.GetFileName(file)} ===\n");

using (var wb = new XLWorkbook(file))
{
    foreach (var ws in wb.Worksheets)
    {
        Console.WriteLine($"Worksheet: {ws.Name}");
        var rows = ws.Rows().ToList();
        Console.WriteLine($"Total filas: {rows.Count}\n");
        
        for (int i = 0; i < rows.Count && i < 20; i++)
        {
            var row = rows[i];
            var usedCells = row.CellsUsed().ToList();
            Console.Write($"Fila {i+1:D2}: ");
            if (usedCells.Count == 0)
            {
                Console.WriteLine("(vacía)");
            }
            else
            {
                Console.WriteLine($"({usedCells.Count} celdas)");
                foreach (var cell in usedCells.Take(20))
                {
                    Console.WriteLine($"      {cell.Address}: [{cell.Value}]");
                }
            }
        }
    }
}
