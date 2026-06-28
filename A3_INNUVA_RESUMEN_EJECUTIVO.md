# A3 Innuva — Resumen Ejecutivo (Referencia Rápida)

**Fecha:** 2026-06-27  
**Basado en:** FLUJO_DATOS_SIG_ES_COMPLETO.md (Secciones 3.1, 4, 9-10)

---

## 🎯 Objetivo
Sincronizar datos de 7 APIs externas + cálculos previos → Nómina descargable en Excel → Envío manual a Wolters Kluwer

---

## 📊 1. FLUJO DE DATOS (5 segundos)

```
7 APIs → Tablas Staging → Motor 2-Pasadas → Excel Descargable → WK (manual)
│
├─ A3 Innuva    → Sueldo base, IRPF, Contratos
├─ PayHawk      → KM, Dietas, Gastos
├─ Bizneo       → Ausencias, Horas perdidas
├─ Intratime    → Fichajes, Horas trabajadas
├─ Celero       → Visitas, KM visitas
├─ TravelPerk   → Viajes (Fase 2)
└─ SGPV         → Datos legacy
```

---

## 🔄 2. ORDEN SINCRONIZACIÓN (Estricto)

```
1️⃣  A3 Innuva (Maestro)       ← PRIMERO
2️⃣  Bizneo (Ausencias)
3️⃣  Intratime (Fichajes)
4️⃣  PayHawk (Gastos)
5️⃣  Celero (Visitas)
6️⃣  TravelPerk (Viajes)
7️⃣  SGPV (Legacy)             ← ÚLTIMO
```

**¿Por qué?** A3 define empleados; Bizneo valida ausencias; Intratime base; PayHawk/Celero dependientes.

---

## 💰 3. CAMPOS FINALES EN NÓMINA

```
NIF   │ Nombre    │ Sueldo │ Visitas │ KM    │ Dietas │ Fee 6.5% │ TOTAL  │ IRPF  │ SS    │ NETO
──────┼───────────┼────────┼─────────┼───────┼────────┼──────────┼────────┼───────┼───────┼──────
12345A│Juan García│1500.00 │  150.00 │  3.45 │ 150.00 │  117.22  │1920.67 │364.93 │121.86 │1433.88
87654B│María López│1800.00 │  200.00 │  0.00 │ 175.00 │  130.03  │2305.03 │437.95 │146.37 │1720.71
```

**Una columna por cada Concepto; suma = Total; Total × IRPF% = Retención; Total − IRPF − SS = Neto.**

---

## 🧮 4. CÁLCULOS (2 Pasadas)

### PASADA 1: Conceptos Base

| # | Concepto | Origen | Cálculo | Ejemplo |
|---|----------|--------|---------|---------|
| 1 | Sueldo Base | A3 Innuva | Directo | 1500.00 |
| 2 | Visitas | Celero | Count × tarifa | 3 × 50 = 150.00 |
| 3 | KM | Celero | (Sum − 300) × 0.23 | (315−300) × 0.23 = 3.45 |
| 4 | Dietas | PayHawk | Sum(Importe, categoria=dieta) | 150.00 |
| 5 | Horas − Ausencias | Intratime − Bizneo | Sum − Sum | 160−8 = 152 (info) |
| | **SUBTOTAL** | | | **1803.45** |

### PASADA 2: Conceptos Fee

| # | Concepto | Origen | Cálculo | Ejemplo |
|---|----------|--------|---------|---------|
| 6 | Fee 6.5% | ConceptRef | Subtotal × 0.065 | 1803.45 × 0.065 = 117.22 |
| | **TOTAL** | | | **1920.67** |

---

## 🔗 5. DATOS QUE NECESITAMOS (No de WK)

| Dato | Sistema | Tabla Staging | Crítico |
|------|---------|---------------|---------|
| **KM recorridos** | Celero | StagingCeleroVisita.km | **SÍ** |
| **Horas trabajadas** | Intratime | StagingIntratimeFichaje.HorasCalculadas | **SÍ** |
| **Horas ausencia** | Bizneo | StagingBizneoAbsencia.Horas | **SÍ** |
| **Dietas** | PayHawk | StagingPayHawkGasto (categoria=dieta) | **SÍ** |
| **Visitas n°** | Celero | StagingCeleroVisita (Count) | **SÍ** |
| Km PayHawk | PayHawk | StagingPayHawkGasto (categoria=km) | NO |
| Taxi/Hotel/Comida | PayHawk | StagingPayHawkGasto | NO |
| Viajes | TravelPerk | StagingTravelPerk | NO (Fase 2) |

---

## ✅ 6. VALIDACIONES CRÍTICAS

