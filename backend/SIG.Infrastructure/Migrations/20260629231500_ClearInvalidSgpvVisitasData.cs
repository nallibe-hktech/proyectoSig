using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClearInvalidSgpvVisitasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all SGPV visitas that have NULL in the new critical fields
            // These are old records that were synced before the field name fixes
            migrationBuilder.Sql(@"
                DELETE FROM staging_sgpv_visitas
                WHERE cliente IS NULL OR tipo_visita IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This is a data cleanup migration - cannot reverse safely
            // Users will need to re-sync from SGPV if they revert this migration
        }
    }
}
