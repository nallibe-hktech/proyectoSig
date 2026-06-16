using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

var file = @"C:\Projects\workspaces\SIG-es\Mediapost\Mediapost\Documentación\infrecep07_20260605_054809.xlsx";
Console.WriteLine($"=== Explorando {System.IO.Path.GetFileName(file)} ===\n");

using (var wb = new XLWorkbook(file))
{
    foreach (var ws in wb.Worksheets)
    {
        Console.WriteLine($"Worksheet: {ws.Name}");
        var rows = ws.Rows().ToList();
        Console.WriteLine($"Total filas: {rows.Count}\n");

        for (int i = 0; i < rows.Count && i < 30; i++)
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
                foreach (var cell in usedCells.Take(15))
                {
                    Console.WriteLine($"      {cell.Address}: [{cell.Value}]");
                }
            }
        }
    }
}
