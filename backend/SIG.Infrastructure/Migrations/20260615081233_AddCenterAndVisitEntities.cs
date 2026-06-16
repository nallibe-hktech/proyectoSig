using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCenterAndVisitEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "centers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    centro_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    direccion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    codigo_postal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    poblacion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    provincia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pais = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    enseña = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_centers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "visits",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    visita_id_externo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: true),
                    service_id = table.Column<int>(type: "integer", nullable: true),
                    center_id = table.Column<int>(type: "integer", nullable: true),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    origen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    notas = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_visits", x => x.id);
                    table.ForeignKey(
                        name: "fk_visits_centers_center_id",
                        column: x => x.center_id,
                        principalTable: "centers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_visits_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_visits_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_centers_centro_id_externo",
                table: "centers",
                column: "centro_id_externo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_visits_center_id",
                table: "visits",
                column: "center_id");

            migrationBuilder.CreateIndex(
                name: "ix_visits_service_id",
                table: "visits",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_visits_user_id",
                table: "visits",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_visits_visita_id_externo",
                table: "visits",
                column: "visita_id_externo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "visits");

            migrationBuilder.DropTable(
                name: "centers");
        }
    }
}
