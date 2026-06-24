using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPartidaPresupuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "margen_objetivo_pct",
                table: "services",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "partidas_presupuesto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    service_id = table.Column<int>(type: "integer", nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    anio = table.Column<int>(type: "integer", nullable: true),
                    presupuesto = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    consumido = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_partidas_presupuesto", x => x.id);
                    table.ForeignKey(
                        name: "fk_partidas_presupuesto_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_partidas_presupuesto_service_id",
                table: "partidas_presupuesto",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "partidas_presupuesto");

            migrationBuilder.DropColumn(
                name: "margen_objetivo_pct",
                table: "services");
        }
    }
}
