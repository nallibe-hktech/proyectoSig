using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignApprovalFlowGrupoFico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) role_id pasa a nullable: el paso Grupo no tiene rol único (rol global + asignación al servicio).
            migrationBuilder.AlterColumn<int>(
                name: "role_id",
                table: "approvals",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            // 2) Remapeo idempotente de los valores enteros del enum ApprovalStep al nuevo flujo
            //    Grupo→FICO→Exportado. Enum viejo: ProjectManager=1, Backoffice=2, Fico=3, Direction=4, SystemExports=5.
            //    Enum nuevo: Grupo=1, Fico=2, SystemExports=3.
            //    Equivalencias: ProjectManager→Grupo, Backoffice→Grupo, Fico→Fico, Direction→Fico, SystemExports→SystemExports.
            //
            //    Las columnas afectadas son: approvals.paso, closures.paso_actual,
            //    approval_history.paso_origen y approval_history.paso_destino (todas int).
            //    El orden de actualización evita pisar valores ya remapeados:
            //      a) 2 → 1  (Backoffice  → Grupo)
            //      b) 4 → 2  (Direction   → Fico)
            //      c) 3 → 2  (Fico        → Fico)   [debe ir DESPUÉS de 4→2; ambos destinan a 2 desde orígenes distintos]
            //      d) 5 → 3  (SystemExports → SystemExports)
            //    (ProjectManager=1 → Grupo=1 no requiere update.)
            migrationBuilder.Sql("""
                -- approvals.paso
                UPDATE approvals SET paso = 1 WHERE paso = 2;
                UPDATE approvals SET paso = 2 WHERE paso = 4;
                UPDATE approvals SET paso = 2 WHERE paso = 3;
                UPDATE approvals SET paso = 3 WHERE paso = 5;

                -- closures.paso_actual
                UPDATE closures SET paso_actual = 1 WHERE paso_actual = 2;
                UPDATE closures SET paso_actual = 2 WHERE paso_actual = 4;
                UPDATE closures SET paso_actual = 2 WHERE paso_actual = 3;
                UPDATE closures SET paso_actual = 3 WHERE paso_actual = 5;

                -- approval_history.paso_origen
                UPDATE approval_history SET paso_origen = 1 WHERE paso_origen = 2;
                UPDATE approval_history SET paso_origen = 2 WHERE paso_origen = 4;
                UPDATE approval_history SET paso_origen = 2 WHERE paso_origen = 3;
                UPDATE approval_history SET paso_origen = 3 WHERE paso_origen = 5;

                -- approval_history.paso_destino
                UPDATE approval_history SET paso_destino = 1 WHERE paso_destino = 2;
                UPDATE approval_history SET paso_destino = 2 WHERE paso_destino = 4;
                UPDATE approval_history SET paso_destino = 2 WHERE paso_destino = 3;
                UPDATE approval_history SET paso_destino = 3 WHERE paso_destino = 5;
                """);

            // 3) Coherencia del FK role_id según el nuevo paso:
            //    - Approvals que ahora son del paso Grupo (1): role_id = NULL (no hay rol único de grupo).
            //    - Approvals que ahora son del paso Fico (2): role_id apunta al rol 'Fico'
            //      (incluye los antiguos Direction, cuyo role_id apuntaba al rol 'Direction').
            migrationBuilder.Sql("""
                UPDATE approvals SET role_id = NULL WHERE paso = 1;
                UPDATE approvals
                   SET role_id = (SELECT id FROM roles WHERE nombre = 'Fico' LIMIT 1)
                 WHERE paso = 2
                   AND EXISTS (SELECT 1 FROM roles WHERE nombre = 'Fico');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "role_id",
                table: "approvals",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
