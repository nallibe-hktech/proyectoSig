using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BiSchemaViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE SCHEMA IF NOT EXISTS bi;");

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW bi.v_cierres_por_periodo AS
                SELECT
                    c.project_id        AS project_id,
                    p.nombre            AS project_nombre,
                    c.period_id         AS period_id,
                    per.nombre          AS periodo_nombre,
                    c.coste_total       AS coste_total,
                    c.facturacion_total AS facturacion_total,
                    c.margen            AS margen,
                    c.estado            AS estado
                FROM public.closures c
                JOIN public.projects p ON p.id = c.project_id
                JOIN public.periods per ON per.id = c.period_id;
            """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW bi.v_lineas_por_concepto AS
                SELECT
                    cl.id           AS closure_line_id,
                    cl.concept_id   AS concept_id,
                    co.nombre       AS concepto_nombre,
                    cl.tipo         AS tipo,
                    cl.importe      AS importe,
                    cl.user_id      AS user_id,
                    COALESCE(u.nombre, '') || ' ' || COALESCE(u.apellidos, '') AS recurso_nombre,
                    c.period_id     AS period_id
                FROM public.closure_lines cl
                JOIN public.closures c ON c.id = cl.closure_id
                JOIN public.concepts co ON co.id = cl.concept_id
                LEFT JOIN public.users u ON u.id = cl.user_id;
            """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW bi.v_aprobaciones_pendientes AS
                SELECT
                    c.id                  AS closure_id,
                    c.project_id          AS project_id,
                    p.nombre              AS proyecto_nombre,
                    c.paso_actual         AS paso_actual,
                    r.nombre              AS rol_pendiente,
                    EXTRACT(DAY FROM (NOW() - c.updated_at))::int AS dias_pendiente
                FROM public.closures c
                JOIN public.projects p ON p.id = c.project_id
                LEFT JOIN public.approvals a ON a.closure_id = c.id AND a.estado = 'Pendiente'
                LEFT JOIN public.roles r ON r.id = a.role_id
                WHERE c.estado IN ('Borrador', 'EnAprobacion', 'Rechazado');
            """);

            migrationBuilder.Sql("""
                CREATE OR REPLACE VIEW bi.v_audit_resumen AS
                SELECT
                    u.id        AS user_id,
                    u.email     AS email,
                    COUNT(a.id) AS acciones_30dias,
                    MAX(a.timestamp) AS ultima_actividad
                FROM public.users u
                LEFT JOIN public.audit_logs a ON a.user_id = u.id AND a.timestamp > (NOW() - INTERVAL '30 days')
                GROUP BY u.id, u.email;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_audit_resumen;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_aprobaciones_pendientes;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_lineas_por_concepto;");
            migrationBuilder.Sql("DROP VIEW IF EXISTS bi.v_cierres_por_periodo;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS bi;");
        }
    }
}
