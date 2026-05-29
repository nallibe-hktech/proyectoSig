using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConceptProjectAndColumnaA3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "columna_a3",
                table: "concepts",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "project_id",
                table: "concepts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_concepts_project_id",
                table: "concepts",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "fk_concepts_projects_project_id",
                table: "concepts",
                column: "project_id",
                principalTable: "projects",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_concepts_projects_project_id",
                table: "concepts");

            migrationBuilder.DropIndex(
                name: "ix_concepts_project_id",
                table: "concepts");

            migrationBuilder.DropColumn(
                name: "columna_a3",
                table: "concepts");

            migrationBuilder.DropColumn(
                name: "project_id",
                table: "concepts");
        }
    }
}
