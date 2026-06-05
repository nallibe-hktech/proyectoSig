# PLAN DE VALIDACIÓN: CIERRES CON DATOS SINCRONIZADOS
**Fecha:** 5 de junio de 2026 | **Estado:** Listos para validar cálculos

---

## OBJETIVO
Validar que el motor de cálculo funciona correctamente utilizando datos REALES sincronizados de sistemas externos:
- **PayHawk:** 992 gastos (dietas, kilometraje, otros)
- **SGPV:** 997 productos (planificación)
- **Celero:** 20,771 visitas

---

## DATOS SINCRONIZADOS DISPONIBLES

### PayHawk Gastos
| Métrica | Valor | Tabla |
|---------|-------|-------|
| Total registros | 992 | `payhawk_gasto` (producción) |
| Período | Enero-Febrero 2025 | `fecha_gasto` |
| Campos clave | `empleado_id`, `cantidad`, `categoría`, `fecha_gasto` | — |
| Validación | DataProcessor ejecutado con 0 errores | — |

### SGPV Productos
| Métrica | Valor | Tabla |
|---------|-------|-------|
| Total registros | 997 | `sgpv_producto` (producción) |
| Campos clave | `nombre`, `descripción`, `precio`, `codigo` | — |
| Validación | DataProcessor ejecutado con 0 errores | — |

### Celero Visitas
| Métrica | Valor | Tabla |
|---------|-------|-------|
| Total registros | 20,771 | `celero_visita` (producción) |
| Período | Datos históricos variados | `fecha_visita` |
| Campos clave | `empleado_id`, `cliente_id`, `duracion_minutos`, `geolocalización` | — |
| Validación | Conexión PostgreSQL directa OK | — |

---

## VALIDACIONES A EJECUTAR

### 1️⃣ PRUEBA: Crear cierre sin conceptos (baseline)
```
Acción: Crear nuevo Closure para período ficticio sin conceptos
Resultado esperado: Closure creado con estado Borrador, 0 líneas de cálculo
```

### 2️⃣ PRUEBA: Crear cierre con concepto simple (número fijo)
```
Acción: Crear Closure + concepto "Viático fijo = 50€" 
Motor debe: Evaluar concepto, generar ClosureLine con Importe = 50€
Resultado esperado: ClosureLine.Importe = 50; CalculationLog registrado
```

### 3️⃣ PRUEBA: Concepto con variable PayHawk (gastos)
```
Acción: Crear concepto:
{
  "tipo": "variable",
  "valor": null,
  "entidad": "payhawk_gasto",
  "operacion": "suma"
}
Closure aplica a período con gastos PayHawk sincronizados
Motor debe: 
  - Cargar datos de `payhawk_gasto` para empleados activos
  - Sumar todos los montos
  - Generar ClosureLine con resultado
Resultado esperado: ClosureLine.Importe = suma real de gastos sincronizados
```

### 4️⃣ PRUEBA: Concepto con variable Celero (visitas)
```
Acción: Crear concepto:
{
  "tipo": "variable",
  "valor": null,
  "entidad": "celero_visita",
  "operacion": "suma_horas"
}
Motor debe:
  - Cargar datos de `celero_visita` 
  - Convertir minutos a horas (duracion_minutos / 60)
  - Sumar horas
  - Aplicar tarifa (si existe)
Resultado esperado: ClosureLine.Importe = suma horas × tarifa
```

### 5️⃣ PRUEBA: Concepto con operación (variable + factor)
```
Acción: Crear concepto:
{
  "tipo": "operacion",
  "operacion": "multiplicacion",
  "operandos": [
    {"tipo": "variable", "entidad": "celero_visita", "operacion": "suma_horas"},
    {"tipo": "numero", "valor": 25} // 25€ por hora
  ]
}
Motor debe:
  - Sumar horas de Celero
  - Multiplicar por tarifa 25€
Resultado esperado: ClosureLine.Importe = (suma horas) × 25
```

### 6️⃣ PRUEBA: Jerarquía de conceptos (global → acción → empleado)
```
Acción: Crear dos conceptos iguales:
  - C1: scope=global (aplica a todos)
  - C2: scope=acción (aplica solo a acción_id=5)
Closure aplica a empleado que tiene visitas en ambas acciones
Motor debe:
  - Aplicar C1 (global)
  - Aplicar C2 (acción_id=5) si empleado tiene visitas en ella
  - Sumar ambas
Resultado esperado: ClosureLine para C1 + ClosureLine para C2
```

### 7️⃣ PRUEBA: Filtrado por período
```
Acción: Crear Closure para período junio 2025, con datos PayHawk (ene-feb) y Celero (any)
Motor debe:
  - Ignorar datos fuera del rango del período
  - Usar solo datos en `fecha >= periodo.inicio && fecha <= periodo.fin`
Resultado esperado: Líneas de cálculo solo con datos del período
```

### 8️⃣ PRUEBA: Validación datos entrada (InputsJson)
```
Acción: Crear closure y revisar CalculationLog.InputsJson
Motor debe:
  - Grabar qué datos se usaron (empleado, visitas, gastos, etc.)
  - Incluir fuente, cantidad, rango fechas
Resultado esperado: InputsJson contiene metadata de datos origen
```

---

## HERRAMIENTAS DE VALIDACIÓN

### Backend
```csharp
// Script para validar cierre
var closure = await closureService.CreateAsync(req, userId, ct);

// Verificar líneas creadas
var lines = await closureLineRepository.GetByClosureIdAsync(closure.Id, ct);
Assert.NotEmpty(lines);

// Verificar logs de cálculo
var logs = await calculationLogRepository.GetByClosureIdAsync(closure.Id, ct);
Assert.All(logs, log => Assert.NotNull(log.InputsJson));
```

