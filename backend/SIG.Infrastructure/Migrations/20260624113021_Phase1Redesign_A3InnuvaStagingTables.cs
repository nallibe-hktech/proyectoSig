using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase1Redesign_A3InnuvaStagingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_a3innuva_agreements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    acuerdo_id_externo = table.Column<string>(type: "text", nullable: false),
                    empleado_id_externo = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    nif = table.Column<string>(type: "text", nullable: false),
                    codigo_acuerdo = table.Column<string>(type: "text", nullable: false),
                    nombre_acuerdo = table.Column<string>(type: "text", nullable: false),
                    tipo_acuerdo = table.Column<string>(type: "text", nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_agreements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_bank_accounts",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cuenta_id_externo = table.Column<string>(type: "text", nullable: false),
                    empleado_id_externo = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    nif = table.Column<string>(type: "text", nullable: false),
                    iban = table.Column<string>(type: "text", nullable: false),
                    bic = table.Column<string>(type: "text", nullable: true),
                    nombre_titular = table.Column<string>(type: "text", nullable: true),
                    tipo_cuenta = table.Column<string>(type: "text", nullable: true),
                    es_principal = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_bank_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_irp_fs",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    irpf_id_externo = table.Column<string>(type: "text", nullable: false),
                    empleado_id_externo = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    nif = table.Column<string>(type: "text", nullable: false),
                    tipo_impuesto = table.Column<string>(type: "text", nullable: false),
                    percentaje_tariacion = table.Column<decimal>(type: "numeric", nullable: false),
                    importe_retencion = table.Column<decimal>(type: "numeric", nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_irp_fs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_remunerations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    remuneracion_id_externo = table.Column<string>(type: "text", nullable: false),
                    empleado_id_externo = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    nif = table.Column<string>(type: "text", nullable: false),
                    tipo_remuneracion = table.Column<string>(type: "text", nullable: false),
                    importe = table.Column<decimal>(type: "numeric", nullable: false),
                    concepto = table.Column<string>(type: "text", nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_remunerations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_salaries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    salary_id_externo = table.Column<string>(type: "text", nullable: false),
                    empleado_id_externo = table.Column<string>(type: "text", nullable: false),
                    contrato_id_externo = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    nif = table.Column<string>(type: "text", nullable: false),
                    importe_bruto = table.Column<decimal>(type: "numeric", nullable: false),
                    importe_neto = table.Column<decimal>(type: "numeric", nullable: false),
                    moneda = table.Column<string>(type: "text", nullable: true),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_salaries", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_a3innuva_agreements");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_bank_accounts");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_irp_fs");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_remunerations");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_salaries");
        }
    }
}
