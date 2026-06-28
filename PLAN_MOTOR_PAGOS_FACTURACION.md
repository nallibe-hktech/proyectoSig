# PLAN: MOTOR DE CÁLCULO DE PAGOS Y FACTURACIÓN POR PROYECTO

> **Fecha:** 27 junio 2026
> **Alcance:** SOLO motor de cálculo personalizado por proyecto (NO export A3 Innuva)
> **Base:** conceptos x proyecto.csv (43 conceptos) + código existente

---

## 1. MAPA DE CONCEPTOS → NODOS AST

### ✅ PATRONES QUE EL AST ACTUAL YA SOPORTA (18 de 20)

| # | Concepto | Patrón | Nodos AST | Proyectos |
|---|----------|--------|-----------|-----------|
| 1 | **Cuota/dieta por día trabajado** | Count distinct fechas × tarifa | `Aggregate(Count, Distinct="Fecha")` × `Number` | Granini, JDE, Dyson, Amex, Ploom |
| 2 | **Cuota fija mensual** | Importe manual | `Number` | Granini, JDE, Daikin, Apple RST, ITC |
| 3 | **Cuota fija mensual por Recurso** | Importe × Count distinct empleados | `Number` × `Aggregate(Count, Distinct="UserId")` | Granini, JDE |
| 4 | **Cuota por cantidad de módulos** | Sum módulos × tarifa | `Aggregate(Sum, field="PuntoMontado")` × `Number` | Molins |
| 5 | **Cuota por hora estimada** | Horas estimadas (mapeo Celero) × coste/hora | `VariableNode` × `Number` | Dyson, DJI, Kobo, Coty Impl, Cosmetica, Cheil |
| 6 | **Cuota por hora trabajada** | Sum realDuration / 60 × cuota | `Aggregate(Sum, field="Horas")` × `Number` | Morrison |
| 7 | **Cuota por hora - tramos** | Base + tramos de 30min | `TramosNode` | Inpost |
| 8 | **Cuota por visita** | Count visitas "done" × cuota | `Aggregate(Count)` × `Number` | Apple RST, Ploom, Molins |
| 9 | **Cuota por visita - según tipo** | Mapeo respuesta Celero → tarifa | `VariableNode` | Cheil, Coty Impl, Cosmetica |
| 10 | **Dietas PayHawk** | Sum gastos cat="Dietas" approved | `Aggregate(Sum)` con filtros | Amex |
| 11 | **Fee sobre conceptos** | % sobre otros conceptos | `ConceptRefNode` | Granini, JDE, Inpost, Molins, Cosmetica, Kobo, ITC |
| 12 | **Gastos PayHawk** | Sum gastos approved (excl. dietas/km) | `Aggregate(Sum)` con filtros | Granini, JDE, Cosmetica, DJI, ITC |
| 13 | **Gastos proyecto** | Importe manual | `Number` | Apple RST, Amex |
| 14 | **Incentivos mensuales** | Importe manual por empleado | `Number` (con override) | Granini, JDE, Apple BA, ITC |
| 15 | **Incentivos trimestrales** | Importe manual trimestral | `Number` (con override + periodicidad) | Granini, Daikin |
| 16 | **Kilometraje PAGO** | Sum PayHawk mileage approved | `Aggregate(Sum)` con filtros | Granini, JDE, Molins, Cosmetica, ITC |
| 17 | **Kilometraje FACTURACIÓN** | Sum km × tarifa | `Aggregate(Sum, field="Km")` × `Number` | Granini, JDE, Cosmetica, ITC, Molins |
| 18 | **Viajes TravelPerk** | Sum CosteSinIVA | `Aggregate(Sum)` | Granini, JDE |

### ❌ PATRONES QUE NECESITAN EXTENSIÓN (2 de 20)

| # | Concepto | Problema | Solución |
|---|----------|----------|----------|
| 19 | **Salario fijo** | Necesita datos de A3 Innuva como fuente | Añadir `SalariosA3` como SourceNode |
| 20 | **Salario ÷ horas dedicadas** | Requiere cálculo cross-servicio (horas de TODOS los servicios del empleado) | Añadir `CrossServiceAggregateNode` |

---

## 2. MATRIZ DE DATOS REQUERIDOS POR CONCEPTO

