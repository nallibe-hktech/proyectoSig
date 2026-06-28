using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNIFToPayHawkGasto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Añadir columna NIF: PayHawk ExternalId ES el NIF/NIE del empleado
            migrationBuilder.AddColumn<string>(
                name: "nif",
                table: "staging_pay_hawk_gastos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Hacer UserId nullable: ya no se usa como identificador primario
            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "staging_pay_hawk_gastos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: false);

            // Índice para búsquedas por NIF
            migrationBuilder.CreateIndex(
                name: "ix_staging_pay_hawk_gastos_nif",
                table: "staging_pay_hawk_gastos",
                column: "nif");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_staging_pay_hawk_gastos_nif",
                table: "staging_pay_hawk_gastos");

            migrationBuilder.DropColumn(
                name: "nif",
                table: "staging_pay_hawk_gastos");

            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "staging_pay_hawk_gastos",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