```
PRE-CÁLCULO:
✓ Todos empleados A3 tienen NIF válido
✓ Período abierto (no Bloqueado)
✓ Hay conceptos aplicables vigentes

DURANTE CÁLCULO:
✓ No hay división por cero
✓ Variables existen
✓ Filtros usan campos válidos

POST-CÁLCULO:
✓ No hay alertas bloqueantes sin confirmar
✓ Totales cuadran (suma líneas = importe cierre)
✓ IRPF + SS < Total bruto

PRE-EXPORT:
✓ Closure.Estado == Aprobado (paso 5)
✓ Dirección autorizó
✓ Sin duplicados en staging (hash unique)
```

---

## 📋 7. TABLA: Origen de Datos por Concepto

| Concepto | Tipo | Fuente | Tabla Staging | Filtros | Cálculo |
|----------|------|--------|---------------|---------|---------|
| Sueldo Base | Pago | A3 Innuva | StagingA3InnuvaContrato | Período | Directo |
| Visitas tipo 2 | Pago | Celero | StagingCeleroVisita | tipoVisita=2, Período | Count × 50€ |
| Km | Pago | Celero | StagingCeleroVisita | Período | (Sum−300) × 0.23€ |
| Dietas | Pago | PayHawk | StagingPayHawkGasto | categoria=dieta | Sum(Importe) |
| Horas netas | Info | Intratime − Bizneo | StagingIntratimeFichaje − StagingBizneoAbsencia | Período | Sum − Sum |
| Fee 6.5% | Pago | ConceptRef | ClosureLines (Pasada 1) | — | Subtotal × 0.065 |

---

## 🔐 8. Tablas Staging (Estructuras)

```csharp
StagingA3InnuvaContrato
  ├─ EmpleadoId: string (WK)
  ├─ NIF: string (mapeo User)
  ├─ SalarioBase: decimal
  ├─ IrpfPct: decimal
  ├─ FechaInicio/Fin: DateOnly
  ├─ PayloadJson: string (idempotencia)
  ├─ Hash: string (SHA-256, unique)
  └─ FlagProcesado: bool

StagingPayHawkGasto
  ├─ GastoIdExterno: string
  ├─ UserIdMapeo: int (por NIF)
  ├─ Fecha: DateOnly
  ├─ Importe: decimal
  ├─ Categoria: string ← CRÍTICO (dieta, km, taxi, etc.)
  ├─ Hash: string (unique)
  └─ FlagProcesado: bool

StagingBizneoAbsencia
  ├─ RegistroIdExterno: string
  ├─ UserIdMapeo: int
  ├─ Fecha: DateOnly
  ├─ Horas: decimal
  ├─ Tipo: string ← CRÍTICO (baja, permiso, vacaciones)
  ├─ Hash: string (unique)
  └─ FlagProcesado: bool

StagingIntratimeFichaje
  ├─ FichajeIdExterno: string
  ├─ UserIdMapeo: int
  ├─ Entrada: DateTime
  ├─ Salida: DateTime (null = jornada abierta)
  ├─ HorasCalculadas: decimal (o calcular)
  ├─ Hash: string (unique)
  └─ FlagProcesado: bool

StagingCeleroVisita
  ├─ VisitaIdExterno: string
  ├─ ResourceNif: string (mapeo User)
  ├─ Fecha: DateOnly
  ├─ PayloadJsonRaw: string
  │  ├─ tipoVisita: int (1/2/3)
  │  ├─ zona: string (A/B/C)
  │  ├─ km: decimal
  │  ├─ horas: decimal
  │  ├─ estado: string (completada/fallida)
  │  ├─ numeroVisita: int (1ª/2ª/3ª)
  │  └─ extra: object
  ├─ Hash: string (unique)
  └─ FlagProcesado: bool
```

---

## 🎬 9. Flujo Motor Cálculo (Código)

```csharp
// PASO 1: Cargar datos
var filas = CargarStagingFiltroPeriodo(periodo);
var conceptos = ObtenerConceptosAplicables(periodo);

// PASO 2: Evaluación Pasada 1
var importesPasada1 = new Dictionary<int, decimal>();
foreach (var concepto in conceptos.Where(c => !c.DepenDeOtros))
{
    var resultado = EvaluarFórmula(
        concepto.FormulaJson,  // AST JSON
        filas,                 // Datos staging
        periodo
    );
    importesPasada1[concepto.Id] = resultado;
    
    // Crear ClosureLine
    closure.Lines.Add(new ClosureLine 
    { 
        ConceptoId = concepto.Id, 
        Importe = resultado 
    });
}

// PASO 3: Evaluación Pasada 2 (ConceptRef)
foreach (var concepto in conceptos.Where(c => c.DepenDeOtros))
{
    var resultado = EvaluarFórmula(
        concepto.FormulaJson,
        filas,
        periodo,
        importesPasada1  // ← Importes previos disponibles
    );
    
    closure.Lines.Add(new ClosureLine 
    { 
        ConceptoId = concepto.Id, 
        Importe = resultado 
    });
}

// PASO 4: Validaciones & Alertas
GenerarAlertasBloquantes(closure);

// PASO 5: Devolver closure calculado
return closure;
```

