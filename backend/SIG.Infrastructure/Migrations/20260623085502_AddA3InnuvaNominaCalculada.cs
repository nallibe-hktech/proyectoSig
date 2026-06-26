using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddA3InnuvaNominaCalculada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_a3innuva_conceptos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_externo = table.Column<string>(type: "text", nullable: false),
                    codigo_empleado = table.Column<string>(type: "text", nullable: false),
                    nombre_empleado = table.Column<string>(type: "text", nullable: false),
                    codigo_concepto = table.Column<int>(type: "integer", nullable: false),
                    descripcion_concepto = table.Column<string>(type: "text", nullable: false),
                    tipo_concepto = table.Column<string>(type: "text", nullable: false),
                    importe = table.Column<decimal>(type: "numeric", nullable: false),
                    unidad = table.Column<string>(type: "text", nullable: true),
                    es_manual = table.Column<bool>(type: "boolean", nullable: false),
                    es_en_especie = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_conceptos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_nominas_calculadas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_externo = table.Column<string>(type: "text", nullable: false),
                    codigo_empleado = table.Column<string>(type: "text", nullable: false),
                    nombre_empleado = table.Column<string>(type: "text", nullable: false),
                    codigo_periodo = table.Column<string>(type: "text", nullable: false),
                    fecha_periodo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_percepciones = table.Column<decimal>(type: "numeric", nullable: false),
                    total_descuentos = table.Column<decimal>(type: "numeric", nullable: false),
                    salario_neto = table.Column<decimal>(type: "numeric", nullable: false),
                    fue_enviado_awk = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_envio = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    response_wk = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_nominas_calculadas", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_a3innuva_conceptos");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_nominas_calculadas");
        }
    }
}
