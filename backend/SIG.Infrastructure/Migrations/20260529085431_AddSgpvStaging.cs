using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSgpvStaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_sgpv_visitas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visita_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    centro_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    centro_nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    service_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    horas_duracion = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    project_id = table.Column<int>(type: "integer", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_sgpv_visitas", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_staging_sgpv_visitas_hash",
                table: "staging_sgpv_visitas",
                column: "hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_sgpv_visitas");
        }
    }
}
