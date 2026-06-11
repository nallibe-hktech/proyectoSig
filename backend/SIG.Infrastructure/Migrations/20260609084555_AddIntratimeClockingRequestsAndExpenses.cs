using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIntratimeClockingRequestsAndExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_intratime_clocking_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    request_id_externo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id_externo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_request = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tipo_request = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    estado = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    razon = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    hora_desde = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    hora_hasta = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_intratime_clocking_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_intratime_expenses",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    expense_id_externo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    user_id_externo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_expense = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    cantidad = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    nombre_expense = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    proyecto_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    project_id = table.Column<int>(type: "integer", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_intratime_expenses", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staging_intratime_clocking_requests_hash",
                table: "staging_intratime_clocking_requests",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staging_intratime_expenses_hash",
                table: "staging_intratime_expenses",
                column: "hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_intratime_clocking_requests");

            migrationBuilder.DropTable(
                name: "staging_intratime_expenses");
        }
    }
}
