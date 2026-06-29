using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SIG.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanupEjemploConceptsAndAddReal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Fix periods with ISO-code nombre ("2026-06") → proper Spanish name ("Junio 2026")
            migrationBuilder.Sql(@"
                UPDATE periods SET nombre = CASE
                    WHEN nombre = '2025-01' THEN 'Enero 2025'
                    WHEN nombre = '2025-02' THEN 'Febrero 2025'
                    WHEN nombre = '2025-03' THEN 'Marzo 2025'
                    WHEN nombre = '2025-04' THEN 'Abril 2025'
                    WHEN nombre = '2025-05' THEN 'Mayo 2025'
                    WHEN nombre = '2025-06' THEN 'Junio 2025'
                    WHEN nombre = '2025-07' THEN 'Julio 2025'
                    WHEN nombre = '2025-08' THEN 'Agosto 2025'
                    WHEN nombre = '2025-09' THEN 'Septiembre 2025'
                    WHEN nombre = '2025-10' THEN 'Octubre 2025'
                    WHEN nombre = '2025-11' THEN 'Noviembre 2025'
                    WHEN nombre = '2025-12' THEN 'Diciembre 2025'
                    WHEN nombre = '2026-01' THEN 'Enero 2026'
                    WHEN nombre = '2026-02' THEN 'Febrero 2026'
                    WHEN nombre = '2026-03' THEN 'Marzo 2026'
                    WHEN nombre = '2026-04' THEN 'Abril 2026'
                    WHEN nombre = '2026-05' THEN 'Mayo 2026'
                    WHEN nombre = '2026-06' THEN 'Junio 2026'
                    WHEN nombre = '2026-07' THEN 'Julio 2026'
                    WHEN nombre = '2026-08' THEN 'Agosto 2026'
                    WHEN nombre = '2026-09' THEN 'Septiembre 2026'
                    WHEN nombre = '2026-10' THEN 'Octubre 2026'
                    WHEN nombre = '2026-11' THEN 'Noviembre 2026'
                    WHEN nombre = '2026-12' THEN 'Diciembre 2026'
                    ELSE nombre
                END
                WHERE nombre ~ '^\d{4}-\d{2}$';
            ");

            // 2. Rename ""Ejemplo — Xxx"" → ""Xxx"" (strip demo prefix, preserve FK refs from closure_lines)
            // ""Ejemplo — "" = 10 chars, SUBSTRING FROM 11 strips it correctly
            migrationBuilder.Sql(@"
                UPDATE concepts
                SET nombre = TRIM(SUBSTRING(nombre FROM 11))
                WHERE nombre LIKE 'Ejemplo — %';
            ");

            // 3. Insert missing real operational concepts (only if not already present)
            migrationBuilder.Sql(@"
                INSERT INTO concepts (nombre, tipo, columna_a3, fecha_desde, formula_json, is_deleted, created_at, updated_at)
                SELECT nombre, tipo, columna_a3, fecha_desde, formula_json::jsonb, false, now(), now()
                FROM (VALUES
                    ('Cuota por visita',                     'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Cuota por hora estimada',              'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Cuota por hora trabajada',             'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Cuota por cantidad de módulos',        'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Cuota fija mensual por Recurso',       'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Cuota o dieta por día trabajado',      'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Dietas (Payhawk)',                     'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Gastos Payhawk',                       'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Incentivos mensuales',                 'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Incentivos trimestrales',              'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Logistica Galán',                      'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Logistica Galán + porcentaje',         'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Logistica MDP',                        'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Logistica MDP + porcentaje',           'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Logistica autónomos',                  'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Salario base dividido entre proyectos','Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Salario fijo',                         'Pago',    'ImporteBruto', DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Cuota por visita (facturación)',        'Factura', NULL,           DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Cuota por visita según tipo',           'Factura', NULL,           DATE '2025-01-01', '{""type"":""Number"",""value"":0}'),
                    ('Gastos proyecto',                       'Factura', NULL,           DATE '2025-01-01', '{""type"":""Number"",""value"":0}')
                ) AS v(nombre, tipo, columna_a3, fecha_desde, formula_json)
                WHERE NOT EXISTS (
                    SELECT 1 FROM concepts c WHERE c.nombre = v.nombre
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM concepts
                WHERE nombre IN (
                    'Cuota por visita', 'Cuota por hora estimada', 'Cuota por hora trabajada',
                    'Cuota por cantidad de módulos', 'Cuota fija mensual por Recurso',
                    'Cuota o dieta por día trabajado', 'Dietas (Payhawk)', 'Gastos Payhawk',
                    'Incentivos mensuales', 'Incentivos trimestrales', 'Logistica Galán',
                    'Logistica Galán + porcentaje', 'Logistica MDP', 'Logistica MDP + porcentaje',
                    'Logistica autónomos', 'Salario base dividido entre proyectos', 'Salario fijo',
                    'Cuota por visita (facturación)', 'Cuota por visita según tipo', 'Gastos proyecto'
                );
            ");
        }
    }
}