---

## 📤 10. Exportación a Wolters Kluwer

```
PRECONDICIÓN: Closure.Estado == Aprobado

DATOS A ENVIAR:
{
  "empresaId": 1,
  "periodoId": 202306,
  "empleados": [
    {
      "nif": "12345678A",
      "nombre": "Juan García",
      "conceptos": {
        "Sueldo": 1500.00,
        "Visitas": 150.00,
        "KM": 3.45,
        "Dietas": 150.00,
        "Fee": 117.22
      }
    },
    ...
  ]
}

ENDPOINT: POST /api/a3innuva/sync/nominas-calculadas
RESPUESTA: 200 OK | 409 Not Approved | 500 Error

AUDITORÍA: AuditLog registra export + timestamp + usuario
```

---

## ⚙️ 11. Conceptos AST (Fórmulas JSON)

```json
// EJEMPLO 1: Número fijo
{ "type": "Number", "value": 1500 }

// EJEMPLO 2: Agregado (contar)
{
  "type": "Aggregate",
  "op": "Count",
  "source": { 
    "type": "Source", 
    "entity": "VisitasCelero",
    "filters": [{ "field": "tipoVisita", "op": "Eq", "value": 2 }]
  }
}

// EJEMPLO 3: Suma con filtro
{
  "type": "Aggregate",
  "op": "Sum",
  "field": "km",
  "source": {
    "type": "Source",
    "entity": "VisitasCelero",
    "filters": []
  }
}

// EJEMPLO 4: Operación binaria (multiplicar)
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": { ... aggregado ... },
  "right": { "type": "Number", "value": 0.23 }
}

// EJEMPLO 5: Modificador (Franquicia)
{
  "type": "Modifier",
  "kind": "Franquicia",
  "threshold": 300,
  "inner": { ... agregado ... }
}

// EJEMPLO 6: Fee sobre conceptos base
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "ConceptRef",
    "conceptIds": []  // vacío = todos
  },
  "right": { "type": "Number", "value": 0.065 }
}

// EJEMPLO 7: Tarifa escalonada
{
  "type": "Tramos",
  "tramos": [
    { "hasta": 1, "precio": 90 },
    { "hasta": null, "precio": 37 }
  ],
  "inner": { ... agregado horas ... }
}
```

---

## 🚨 12. Alertas Críticas

| Código | Descripción | Tipo | Causa |
|--------|-------------|------|-------|
| NOM-001 | Concepto sin datos | Advertencia | Aggregate devuelve 0 |
| NOM-002 | Empleado sin contrato | Bloqueante | FechaInicio > período |
| NOM-003 | División por cero | Bloqueante | Aggregate.Div = 0 |
| NOM-004 | Variable no existe | Bloqueante | VariableId inválido |
| FACT-001 | Sobrefactura presupuesto | Advertencia | Línea > presupuesto |
| FACT-002 | Línea sin empleado | Advertencia | UserId nulo (Pago) |

---

## 📅 13. Estados Aprobación (5 Pasos)

```
1. PendienteValidacion  → Backoffice/FICO confirma alertas bloqueantes
                        ↓
2. PendienteGrupo       → Gestor regional revisa líneas
                        ↓
3. PendienteFICO        → Control financiero valida cálculos
                        ↓
4. PendienteDireccion   → Dirección autoriza
                        ↓
5. Aprobado             → LISTO para exportar a WK

Rechazo en cualquier paso: retorna a "Abierto" para recalcular
```

---

## 🔑 14. Mapeos Clave (NIF → User)

```
StagingA3InnuvaContrato.NIF             → User.NIF (validation)
StagingPayHawkGasto.UserIdMapeo         → User.Id
StagingBizneoAbsencia.UserIdMapeo       → User.Id
StagingIntratimeFichaje.UserIdMapeo     → User.Id
StagingCeleroVisita.ResourceNif         → User.NIF (validation)
```

**Crítico:** Todos los NIFs deben existir en `User` antes de calcular.

---

## 📌 Checklist Pre-Export

- [ ] ¿Closure.Estado == Aprobado (paso 5)?
- [ ] ¿Dirección autorizó (ApprovalHistory)?
- [ ] ¿Sin alertas bloqueantes sin confirmar?
- [ ] ¿Totales cuadran (suma líneas = importe cierre)?
- [ ] ¿IRPF + SS < Total bruto?
- [ ] ¿Todos empleados con NIF válido?
- [ ] ¿Sin duplicados en staging (hash unique respetado)?

---

**Listo para implementar. Basado en FLUJO_DATOS_SIG_ES_COMPLETO.md.**
