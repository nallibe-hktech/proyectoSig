using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddA3InnuvaContractTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_a3innuva_contract_agreements",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contrato_id_externo = table.Column<string>(type: "text", nullable: false),
                    empleado_id_externo = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    codigo_contrato = table.Column<string>(type: "text", nullable: true),
                    descripcion_contrato = table.Column<string>(type: "text", nullable: true),
                    fecha_inicio_periodo_laboral = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    fecha_fin_periodo_laboral = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    tipo_aportacion_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_aportacion = table.Column<string>(type: "text", nullable: true),
                    modalidad_aportacion = table.Column<string>(type: "text", nullable: true),
                    codigo_ocupacion_cno = table.Column<string>(type: "text", nullable: true),
                    monto_añual_bruto = table.Column<decimal>(type: "numeric", nullable: true),
                    tipo_cobro_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_cobro = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_contract_agreements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_contract_timetables",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    horario_id_externo = table.Column<string>(type: "text", nullable: false),
                    empleado_id_externo = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    tipo_dia_laboral_id = table.Column<string>(type: "text", nullable: true),
                    total_horas_semanal = table.Column<decimal>(type: "numeric", nullable: true),
                    dia_laboral_completo_inicio = table.Column<string>(type: "text", nullable: true),
                    dia_laboral_completo_fin = table.Column<string>(type: "text", nullable: true),
                    tiene_horas_complementarias = table.Column<bool>(type: "boolean", nullable: true),
                    tipo_periodo_partial = table.Column<string>(type: "text", nullable: true),
                    horas_partial = table.Column<decimal>(type: "numeric", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_contract_timetables", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_a3innuva_contract_agreements");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_contract_timetables");
        }
    }
}
