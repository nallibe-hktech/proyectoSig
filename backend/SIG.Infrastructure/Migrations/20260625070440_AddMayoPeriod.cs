using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMayoPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insertar período Mayo 2026
            migrationBuilder.Sql(
                @"INSERT INTO periods (nombre, fecha_inicio, fecha_fin, dia_pago, estado, created_at, updated_at)
                  VALUES ('Mayo 2026', '2026-05-01'::date, '2026-05-31'::date, 30, 'Abierto', NOW(), NOW());"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar período Mayo 2026
            migrationBuilder.Sql("DELETE FROM periods WHERE nombre = 'Mayo 2026' AND fecha_inicio = '2026-05-01'::date;");
        }
    }
}
