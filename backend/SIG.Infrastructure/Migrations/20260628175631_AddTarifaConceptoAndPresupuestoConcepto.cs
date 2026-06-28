using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTarifaConceptoAndPresupuestoConcepto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "presupuestos_concepto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: true),
                    period_id = table.Column<int>(type: "integer", nullable: true),
                    tipo = table.Column<int>(type: "integer", nullable: false),
                    importe = table.Column<decimal>(type: "numeric", nullable: false),
                    descripcion = table.Column<string>(type: "text", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_presupuestos_concepto", x => x.id);
                    table.ForeignKey(
                        name: "fk_presupuestos_concepto_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_presupuestos_concepto_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_presupuestos_concepto_periods_period_id",
                        column: x => x.period_id,
                        principalTable: "periods",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_presupuestos_concepto_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "tarifas_concepto",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    concept_id = table.Column<int>(type: "integer", nullable: false),
                    client_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: true),
                    valor = table.Column<decimal>(type: "numeric", nullable: false),
                    unidad = table.Column<string>(type: "text", nullable: true),
                    fecha_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    fecha_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tarifas_concepto", x => x.id);
                    table.ForeignKey(
                        name: "fk_tarifas_concepto_clients_client_id",
                        column: x => x.client_id,
                        principalTable: "clients",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_tarifas_concepto_concepts_concept_id",
                        column: x => x.concept_id,
                        principalTable: "concepts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tarifas_concepto_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_presupuestos_concepto_client_id",
                table: "presupuestos_concepto",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_presupuestos_concepto_concept_id",
                table: "presupuestos_concepto",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_presupuestos_concepto_period_id",
                table: "presupuestos_concepto",
                column: "period_id");

            migrationBuilder.CreateIndex(
                name: "ix_presupuestos_concepto_service_id",
                table: "presupuestos_concepto",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_tarifas_concepto_client_id",
                table: "tarifas_concepto",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_tarifas_concepto_concept_id",
                table: "tarifas_concepto",
                column: "concept_id");

            migrationBuilder.CreateIndex(
                name: "ix_tarifas_concepto_service_id",
                table: "tarifas_concepto",
                column: "service_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "presupuestos_concepto");

            migrationBuilder.DropTable(
                name: "tarifas_concepto");
        }
    }
}
