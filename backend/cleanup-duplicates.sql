-- ============================================================
-- CLEANUP SCRIPT: Eliminar duplicados de A3 Innuva
-- ============================================================

-- 1. Eliminar conceptos duplicados (mantener el más reciente)
DELETE FROM staging_a3_innuva_conceptos
WHERE id NOT IN (
    SELECT MAX(id)
    FROM staging_a3_innuva_conceptos
    GROUP BY codigo_concepto, codigo_empleado
);

-- 2. Eliminar empleados duplicados (mantener el más reciente)
DELETE FROM staging_a3_innuva_empleados
WHERE id NOT IN (
    SELECT MAX(id)
    FROM staging_a3_innuva_empleados
    GROUP BY empleado_id_externo
);

-- 3. Verificar resultado
SELECT 'Empleados únicos' as tabla, COUNT(DISTINCT empleado_id_externo) as count
FROM staging_a3_innuva_empleados
UNION ALL
SELECT 'Conceptos únicos', COUNT(DISTINCT CONCAT(codigo_concepto, '_', codigo_empleado))
FROM staging_a3_innuva_conceptos;
