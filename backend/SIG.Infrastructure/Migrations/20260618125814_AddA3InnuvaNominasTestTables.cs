using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddA3InnuvaNominasTestTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_a3innuva_companies_test",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_externo = table.Column<string>(type: "text", nullable: false),
                    codigo = table.Column<string>(type: "text", nullable: false),
                    nombre = table.Column<string>(type: "text", nullable: false),
                    nif = table.Column<string>(type: "text", nullable: false),
                    direccion = table.Column<string>(type: "text", nullable: true),
                    ciudad = table.Column<string>(type: "text", nullable: true),
                    pais = table.Column<string>(type: "text", nullable: true),
                    email_contacto = table.Column<string>(type: "text", nullable: true),
                    telefono_contacto = table.Column<string>(type: "text", nullable: true),
                    fecha_ultima_actualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_companies_test", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_payrolls_test",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    id_externo = table.Column<string>(type: "text", nullable: false),
                    id_empleado = table.Column<string>(type: "text", nullable: false),
                    nombre_empleado = table.Column<string>(type: "text", nullable: false),
                    codigo_periodo = table.Column<string>(type: "text", nullable: false),
                    salario_base = table.Column<decimal>(type: "numeric", nullable: false),
                    deducciones = table.Column<decimal>(type: "numeric", nullable: false),
                    salario_neto = table.Column<decimal>(type: "numeric", nullable: false),
                    fecha_procesamiento = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_payrolls_test", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_a3innuva_companies_test");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_payrolls_test");
        }
    }
}