| Concepto | Fuente Principal | Campo Clave | Filtros Necesarios |
|----------|-----------------|-------------|-------------------|
| Dieta por día | Celero/SGPV/Intratime/Bizneo | Fecha (distinct) | ServiceId, período, estado="done" |
| Cuota fija | Manual | - | Período (recurrente vs puntual) |
| Cuota por recurso | Celero/SGPV | UserId (distinct) | ServiceId, período |
| Módulos | SGPV PayloadJson | PuntoMontado | ServiceId, período |
| Hora estimada | Celero PayloadJson | idQuestion → horas | ServiceId, período, mapeo respuesta |
| Hora trabajada | Celero RealDuration | Horas | ServiceId, período |
| Hora tramos | Celero RealDuration + PayloadJson | Horas + tipo visita | ServiceId, período, tipo |
| Visita | Celero | Count estado="done" | ServiceId, período |
| Visita por tipo | Celero PayloadJson | idQuestion → tipo | ServiceId, período, mapeo |
| Dietas PayHawk | PayHawk | Categoria="Dietas" | ApprovalStatus="Approved" |
| Fee | Otros conceptos del cierre | ConceptRef | ServiceId, período |
| Gastos PayHawk | PayHawk | Excl. Dietas/Kilometraje | ApprovalStatus="Approved" |
| Gastos proyecto | Manual | - | Período |
| Incentivos | Manual | Por empleado | Período, empleado |
| Kilometraje pago | PayHawk | PaymentType="mileage" | ApprovalStatus="Approved" |
| Kilometraje fact | PayHawk | Km × tarifa | ApprovalStatus="Approved" |
| Salario fijo | A3 Innuva | SueldoMensual | Empleado, período |
| Salario ÷ horas | Celero TODOS los servicios | RealDuration | Empleado, período, todos los servicios |
| Viajes | TravelPerk | CosteSinIVA | ServiceId, período |

---

## 3. GAP ANALYSIS: MOTOR ACTUAL vs REQUERIDO

### ✅ YA IMPLEMENTADO (no hay que tocar)
- AST con 7 tipos de nodo (Number, Variable, Source, Aggregate, BinaryOp, Modifier, Tramos, ConceptRef)
- 7 fuentes de datos (PayHawk, Celero, Bizneo, Intratime, Tarifas, SGPV, TravelPerk)
- Filtros implícitos (período, servicio, usuario) + explícitos (Eq, Neq, Gt, etc.)
- PayloadJson parsing para atributos dinámicos de Celero
- VariableResolver para mapeo idQuestion → valor numérico
- TramosNode para tarifas incrementales
- ConceptRefNode para fee sobre conceptos
- ModifierNode (Min, Max, FloorZero, Franquicia)
- Dos pasadas (base → fee)
- Audit log inmutable
- 697 líneas de tests

### 🔧 NECESITA FIX (bugs en integraciones)
1. **Bizneo NIF = Email** → romper cruces entre sistemas
2. **Bizneo Ausencias.Horas = 0** → absentismo siempre 0
3. **PayHawk sin filtro fecha** → descarga todo cada sync

### ➕ NECESITA AÑADIR (extensiones al motor)
1. **SourceNode "SalariosA3"** → para salario fijo
2. **CrossServiceAggregateNode** → para "salario ÷ horas dedicadas"
3. **IntratimeExpenses** como fuente → si el cliente da permisos
4. **Configuración de conceptos por servicio** → UI/API para definir fórmulas

---

## 4. PLAN DE IMPLEMENTACIÓN

### FASE 0: FIX INTEGRACIONES (datos limpios)

**Objetivo:** Que los datos lleguen correctos al motor.

#### Tarea 0.1: Fix Bizneo NIF mapping
**Archivo:** `HttpClients.cs` (Bizneo sync, ~líneas 17-119)
**Problema:** `email` se guarda como `NIF`
**Solución:** Bizneo NO tiene campo NIF en su API → cruzar por email con `StagingA3InnuvaEmpleado` para obtener NIF real
**Cambio:**
```csharp
// ANTES: NIF = u.Email
// DESPUÉS: Buscar en StagingA3InnuvaEmpleado donde Email == u.Email → obtener NIF
```

