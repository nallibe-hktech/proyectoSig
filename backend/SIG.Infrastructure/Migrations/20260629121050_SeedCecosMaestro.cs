using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedCecosMaestro : Migration
    {
        // Maestro de CECOs (6 cifras) de SIGES, confirmado por el cliente (2026-06-29).
        // Formato: 0 + proyecto(3) + subcuenta(2); 01 = PERS. CAMPO, 02 = OPERACIONES.
        // Versionado para que TODOS los entornos tengan el mismo maestro (git pull + ef database update).
        private const string Valores = @"
            ('010201','COTY PERS. CAMPO'),
            ('010202','COTY OPERACIONES'),
            ('010301','GRANINI PERS. CAMPO'),
            ('010302','GRANINI OPERACIONES'),
            ('010401','SALES FORCE PERS. CAMPO'),
            ('010402','SALES FORCE OPERACIONES'),
            ('010901','SALES TACTICAL PERS. CAMPO'),
            ('010902','SALES TACTICAL OPERACIONES'),
            ('013001','iTUNES PERS. CAMPO'),
            ('013002','iTUNES OPERACIONES'),
            ('013501','SALES OPTIMISING PERS. CAMPO'),
            ('013502','SALES OPTIMISING OPERACIONES'),
            ('013701','SALES AUDIT PERS. CAMPO (AMEX)'),
            ('013702','SALES AUDIT OPERACIONES (AMEX)'),
            ('013801','ITC PERS. CAMPO'),
            ('013802','ITC OPERACIONES'),
            ('013901','LOCKERS PERS. CAMPO'),
            ('013902','LOCKERS OPERACIONES'),
            ('014001','SALES TACTICAL PERS. CAMPO'),
            ('014002','SALES TACTICAL OPERACIONES'),
            ('021301','OTROS PROYECTOS B2C PERS. CAMPO (JTI/PLOOM)'),
            ('021302','OTROS PROYECTOS B2C OPERACIONES (JTI/PLOOM)'),
            ('021501','DYSON PERS. CAMPO'),
            ('021502','DYSON OPERACIONES'),
            ('021601','CHEIL PERS. CAMPO'),
            ('021602','CHEIL OPERACIONES'),
            ('023101','SALES SERVICES PERS. CAMPO (APPLE BA)'),
            ('023102','SALES SERVICES OPERACIONES (APPLE BA)'),
            ('023201','SALES TRAINING PERS. CAMPO (APPLE RST FORMADORES)'),
            ('023202','SALES TRAINING OPERACIONES (APPLE RST FORMADORES)'),
            ('023301','NEW BUSINESS SALES SERVICES PERS. CAMPO'),
            ('023302','NEW BUSINESS SALES SERVICES OPERACIONES'),
            ('0315','DIRECCION'),
            ('0316','COMERCIAL'),
            ('0317','FINANZAS'),
            ('0318','RRHH'),
            ('0319','BI'),
            ('0320','MARKETING'),
            ('0321','COMERCIAL'),
            ('032201','ADMIN TEMPORAL CAMPO (BO)'),
            ('032202','ADMIN TEMPORAL ESTRUCTURA (BO)')";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente: inserta los que falten (por código) y corrige el nombre de los que ya existan,
            // para que la BD converja al maestro sin duplicar (no hay índice único en codigo).
            migrationBuilder.Sql($@"
                INSERT INTO cost_centers (codigo, nombre, is_deleted, created_at, updated_at)
                SELECT v.codigo, v.nombre, false, now(), now()
                FROM (VALUES {Valores}) AS v(codigo, nombre)
                WHERE NOT EXISTS (SELECT 1 FROM cost_centers c WHERE c.codigo = v.codigo);

                UPDATE cost_centers c
                SET nombre = v.nombre, updated_at = now()
                FROM (VALUES {Valores}) AS v(codigo, nombre)
                WHERE c.codigo = v.codigo AND c.nombre <> v.nombre;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: no borramos el maestro al revertir (podría estar referenciado por servicios/cierres).
        }
    }
}
