using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStagingCeleroVisita : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "punto_montado",
                table: "staging_celero_visitas");

            migrationBuilder.DropColumn(
                name: "tipo_visita",
                table: "staging_celero_visitas");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "project_id",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "action_id",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "mission_name",
                table: "staging_celero_visitas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "resource_nif",
                table: "staging_celero_visitas",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "service_name",
                table: "staging_celero_visitas",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mission_name",
                table: "staging_celero_visitas");

            migrationBuilder.DropColumn(
                name: "resource_nif",
                table: "staging_celero_visitas");

            migrationBuilder.DropColumn(
                name: "service_name",
                table: "staging_celero_visitas");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "project_id",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "action_id",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "punto_montado",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "tipo_visita",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
