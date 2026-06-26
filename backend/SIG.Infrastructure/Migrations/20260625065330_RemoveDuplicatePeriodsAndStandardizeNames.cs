using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicatePeriodsAndStandardizeNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Estandarizar nombres de períodos al formato "Mes Año"
            migrationBuilder.Sql("UPDATE periods SET nombre = 'Junio 2026' WHERE id = 7 AND nombre = '2026-06';");

            // Eliminar período duplicado (id:6 es duplicado de id:3 - mismo rango de fechas)
            migrationBuilder.Sql("DELETE FROM periods WHERE id = 6 AND nombre = '2026-01';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir - restaurar período eliminado
            migrationBuilder.Sql("INSERT INTO periods (id, nombre, fecha_inicio, fecha_fin, dia_pago, estado, deleted_at) " +
                "VALUES (6, '2026-01', '2026-01-01', '2026-01-31', 28, 'Abierto', NULL);");

            // Revertir nombres estandarizados
            migrationBuilder.Sql("UPDATE periods SET nombre = '2026-06' WHERE id = 7 AND nombre = 'Junio 2026';");
        }
    }
}