#### Tarea 0.2: Fix Bizneo Ausencias.Horas
**Archivo:** `HttpClients.cs` (Bizneo absence sync)
**Problema:** `Horas` hardcoded a 0
**Solución:** Calcular horas = `(end_at - start_at).TotalDays * 8` (jornada laboral)
**Cambio:** Guardar `end_at` en staging y calcular horas

#### Tarea 0.3: Fix PayHawk date filtering
**Archivo:** `HttpClients.cs` (PayHawk client, ~líneas 475-638)
**Problema:** Filtro de fecha comentado
**Solución:** Pasar `createdAfter` y `createdBefore` como query params a la API de PayHawk

---

### FASE 1: EXTENSIONES AL MOTOR AST

**Objetivo:** Soportar los 2 patrones que faltan.

#### Tarea 1.1: Añadir SourceNode "SalariosA3"
**Archivos:**
- `CalculationContext.cs` → añadir `List<StagingA3InnuvaSalary> SalariosA3`
- `CalculationDataLoader.cs` → cargar salarios filtrados por período
- `FormulaNode.cs` → documentar nuevo entity "SalariosA3"
- `CalculationContext.cs` → añadir case en `FilteredRows` switch
- `RowAdapter` → nuevo método `FromSalarioA3`

**RowAdapter para SalariosA3:**
```csharp
public static RowAdapter FromSalarioA3(StagingA3InnuvaSalary s) => new()
{
    UserId = s.UserId,
    Importe = s.ImporteBruto,
    Fecha = s.FechaDesde, // o la fecha del período
};
```

#### Tarea 1.2: Añadir CrossServiceAggregateNode (salario ÷ horas)
**Archivos:**
- `FormulaNode.cs` → nuevo nodo `CrossServiceAggregateNode`
- `CalculationEngine.cs` → evaluador del nuevo nodo
- `CalculationDataLoader.cs` → método para cargar datos de TODOS los servicios

**Diseño del nodo:**
```csharp
public sealed class CrossServiceAggregateNode : FormulaNode
{
    // Qué agregar: "Sum" de "Horas" de "VisitasCelero"
    public string Op { get; set; } = null!;     // Sum, Count
    public string Entity { get; set; } = null!; // VisitasCelero, VisitasSgpv
    public string Field { get; set; } = null!;  // Horas, etc.

    // El cálculo se hace sobre TODOS los servicios del empleado en el período
    // (ignora el ServiceId del target)
}
```

**Evaluación:**
```csharp
// 1. Cargar TODAS las visitas del empleado en el período (todos los servicios)
// 2. Sumar horas totales
// 3. Cargar visitas del servicio específico
// 4. Calcular porcentaje = horas_servicio / horas_totales
// 5. Resultado = salario_base × porcentaje
```

#### Tarea 1.3: Añadir IntratimeExpenses como fuente (si aplica)
**Archivos:**
- `CalculationContext.cs` → añadir `List<StagingIntratimeExpense> GastosIntratime`
- `CalculationDataLoader.cs` → cargar expenses
- `FormulaNode.cs` → documentar entity "GastosIntratime"

---

### FASE 2: CONFIGURACIÓN DE CONCEPTOS POR SERVICIO

**Objetivo:** Poder definir qué conceptos aplican a cada servicio con sus fórmulas.

#### Tarea 2.1: API para gestionar conceptos por servicio
**Archivos:**
- `ConceptsController.cs` → endpoints existentes ya soportan ServiceId
- `ConceptService.cs` → validar fórmulas antes de guardar

**Lo que ya existe:**
- `Concept` tiene `ServiceId?` (null = aplica a todos)
- `Concept` tiene `FormulaJson` (AST serializado)
- `Concept` tiene `Tipo` (Pago/Factura)
- `Concept` tiene `FechaDesde` / `FechaHasta`
- `POST api/concepts/{id}/validar-formula` → valida fórmula

**Lo que falta:**
- UI para crear/editar conceptos (pero eso es frontend, fuera de alcance)
- Ejemplos de fórmulas JSON para cada tipo de concepto

#### Tarea 2.2: Catálogo de fórmulas template
**Archivo nuevo:** `FormulaTemplates.cs`

Generar fórmulas JSON predefinidas para cada tipo de concepto:

