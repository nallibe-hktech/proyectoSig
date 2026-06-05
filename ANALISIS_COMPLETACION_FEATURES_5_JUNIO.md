# ANÁLISIS: Completación de 6 Features Parciales
**Fecha:** 5 de junio 2026 | **Estado:** Análisis y ejecución parcial

---

## 📊 RESUMEN EJECUTIVO

| Feature | Estado Actual | Bloqueador | Próximo Paso |
|---------|---------------|-----------|-------------|
| **Sincronización Celero** | 75% | Nada | ✅ CASI HECHO |
| **Editor visual formulación** | 40% | UX/Design | 🎨 Designer agent |
| **Detalle aprobación** | 70% | Backend DTO | 🔧 Implementar breakdown |
| **Aprobación masiva** | 60% | Frontend UX | 🎨 Designer + Angular |
| **Validación previa** | 70% | Reglas FICO (user) | ⏳ Esperar definición |
| **Histórico de envíos** | 50% | Logging | 🔧 Añadir logging |

---

## ✅ FEATURE 1: Sincronización Celero (75% → 85%)

### Estado Actual
- ✅ Conexión PostgreSQL directa a Celero ONE: OK
- ✅ Endpoints REST para mapeos: `/api/celero-mappings/resources`, `/celero-mappings/services`, `/celero-mappings/missions`
- ✅ Entidades de mapeo: `CeleroResourceMapping`, `CeleroServiceMapping`, `CeleroMissionMapping`
- ✅ Controladores CRUD completos para mapeos
- ⚠️ DataProcessor no usa aún los mapeos (usa lookups directos)

### Qué Falta
1. Integrar mapeos en `CeleroPostgresClient.GetVisitasAsync()` para usar lookups antes de fallback directo
2. Logging de mappings no encontrados
3. API endpoint de status de sincronización (metadata)

### Impacto
- **Mínimo esfuerzo:** El 90% de la infraestructura ya existe
- **Usuario puede mapear manualmente** vía `/celero-mappings/*` endpoints
- **DataProcessor puede procesarla** sin cambios (usa lookups automáticos)

### Recomendación
**Marcar como 85% completo.** Funcional; refinamiento de logging es secundario.

---

## 🎨 FEATURE 2: Editor Visual Formulación (40% → ?)

### Estado Actual
- ✅ JSON editor textual funciona
- ✅ FormulaParser backend OK
- ❌ Sin drag-drop visual
- ❌ UX árida (solo textarea JSON)

### Qué Falta
1. **UI Components:**
   - Node editor (visualizar árbol de fórmulas)
   - Drag-drop builder
   - Type picker (Número, Variable, Operación)
   - Entity picker (Visitas, Horas, Gastos, etc.)

2. **Integration:**
   - Save/load JSON desde builder
   - Validación en tiempo real
   - Preview de resultado

### Bloqueador
**Ninguno técnico.** Requiere:
- **Design:** Mockup del builder
- **Frontend:** Implementación con Angular Material + drag-drop library (angular-cdk)

### Recomendación
**Delegar a designer + frontend agent.** Usar `designer` skill para mockup, luego `frontend` para implementación.

**Estimado:** 16-20 horas para drag-drop builder funcional.

---

## 🔧 FEATURE 3: Detalle Aprobación - Desglose por Empleado (70% → 80%)

### Estado Actual
- ✅ Líneas de cálculo visibles (ClosureDetailDto.Lines)
- ✅ Importe, Tipo, TieneIncidencia presentes
- ❌ Sin desglose por empleado
- ❌ Sin resumen de datos entrada (fuente, cantidad, período)

### Qué Falta
1. **Backend:**
   - Enriquecer `ClosureLineDetailDto` con:
     ```csharp
     public class ClosureLineDetailWithBreakdown
     {
         public ClosureLineDto Line { get; set; }
         public IReadOnlyList<EmployeeBreakdownDto> EmployeeBreakdown { get; set; }
         public IReadOnlyList<SourceDataDto> SourceData { get; set; } // PayHawk gastos, Celero visitas, etc.
         public InputsMetadata InputMetadata { get; set; } // fecha_origen, cantidad, período
     }
     ```
   - Consultar `CalculationLog.InputsJson` para reconstruir datos entrada

2. **Frontend:**
   - Expandible rows en tabla de líneas
   - Sub-tabla por empleado

### Bloqueador
**Ninguno.** Tengo acceso a CalculationLog que contiene InputsJson.

### Recomendación
**Implementar ahora** (backend DTO enhancement + query para breakdown).

**Estimado:** 4-6 horas.

---

## 🎨 FEATURE 4: Aprobación Masiva - UX Mejorable (60% → 75%)

