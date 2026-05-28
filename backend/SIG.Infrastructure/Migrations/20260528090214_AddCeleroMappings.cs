using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCeleroMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "celero_mission_mappings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    celero_mission_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    action_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_celero_mission_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_celero_mission_mappings_actions_action_id",
                        column: x => x.action_id,
                        principalTable: "actions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "celero_resource_mappings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    celero_nif = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_celero_resource_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_celero_resource_mappings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "celero_service_mappings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    celero_service_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    project_id = table.Column<int>(type: "integer", nullable: false),
                    descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_celero_service_mappings", x => x.id);
                    table.ForeignKey(
                        name: "fk_celero_service_mappings_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_celero_mission_mappings_action_id",
                table: "celero_mission_mappings",
                column: "action_id");

            migrationBuilder.CreateIndex(
                name: "ix_celero_mission_mappings_celero_mission_name",
                table: "celero_mission_mappings",
                column: "celero_mission_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_celero_resource_mappings_celero_nif",
                table: "celero_resource_mappings",
                column: "celero_nif",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_celero_resource_mappings_user_id",
                table: "celero_resource_mappings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_celero_service_mappings_celero_service_name",
                table: "celero_service_mappings",
                column: "celero_service_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_celero_service_mappings_project_id",
                table: "celero_service_mappings",
                column: "project_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "celero_mission_mappings");

            migrationBuilder.DropTable(
                name: "celero_resource_mappings");

            migrationBuilder.DropTable(
                name: "celero_service_mappings");
        }
    }
}
