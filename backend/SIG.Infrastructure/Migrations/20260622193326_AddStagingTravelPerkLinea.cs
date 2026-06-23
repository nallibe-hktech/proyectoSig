using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStagingTravelPerkLinea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "staging_travel_perk_lineas",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    trip_id = table.Column<string>(type: "text", nullable: false),
                    service = table.Column<string>(type: "text", nullable: false),
                    cost_object = table.Column<string>(type: "text", nullable: true),
                    ceco = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<int>(type: "integer", nullable: true),
                    coste_sin_iva = table.Column<decimal>(type: "numeric", nullable: false),
                    fecha_gasto = table.Column<DateOnly>(type: "date", nullable: true),
                    traveler_email = table.Column<string>(type: "text", nullable: true),
                    currency = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    hash = table.Column<string>(type: "text", nullable: false),
                    fecha_ultima_sincronizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    flag_procesado = table.Column<bool>(type: "boolean", nullable: false),
                    error_procesamiento = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staging_travel_perk_lineas", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staging_travel_perk_lineas");
        }
    }
}
