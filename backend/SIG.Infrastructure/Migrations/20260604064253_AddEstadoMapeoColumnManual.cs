using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEstadoMapeoColumnManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    ALTER TABLE staging_celero_visitas ADD COLUMN estado_mapeo text;
                EXCEPTION
                    WHEN duplicate_column THEN NULL;
                END $$;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "estado_mapeo",
                table: "staging_celero_visitas");
        }
    }
}
