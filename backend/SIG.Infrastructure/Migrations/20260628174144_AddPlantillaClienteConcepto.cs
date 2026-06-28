using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlantillaClienteConcepto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plantillas_cliente_concepto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    client_id = table.Column<int>(type: "integer", nullable: false),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    formula_json_override = table.Column<string>(type: "text", nullable: true),
                    configuracion_json = table.Column<string>(type: "text", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plantillas_cliente_concepto", x => x.id);
                    table.ForeignKey(
                        name: "fk_plantillas_cliente_concepto_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_plantillas_cliente_concepto_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_plantillas_cliente_concepto_client_id",
                table: "plantillas_cliente_concepto",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_plantillas_cliente_concepto_concept_id",
                table: "plantillas_cliente_concepto",
                column: "concept_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plantillas_cliente_concepto");
        }
    }
}
