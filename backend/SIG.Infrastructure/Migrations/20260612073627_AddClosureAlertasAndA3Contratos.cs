using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddClosureAlertasAndA3Contratos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "closure_alertas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    closure_id = table.Column<int>(type: "integer", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    codigo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    detalle = table.Column<string>(type: "jsonb", nullable: true),
                    confirmada = table.Column<bool>(type: "boolean", nullable: false),
                    confirmada_por_user_id = table.Column<int>(type: "integer", nullable: true),
                    fecha_confirmacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_closure_alertas", x => x.id);
                    table.ForeignKey(
                        name: "fk_closure_alertas_closures_closure_id",
                        column: x => x.closure_id,
                        principalTable: "closures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_closure_alertas_users_confirmada_por_user_id",
                        column: x => x.confirmada_por_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "staging_a3innuva_contratos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    contrato_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    fecha_inicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    fecha_fin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    importe_bruto = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_a3innuva_contratos", x => x.id);
                    table.ForeignKey(
                        name: "fk_staging_a3innuva_contratos_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_closure_alertas_closure_id",
                table: "closure_alertas",
                column: "closure_id");

            migrationBuilder.CreateIndex(
                name: "ix_closure_alertas_confirmada_por_user_id",
                table: "closure_alertas",
                column: "confirmada_por_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_staging_a3innuva_contratos_fecha_inicio",
                table: "staging_a3innuva_contratos",
                column: "fecha_inicio");

            migrationBuilder.CreateIndex(
                name: "ix_staging_a3innuva_contratos_hash",
                table: "staging_a3innuva_contratos",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_staging_a3innuva_contratos_nif",
                table: "staging_a3innuva_contratos",
                column: "nif");

            migrationBuilder.CreateIndex(
                name: "ix_staging_a3innuva_contratos_user_id",
                table: "staging_a3innuva_contratos",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "closure_alertas");

            migrationBuilder.DropTable(
                name: "staging_a3innuva_contratos");
        }
    }
}
