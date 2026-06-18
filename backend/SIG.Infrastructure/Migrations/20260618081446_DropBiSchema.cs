using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropBiSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El cliente no usa Power BI. Las vistas bi.v_* eran andamiaje del scaffolding inicial y
            // ningún código las consume; el reporting es nativo (PPT slide 23). Se eliminan.
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_audit_resumen;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_aprobaciones_pendientes;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_lineas_por_concepto;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_cierres_por_periodo;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS bi CASCADE;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No se restaura: era andamiaje Power BI descartado por decisión de negocio.
            // Si se necesitara, recrear desde la migración BiSchemaViews.
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS bi;");
        }
    }
}
