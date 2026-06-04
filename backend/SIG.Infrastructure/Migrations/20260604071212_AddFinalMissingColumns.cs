using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalMissingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_mapeo",
                table: "staging_celero_visitas",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mapeado_por",
                table: "staging_celero_visitas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notas",
                table: "staging_celero_visitas",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "fecha_mapeo",
                table: "staging_celero_visitas");

            migrationBuilder.DropColumn(
                name: "mapeado_por",
                table: "staging_celero_visitas");

            migrationBuilder.DropColumn(
                name: "notas",
                table: "staging_celero_visitas");
        }
    }
}
