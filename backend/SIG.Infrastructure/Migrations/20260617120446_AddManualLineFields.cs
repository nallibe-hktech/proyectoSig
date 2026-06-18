using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddManualLineFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ola 2 (#3a): override manual de importe e incentivos manuales en ClosureLine. Aditivo.
            migrationBuilder.AddColumn<bool>(
                name: "es_manual",
                table: "closure_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "importe_original",
                table: "closure_lines",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "motivo_manual",
                table: "closure_lines",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "es_manual",
                table: "closure_lines");

            migrationBuilder.DropColumn(
                name: "importe_original",
                table: "closure_lines");

            migrationBuilder.DropColumn(
                name: "motivo_manual",
                table: "closure_lines");
        }
    }
}