```csharp
public static class FormulaTemplates
{
    // Dieta por día trabajado (Celero)
    public static string DietaPorDiaCelero(decimal cuota) => $$"""
    {"type":"BinaryOp","op":"Mul",
      "left":{"type":"Aggregate","op":"Count","source":{"entity":"VisitasCelero","filters":[{"field":"Estado","op":"Eq","value":"done"}]},"distinct":"Fecha"},
      "right":{"type":"Number","value":{{cuota}}}}
    """;

    // Cuota por visita
    public static string CuotaPorVisita(decimal cuota) => $$"""
    {"type":"BinaryOp","op":"Mul",
      "left":{"type":"Aggregate","op":"Count","source":{"entity":"VisitasCelero","filters":[{"field":"Estado","op":"Eq","value":"done"}]}},
      "right":{"type":"Number","value":{{cuota}}}}
    """;

    // Kilometraje facturación
    public static string KilometrajeFacturacion(decimal tarifaKm) => $$"""
    {"type":"BinaryOp","op":"Mul",
      "left":{"type":"Aggregate","op":"Sum","source":{"entity":"GastosPayHawk","filters":[{"field":"Categoria","op":"Eq","value":"kilometraje"},{"field":"ApprovalStatus","op":"Eq","value":"Approved"}]},"field":"Km"},
      "right":{"type":"Number","value":{{tarifaKm}}}}
    """;

    // Fee sobre conceptos
    public static string FeeSobreConceptos(decimal porcentaje, params int[] conceptIds) => $$"""
    {"type":"ConceptRef","conceptIds":[{{string.Join(",", conceptIds)}}],"percentage":{{porcentaje}}}
    """;
}
```

---

### FASE 3: CÁLCULO POR EMPLEADO

**Objetivo:** El motor calcula conceptos POR EMPLEADO, no agregado.

#### Tarea 3.1: Modificar ComputeLinesAsync
**Archivo:** `CierreServices.cs` (~línea 354)

**Cambio principal:**
```csharp
// ANTES (agregado por servicio):
foreach (var concept in aplicables) {
    var result = await _engine.EvaluateAsync(concept, target, recursoId: null, ct);
    lines.Add(new ClosureLine { ConceptId = concept.Id, Importe = result.Resultado });
}

// DESPUÉS (por empleado):
var empleados = await _repo.GetEmpleadosActivosAsync(cierre.ServiceId, cierre.PeriodId, ct);
foreach (var empleado in empleados) {
    foreach (var concept in aplicables) {
        var result = await _engine.EvaluateAsync(concept, target, recursoId: empleado.Id, ct);
        if (result.Resultado != 0m || concept.Tipo == TipoConcepto.Pago) { // incluir aunque sea 0 para pagos
            lines.Add(new ClosureLine {
                ConceptId = concept.Id,
                UserId = empleado.Id,
                Importe = result.Resultado,
                // ...
            });
        }
    }
}
```

#### Tarea 3.2: Obtener lista de empleados activos
**Archivo nuevo:** `IEmpleadoRepository.cs` + implementación

**Criterio:** Empleados que tienen:
- Contrato vigente en el período (StagingA3InnuvaContrato)
- O que han tenido actividad en el servicio (visitas, gastos, fichajes)

---

### FASE 4: LOGÍSTICA (Galán, MDP, con margen)

**Objetivo:** Soportar conceptos de logística con cálculo de margen.

#### Tarea 4.1: Logística Galán
**Fuente:** Archivos Excel de Galán (ya se cargan en `StagingGalanEntrada`, `StagingGalanSalida`, `StagingGalanAlmacenaje`)

**Cálculo:**
1. Sumar conceptos facturables del período (campo "FAC. LOGÍSTICA")
2. Aplicar recargo combustible solo a TRANSPORTE (9% Península / 10% Canarias)
3. Almacenaje = dato del último día hábil del mes
4. Aplicar tarifa pactada por marca

**Fórmula AST:**
```json
// Logística Galán base
{"type":"Aggregate","op":"Sum","source":{"entity":"GalanAlmacenaje","filters":[{"field":"FacLogistica","op":"Eq","value":"Sí"}]}}

// Con margen (ModifierNode Pct)
{"type":"BinaryOp","op":"Pct",
  "left":{"type":"Aggregate","op":"Sum","source":{"entity":"GalanAlmacenaje"}},
  "right":{"type":"Number","value":20}}
```

