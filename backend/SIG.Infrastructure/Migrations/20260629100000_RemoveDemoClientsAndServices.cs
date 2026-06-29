using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDemoClientsAndServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Soft-delete los clientes de demo sembrados por DataSeeder.
            // Se usa soft-delete (is_deleted=true) para no romper FKs con DeleteBehavior.Restrict.
            // Los clientes reales (Granini, JDE, Dyson, etc.) se añaden manualmente desde la UI.
            migrationBuilder.Sql(@"
                UPDATE clients
                SET is_deleted = true,
                    deleted_at  = now(),
                    updated_at  = now()
                WHERE nif IN ('A12345678', 'B23456789', 'C34567890')
                  AND is_deleted = false;
            ");

            // Soft-delete los servicios que pertenecen a esos clientes demo.
            migrationBuilder.Sql(@"
                UPDATE services s
                SET is_deleted = true,
                    deleted_at  = now(),
                    updated_at  = now()
                FROM clients c
                WHERE s.client_id = c.id
                  AND c.nif IN ('A12345678', 'B23456789', 'C34567890')
                  AND s.is_deleted = false;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaurar clientes y servicios demo si se revierte la migración
            migrationBuilder.Sql(@"
                UPDATE clients
                SET is_deleted = false,
                    deleted_at  = null,
                    updated_at  = now()
                WHERE nif IN ('A12345678', 'B23456789', 'C34567890');
            ");

            migrationBuilder.Sql(@"
                UPDATE services s
                SET is_deleted = false,
                    deleted_at  = null,
                    updated_at  = now()
                FROM clients c
                WHERE s.client_id = c.id
                  AND c.nif IN ('A12345678', 'B23456789', 'C34567890');
            ");
        }
    }
}
