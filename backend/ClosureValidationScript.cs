#!/usr/bin/env dotnet-script
// Script de validación: Cierres con datos sincronizados (PayHawk, SGPV, Celero)
// Ejecutar: dotnet script ClosureValidationScript.cs

#r "nuget: Microsoft.EntityFrameworkCore, 9.0.0"
#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 9.0.0"

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("🧪 VALIDACIÓN DE CIERRES CON DATOS SINCRONIZADOS");
Console.WriteLine("================================================\n");

// Verificar conexión a BD
var connStr = "Host=localhost;Port=5433;Database=siges;Username=sigesbi;Password=SigEs@2026;SslMode=Require";

try
{
    using var conn = new Npgsql.NpgsqlConnection(connStr);
    await conn.OpenAsync();
    Console.WriteLine("✅ Conexión a BD exitosa\n");

    // Verificar datos sincronizados
    var cmd = conn.CreateCommand();
    cmd.CommandText = @"
        SELECT 'PayHawk Gastos' as tabla, COUNT(*) as registros FROM payhawk_gasto
        UNION ALL
        SELECT 'SGPV Productos', COUNT(*) FROM sgpv_producto
        UNION ALL
        SELECT 'Celero Visitas', COUNT(*) FROM celero_visita
        UNION ALL
        SELECT 'Períodos', COUNT(*) FROM periodo
        UNION ALL
        SELECT 'Proyectos', COUNT(*) FROM proyecto
        UNION ALL
        SELECT 'Conceptos', COUNT(*) FROM concepto
        UNION ALL
        SELECT 'Cierres', COUNT(*) FROM cierre
    ";

    using var reader = await cmd.ExecuteReaderAsync();
    Console.WriteLine("📊 ESTADO DE DATOS SINCRONIZADOS:");
    while (await reader.ReadAsync())
    {
        var tabla = reader.GetString(0);
        var registros = reader.GetInt64(1);
        var emoji = registros > 0 ? "✅" : "⚠️";
        Console.WriteLine($"  {emoji} {tabla}: {registros:N0} registros");
    }

    await conn.CloseAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error de conexión: {ex.Message}");
    Environment.Exit(1);
}

Console.WriteLine("\n🔍 PRÓXIMOS PASOS:");
Console.WriteLine("  1. Crear cierre de prueba con período existente");
Console.WriteLine("  2. Ejecutar cálculos del motor");
Console.WriteLine("  3. Validar que se usen datos PayHawk + SGPV + Celero");
Console.WriteLine("  4. Verificar CalculationLog.InputsJson");
Console.WriteLine("\n📝 Ver PLAN_VALIDACION_CIERRES_5_JUNIO.md para detalles de pruebas");