#### Tarea 4.2: Logística MDP
**Fuente:** Factura mensual de Media Post (importes manuales o Excel)

**Cálculo:** Similar a Galán pero con desglose por bloques (almacenaje, recepción, manipulación, transporte, embalajes)

#### Tarea 4.3: Añadir fuentes Galán al CalculationContext
**Archivos:**
- `CalculationContext.cs` → añadir `List<StagingGalanEntrada>`, `List<StagingGalanSalida>`, `List<StagingGalanAlmacenaje>`
- `CalculationDataLoader.cs` → cargar datos Galán
- `FormulaNode.cs` → documentar entities "GalanEntrada", "GalanSalida", "GalanAlmacenaje"

---

## 5. ORDEN DE PRIORIDADES

| Prioridad | Tarea | Impacto | Esfuerzo | Dependencias |
|-----------|-------|---------|----------|--------------|
| **P0** | 0.1 Fix Bizneo NIF | 🔴 Crítico | Bajo | Ninguna |
| **P0** | 0.2 Fix Bizneo Ausencias | 🔴 Crítico | Bajo | Ninguna |
| **P0** | 0.3 Fix PayHawk fechas | 🔴 Crítico | Bajo | Ninguna |
| **P0** | 3.1 Cálculo por empleado | 🔴 Crítico | Medio | 0.1, 0.2 |
| **P1** | 1.1 SalariosA3 fuente | 🟡 Importante | Bajo | Ninguna |
| **P1** | 1.2 CrossServiceAggregate | 🟡 Importante | Alto | 3.1 |
| **P1** | 2.2 FormulaTemplates | 🟡 Importante | Medio | Ninguna |
| **P2** | 1.3 IntratimeExpenses | 🟢 Mejora | Bajo | Permisos API |
| **P2** | 4.1-4.3 Logística Galán/MDP | 🟢 Mejora | Alto | Datos Galán |

---

## 6. EJEMPLOS DE FÓRMULAS JSON POR CONCEPTO

### Pago: Dieta por día (Granini)
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Count",
    "source": {
      "entity": "VisitasCelero",
      "filters": [{"field": "Estado", "op": "Eq", "value": "done"}]
    },
    "distinct": "Fecha"
  },
  "right": {"type": "Number", "value": 25}
}
```

### Pago: Salario fijo (Granini)
```json
{
  "type": "Aggregate",
  "op": "Sum",
  "source": {"entity": "SalariosA3"},
  "field": "Importe"
}
```

### Pago: Kilometraje (Granini)
```json
{
  "type": "Aggregate",
  "op": "Sum",
  "source": {
    "entity": "GastosPayHawk",
    "filters": [
      {"field": "Categoria", "op": "Eq", "value": "kilometraje"},
      {"field": "ApprovalStatus", "op": "Eq", "value": "Approved"}
    ]
  },
  "field": "Importe"
}
```

### Facturación: Kilometraje (Granini)
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Sum",
    "source": {
      "entity": "GastosPayHawk",
      "filters": [
        {"field": "Categoria", "op": "Eq", "value": "kilometraje"},
        {"field": "ApprovalStatus", "op": "Eq", "value": "Approved"}
      ]
    },
    "field": "Km"
  },
  "right": {"type": "Number", "value": 0.23}
}
```

### Facturación: Fee 6.5% sobre logística + KM (Granini)
```json
{
  "type": "ConceptRef",
  "conceptIds": [/* IDs de conceptos KM y Logística */]
}
```

