using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <summary>
    /// Añade columna NIF a staging_pay_hawk_gastos y hace UserId nullable.
    /// PayHawk usa el ExternalId del empleado como NIF/NIE — se almacena directamente
    /// sin conversión numérica para permitir cruce correcto contra el master de empleados.
    /// </summary>
    public partial class AddNIFToPayHawkGasto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Añadir columna NIF (nullable varchar)
            migrationBuilder.AddColumn<string>(
                name: "nif",
                table: "staging_pay_hawk_gastos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // 2. Hacer UserId nullable (era int NOT NULL con valores basura)
            migrationBuilder.AlterColumn<int>(
                name: "user_id",
                table: "staging_pay_hawk_gastos",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: false);

            // 3. Índice para acelerar búsquedas por NIF
            migrationBuilder.CreateIndex(
                name: "ix_staging_pay_hawk_gastos_nif",
                table: "staging_pay_hawk_gastos",
                column: "nif");
        }

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
