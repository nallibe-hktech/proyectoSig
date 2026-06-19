using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidenciaOrigenAperturaYHistorial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_apertura",
                table: "cliente_incidencias",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Las incidencias ya existentes no tenían fecha de apertura explícita: usamos su fecha de alta.
            migrationBuilder.Sql("UPDATE cliente_incidencias SET fecha_apertura = created_at;");

            migrationBuilder.AddColumn<string>(
                name: "origen",
                table: "cliente_incidencias",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "incidencia_historiales",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    incidencia_id = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nota = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    responsable = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_incidencia_historiales", x => x.id);
                    table.ForeignKey(
                        name: "fk_incidencia_historiales_cliente_incidencias_incidencia_id",
                        column: x => x.incidencia_id,
                        principalTable: "cliente_incidencias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_incidencia_historiales_incidencia_id",
                table: "incidencia_historiales",
                column: "incidencia_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "incidencia_historiales");

            migrationBuilder.DropColumn(
                name: "fecha_apertura",
                table: "cliente_incidencias");

            migrationBuilder.DropColumn(
                name: "origen",
                table: "cliente_incidencias");
        }
    }
}