### Facturación: Cuota por hora estimada (Cosmetica)
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Sum",
    "source": {
      "entity": "VisitasCelero",
      "filters": [{"field": "Estado", "op": "Eq", "value": "done"}]
    },
    "field": "HorasEstimadas"
  },
  "right": {"type": "Number", "value": 11.92}
}
```

### Facturación: Cuota por hora - tramos (Inpost)
```json
{
  "type": "Tramos",
  "cantidad": {
    "type": "Aggregate",
    "op": "Sum",
    "source": {"entity": "VisitasCelero"},
    "field": "MinutosTrabajados"
  },
  "tramos": [
    {"hasta": 74, "precio": 150},
    {"hasta": 104, "precio": 40},
    {"hasta": 134, "precio": 40},
    {"hasta": null, "precio": 40}
  ]
}
```

### Pago: Salario ÷ horas dedicadas (Inpost)
```json
{
  "type": "CrossService",
  "baseSalary": {
    "type": "Aggregate",
    "op": "Sum",
    "source": {"entity": "SalariosA3"},
    "field": "Importe"
  },
  "hoursEntity": "VisitasCelero",
  "hoursField": "Horas"
}
```

---

## 7. ARCHIVOS A MODIFICAR/CREAR

### Fix Integraciones (Fase 0)
| Archivo | Acción |
|---------|--------|
| `HttpClients.cs` | Fix Bizneo NIF, Ausencias.Horas, PayHawk fechas |

### Extensiones Motor (Fase 1)
| Archivo | Acción |
|---------|--------|
| `CalculationContext.cs` | +SalariosA3, +Galan entities |
| `CalculationDataLoader.cs` | Cargar nuevas fuentes |
| `FormulaNode.cs` | +CrossServiceAggregateNode |
| `CalculationEngine.cs` | Evaluador CrossServiceAggregateNode |

### Cálculo por Empleado (Fase 3)
| Archivo | Acción |
|---------|--------|
| `CierreServices.cs` | ComputeLinesAsync por empleado |
| `IEmpleadoRepository.cs` | **NUEVO** - GetEmpleadosActivosAsync |

### Templates (Fase 2)
| Archivo | Acción |
|---------|--------|
| `FormulaTemplates.cs` | **NUEVO** - fórmulas predefinidas |

---

## 8. CRITERIOS DE ACEPTACIÓN

### Fase 0: Integraciones
- [ ] Bizneo NIF coincide con A3 Innuva NIF
- [ ] Bizneo Ausencias.Horas > 0 cuando hay ausencias
- [ ] PayHawk solo descarga gastos del período

### Fase 1: Extensiones AST
- [ ] SourceNode "SalariosA3" funciona en fórmulas
- [ ] CrossServiceAggregateNode calcula correctamente salario ÷ horas
- [ ] Tests nuevos para nodos añadidos

### Fase 2: Templates
- [ ] Fórmulas JSON generadas para los 20 tipos de concepto
- [ ] Cada fórmula valida correctamente con `ValidarFormula`

### Fase 3: Cálculo por Empleado
- [ ] Cada ClosureLine tiene UserId relleno
- [ ] Un empleado con 3 conceptos → 3 ClosureLines
- [ ] Conceptos con resultado 0 no se crean (excepto Pago)

### Fase 4: Logística
- [ ] Galán: coste base + recargo combustible + margen
- [ ] MDP: desglose por bloques + margen
- [ ] Fórmulas de logística generan importes correctos

---

## 9. NOTAS CLAVE

### Lo que el motor YA hace (no reinventar)
1. **Parser JSON → AST**: `FormulaParser.cs`
2. **Evaluador recursivo**: `CalculationEngine.cs` (134 líneas)
3. **Filtros implícitos**: período, servicio, usuario
4. **Filtros explícitos**: Eq, Neq, Gt, Gte, Lt, Lte, In
5. **PayloadJson parsing**: atributos dinámicos de Celero
6. **VariableResolver**: mapeo idQuestion → valor
7. **Tramos**: tarifas incrementales
8. **ConceptRef**: fee sobre otros conceptos (2ª pasada)
9. **Modifiers**: Min, Max, FloorZero, Franquicia
10. **Audit log**: formula snapshot + inputs por evaluación

### Lo que hay que añadir
1. **SalariosA3** como fuente de datos
2. **CrossServiceAggregate** para cálculo cross-servicio
3. **Galán/MDP** como fuentes de datos
4. **Cálculo por empleado** en ComputeLinesAsync
5. **FormulaTemplates** para facilitar configuración

### Lo que NO toca el motor
- UI de configuración de conceptos (frontend)
- Export A3 Innuva (formato Excel)
- Power BI (no MVP)
- Azure AD / SSO (Fase 2)
- A3 ERP integración

---

**FIN DEL PLAN**
