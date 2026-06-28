using Microsoft.EntityFrameworkCore.Migrations;

namespace SIG.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class AddMissingConceptsFromCsv : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Clean up "Ejemplo — " prefix from existing concepts (can't fix before adding new ones due to constraints)
        migrationBuilder.Sql(@"
            UPDATE concepts
            SET nombre = TRIM(SUBSTRING(nombre FROM 12))
            WHERE nombre LIKE 'Ejemplo — %';
        ");

        // Add missing concepts - using minimal formula_json to avoid escaping issues
        // Will need to be updated via ConceptsController with proper formulas
        migrationBuilder.InsertData(
            table: "concepts",
            columns: new[] { "nombre", "tipo", "columna_a3", "fecha_desde", "formula_json", "created_at", "updated_at" },
            values: new object[,]
            {
                { "Cuota por visita", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Cuota por hora estimada", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Cuota por hora trabajada", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Cuota por cantidad de módulos", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Cuota fija mensual por Recurso", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Cuota o dieta por día trabajado", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Dietas (Payhawk)", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Gastos Payhawk", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Incentivos mensuales", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Incentivos trimestrales", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Logistica Galán", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Logistica Galán + porcentaje", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Logistica MDP", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Logistica MDP + porcentaje", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Logistica autónomos", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Salario base dividido entre proyectos", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Salario fijo", "Pago", "ImporteBruto", new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Cuota por visita (facturación)", "Factura", null, new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Cuota por visita según tipo", "Factura", null, new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
                { "Gastos proyecto", "Factura", null, new DateOnly(2025, 1, 1), "{\"type\":\"Number\",\"value\":0}", DateTime.UtcNow, DateTime.UtcNow },
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DeleteData(
            table: "concepts",
            keyColumn: "nombre",
            keyValues: new object[] {
                "Cuota por visita", "Cuota por hora estimada", "Cuota por hora trabajada",
                "Cuota por cantidad de módulos", "Cuota fija mensual por Recurso",
                "Cuota o dieta por día trabajado", "Dietas (Payhawk)", "Gastos Payhawk",
                "Incentivos mensuales", "Incentivos trimestrales", "Logistica Galán",
                "Logistica Galán + porcentaje", "Logistica MDP", "Logistica MDP + porcentaje",
                "Logistica autónomos", "Salario base dividido entre proyectos", "Salario fijo",
                "Cuota por visita (facturación)", "Cuota por visita según tipo", "Gastos proyecto"
            });
    }
}
