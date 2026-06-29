using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCeleroDepartmentIdToDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "celero_department_id",
                table: "departments",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_celero_department_id",
                table: "departments",
                column: "celero_department_id",
                unique: true,
                filter: "\"celero_department_id\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_celero_department_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "celero_department_id",
                table: "departments");
        }
    }
}