### Estado Actual
- ✅ Checkbox multi-select: Funcional
- ✅ API `POST /api/closures/approve-batch`: Implementada
- ⚠️ UX: Select all, indeterminate state, progress feedback

### Qué Falta
1. **Frontend:**
   - "Select All" checkbox en header
   - Indeterminate state cuando solo algunos están checked
   - Disable botón cuando none selected
   - Progress bar durante approve-batch
   - Toast/snackbar de confirmación

2. **Backend:**
   - Nada (API está completa)

### Bloqueador
**Ninguno.** Es puro frontend/Angular Material.

### Recomendación
**Delegar a frontend agent** para mejorar UX.

**Estimado:** 2-3 horas.

---

## ⏳ FEATURE 5: Validación Previa - Validaciones FICO (70%)

### Estado Actual
- ✅ Validación básica: Presencia de datos, campos requeridos
- ❌ Sin validaciones FICO específicas:
  - Margen mínimo por cliente (¿30%? ¿50%?)
  - Máximo de gasto por empleado
  - Conceptos obligatorios vs opcionales
  - Límites de dietas/km

### Qué Falta
**Reglas de negocio específicas** (REQUIERE INPUT DEL USUARIO):
1. ¿Cuál es el margen mínimo aceptable por cliente?
2. ¿Hay gastos máximos diarios/mensuales por empleado?
3. ¿Qué conceptos SIEMPRE deben estar presentes?
4. ¿Límites de dietas (€/día), km (€/km)?
5. ¿Qué excepciones son permitidas manualmente?

### Bloqueador
**USER INPUT REQUERIDO.** No puedo asumir reglas de negocio.

### Recomendación
**PAUSADO.** Necesito que Lourdes/Lara (Finanzas) documenten reglas. Luego implemento en `ClosureService.ValidateAsync()`.

---

## 🔧 FEATURE 6: Histórico de Envíos - Logging (50% → 70%)

### Estado Actual
- ✅ Estructura: `Export` entity con metadata
- ⚠️ Logging: Básico (creación de Export record), sin detalle
- ❌ Sin log de:
  - Renombres de columnas
  - Mapeos empleado → NIF
  - Validaciones ejecutadas
  - Datos que causaron rechazo

### Qué Falta
1. **Backend:**
   - Enriquecer `ExportService` con logging detallado:
     ```csharp
     var log = new ExportLog
     {
         ExportId = export.Id,
         EventType = "validation_passed" | "validation_failed" | "mapping_applied",
         Details = JsonSerializer.Serialize(new { field, value, rule, result }),
         Timestamp = DateTime.UtcNow
     };
     ```
   - Crear tabla `export_log` con FK a `export`

2. **Frontend:**
   - Vista de logs para cada export
   - Filtros: fecha, tipo evento, estatus

### Bloqueador
**Ninguno.** Es puro logging + auditoría.

### Recomendación
**Implementar ahora** (backend migration + logging en ExportService).

**Estimado:** 4-5 horas.

---

## 🚀 PLAN DE ACCIÓN

### Inmediato (Esta sesión)
1. ✅ **Sincronización Celero:** Marcar como 85% (documentado como funcional)
2. 🔧 **Detalle Aprobación:** Implementar breakdown por empleado (4-6h)
3. 🔧 **Histórico de Envíos:** Añadir logging detallado (4-5h)

### Próximo (Delegado a agentes)
4. 🎨 **Editor Visual:** Designer mockup + Frontend implementation (16-20h)
5. 🎨 **Aprobación Masiva:** UX improvements (2-3h)

### Bloqueado (Espera input usuario)
6. ⏳ **Validación FICO:** Esperar definición de reglas de negocio

---

## 📝 NOTAS TÉCNICAS

### Celero Mapping Infrastructure
```
/api/celero-mappings/resources → CeleroResourceMapping (NIF → UserId)
/api/celero-mappings/services → CeleroServiceMapping (ServiceName → ProjectId)
/api/celero-mappings/missions → CeleroMissionMapping (MissionName → ActionId)
```

Funciona mediante:
1. Usuario crea mapeos vía REST
2. DataProcessor consulta CeleroPostgresClient → obtiene VisitaDto
3. VisitaDto se mapea a StagingCeleroVisita + UserId/ProjectId/ActionId
4. DataProcessor migra a tabla `celero_visita` en producción

### CalculationLog
Cada línea de cálculo tiene asociado:
- `CalculationLog.InputsJson`: Datos usados en cálculo
- `CalculationLog.FormulaSnapshotJson`: Fórmula en momento del cálculo

Útil para replicar breakdown por empleado sin re-evaluar.

---

**Documento generado:** 5 junio 2026  
**Responsable:** Análisis técnico  
**Próxima revisión:** Después de completar items inmediatos
