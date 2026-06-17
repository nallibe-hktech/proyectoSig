using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContratoIgnorado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ola 2 (#2): marcar contratos de un día "a ignorar" en cierre, con motivo. Aditivo.
            migrationBuilder.AddColumn<bool>(
                name: "ignorado_en_cierre",
                table: "staging_a3innuva_contratos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "motivo_ignorar",
                table: "staging_a3innuva_contratos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ignorado_en_cierre",
                table: "staging_a3innuva_contratos");

            migrationBuilder.DropColumn(
                name: "motivo_ignorar",
                table: "staging_a3innuva_contratos");
        }
    }
}
