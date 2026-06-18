using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiaPagoToPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ola 2 (#9): día de pago del periodo (30/15/9). Default 30 para filas existentes.
            migrationBuilder.AddColumn<int>(
                name: "dia_pago",
                table: "periods",
                type: "integer",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dia_pago",
                table: "periods");
        }
    }
}
