using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStagingSgpvVisitaWithRealApiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "centro_id",
                table: "staging_sgpv_visitas",
                newName: "id_centro");

            migrationBuilder.AlterColumn<string>(
                name: "resource_nif",
                table: "staging_sgpv_visitas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "cliente",
                table: "staging_sgpv_visitas",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "codigo_centro",
                table: "staging_sgpv_visitas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "gpv",
                table: "staging_sgpv_visitas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_cliente",
                table: "staging_sgpv_visitas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "id_gpv",
                table: "staging_sgpv_visitas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tipo_visita",
                table: "staging_sgpv_visitas",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cliente",
                table: "staging_sgpv_visitas");

            migrationBuilder.DropColumn(
                name: "codigo_centro",
                table: "staging_sgpv_visitas");

            migrationBuilder.DropColumn(
                name: "gpv",
                table: "staging_sgpv_visitas");

            migrationBuilder.DropColumn(
                name: "id_cliente",
                table: "staging_sgpv_visitas");

            migrationBuilder.DropColumn(
                name: "id_gpv",
                table: "staging_sgpv_visitas");

            migrationBuilder.DropColumn(
                name: "tipo_visita",
                table: "staging_sgpv_visitas");

            migrationBuilder.RenameColumn(
                name: "id_centro",
                table: "staging_sgpv_visitas",
                newName: "centro_id");

            migrationBuilder.AlterColumn<string>(
                name: "resource_nif",
                table: "staging_sgpv_visitas",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }
    }
}
