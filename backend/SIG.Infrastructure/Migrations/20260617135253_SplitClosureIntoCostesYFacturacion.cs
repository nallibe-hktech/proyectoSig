using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitClosureIntoCostesYFacturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── PASO 2: añadir columnas FK nullable a las tablas hijas ──
            migrationBuilder.AddColumn<int>(
                name: "cierre_costes_id",
                table: "closure_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_facturacion_id",
                table: "closure_lines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_costes_id",
                table: "closure_alertas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_facturacion_id",
                table: "closure_alertas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_costes_id",
                table: "approvals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_facturacion_id",
                table: "approvals",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_costes_id",
                table: "approval_history",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cierre_facturacion_id",
                table: "approval_history",
                type: "integer",
                nullable: true);

            // ── PASO 1: crear las dos tablas raíz ──
            migrationBuilder.CreateTable(
                name: "cierres_costes",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    paso_actual = table.Column<int>(type: "integer", nullable: false),
                    comentarios = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cierres_costes", x => x.id);
                    table.ForeignKey(
                        name: "fk_cierres_costes_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cierres_costes_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cierres_facturacion",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    period_id = table.Column<int>(type: "integer", nullable: false),
                    total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    paso_actual = table.Column<int>(type: "integer", nullable: false),
                    comentarios = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cierres_facturacion", x => x.id);
                    table.ForeignKey(
                        name: "fk_cierres_facturacion_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cierres_facturacion_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_closure_lines_cierre_costes_id",
                table: "closure_lines",
                column: "cierre_costes_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_lines_cierre_facturacion_id",
                table: "closure_lines",
                column: "cierre_facturacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_alertas_cierre_costes_id",
                table: "closure_alertas",
                column: "cierre_costes_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_alertas_cierre_facturacion_id",
                table: "closure_alertas",
                column: "cierre_facturacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_cierre_costes_id",
                table: "approvals",
                column: "cierre_costes_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_cierre_facturacion_id",
                table: "approvals",
                column: "cierre_facturacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_history_cierre_costes_id",
                table: "approval_history",
                column: "cierre_costes_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_history_cierre_facturacion_id",
                table: "approval_history",
                column: "cierre_facturacion_id");

            migrationBuilder.CreateIndex(
                name: "ix_cierres_costes_period_id",
                table: "cierres_costes",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_cierres_costes_service_id_period_id",
                table: "cierres_costes",
                columns: new[] { "service_id", "period_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cierres_facturacion_period_id",
                table: "cierres_facturacion",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_cierres_facturacion_service_id_period_id",
                table: "cierres_facturacion",
                columns: new[] { "service_id", "period_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_history_cierres_costes_cierre_costes_id",
                table: "approval_history",
                column: "cierre_costes_id",
                principalTable: "cierres_costes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_history_cierres_facturacion_cierre_facturacion_id",
                table: "approval_history",
                column: "cierre_facturacion_id",
                principalTable: "cierres_facturacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_approvals_cierres_costes_cierre_costes_id",
                table: "approvals",
                column: "cierre_costes_id",
                principalTable: "cierres_costes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_approvals_cierres_facturacion_cierre_facturacion_id",
                table: "approvals",
                column: "cierre_facturacion_id",
                principalTable: "cierres_facturacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_closure_alertas_cierres_costes_cierre_costes_id",
                table: "closure_alertas",
                column: "cierre_costes_id",
                principalTable: "cierres_costes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_closure_alertas_cierres_facturacion_cierre_facturacion_id",
                table: "closure_alertas",
                column: "cierre_facturacion_id",
                principalTable: "cierres_facturacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_closure_lines_cierres_costes_cierre_costes_id",
                table: "closure_lines",
                column: "cierre_costes_id",
                principalTable: "cierres_costes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_closure_lines_cierres_facturacion_cierre_facturacion_id",
                table: "closure_lines",
                column: "cierre_facturacion_id",
                principalTable: "cierres_facturacion",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // ── PASO 3: migración de datos idempotente ──
            // Por cada closure existente se crea un cierre de costes (Total = coste_total) y uno de
            // facturación (Total = facturacion_total), conservando estado/paso/audit. Las líneas y los
            // demás hijos se reapuntan al cierre correspondiente. Sólo se ejecuta si la tabla closures existe
            // y aún no se han generado los cierres (idempotencia ante reejecución manual).
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'closures') THEN

        -- 3.0 Relajar closure_id a NULL en approvals: el nuevo Approval de facturación (3.5) se inserta
        --     sin closure_id, y la columna no se elimina hasta el PASO 4. Idempotente.
        ALTER TABLE approvals ALTER COLUMN closure_id DROP NOT NULL;

        -- 3.1 Cierres de COSTES (uno por closure) si no existen ya para (service, period).
        INSERT INTO cierres_costes (service_id, period_id, total, estado, paso_actual, comentarios, fecha_creacion, created_at, updated_at)
        SELECT c.service_id, c.period_id, c.coste_total, c.estado, c.paso_actual, c.comentarios, c.fecha_creacion, c.created_at, c.updated_at
        FROM closures c
        WHERE NOT EXISTS (
            SELECT 1 FROM cierres_costes cc WHERE cc.service_id = c.service_id AND cc.period_id = c.period_id);

        -- 3.2 Cierres de FACTURACIÓN (uno por closure).
        INSERT INTO cierres_facturacion (service_id, period_id, total, estado, paso_actual, comentarios, fecha_creacion, created_at, updated_at)
        SELECT c.service_id, c.period_id, c.facturacion_total, c.estado, c.paso_actual, c.comentarios, c.fecha_creacion, c.created_at, c.updated_at
        FROM closures c
        WHERE NOT EXISTS (
            SELECT 1 FROM cierres_facturacion cf WHERE cf.service_id = c.service_id AND cf.period_id = c.period_id);

        -- 3.3 Reapuntar líneas: Tipo='Pago' -> cierre_costes; resto (Factura) -> cierre_facturacion.
        UPDATE closure_lines l
        SET cierre_costes_id = cc.id
        FROM closures c
        JOIN cierres_costes cc ON cc.service_id = c.service_id AND cc.period_id = c.period_id
        WHERE l.closure_id = c.id AND l.tipo = 'Pago' AND l.cierre_costes_id IS NULL AND l.cierre_facturacion_id IS NULL;

        UPDATE closure_lines l
        SET cierre_facturacion_id = cf.id
        FROM closures c
        JOIN cierres_facturacion cf ON cf.service_id = c.service_id AND cf.period_id = c.period_id
        WHERE l.closure_id = c.id AND l.tipo <> 'Pago' AND l.cierre_costes_id IS NULL AND l.cierre_facturacion_id IS NULL;

        -- 3.4 approvals, approval_history y closure_alertas existentes -> cierre de COSTES correspondiente.
        UPDATE approvals a
        SET cierre_costes_id = cc.id
        FROM closures c
        JOIN cierres_costes cc ON cc.service_id = c.service_id AND cc.period_id = c.period_id
        WHERE a.closure_id = c.id AND a.cierre_costes_id IS NULL AND a.cierre_facturacion_id IS NULL;

        UPDATE approval_history h
        SET cierre_costes_id = cc.id
        FROM closures c
        JOIN cierres_costes cc ON cc.service_id = c.service_id AND cc.period_id = c.period_id
        WHERE h.closure_id = c.id AND h.cierre_costes_id IS NULL AND h.cierre_facturacion_id IS NULL;

        UPDATE closure_alertas al
        SET cierre_costes_id = cc.id
        FROM closures c
        JOIN cierres_costes cc ON cc.service_id = c.service_id AND cc.period_id = c.period_id
        WHERE al.closure_id = c.id AND al.cierre_costes_id IS NULL AND al.cierre_facturacion_id IS NULL;

        -- 3.5 El cierre de FACTURACIÓN arranca con un Approval nuevo pendiente en paso Grupo (=1),
        --     salvo que ya tenga aprobaciones (idempotencia).
        INSERT INTO approvals (cierre_facturacion_id, role_id, paso, estado, created_at, updated_at)
        SELECT cf.id, NULL, 1, 'Pendiente', NOW(), NOW()
        FROM cierres_facturacion cf
        WHERE NOT EXISTS (SELECT 1 FROM approvals a WHERE a.cierre_facturacion_id = cf.id);

    END IF;
END $$;
");

            // ── PASO 4: eliminar la tabla closures y las columnas closure_id de los hijos ──
            // Se usa DROP ... CASCADE para arrastrar de forma robusta los FK/índices dependientes
            // (los nombres pueden variar entre entornos migrados manualmente).
            migrationBuilder.Sql(@"
ALTER TABLE closure_lines    DROP COLUMN IF EXISTS closure_id CASCADE;
ALTER TABLE closure_alertas  DROP COLUMN IF EXISTS closure_id CASCADE;
ALTER TABLE approvals        DROP COLUMN IF EXISTS closure_id CASCADE;
ALTER TABLE approval_history DROP COLUMN IF EXISTS closure_id CASCADE;
DROP TABLE IF EXISTS closures CASCADE;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_approval_history_cierres_costes_cierre_costes_id",
                table: "approval_history");

            migrationBuilder.DropForeignKey(
                name: "fk_approval_history_cierres_facturacion_cierre_facturacion_id",
                table: "approval_history");

            migrationBuilder.DropForeignKey(
                name: "fk_approvals_cierres_costes_cierre_costes_id",
                table: "approvals");

            migrationBuilder.DropForeignKey(
                name: "fk_approvals_cierres_facturacion_cierre_facturacion_id",
                table: "approvals");

            migrationBuilder.DropForeignKey(
                name: "fk_closure_alertas_cierres_costes_cierre_costes_id",
                table: "closure_alertas");

            migrationBuilder.DropForeignKey(
                name: "fk_closure_alertas_cierres_facturacion_cierre_facturacion_id",
                table: "closure_alertas");

            migrationBuilder.DropForeignKey(
                name: "fk_closure_lines_cierres_costes_cierre_costes_id",
                table: "closure_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_closure_lines_cierres_facturacion_cierre_facturacion_id",
                table: "closure_lines");

            migrationBuilder.DropTable(
                name: "cierres_costes");

            migrationBuilder.DropTable(
                name: "cierres_facturacion");

            migrationBuilder.DropIndex(
                name: "ix_closure_lines_cierre_costes_id",
                table: "closure_lines");

            migrationBuilder.DropIndex(
                name: "ix_closure_lines_cierre_facturacion_id",
                table: "closure_lines");

            migrationBuilder.DropIndex(
                name: "ix_closure_alertas_cierre_costes_id",
                table: "closure_alertas");

            migrationBuilder.DropIndex(
                name: "ix_closure_alertas_cierre_facturacion_id",
                table: "closure_alertas");

            migrationBuilder.DropIndex(
                name: "ix_approvals_cierre_costes_id",
                table: "approvals");

            migrationBuilder.DropIndex(
                name: "ix_approvals_cierre_facturacion_id",
                table: "approvals");

            migrationBuilder.DropIndex(
                name: "ix_approval_history_cierre_costes_id",
                table: "approval_history");

            migrationBuilder.DropIndex(
                name: "ix_approval_history_cierre_facturacion_id",
                table: "approval_history");

            migrationBuilder.DropColumn(
                name: "cierre_costes_id",
                table: "closure_lines");

            migrationBuilder.DropColumn(
                name: "cierre_facturacion_id",
                table: "closure_lines");

            migrationBuilder.DropColumn(
                name: "cierre_costes_id",
                table: "closure_alertas");

            migrationBuilder.DropColumn(
                name: "cierre_facturacion_id",
                table: "closure_alertas");

            migrationBuilder.DropColumn(
                name: "cierre_costes_id",
                table: "approvals");

            migrationBuilder.DropColumn(
                name: "cierre_facturacion_id",
                table: "approvals");

            migrationBuilder.DropColumn(
                name: "cierre_costes_id",
                table: "approval_history");

            migrationBuilder.DropColumn(
                name: "cierre_facturacion_id",
                table: "approval_history");

            migrationBuilder.AddColumn<int>(
                name: "closure_id",
                table: "closure_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "closure_id",
                table: "closure_alertas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "closure_id",
                table: "approvals",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "closure_id",
                table: "approval_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "closures",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    period_id = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    comentarios = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    coste_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    facturacion_total = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    margen = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    paso_actual = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_closures", x => x.id);
                    table.ForeignKey(
                        name: "fk_closures_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_closures_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_closure_lines_closure_id",
                table: "closure_lines",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_alertas_closure_id",
                table: "closure_alertas",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_approvals_closure_id",
                table: "approvals",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_approval_history_closure_id",
                table: "approval_history",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_closures_period_id",
                table: "closures",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_closures_service_id_period_id",
                table: "closures",
                columns: new[] { "service_id", "period_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_approval_history_closures_closure_id",
                table: "approval_history",
                column: "closure_id",
                principalTable: "closures",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_approvals_closures_closure_id",
                table: "approvals",
                column: "closure_id",
                principalTable: "closures",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_closure_alertas_closures_closure_id",
                table: "closure_alertas",
                column: "closure_id",
                principalTable: "closures",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_closure_lines_closures_closure_id",
                table: "closure_lines",
                column: "closure_id",
                principalTable: "closures",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