### Frontend (Angular)
```typescript
// Login y navegar a Closures
login(demoEmail, demoPwd);
navigate('/closures/new');

// Crear closure
const newClosure = {
  projectId: 1,
  periodId: 1,
  comentarios: 'Test con datos PayHawk + SGPV'
};
await closureService.create(newClosure);

// Verificar líneas de cálculo renderizadas
const lines = document.querySelectorAll('[data-test="closure-line"]');
assert(lines.length > 0);
```

### Base de datos
```sql
-- Verificar datos sincronizados
SELECT 'PayHawk' as origen, COUNT(*) as total FROM payhawk_gasto
UNION ALL
SELECT 'SGPV', COUNT(*) FROM sgpv_producto
UNION ALL  
SELECT 'Celero', COUNT(*) FROM celero_visita;

-- Verificar cierres creados
SELECT 
  c.id, 
  c.periodo_id, 
  COUNT(cl.id) as num_lineas,
  SUM(cl.importe) as total_importe
FROM cierre c
LEFT JOIN cierre_linea cl ON c.id = cl.cierre_id
GROUP BY c.id, c.periodo_id;

-- Verificar logs de cálculo
SELECT 
  cl.id,
  cl.cierre_linea_id,
  cl.concepto_id,
  cl.resultado,
  SUBSTR(cl.entrada_json, 1, 100) as entrada_preview
FROM calculo_log cl
LIMIT 10;
```

---

## FLUJO DE EJECUCIÓN

1. **Verificar datos sincronizados** (SQL query arriba)
2. **Crear período de prueba** (ej: junio 2025, estatus Abierto)
3. **Crear proyecto de prueba** (ej: "Test PayHawk + SGPV")
4. **Ejecutar pruebas 1-8** en orden progresivo
5. **Registrar resultados**: ✅ Pasó / ❌ Falló / ⚠️ Parcial
6. **Inspeccionar InputsJson** si hay discrepancias
7. **Validar ApprovalFlow** (¿se crean aprobaciones correctamente?)

---

## CASOS ESPECIALES A PROBAR

### Edge Case 1: Empleado sin visitas Celero
- ¿Qué ocurre si aplico concepto con variable Celero a empleado sin visitas?
- Esperado: ClosureLine.Importe = 0 (o null)

### Edge Case 2: Período sin datos
- ¿Qué ocurre si período está completamente vacío de datos?
- Esperado: Cierre se crea, líneas vacías, estado Borrador

### Edge Case 3: Concepto con múltiples variables
```json
{
  "tipo": "operacion",
  "operacion": "suma",
  "operandos": [
    {"tipo": "variable", "entidad": "payhawk_gasto", "operacion": "suma"},
    {"tipo": "variable", "entidad": "celero_visita", "operacion": "suma_horas"},
    {"tipo": "numero", "valor": 100}
  ]
}
```
Esperado: Suma gastos + suma horas + 100

### Edge Case 4: Dates boundaries
- Closure con período: 2025-06-01 a 2025-06-30
- Datos PayHawk: 2025-05-31, 2025-06-01, 2025-06-30, 2025-07-01
- Esperado: Incluir solo 2025-06-01 a 2025-06-30

---

## RESULTADOS ESPERADOS

| Prueba | Esperado | Estado | Notas |
|--------|----------|--------|-------|
| 1: Baseline | Cierre sin líneas | — | Placeholder |
| 2: Número fijo | Importe = 50 | — | Placeholder |
| 3: PayHawk suma | Importe = suma real | — | Validar contra DB |
| 4: Celero horas | Importe = horas × factor | — | Validar conversión |
| 5: Multiplicación | Importe = suma × tarifa | — | Validar operación |
| 6: Jerarquía | 2 líneas, ambas presentes | — | Validar scope |
| 7: Filtro período | Solo datos del período | — | Validar DateRange |
| 8: InputsJson | Metadata completa | — | Validar JSON |

---

## OBSERVACIONES & GOTCHAS

1. **NIF Parsing en PayHawk:** Los IDs son alfanuméricos (ej: "44175805G"). El código extrae solo dígitos. Verificar que `empleado_id` mapea correctamente.

2. **Timezone:** Los datos de PayHawk pueden venir en UTC. Verificar que los filtros de período respetan la zona horaria correcta.

3. **Conversión de unidades:** Celero visitas están en minutos. Asegurarse que la conversión a horas es correcta (divid por 60, no por 24).

4. **Soft-delete:** Si un concepto o período está marcado como soft-deleted, ¿se incluye en los cálculos? (Debería NO).

5. **Performance:** Con 20K visitas Celero, validar que `CalculationDataLoader` no es lento. Si tarda >30s, hay que optimizar con índices.

---

## PRÓXIMOS PASOS DESPUÉS DE VALIDACIÓN

1. Si todas las pruebas pasan → **COMMIT & DOCUMENTAR** en VALIDACION_CIERRES.md
2. Si hay fallos → **DEBUG** usando CalculationLog.InputsJson y logs de BD
3. **Integración Bizneo:** Esperar endpoint de horas detalladas
4. **Integración Intratime:** Esperar token válido
5. **Cierres reales:** Solicitar datos reales SIG al cliente (períodos mayo-julio 2026)

---

**Documento generado:** 5 de junio 2026  
**Responsable:** Dev Team  
**Próxima revisión:** Después de ejecutar pruebas
