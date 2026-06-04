using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseEmployeeStagingFieldSizes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Aumentar tamaño de campos en staging tables para acomodar datos reales de APIs

            // staging_bizneo_empleados
            migrationBuilder.AlterColumn<string>(
                name: "nif",
                table: "staging_bizneo_empleados",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            // staging_bizneo_horas
            migrationBuilder.AlterColumn<string>(
                name: "registro_id_externo",
                table: "staging_bizneo_horas",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // staging_intratime_fichajes
            migrationBuilder.AlterColumn<string>(
                name: "fichaje_id_externo",
                table: "staging_intratime_fichajes",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            // staging_payhawk_gastos - solo si existe la tabla
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT FROM information_schema.tables
                            WHERE table_name = 'staging_payhawk_gastos'
                        ) THEN
                            ALTER TABLE staging_payhawk_gastos ALTER COLUMN gasto_id_externo TYPE character varying(255);
                        END IF;
                    END $$;
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revertir cambios
            migrationBuilder.AlterColumn<string>(
                name: "nif",
                table: "staging_bizneo_empleados",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "registro_id_externo",
                table: "staging_bizneo_horas",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "fichaje_id_externo",
                table: "staging_intratime_fichajes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            // staging_payhawk_gastos - solo si existe la tabla
            if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                migrationBuilder.Sql(@"
                    DO $$
                    BEGIN
                        IF EXISTS (
                            SELECT FROM information_schema.tables
                            WHERE table_name = 'staging_payhawk_gastos'
                        ) THEN
                            ALTER TABLE staging_payhawk_gastos ALTER COLUMN gasto_id_externo TYPE character varying(100);
                        END IF;
                    END $$;
                ");
            }
        }
    }
}
