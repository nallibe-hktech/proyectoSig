# Flujo Completo de Datos — SIG-es Plataforma de Cierres Integrales

**Versión:** 2.0 | **Fecha:** 2026-06-24 | **Audiencia:** Desarrolladores, arquitectos, operadores | **Estado:** Producción + Ola 2 Integrada

---

## Índice

1. [Diagrama de Flujo General](#1-diagrama-de-flujo-general)
2. [Fases del Proceso](#2-fases-del-proceso)
3. [Sistemas Externos — Integración por API](#3-sistemas-externos--integración-por-api)
4. [Motor de Cálculo — Conceptos y Fórmulas](#4-motor-de-cálculo--conceptos-y-fórmulas)
5. [Entidades y Modelos](#5-entidades-y-modelos)
6. [Matriz de Dependencias — Conceptos × Fuentes](#6-matriz-de-dependencias--conceptos--fuentes)
7. [Validaciones Críticas](#7-validaciones-críticas)
8. [Checklists por Fase](#8-checklists-por-fase)
9. [Ejemplo Real — Cálculo Completo](#9-ejemplo-real--cálculo-completo)
10. [Contabilidad — Exportación a A3 ERP](#10-contabilidad--exportación-a-a3-erp)

---

## 1. Diagrama de Flujo General

```
FUENTES EXTERNAS
│
├─ Wolters Kluwer A3 INNUVA (OAuth)
│  ├─ Empresas (cliente maestro)
│  ├─ Empleados (contratación)
│  ├─ Salarios base (remuneración)
│  ├─ IRPF / Cotizaciones SS
│  └─ Contratos (vigencia, exclusiones)
│
├─ Intratime (API REST)
│  └─ Fichajes (entrada/salida, horas reales)
│
├─ PayHawk (API REST)
│  └─ Gastos (dietas, kilómetros, tickets)
│
├─ Celero One (PostgreSQL AlloyDB)
│  ├─ Visitas de campo (GPV, tipo, zona, rendimiento)
│  └─ Clientes/Servicios
│
├─ Bizneo (API REST)
│  ├─ Empleados (RRHH maestro)
│  └─ Ausencias (permisos, bajas)
│
├─ TravelPerk (API REST — Fase 2)
│  └─ Viajes corporativos
│
└─ SGPV (Interno)
   └─ Visitas SGPV (formato legacy)
│
│
▼
┌────────────────────────────────────────────────────────────────────┐
│         PHASE 1: SYNC (Sincronización de datos crudos)             │
├────────────────────────────────────────────────────────────────────┤
│ ✓ Descargar datos de cada API                                      │
│ ✓ Aplicar idempotencia (SHA-256 hash del payload)                  │
│ ✓ Persistir en tablas Staging{Sistema}                             │
│ ✓ Marcar procesados / errores                                      │
└────────────────────────────────────────────────────────────────────┘
│
▼
┌────────────────────────────────────────────────────────────────────┐
│       PHASE 2: PREPARE (Preparación de datos para cálculo)         │
├────────────────────────────────────────────────────────────────────┤
│ ✓ Cargar maestros (Servicios, Conceptos, Variables)                │
│ ✓ Mapear datos staging a entidades internas                        │
│ ✓ Validar existencia de empleados, servicios, períodos             │
│ ✓ Crear/recalcular Closures (CierreCostes, CierreFacturacion)      │
└────────────────────────────────────────────────────────────────────┘
│
▼
┌────────────────────────────────────────────────────────────────────┐
│      PHASE 3: CALCULATE (Motor de cálculo — 2 pasadas)             │
├────────────────────────────────────────────────────────────────────┤
│ PASADA 1 — Conceptos Base (Pago / Factura)                         │
│ ├─ Evaluar cada fórmula contra datos staging                       │
│ ├─ Generar ClosureLine con importe calculado                       │
│ └─ Registrar CalculationLog (traza inmutable)                      │
│                                                                    │
│ PASADA 2 — Conceptos Fee (dependientes)                            │
│ ├─ ConceptRef: calcular % sobre suma de líneas previas             │
│ └─ Generar ClosureLine de fee                                      │
│                                                                    │
│ Paralelo: Validación & Alertas                                     │
│ ├─ ClosureAlerta: Bloqueantes y Advertencias                       │
│ └─ Confirmación manual requerida antes de aprobar                  │
└────────────────────────────────────────────────────────────────────┘
│
▼
┌────────────────────────────────────────────────────────────────────┐
│       PHASE 4: OVERRIDE (Ajustes manuales e incentivos)            │
├────────────────────────────────────────────────────────────────────┤
│ ✓ Agregar líneas manuales (override de excepción)                  │
│ ✓ Modificar importes con justificación                             │
│ ✓ Aplicar incentivos según criterios (2ª visita, nocturnidad...)   │
│ ✓ Auditar cambios (usuario, motivo, fecha)                         │
└────────────────────────────────────────────────────────────────────┘
│
▼
┌────────────────────────────────────────────────────────────────────┐
│        PHASE 5: APPROVAL (Flujo de aprobaciones — 5 pasos)         │
├────────────────────────────────────────────────────────────────────┤
│ 1. Cierre.CreacionAutomatica → Pendiente de Validación (Sistema)   │
│ 2. Validación (Backoffice/FICO) → Pendiente de Grupo               │
│ 3. Grupo (Gestor regional) → Pendiente de FICO                     │
│ 4. FICO (Control financiero) → Pendiente de Dirección              │
│ 5. Dirección → APROBADO / RECHAZADO (fin)                          │
│                                                                    │
│ ↻ Rechazo en cualquier paso: retorna a Creación (para recalcular)  │
│ ↻ Alertas bloqueantes: no avanza hasta confirmadas                 │
└────────────────────────────────────────────────────────────────────┘
│
▼
┌────────────────────────────────────────────────────────────────────┐
│          PHASE 6: EXPORT (Exportación a sistemas externos)         │
├────────────────────────────────────────────────────────────────────┤
│ A3 INNUVA (Nóminas)                                                │
│ ├─ Formato: XML/EDI A3                                             │
│ ├─ Contenido: empleado → conceptos (tipos Pago)                    │
│ └─ Endpoint: GET /api/exports/a3-innuva/{closureId}                │
│                                                                    │
│ A3 ERP (Contabilidad)                                              │
│ ├─ Formato: XML/EDI A3                                             │
│ ├─ Contenido: cliente → líneas (tipos Factura)                     │
│ ├─ Asientos contables generados automáticamente                    │
│ └─ Endpoint: GET /api/exports/a3-erp/{closureId}                   │
│                                                                    │
│ Auditoría: AuditLog de cada export con usuario y timestamp         │
└────────────────────────────────────────────────────────────────────┘
│
▼
┌────────────────────────────────────────────────────────────────────┐
│        PHASE 7: REPORTING & ANALYTICS                              │
├────────────────────────────────────────────────────────────────────┤
│ Dashboard: KPIs período activo (cierres, facturación, márgenes)     │
│ Informes Nativos: resultados, forecast vs real, deviaciones        │
│ AuditLog: trazabilidad completa (login, cambios, exports)          │
│ CalculationLog: traza detallada por línea de cierre                 │
└────────────────────────────────────────────────────────────────────┘
```

---

## 2. Fases del Proceso

### FASE 1: SYNC (Sincronización)

**Objetivo:** Traer datos crudos de sistemas externos a tablas staging locales.

**Trigger:**
- Manual: usuario hace clic "Sincronizar"
- Automático: scheduler (configuración pendiente)
- Bajo demanda: desde calculadora de cierres

**Patrón:**
```
API Externa → I{Sistema}Client → SHA-256(payload) → Staging{Sistema}
                                  ↓ (si hash existe)
                            Ignorar (idempotencia)
```

**Idempotencia:**
- Se calcula `SHA-256` del `PayloadJson` completo.
- BD tiene unique constraint `(Hash)` en cada tabla staging.
- Si el hash ya existe, la fila se ignora (no duplicados).

**Tablas Staging Creadas:**

| Tabla | Origen | Campos Clave |
|-------|--------|--------------|
| `StagingA3InnuvaContrato` | Wolters Kluwer | EmpleadoId, FechaInicio, FechaFin, Concepto, Importe |
| `StagingCeleroVisita` | Celero One | VisitaIdExterno, ResourceNif, ServiceId, Fecha, PayloadJson |
| `StagingIntratimeFichaje` | Intratime | FichajeIdExterno, UserId, Entrada, Salida |
| `StagingPayHawkGasto` | PayHawk | GastoIdExterno, UserId, ServiceId, Fecha, Importe, Categoria |
| `StagingBizneoEmpleado` | Bizneo | EmpleadoIdExterno, NIF, Nombre, Departamento |
| `StagingBizneoAbsencia` | Bizneo | RegistroIdExterno, UserId, ServiceId, Fecha, Horas |
| `StagingTravelPerk` | TravelPerk | ViajeIdExterno, UserId, Importe, Fecha (Fase 2) |
| `StagingSgpv` | SGPV | VisitaIdExterno, UserNif, ServiceCode, Fecha, PayloadJson |

**Error Handling:**
- Conexión fallida → log + email + dashboard alert
- Payload inválido → marcar fila como `FlagProcesado = false` + guardar error en `PayloadErrorJson`
- Timeout 30s → reintentar (Fase 2: Polly retry policy)

---

### FASE 2: PREPARE (Preparación)

**Objetivo:** Transformar datos staging en entidades de cálculo validadas.

**Pasos:**

1. **Cargar maestros en memoria:**
   - `Service`, `Concept`, `Variable`, `Period`, `User`, `Client`
   - Índices: ServiceId, ConceptId, VariableId

2. **Mapear staging a objetos de cálculo (`RowAdapter`):**
   ```csharp
   // Ejemplo: Visita Celero → RowAdapter
   var row = new RowAdapter {
       UserId = visita.ResourceNif → User.Id,
       ServiceId = visita.ServiceId,
       Fecha = visita.Fecha,
       //Campos normales
       Visitas = 1,
       Km = visita.PayloadJson["km"],
       // Extra (filtrable)
       Extra = new Dict { 
           { "tipoVisita", visita.PayloadJson["tipoVisita"] },
           { "zona", visita.PayloadJson["zona"] },
           ...
       }
   };
   ```

3. **Validaciones pre-cálculo:**
   - ¿Existe `User` por NIF?
   - ¿Existe `Service` por código externo?
   - ¿Está vigente el `Period` (abierto, no bloqueado)?
   - ¿Hay conceptos aplicables (tipo Pago/Factura, vigentes, del servicio)?

4. **Crear o recalcular Closure:**
   ```csharp
   var closure = new CierreCostes {
       ServiceId = serviceId,
       PeriodId = periodId,
       Estado = EstadoCierre.Abierto,
       CreatedAt = DateTime.UtcNow
   };
   await _closureService.CreateAsync(closure);
   ```

---

### FASE 3: CALCULATE (Motor de cálculo)

**Concepto:** Cada concepto (`Concept`) tiene una **fórmula** guardada como JSON AST (Abstract Syntax Tree). El motor recorre el árbol, evalúa cada nodo y devuelve un número.

#### 3.1 Estructura de Nodos (Primitivas)

| Nodo | Parámetros | Salida | Ejemplo |
|------|-----------|--------|---------|
| **Number** | `value: decimal` | Número fijo | 1500 (cuota fija mensual) |
| **Variable** | `variableId: int` | Valor de variable | TarifaHora = 18.5 |
| **Source** | `entity, filters[]` | (Dentro de Aggregate) | VisitasCelero, filtro zona="A" |
| **Aggregate** | `op: Count/Sum/Min/Max, field?, distinct?` | Número (conteo/suma) | Count(VisitasCelero) = 3 visitas |
| **BinaryOp** | `op: +/−/×/÷/%, left, right` | Número | Count × Tarifa = 3 × 5 = 15 |
| **Modifier** | `kind: Min/Max/FloorZero/Franquicia, threshold, inner` | Número con tope/piso | Min[250](100) = 250 |
| **Tramos** | `tramos: [{hasta, precio}], inner` | Número (tarifa escalonada) | 1ª hora 90€, 2ª+ 37€ |
| **ConceptRef** | `conceptIds?: int[]` | Suma de conceptos previos (fee) | Fee 10% sobre base |

#### 3.2 Evaluación de una Fórmula (Ejemplo Real)

**Concepto:** "Visitas × 5€ por visita"

Fórmula JSON:
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Count",
    "source": {
      "type": "Source",
      "entity": "VisitasCelero",
      "filters": []
    }
  },
  "right": {
    "type": "Number",
    "value": 5
  }
}
```

Recorrido del motor:
```
1. Evaluar nodo raíz (BinaryOp Mul)
   ├─ Evaluar left (Aggregate Count)
   │  └─ Cargar VisitasCelero del staging (filtros: período, servicio, usuario)
   │     → 3 visitas encontradas
   │     → Devuelve 3
   └─ Evaluar right (Number)
      → Devuelve 5

2. Aplicar operación
   → 3 × 5 = 15

3. Redondear a 2 decimales → 15.00

4. Guardar CalculationLog
   {
     "ClosureLineId": 102,
     "FormulaSnapshotJson": {...},
     "InputsJson": { "nVisitas": 3, "período": "2026-06" },
     "Resultado": 15.00,
     "SistemaOrigen": "Celero",
     "Incidencias": []
   }
```

#### 3.3 Filtros Implícitos y Explícitos

**Implícitos (siempre se aplican):**
- **Período:** solo registros dentro del rango `Period.FechaInicio` a `Period.FechaFin`
- **Servicio:** solo registros del `ServiceId` del cierre
- **Recurso:** si se calcula por usuario, solo sus registros

**Explícitos (defines en el Source):**
```json
"filters": [
  { "field": "tipoVisita", "op": "Eq", "value": 2 },
  { "field": "zona", "op": "In", "value": ["A", "B"] }
]
```

Si tras los filtros no quedan filas → se devuelve **0** + incidencia `EmptyDataset` (aviso, no error).

#### 3.4 Dos Pasadas de Cálculo

**PASADA 1 — Conceptos Base:**
```csharp
var conceptosBase = concepts.Where(c => !c.DepenDeOtros).ToList();
foreach (var concepto in conceptosBase)
{
    var resultado = EvaluarFórmula(concepto.FormulaJson, staging, rows);
    var line = new ClosureLine { ConceptoId = concepto.Id, Importe = resultado };
    closure.Lines.Add(line);
}
```

**PASADA 2 — Conceptos Fee (ConceptRef):**
```csharp
var conceptosFee = concepts.Where(c => c.DepenDeOtros).ToList();
foreach (var concepto in conceptosFee)
{
    // IMPORTANTE: now ConceptRef nodes can access CalculationTarget.ImportesPrevios
    // que contiene { conceptoId → importe } de la pasada 1
    var resultado = EvaluarFórmula(concepto.FormulaJson, staging, rows, importesPrevios);
    var line = new ClosureLine { ConceptoId = concepto.Id, Importe = resultado };
    closure.Lines.Add(line);
}
```

#### 3.5 Validaciones y Alertas

Mientras se calcula, se generan **ClosureAlerta**:

```csharp
public class ClosureAlerta
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public string Codigo { get; set; }        // "A001", "F002", etc.
    public string Descripcion { get; set; }   // "Empleado sin fichaje", "Sobrefactura"
    public TipoAlerta Tipo { get; set; }      // Bloqueante, Advertencia
    public bool Confirmada { get; set; }      // false → detiene el cierre
    public int? UsuarioConfirmacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
}

public enum TipoAlerta { Bloqueante, Advertencia }
```

**Ejemplos de Alertas:**

| Código | Descripción | Tipo | Causa |
|--------|-------------|------|-------|
| `NOM-001` | Concepto sin datos en período | Advertencia | Aggregate devuelve 0 registros |
| `NOM-002` | Empleado sin contrato vigente | Bloqueante | FechaInicio > período |
| `NOM-003` | División por cero en fórmula | Bloqueante | Aggregate.op=Div y denominador=0 |
| `FACT-001` | Facturación superior a presupuesto | Advertencia | Línea.Importe > Presupuesto.Restante |
| `FACT-002` | Coste de recurso sin asignación | Advertencia | ClosureLine.UserId nulo |

---

### FASE 4: OVERRIDE (Ajustes Manuales e Incentivos)

**Contexto:** Los cálculos automáticos no cubren todas las excepciones (fallidas, 2ª visita, nocturnidad, adelantos, embargos). Se usan **override/incentivos** para estos casos.

**Mecanismo:**

1. Usuario abre el detalle de cierre.
2. Selecciona una línea o crea una nueva.
3. Abre dialogo "Override / Incentivo".
4. Rellena:
   - **Tipo:** Excepción / Incentivo / Ajuste manual
   - **Importe:** nuevo valor (o delta)
   - **Motivo:** texto libre (auditable)
   - **Confirmar:** checkbox "He revisado esta línea"

5. Se crea **nueva ClosureLine**:
   ```csharp
   var override = new ClosureLine {
       ClosureId = closure.Id,
       ConceptoId = null,  // NO viene de concepto
       Importe = importeManual,
       EsOverride = true,
       UsuarioOverride = usuarioActual.Id,
       MotivoOverride = motivo,
       CreatedAt = DateTime.UtcNow
   };
   closure.Lines.Add(override);
   ```

6. Se registra en **AuditLog**:
   ```json
   {
     "Entidad": "ClosureLine",
     "Accion": "Create",
     "ClosureId": 123,
     "UserId": 5,
     "Timestamp": "2026-06-24T10:15:00Z",
     "Cambios": {
       "Importe": { "antes": null, "ahora": 250 },
       "MotivoOverride": { "ahora": "2ª visita — tarifa reducida 50%" }
     }
   }
   ```

**Tipos de Override Comunes:**

| Tipo | Motivo | Ejemplo | Importe |
|------|--------|---------|---------|
| Fallida | Visita no se completó | Cierre accidentalmente | 50% del cálculo |
| 2ª visita | 2ª o 3ª llamada al mismo cliente | Tarifa escalonada | 50% de la 1ª visita |
| Nocturnidad | Más allá de horario laboral | Multiplicador +50% | +50% base |
| Pernocta | Pernoctación requerida | Adicional fijo | 50€ fijos |
| Adelanto | Anticipo de sueldo | Deducción | -500€ (negativo) |
| Embargo | Embargo judicial | Deducción | -cantidad |

---

### FASE 5: APPROVAL (Flujo de Aprobaciones)

**Estados del Cierre:**

```
┌─────────────────────────────────────────────────────────────────┐
│                    CIERRE ABIERTO                                │
│         (Automático tras cálculo + override)                     │
│         Pendiente de Validación                                  │
└─────────────────────────────────────────────────────────────────┘
   │ (Validación: revisar alertas)
   ▼
┌─────────────────────────────────────────────────────────────────┐
│                  PENDIENTE GRUPO                                 │
│    (Backoffice/FICO confirma alertas bloqueantes)                │
│    Pendiente de revisión regional                                │
└─────────────────────────────────────────────────────────────────┘
   │ (Grupo regional revisa)
   ▼
┌─────────────────────────────────────────────────────────────────┐
│                   PENDIENTE FICO                                 │
│        (Gestor regional aprueba o rechaza)                       │
│        Pendiente de control financiero                           │
└─────────────────────────────────────────────────────────────────┘
   │ (FICO revisa cálculos)
   ▼
┌─────────────────────────────────────────────────────────────────┐
│               PENDIENTE DIRECCIÓN                                │
│          (FICO aprueba o rechaza)                                │
│          Pendiente de autorización final                         │
└─────────────────────────────────────────────────────────────────┘
   │ (Dirección autoriza o rechaza)
   ▼
┌─────────────────────────────────────────────────────────────────┐
│                      APROBADO                                    │
│         (Listo para exportar a A3 / Contabilidad)                │
│         Inmutable — auditable                                    │
└─────────────────────────────────────────────────────────────────┘
    ↳ Export a A3 INNUVA (Nóminas)
    ↳ Export a A3 ERP (Asientos contables)
```

**Flujo Detallado:**

| Paso | Rol Responsable | Acción | Siguiente Estado |
|------|-----------------|--------|------------------|
| 1 | Sistema | Crear cierre automático | PendienteValidacion |
| 2 | Backoffice/FICO | Revisar, confirmar alertas bloqueantes | PendienteGrupo |
| 3 | Grupo (Gestor regional) | Revisar líneas, validar servicios | PendienteFICO |
| 4 | FICO (Control financiero) | Revisar cálculos, márgenes | PendienteDireccion |
| 5 | Dirección | Autorizar o rechazar | APROBADO / RECHAZADO |

**Rechazo:**
- En cualquier paso se puede rechazar.
- Cierre retorna a estado **Abierto** para recalcular.
- Se registra motivo del rechazo en `ApprovalHistory`.

**Historial:**
```csharp
public class ApprovalHistory
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public int UserId { get; set; }
    public EstadoCierre EstadoAnterior { get; set; }
    public EstadoCierre EstadoNuevo { get; set; }
    public string? Motivo { get; set; }
    public DateTime FechaTransicion { get; set; }
}
```

---

### FASE 6: EXPORT (Exportación a Sistemas Externos)

**Endpoint A3 INNUVA (Nóminas):**

```
GET /api/exports/a3-innuva/{closureId}
→ 200 OK (attachment; filename="Nominas_2026-06_aprobado.xml")
→ 409 ClosureNotApprovedException (si no está aprobado)
```

**Flujo:**

1. Verificar `Closure.Estado == Aprobado`.
2. Cargar líneas donde `TipoConcepto == Pago`.
3. Agrupar por `UserId` (empleado).
4. Para cada empleado:
   ```
   Empleado
   ├─ NIF
   ├─ Nombre
   └─ Conceptos
       ├─ [Concepto 1: tipo Pago, importe 1500]
       ├─ [Concepto 2: tipo Pago, importe 250]
       └─ Total: 1750
   ```
5. Generar XML (formato A3 pendiente de confirmación contractual).
6. Grabar `AuditLog` con `Accion=Export`.
7. Devolver `FileContentResult`.

**Endpoint A3 ERP (Contabilidad):**

```
GET /api/exports/a3-erp/{closureId}
→ 200 OK (attachment; filename="Facturas_2026-06_aprobado.xml")
→ 409 ClosureNotApprovedException
```

**Flujo:**

1. Verificar estado aprobado.
2. Cargar líneas donde `TipoConcepto == Factura`.
3. Agrupar por `ClientId`.
4. Para cada cliente:
   ```
   Factura
   ├─ Cliente (NIF, Nombre)
   ├─ Lineas
   │  ├─ [Concepto 1: importe 5000]
   │  ├─ [Concepto 2: importe 1200]
   │  └─ Total: 6200
   └─ CentroCosto (del servicio)
   ```
5. Asientos contables generados:
   ```
   Débito: Gasto (Concepto) — Crédito: CxP Proveedor
   Débito: Banco/Caja — Crédito: CxC Cliente
   ```
6. Auditoría + descarga.

---

### FASE 7: REPORTING & ANALYTICS

**Dashboard:**
- KPIs período activo (facturación/coste/margen reales vs objetivo)
- Cierres completados (Costes/Facturación)
- Alertas activas (bloqueantes + avisos)
- Mis Servicios filtrados por ownership

**Informes Nativos:**
- **Resultado:** Drill-down Dpto → Cliente → Servicio (Facturación/Coste/Margen)
- **Prevención vs Real:** Forecast vs Cierres por mes/dpto

**AuditLog Completo:**
- Logins / Logouts
- Cambios en entidades maestras
- Cálculos de cierre
- Exports
- Confirmación de alertas

**CalculationLog:**
- Traza inmutable por línea de cierre
- Fórmula evaluada, inputs, resultado, sistema origen, incidencias

---

## 3. Sistemas Externos — Integración por API

### 3.1 Wolters Kluwer A3 INNUVA

| Atributo | Valor |
|----------|-------|
| **Tipo Conexión** | OAuth 2.0 + REST HTTP |
| **BaseUri** | `https://api.wolterskluwer.es/a3innuva/v1` |
| **Autenticación** | ClientId + ClientSecret (configuración local) |
| **Dirección** | Lectura + Escritura (nóminas calculadas) |
| **Periodicidad** | Manual / Scheduled (1x/mes antes de cierre) |

**Datos Consumidos:**

| Entidad | Tabla Staging | Descripción |
|---------|---------------|-------------|
| Empresas | — (sincronización directa a `Client`) | Clientes contratantes (Alpha Foods, Beta Cosmetics, etc.) |
| Empleados | — (sincronización a `User`) | Maestro de empleados (NIF, nombre, departamento) |
| Contratos | `StagingA3InnuvaContrato` | Período vigencia, salario base, concepto contratado |
| Salarios | — (incorporados en contrato) | Remuneración base mensual |
| IRPF/SS | — (campos en contrato) | Porcentajes de retención |

**Interface:**

```csharp
public interface IA3InnuvaClient
{
    // Lectura (PHASE 1: SYNC)
    Task<IReadOnlyList<A3InnuvaContratoBrutoDto>> GetContratosAsync(int empresaId, DateOnly periodo, CancellationToken ct);
    Task<A3InnuvaEmpresaDto> GetEmpresaAsync(int empresaId, CancellationToken ct);
    
    // Escritura (PHASE 6: EXPORT)
    Task<HttpResponseMessage> PostNominasCalculadasAsync(
        int empresaId, 
        int periodoId,
        A3InnuvaNominasCalculadasRequest request, 
        CancellationToken ct);
}

public record A3InnuvaContratoBrutoDto(
    string EmpleadoId,
    string NIF,
    string Nombre,
    decimal SalarioBase,
    decimal IrpfPct,
    decimal CotizacionSsPct,
    DateOnly FechaInicio,
    DateOnly FechaFin
);

public record A3InnuvaNominasCalculadasRequest(
    int EmpresaId,
    int PeriodoId,
    List<EmpleadoNominaDto> Empleados
);

public record EmpleadoNominaDto(
    string NIF,
    string Nombre,
    Dictionary<string, decimal> Conceptos  // { "Sueldo": 1500, "Dietas": 100, ... }
);
```

**Flujo de Lectura (SYNC):**

```
1. Usuario hace clic "Sincronizar A3" en período
2. Backend:
   a) Obtener `Period.PeriodoId` (número de período A3)
   b) Llamar `A3InnuvaClient.GetContratosAsync(empresaId, periodo)`
   c) Almacenar respuesta completa en `StagingA3InnuvaContrato.PayloadJson`
   d) Calcular SHA-256 → unique constraint
   e) Marcar `FlagProcesado = false` (aún no leído para cálculo)
   f) Registrar en AuditLog

3. Validaciones post-sync:
   - ¿Existen todos los empleados en BD local (User)?
   - ¿Contratos vigentes en el período?
   - Generar alertas si hay gaps
```

**Flujo de Escritura (EXPORT):**

```
1. Usuario aprueba cierre de costes
2. Backend:
   a) Cargar ClosureLines tipo Pago del cierre
   b) Agrupar por empleado (UserId)
   c) Construir `A3InnuvaNominasCalculadasRequest`
   d) Llamar `A3InnuvaClient.PostNominasCalculadasAsync(...)`
   e) Si 200 OK → marcar cierre como "Exportado A3"
   f) Si error → generar alerta bloqueante
   g) Auditar export

3. Ejemplo de payload:
   {
     "empresaId": 1,
     "periodoId": 202306,
     "empleados": [
       {
         "nif": "12345678A",
         "nombre": "Juan García",
         "conceptos": {
           "Sueldo": 1500.00,
           "Dietas": 150.00,
           "Incentivos": 200.00
         }
       }
     ]
   }
```

---

### 3.2 Intratime (Fichajes)

| Atributo | Valor |
|----------|-------|
| **Tipo Conexión** | REST API HTTP |
| **BaseUri** | `https://api.intratime.es/v1` |
| **Autenticación** | ApiToken (configuración local) |
| **Dirección** | Lectura |
| **Periodicidad** | Diario / bajo demanda |

**Datos Consumidos:**

| Entidad | Tabla Staging | Descripción |
|---------|---------------|-------------|
| Fichajes (entrada/salida) | `StagingIntratimeFichaje` | Hora entrada, hora salida, horas trabajadas |

**Interface:**

```csharp
public interface IIntratimeClient
{
    Task<IReadOnlyList<IntratimeFichajeDto>> GetFichajesAsync(
        DateOnly desde, 
        DateOnly hasta, 
        CancellationToken ct);
}

public record IntratimeFichajeDto(
    string FichajeIdExterno,  // id único en Intratime
    int UserId,               // mapeo a User local
    DateTime Entrada,         // marca de entrada
    DateTime? Salida,         // marca de salida (null si aún activo)
    decimal? HorasCalculadas  // opcionalmente ya calculadas
);
```

**Validaciones:**
- `Entrada` siempre presente.
- `Salida` > `Entrada` si existe.
- `HorasCalculadas` = (Salida − Entrada) / 3600 segundos.
- Si `Salida` nula → se asume jornada abierta (no se contabiliza).

**Cálculos en Motor:**
- Agregado `Sum(Horas)` de fichajes en período para concepto "Horas trabajadas".
- Filtros: tipo de jornada (normal, extra, reducida) extraídos de `PayloadJson.Extra`.

---

### 3.3 PayHawk (Gastos)

| Atributo | Valor |
|----------|-------|
| **Tipo Conexión** | REST API HTTP |
| **BaseUri** | `https://api.payhawk.com/v2` |
| **Autenticación** | BearerToken (configuración local) |
| **Dirección** | Lectura |
| **Periodicidad** | Diario / bajo demanda |

**Datos Consumidos:**

| Entidad | Tabla Staging | Descripción |
|---------|---------------|-------------|
| Gastos | `StagingPayHawkGasto` | Dietas, kilómetros, tickets de comida/taxi |

**Interface:**

```csharp
public interface IPayHawkClient
{
    Task<IReadOnlyList<PayHawkGastoDto>> GetGastosAsync(
        DateOnly desde, 
        DateOnly hasta, 
        CancellationToken ct);
}

public record PayHawkGastoDto(
    string GastoIdExterno,    // id único en PayHawk
    int UserId,               // mapeo a User local
    int ServiceId,            // proyecto/servicio
    DateOnly Fecha,
    decimal Importe,          // cantidad en EUR
    string Categoria,         // "dieta", "km", "taxi", "hotel"
    string? Descripcion       // texto libre
);
```

**Categorías Soportadas:**
- `dieta`: cantidad diaria para manutención
- `km`: coste de kilómetro a tarifa variable
- `taxi`: transporte local
- `hotel`: pernocta
- `comida`: comidas de cliente
- `otro`: gastos diversos

**Cálculos en Motor:**
- Filtro por categoría: `Source(StagingPayHawkGasto, filters=[{Categoria=Eq, dieta}])`
- Agregado: `Sum(Importe)` de gastos filtrados.
- Modificadores: franquicia, tramos por tipo de gasto.

---

### 3.4 Celero One (Visitas de Campo)

| Atributo | Valor |
|----------|-------|
| **Tipo Conexión** | PostgreSQL directo (AlloyDB Google Cloud) |
| **Host/Puerto** | Configuración IAM Google Cloud |
| **Dirección** | Lectura |
| **Periodicidad** | Tiempo real / bajo demanda |

**Datos Consumidos:**

| Entidad | Tabla Staging | Descripción |
|---------|---------------|-------------|
| Visitas | `StagingCeleroVisita` | GPV, tipo visita, zona, rendimiento, km, horas |
| Clientes | — (sincronización directa a `Client`) | Catálogo de clientes |
| Servicios | — (sincronización directa a `Service`) | Proyectos/acciones contratadas |

**Interface:**

```csharp
public interface ICeleroClient
{
    Task<IReadOnlyList<CeleroVisitaDto>> GetVisitasAsync(
        DateOnly desde, 
        DateOnly hasta, 
        CancellationToken ct);
    
    Task<IReadOnlyList<CeleroClienteDto>> GetClientesAsync(CancellationToken ct);
    Task<IReadOnlyList<CeleroServicioDto>> GetServiciosAsync(CancellationToken ct);
}

public record CeleroVisitaDto(
    string VisitaIdExterno,       // id único en Celero
    string ResourceNif,            // NIF del GPV → mapeo a User
    string ServiceName,            // nombre del servicio
    string? MissionName,           // misión/punto de venta
    DateOnly Fecha,
    string PayloadJsonRaw          // JSON crudo con campos adicionales
);

public record CeleroClienteDto(
    string ClienteIdExterno,
    string Nombre,
    string NIF
);

public record CeleroServicioDto(
    string ServicioIdExterno,
    string Nombre,
    string ClienteIdExterno,      // vinculación a cliente
    string TipoServicio           // "venta", "auditoría", "merchandising"
);
```

**Campos en PayloadJson (Variables por Pregunta):**

```json
{
  "tipoVisita": 2,           // 1=estándar, 2=reposición, 3=auditoría
  "puntoMontado": "Premium", // "Premium", "Regular", "Básico"
  "zona": "A",               // zona geográfica
  "km": 45.3,                // kilómetros recorridos
  "horas": 2.5,              // horas dedicadas
  "importe": 250.00,         // tarifa prenegociada
  "categoria": "visita_venta",
  "idQuestion": "Q001",      // pregunta Celero (respuesta variable)
  "respuesta": "Sí",         // respuesta a la pregunta
  "estado": "completada",    // "completada", "fallida", "cancelada"
  "numeroVisita": 1,         // 1ª, 2ª, 3ª visita del mes
  "nocturnidad": false,
  "pernocta": false,
  "extra": { "merchandising": "surtido", ... }
}
```

**Cálculos en Motor:**
```
// Ejemplo: "Visitas tipo 2 × 5€"
Aggregate(
  Count,
  Source(VisitasCelero, 
    filters=[{ tipoVisita = Eq 2 }])
) × 5
→ 3 visitas × 5 = 15€

// Ejemplo: "Km × 0.23€"
Aggregate(
  Sum, field=km,
  Source(VisitasCelero)
) × 0.23
→ 315 km × 0.23 = 72.45€

// Ejemplo: "Días con actividad × 150€"
Aggregate(
  Count, distinct=Fecha,
  Source(VisitasCelero)
) × 150
→ 5 días con al menos 1 visita × 150 = 750€
```

**Variables por Pregunta (idQuestion):**

Celero permite hacer preguntas (`idQuestion`) cuya respuesta varía por visita. El motor resuelve:

```csharp
// Resolver variable "Disponibilidad de stock" (pregunta Q001)
var idQuestion = "Q001";
var respuestasVisitas = staging
  .Where(v => v.PayloadJson["idQuestion"] == idQuestion)
  .Select(v => v.PayloadJson["respuesta"])
  .ToList();  // ["Sí", "Sí", "No", "Sí", "Sí"]

// Colapso a escalar: respuesta más frecuente (mode)
var modeRespuesta = "Sí";  // 4 de 5

// Mapeo a número
var mapeo = variable.MapeoValoresJson;  // {"Sí": 1, "No": 0}
var valor = mapeo[modeRespuesta];  // 1

// Usar en fórmula
var resultado = Aggregate(...) * valor;
```

---

### 3.5 Bizneo (RRHH & Ausencias)

| Atributo | Valor |
|----------|-------|
| **Tipo Conexión** | REST API HTTP |
| **BaseUri** | `https://api.bizneo.com/v2` |
| **Autenticación** | ApiKey (configuración local) |
| **Dirección** | Lectura |
| **Periodicidad** | Diario / bajo demanda |

**Datos Consumidos:**

| Entidad | Tabla Staging | Descripción |
|---------|---------------|-------------|
| Empleados | `StagingBizneoEmpleado` | RRHH maestro (NIF, nombre, dpto) |
| Ausencias | `StagingBizneoAbsencia` | Bajas, permisos, horas no trabajadas |

**Interface:**

```csharp
public interface IBizneoClient
{
    Task<IReadOnlyList<BizneoEmpleadoDto>> GetEmpleadosAsync(CancellationToken ct);
    Task<IReadOnlyList<BizneoAbsenceDto>> GetAbsencesAsync(
        DateOnly desde, 
        DateOnly hasta, 
        CancellationToken ct);
}

public record BizneoEmpleadoDto(
    string EmpleadoIdExterno,
    string NIF,
    string Nombre,
    string? Departamento
);

public record BizneoAbsenceDto(
    string RegistroIdExterno,
    int UserId,           // mapeo a User
    int ServiceId,        // proyecto (ausencia en contexto de proyecto)
    DateOnly Fecha,
    decimal Horas,        // horas perdidas
    string Tipo           // "permiso", "baja", "vacaciones"
);
```

**Cálculos en Motor:**
- Descontar horas de ausencia de "horas trabajadas".
- Filtro: `Source(StagingBizneoAbsencia, filters=[{Tipo=Eq, baja}])`
- Agregado: `Sum(Horas)` de ausencias → se resta del total de horas.

---

### 3.6 TravelPerk (Viajes — Fase 2)

| Atributo | Valor |
|----------|-------|
| **Tipo Conexión** | REST API HTTP |
| **BaseUri** | `https://api.travelperk.com/v2` |
| **Autenticación** | BearerToken |
| **Dirección** | Lectura |
| **Periodicidad** | Bajo demanda (Fase 2) |
| **Estado** | Pending implementation |

**Datos Previstos:**

| Entidad | Tabla Staging | Descripción |
|---------|---------------|-------------|
| Viajes | `StagingTravelPerk` | Vuelos, hoteles, gastos de viaje por empleado |

**Cálculos en Motor (futuros):**
- Importe total viajes por empleado/período.
- Filtros por tipo (doméstico/internacional).
- Aplicar regla de refacturación (coste + % margen).

---

### 3.7 SGPV (Legacy)

| Atributo | Valor |
|----------|-------|
| **Tipo Conexión** | Importación JSON |
| **Dirección** | Lectura |
| **Formato** | `SGPV_Productos.json` en workspace |

**Datos:**
```json
{
  "visitas": [
    {
      "visitaIdExterno": "SGP20260601001",
      "userNif": "12345678A",
      "serviceCode": "SERV-01",
      "fecha": "2026-06-01",
      "importe": 150.00
    }
  ]
}
```

---

## 4. Motor de Cálculo — Conceptos y Fórmulas

### 4.1 Primitivas Completamente Soportadas

#### A. Número Fijo
```json
{ "type": "Number", "value": 1500 }
→ 1500.00
```
**Uso:** Cuota fija mensual, cantidad constante.

#### B. Variable (Parámetro)
```json
{ "type": "Variable", "variableId": 4 }
→ 18.5 (TarifaHora)
```
**Uso:** Tarifas que cambian sin tocar la fórmula.

#### C. Agregado (Conteo/Suma)
```json
{
  "type": "Aggregate",
  "op": "Count",
  "source": { "type": "Source", "entity": "VisitasCelero", "filters": [] }
}
→ 3 (nº de visitas)

{
  "type": "Aggregate",
  "op": "Sum",
  "field": "Importe",
  "source": { "type": "Source", "entity": "StagingPayHawkGasto", "filters": [] }
}
→ 450.50 (suma de gastos)

{
  "type": "Aggregate",
  "op": "Count",
  "distinct": "Fecha",
  "source": { "type": "Source", "entity": "VisitasCelero", "filters": [] }
}
→ 5 (días únicos con actividad)
```

#### D. Operación Binaria
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": { ... },
  "right": { ... }
}
```
**Operadores:** `Add`, `Sub`, `Mul`, `Div`, `Pct` (porcentaje aditivo).

**Ejemplos:**
- `Pct(100, 15)` = 100 × (1 + 15/100) = 115 (refacturación con +15% margen)
- `Div(Total, 30)` = Total / 30 (distribuir entre días del mes)

#### E. Modificador (Tope/Piso/Franquicia)
```json
{
  "type": "Modifier",
  "kind": "Min",
  "threshold": 250,
  "inner": { "type": "Aggregate", "op": "Sum", "field": "Km", ... }
}
→ Si inner=100, devuelve 250 (suelo mínimo)
→ Si inner=300, devuelve 300 (no se modifica)

{
  "type": "Modifier",
  "kind": "Franquicia",
  "threshold": 300,
  "inner": { ... }
}
→ Si inner=315, devuelve 315−300=15 (los primeros 300 no cuentan)
```

| kind | Lógica | Uso |
|------|--------|-----|
| `Min` | Si inner < threshold → threshold | Suelo/cantidad mínima |
| `Max` | Si inner > threshold → threshold | Techo/cantidad máxima |
| `FloorZero` | Si inner < threshold → 0 | Umbral mínimo (sin piso) |
| `Franquicia` | inner − threshold (mín 0) | Descuento en los primeros X |

#### F. Tarifa Escalonada (Tramos)
```json
{
  "type": "Tramos",
  "tramos": [
    { "hasta": 1, "precio": 90 },
    { "hasta": null, "precio": 37 }
  ],
  "inner": { "type": "Number", "value": 3 }
}
→ 1×90 + 2×37 = 164
```
**Uso:** 1ª hora 90€, siguientes 37€ (Molins, Inpost, etc.).

#### G. Fee Sobre Conceptos
```json
{
  "type": "ConceptRef",
  "conceptIds": [],  // vacío = todos los conceptos base
  "op": "Mul",
  "factor": 0.10
}
→ Si suma de conceptos base = 500, fee = 500 × 0.10 = 50
```
**Uso:** Fee del 6.5% sobre total de nómina, comisión sobre ventas.

**IMPORTANTE:** Se calcula en PASADA 2, cuando los conceptos base ya tienen importe.

#### H. Filtros Explícitos (en Source)
```json
{
  "type": "Source",
  "entity": "VisitasCelero",
  "filters": [
    { "field": "tipoVisita", "op": "Eq", "value": 2 },
    { "field": "zona", "op": "In", "value": ["A", "B"] }
  ]
}
```

**Operadores:** `Eq`, `Neq`, `Gt`, `Gte`, `Lt`, `Lte`, `In`, `NotIn`, `Contains`.

**Campos soportados (dependen de la entidad):**
- **VisitasCelero:** tipoVisita, puntoMontado, zona, km, horas, importe, categoria, estado, numeroVisita, nocturnidad, pernocta, + Extra.*
- **StagingPayHawkGasto:** categoria, descripcion
- **StagingIntratimeFichaje:** estado
- **StagingBizneoAbsencia:** tipo

---

### 4.2 Composición de Fórmulas Complejas

**Ejemplo 1: Kilometraje con franquicia**

*"Los primeros 300 km del mes no se pagan; el resto a 0,23€/km."*

```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Modifier",
    "kind": "Franquicia",
    "threshold": 300,
    "inner": {
      "type": "Aggregate",
      "op": "Sum",
      "field": "km",
      "source": { "type": "Source", "entity": "VisitasCelero", "filters": [] }
    }
  },
  "right": { "type": "Number", "value": 0.23 }
}
```

**Cálculo:** 315 km → (315−300) × 0.23 = 15 × 0.23 = **3,45€**

---

**Ejemplo 2: Visitas con tarifa escalonada**

*"1ª hora 90€, siguientes 37€"*

```json
{
  "type": "Tramos",
  "tramos": [
    { "hasta": 1, "precio": 90 },
    { "hasta": null, "precio": 37 }
  ],
  "inner": {
    "type": "Aggregate",
    "op": "Sum",
    "field": "horas",
    "source": { "type": "Source", "entity": "VisitasCelero", "filters": [] }
  }
}
```

**Cálculo:** 3 horas → 1×90 + 2×37 = **164€**

---

**Ejemplo 3: Fee sobre conceptos base (2ª pasada)**

*"Fee del 6,5% sobre total de nómina"*

Conceptos Base (PASADA 1):
- Sueldo: 1500€
- Dietas: 150€
- Viajes: 200€
- **Subtotal:** 1850€

Concepto Fee (PASADA 2):
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "ConceptRef",
    "conceptIds": []  // todos los conceptos base
  },
  "right": { "type": "Number", "value": 0.065 }
}
```

**Cálculo:** 1850 × 0.065 = **120,25€**

---

**Ejemplo 4: Visitas por tipo con filtros**

*"Visitas tipo 2 (reposición) × 5€, visitas tipo 3 (auditoría) × 8€"*

Concepto 1:
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Count",
    "source": {
      "type": "Source",
      "entity": "VisitasCelero",
      "filters": [{ "field": "tipoVisita", "op": "Eq", "value": 2 }]
    }
  },
  "right": { "type": "Number", "value": 5 }
}
```

Concepto 2:
```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Count",
    "source": {
      "type": "Source",
      "entity": "VisitasCelero",
      "filters": [{ "field": "tipoVisita", "op": "Eq", "value": 3 }]
    }
  },
  "right": { "type": "Number", "value": 8 }
}
```

---

## 5. Entidades y Modelos

### 5.1 Maestros (No Staging)

```csharp
// Cliente (empresa contratante)
public class Client
{
    public int Id { get; set; }
    public string Nombre { get; set; }        // "Alpha Foods"
    public string NIF { get; set; }
    public string? Direccion { get; set; }
    public bool IsDeleted { get; set; }       // Soft delete
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Service> Services { get; set; }
    public ICollection<ClienteIncidencia> Incidencias { get; set; }
}

// Servicio (acción/proyecto del cliente)
public class Service
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Codigo { get; set; }        // "ECRES-01"
    public string Nombre { get; set; }        // "Merchandising ECRES"
    public int DepartmentId { get; set; }     // RRHH, Ventas, etc.
    public decimal? MargenObjetivoPct { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Client Client { get; set; }
    public ICollection<ServiceCostCenter> CostCenters { get; set; }  // N:M
    public ICollection<ServiceUser> Users { get; set; }               // N:M (ownership)
    public ICollection<ServiceConcept> Concepts { get; set; }         // N:M
    public ICollection<Closure> Closures { get; set; }
    public ICollection<Forecast> Forecasts { get; set; }
}

// Concepto (regla de cálculo)
public class Concept
{
    public int Id { get; set; }
    public int? ServiceId { get; set; }       // null = global aplicable a todos
    public string Nombre { get; set; }        // "Dietas"
    public string Descripcion { get; set; }
    public TipoConcepto Tipo { get; set; }    // Pago, Factura
    public string FormulaJson { get; set; }   // AST serializado
    public DateOnly FechaDesde { get; set; }  // vigencia
    public DateOnly FechaHasta { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Service? Service { get; set; }
    public ICollection<ClosureLine> Lines { get; set; }
    public ICollection<ServiceConcept> ServiceConcepts { get; set; }
}

public enum TipoConcepto { Pago, Factura }

// Variable (parámetro de fórmula)
public class Variable
{
    public int Id { get; set; }
    public string Nombre { get; set; }        // "TarifaHora"
    public string QuestionIdExterno { get; set; }  // id pregunta Celero
    public string MapeoValoresJson { get; set; }   // {"Sí": 1, "No": 0, "Default": 0}
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

// Usuario
public class User
{
    public int Id { get; set; }
    public string NIF { get; set; }           // único
    public string Nombre { get; set; }
    public string Apellidos { get; set; }
    public string Email { get; set; }         // único
    public string PasswordHash { get; set; }  // BCrypt
    public int? DepartmentId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Department? Department { get; set; }
    public ICollection<UserRole> UserRoles { get; set; }
    public ICollection<ServiceUser> ServiceUsers { get; set; }  // ownership
    public ICollection<ClosureLine> Lines { get; set; }         // líneas asignadas
}

// Período de cierre
public class Period
{
    public int Id { get; set; }
    public int Numero { get; set; }           // 202306
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public int DiaPago { get; set; }          // 30, 15 o 9
    public EstadoPeriod Estado { get; set; }  // Abierto, Cerrado, Bloqueado
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Closure> Closures { get; set; }
}

public enum EstadoPeriod { Abierto, Cerrado, Bloqueado }
```

### 5.2 Cierres (Núcleo de Cálculo)

```csharp
// Cierre (de costes o facturación)
public abstract class Closure
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int PeriodId { get; set; }
    public EstadoCierre Estado { get; set; }  // Abierto, PendienteValidacion, ...
    public int? UsuarioCreacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] xmin { get; set; }          // Optimistic concurrency (PostgreSQL)
    public Service Service { get; set; }
    public Period Period { get; set; }
    public ICollection<ClosureLine> Lines { get; set; }
    public ICollection<ClosureAlerta> Alertas { get; set; }
    public ICollection<Approval> Approvals { get; set; }
    public ICollection<ApprovalHistory> ApprovalHistory { get; set; }
}

// Subclases concretas
public class CierreCostes : Closure { }
public class CierreFacturacion : Closure { }

public enum EstadoCierre
{
    Abierto,                    // Recién creado, cálculo completado
    PendienteValidacion,        // Esperando validación
    PendienteGrupo,             // Validado, esperando grupo regional
    PendienteFICO,              // Grupo revisó, esperando FICO
    PendienteDireccion,         // FICO revisó, esperando dirección
    Aprobado,                   // Aprobado definitivamente
    Rechazado,                  // Rechazado (retorna a Abierto)
    Exportado                   // Exportado a A3
}

// Línea de cierre
public class ClosureLine
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public int? ConceptoId { get; set; }      // null si es override manual
    public int? UserId { get; set; }          // opcional (recurso asignado)
    public decimal Importe { get; set; }
    public bool EsOverride { get; set; }      // true si es manual
    public int? UsuarioOverride { get; set; }
    public string? MotivoOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Closure Closure { get; set; }
    public Concept? Concepto { get; set; }
    public User? Usuario { get; set; }
    public CalculationLog? CalculationLog { get; set; }  // 1:1
}

// Traza de cálculo (inmutable)
public class CalculationLog
{
    public int Id { get; set; }
    public int ClosureLineId { get; set; }
    public string FormulaSnapshotJson { get; set; }   // Fórmula exacta usada
    public string InputsJson { get; set; }            // Datos de entrada
    public decimal Resultado { get; set; }
    public string SistemaOrigen { get; set; }         // "Celero", "PayHawk", "Mixto"
    public string? Incidencias { get; set; }          // JSON array de alertas
    public DateTime CreatedAt { get; set; }
    public ClosureLine ClosureLine { get; set; }
}

// Alerta de cierre
public class ClosureAlerta
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public string Codigo { get; set; }        // "NOM-001", "FACT-002"
    public string Descripcion { get; set; }
    public TipoAlerta Tipo { get; set; }      // Bloqueante, Advertencia
    public bool Confirmada { get; set; }      // false → detiene el cierre
    public int? UsuarioConfirmacion { get; set; }
    public DateTime? FechaConfirmacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public Closure Closure { get; set; }
}

public enum TipoAlerta { Bloqueante, Advertencia }
```

### 5.3 Aprobaciones

```csharp
// Estado actual de aprobación
public class Approval
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public int Paso { get; set; }             // 1-5 (5 pasos del flujo)
    public EstadoAprobacion Estado { get; set; }
    public int? UsuarioResponsable { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Closure Closure { get; set; }
    public User? Usuario { get; set; }
}

public enum EstadoAprobacion { Pendiente, Aprobado, Rechazado }

// Historial de transiciones
public class ApprovalHistory
{
    public int Id { get; set; }
    public int ClosureId { get; set; }
    public int UserId { get; set; }
    public EstadoCierre EstadoAnterior { get; set; }
    public EstadoCierre EstadoNuevo { get; set; }
    public string? Motivo { get; set; }      // Motivo del rechazo
    public DateTime FechaTransicion { get; set; }
    public Closure Closure { get; set; }
    public User Usuario { get; set; }
}
```

### 5.4 Auditoría

```csharp
// Registro de cambios
public class AuditLog
{
    public int Id { get; set; }
    public string Entidad { get; set; }          // "ClosureLine", "Concept", etc.
    public int EntidadId { get; set; }
    public int? UserId { get; set; }
    public string Accion { get; set; }           // "Create", "Update", "Delete", "Export"
    public string? CambiosJson { get; set; }     // { "campo": { "antes": X, "ahora": Y } }
    public DateTime Timestamp { get; set; }
    public User? Usuario { get; set; }
}
```

### 5.5 Configuración

```csharp
// Incidencias de cliente
public class ClienteIncidencia
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Titulo { get; set; }
    public string Descripcion { get; set; }
    public EstadoIncidencia Estado { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Client Client { get; set; }
}

public enum EstadoIncidencia { Abierta, EnProceso, Resuelta }

// Categorías de facturación
public class CategoriaFactura
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string Nombre { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Client Client { get; set; }
    public ICollection<CategoriaFacturaConcepto> Conceptos { get; set; }
}

public class CategoriaFacturaConcepto
{
    public int CategoriaId { get; set; }
    public int ConceptoId { get; set; }
    public CategoriaFactura Categoria { get; set; }
    public Concept Concepto { get; set; }
}

// Forecast de ventas y GPP
public class Forecast
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public decimal? VentasPrevistas { get; set; }
    public decimal? MargenPrevisto { get; set; }
    public int? PersonasCampo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Service Service { get; set; }
}

// Partidas de presupuesto
public class PartidaPresupuesto
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string Nombre { get; set; }
    public decimal Presupuesto { get; set; }  // importe total
    public decimal Consumido { get; set; }    // importe consumido
    public TipoPresupuesto Tipo { get; set; } // Anual, TotalAccion
    public int? Ejercicio { get; set; }       // Si Anual
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Service Service { get; set; }
}

public enum TipoPresupuesto { Anual, TotalAccion }
```

---

## 6. Matriz de Dependencias — Conceptos × Fuentes

Esta tabla muestra **de dónde obtiene cada concepto típico sus datos, cómo se calcula y a qué se imputa**.

| Concepto | Tipo | Fuente (Staging) | Filtros | Cálculo | Imputación | Ejemplo Valor |
|----------|------|------------------|---------|---------|------------|---------------|
| **Sueldo Base** | Pago | A3 Innuva | Período vigencia | Lectura directa | Empleado | 1500.00 |
| **Visitas de campo** | Pago | Celero Visitas | Tipo=2, Período | Count × Tarifa | Empleado | 5 × 50 = 250 |
| **Dietas** | Pago | PayHawk Gastos | Categoría="dieta", Período | Sum(Importe) | Empleado | 150.00 |
| **Kilometraje** | Pago | Celero Visitas | Field=km, Período | Sum(km) × 0.23 € | Empleado | 315 km × 0.23 = 72.45 |
| **Horas trabajadas** | Pago | Intratime Fichajes | Período | Sum(Horas) | Empleado | 160 horas |
| **Ausencias** | Pago (negativo) | Bizneo Ausencias | Tipo="baja", Período | Sum(Horas) × TarifaHora | Empleado | −8 × 18.5 = −148 |
| **Tarifa por zona** | Pago | Celero Visitas | Zona="A", Período | Count × TarifaZona | Empleado | 3 × 75 = 225 |
| **Tarifa escalonada** | Pago | Intratime Fichajes | Período | Tramos(1ª 90€, 2ª+ 37€) | Empleado | 1×90 + 2×37 = 164 |
| **Fee sobre conceptos** | Pago | — (ConceptRef) | Conceptos base | Sum(Base) × 6.5% | Empleado | 1850 × 0.065 = 120.25 |
| **Refacturación cliente** | Factura | Galán/Mediapost (Fase 2) | Período | Coste × (1 + margen%) | Cliente | 1000 × 1.15 = 1150 |
| **Cuota servicio mensual** | Factura | Presupuesto | Período | Fixed | Cliente | 5000.00 |
| **Sobrefactura logística** | Factura | Mediapost | Período | m³ × Tarifa + margen | Cliente | 50 × 2.5 × 1.10 = 137.50 |

---

## 7. Validaciones Críticas

### 7.1 Pre-Cálculo

| Validación | Tipo Alerta | Acción |
|-----------|------------|--------|
| ¿Existe `User` por NIF en BD? | Bloqueante | Rechazar línea de staging; registrar error |
| ¿Existe `Service` por código externo? | Bloqueante | Rechazar cierre; requerir configuración |
| ¿Está `Period` abierto (no bloqueado)? | Bloqueante | Impedir cálculo; requerir reapertura |
| ¿Hay conceptos aplicables vigentes? | Advertencia | Crear alerta `"Sin conceptos aplicables en período"` |
| ¿Existen datos de fuentes (staging no vacío)? | Advertencia | Crear alerta `"Período sin datos de Celero/PayHawk..."` |
| ¿Hay contrato vigente para el empleado? | Bloqueante | Alerta `"Empleado sin contrato en período"` |

### 7.2 Durante Cálculo

| Validación | Tipo Alerta | Valor Devuelto |
|-----------|------------|--------|
| Aggregate devuelve 0 registros | Advertencia | 0 + incidencia `EmptyDataset` |
| División por cero en BinaryOp | Bloqueante | Error + alerta `"División por cero"` |
| Variable no encontrada | Bloqueante | Error + alerta `"Variable {id} no existe"` |
| Concepto no vigente en período | Advertencia | Saltar concepto (no crear línea) |
| Modificador causa valor negativo | Advertencia | Ajustar a 0 (si FloorZero) o dejar negativo |

### 7.3 Post-Cálculo / Pre-Aprobación

| Validación | Tipo Alerta | Acción |
|-----------|------------|--------|
| Línea sin asignación a usuario (Pago) | Advertencia | Crear alerta `"Línea sin empleado asignado"` |
| Facturación > Presupuesto del servicio | Advertencia | Alerta `"Sobrefactura vs presupuesto"` |
| Cierre incompleto (líneas = 0) | Bloqueante | Alerta `"Cierre sin líneas calculadas"` |
| Alerta bloqueante sin confirmar | Bloqueante | Impedir avance en flujo aprobación |
| Totales no cuadran contablemente | Bloqueante | Revisar cálculo; auditar origen |

---

## 8. Checklists por Fase

### PHASE 1: SYNC — Pre-Sincronización

- [ ] ¿Credenciales de cada API configuradas en `appsettings.Development.json`?
- [ ] ¿`Integrations.UseFake` = false (usar real) o true (usar fake)?
- [ ] ¿Período objetivo abierto en BD?
- [ ] ¿Carpeta de logs limpia y accesible?

**Ejecutar:**
```bash
POST /api/sync/celero
POST /api/sync/payhawk
POST /api/sync/intratime
POST /api/sync/bizneo
POST /api/sync/a3innuva
```

**Validar:**
- [ ] Staging tables contienen filas (SELECT COUNT FROM StagingCeleroVisita, etc.)
- [ ] No hay duplicados (hash unique constraint respetado)
- [ ] Tabla `AuditLog` registro con `Accion=Sync`

---

### PHASE 2: PREPARE — Pre-Cálculo

- [ ] ¿Todos los empleados (NIF del staging) existen en `User`?
- [ ] ¿Todos los servicios (código del staging) existen en `Service`?
- [ ] ¿Período está abierto (no Cerrado ni Bloqueado)?
- [ ] ¿Hay conceptos aplicables (vigentes, del servicio o globales)?

**Ejecutar:**
```bash
POST /api/closures/create-batch
  { "periodId": 1, "serviceIds": [1, 2, 3], "tipo": "CierreCostes" }
```

**Validar:**
- [ ] Closure creado con `Estado = Abierto`
- [ ] ClosureLine vacía (aún no calculadas)

---

### PHASE 3: CALCULATE — Cálculo

- [ ] ¿Fórmulas de conceptos son JSON válido?
- [ ] ¿Todas las Variables referenciadas existen?
- [ ] ¿Filtros explícitos usan campos válidos?
- [ ] ¿No hay circular dependencies (concepto A depende de B que depende de A)?

**Ejecutar:**
```bash
POST /api/closures/{closureId}/recalcular
```

**Validar:**
- [ ] ClosureLines creadas (una por concepto aplicable)
- [ ] CalculationLog generado (traza inmutable)
- [ ] ClosureAlerta si hay incidencias
- [ ] Totales cuadran (suma líneas = importe cierre)

---

### PHASE 4: OVERRIDE — Ajustes Manuales

- [ ] ¿Usuario autorizado (rol Administrator/Backoffice)?
- [ ] ¿Motivo rellenado (auditable)?
- [ ] ¿Importe válido (positivo o negativo según tipo)?

**Ejecutar:**
```bash
POST /api/closures/{closureId}/lines/override
  { "conceptoId": 5, "importe": 250, "motivo": "2ª visita — tarifa 50%" }
```

**Validar:**
- [ ] ClosureLine nueva con `EsOverride=true`
- [ ] AuditLog con cambios

---

### PHASE 5: APPROVAL — Flujo de Aprobaciones

**Paso 1 — Validación:**
- [ ] ¿Alertas bloqueantes confirmadas?
- [ ] ¿Total cierre razonable vs período anterior?

**Paso 2 — Grupo:**
- [ ] ¿Servicios del cierre son del gestor?
- [ ] ¿Márgenes dentro de rangos operativos?

**Paso 3 — FICO:**
- [ ] ¿Cálculos auditados (CalculationLog revisados)?
- [ ] ¿Deviaciones contables reconciliadas?

**Paso 4 — Dirección:**
- [ ] ¿Aprobación final con firma digital (futura)?

**Ejecutar:**
```bash
POST /api/closures/{closureId}/aprobar
  { "paso": 2, "motivo": "Revisado y conforme" }

POST /api/closures/{closureId}/rechazar
  { "motivo": "Recalcular con filtros nuevos" }
```

**Validar:**
- [ ] ApprovalHistory registrada
- [ ] Estado del Closure transicionó correctamente
- [ ] AuditLog con usuario responsable

---

### PHASE 6: EXPORT — Exportación a A3

- [ ] ¿Closure estado = Aprobado?
- [ ] ¿Todas las líneas con importe > 0?
- [ ] ¿Formato XML/EDI A3 validado con cliente?

**Ejecutar:**
```bash
GET /api/exports/a3-innuva/{closureId}
GET /api/exports/a3-erp/{closureId}
```

**Validar:**
- [ ] Archivo descargado sin errores
- [ ] AuditLog con `Accion=Export`
- [ ] Archivo XML bien formado (validar schema A3)
- [ ] Líneas agrupadas correctamente (por empleado/cliente)

---

### PHASE 7: REPORTING

- [ ] Dashboard muestra KPIs correctos?
- [ ] AuditLog filtrable por usuario/fecha/acción?
- [ ] CalculationLog accesible desde detalle línea?

**Validar:**
- [ ] GET `/api/dashboard` devuelve KPIs del período
- [ ] GET `/api/audit?userId=5&accion=Export` devuelve registros
- [ ] GET `/api/calculations/{lineId}` devuelve traza completa

---

## 9. Ejemplo Real — Cálculo Completo

### Escenario: Empleado "Juan García" — Período Junio 2026

**Datos de entrada:**

| Sistema | Dato | Valor |
|---------|------|-------|
| A3 Innuva | Sueldo base | 1500.00 € |
| Celero | Visitas tipo 2 | 3 visitas |
| Celero | Kilómetros | 315 km |
| PayHawk | Dietas | 150.00 € |
| Intratime | Horas trabajadas | 160 horas |
| Bizneo | Baja enfermedad | 8 horas |

**Conceptos aplicables (PASADA 1):**

| ID | Nombre | Tipo | Fórmula |
|----|--------|------|---------|
| 1 | Sueldo | Pago | Number(1500) |
| 2 | Visitas × 50€ | Pago | Count(Celero, tipo=2) × 50 |
| 3 | Kilometraje | Pago | (Sum(km) − 300) × 0.23 |
| 4 | Dietas | Pago | Sum(PayHawk, dieta) |
| 5 | Fee 6,5% | Pago | ConceptRef[] × 0.065 |

**Evaluación:**

```
PASADA 1:

Concepto 1 (Sueldo):
  Fórmula: Number(1500)
  Resultado: 1500.00 €
  CalculationLog: { inputs: {}, resultado: 1500.00, formula: ... }

Concepto 2 (Visitas):
  Fórmula: Count(VisitasCelero, tipoVisita=2) × 50
  Evaluación:
    ├─ Count staging → 3 visitas
    └─ 3 × 50 = 150.00 €
  Resultado: 150.00 €

Concepto 3 (Kilometraje):
  Fórmula: (Sum(km) − 300) × 0.23
  Evaluación:
    ├─ Sum(km) → 315
    ├─ Modifier(Franquicia[300]) → 315 − 300 = 15
    └─ 15 × 0.23 = 3.45 €
  Resultado: 3.45 €

Concepto 4 (Dietas):
  Fórmula: Sum(PayHawk, categoria=dieta)
  Evaluación:
    └─ Sum → 150.00 €
  Resultado: 150.00 €

Subtotal conceptos base: 1500.00 + 150.00 + 3.45 + 150.00 = 1803.45 €

---

PASADA 2:

Concepto 5 (Fee 6,5%):
  Fórmula: ConceptRef[] × 0.065
  Evaluación:
    ├─ Leer importes previos: {1: 1500, 2: 150, 3: 3.45, 4: 150}
    ├─ Suma: 1803.45
    └─ 1803.45 × 0.065 = 117.22 €
  Resultado: 117.22 €

---

TOTAL NÓMINA: 1803.45 + 117.22 = 1920.67 €

---

VALIDACIONES POST-CÁLCULO:

  ✓ Líneas sin vacíos
  ✓ Totales cuadran
  ⚠ Aviso: "Baja de 8 horas no aplicada" (Bizneo no está en conceptos)
    → Se recomienda crear concepto "Descuento ausencias: −8 × 18.5 = −148"
```

**AuditLog:**

```json
{
  "Entidad": "Closure",
  "EntidadId": 123,
  "Accion": "Recalcular",
  "UserId": 5,
  "Timestamp": "2026-06-24T10:15:00Z",
  "CambiosJson": {
    "Estado": { "antes": "Abierto", "ahora": "Abierto" },
    "LineCount": { "antes": 0, "ahora": 5 }
  }
}
```

**CalculationLogs (uno por línea):**

```json
[
  {
    "ClosureLineId": 1001,
    "FormulaSnapshotJson": "{ \"type\": \"Number\", \"value\": 1500 }",
    "InputsJson": "{ \"source\": \"A3Innuva\" }",
    "Resultado": 1500.00,
    "SistemaOrigen": "A3Innuva",
    "Incidencias": []
  },
  {
    "ClosureLineId": 1002,
    "FormulaSnapshotJson": "{ ... BinaryOp×Aggregate ... }",
    "InputsJson": "{ \"nVisitas\": 3, \"período\": \"2026-06\" }",
    "Resultado": 150.00,
    "SistemaOrigen": "Celero",
    "Incidencias": []
  },
  ...
]
```

---

## 10. Contabilidad — Exportación a A3 ERP

### Proceso de Exportación de Nóminas Calculadas

**Trigger:** Cierre aprobado (Estado = Aprobado)

**Endpoint:**
```
GET /api/exports/a3-innuva/{closureId}
→ Content-Type: application/xml
→ Content-Disposition: attachment; filename="Nominas_2026-06_aprobado.xml"
```

**Validación:**

```csharp
if (closure.Estado != EstadoCierre.Aprobado)
    throw new ClosureNotApprovedException(409);
```

**Transformación:**

```
CierreCostes {
  ServiceId: 1,
  PeriodId: 1,
  Lines: [
    { ConceptoId: 1, UserId: 5, Importe: 1500.00 },  // Sueldo
    { ConceptoId: 2, UserId: 5, Importe: 150.00 },   // Visitas
    { ConceptoId: 3, UserId: 5, Importe: 3.45 },     // Km
    { ConceptoId: 4, UserId: 5, Importe: 150.00 },   // Dietas
    { ConceptoId: 5, UserId: 5, Importe: 117.22 },   // Fee
  ]
}
  ↓ (Agrupar por UserId)
  ↓
Usuario 5 (Juan García, NIF 12345678A):
  Concepto Sueldo: 1500.00
  Concepto Visitas: 150.00
  Concepto Km: 3.45
  Concepto Dietas: 150.00
  Concepto Fee: 117.22
  Total: 1920.67
```

**XML Generado (Plantilla A3):**

```xml
<?xml version="1.0" encoding="UTF-8"?>
<Nóminas>
  <Período Número="202306" FechaInicio="2026-06-01" FechaFin="2026-06-30"/>
  <Empresa ID="1" Nombre="SIG ES"/>
  <Empleado>
    <NIF>12345678A</NIF>
    <Nombre>Juan García</Nombre>
    <Conceptos>
      <Concepto Código="001" Nombre="Sueldo">1500.00</Concepto>
      <Concepto Código="002" Nombre="Visitas">150.00</Concepto>
      <Concepto Código="003" Nombre="Km">3.45</Concepto>
      <Concepto Código="004" Nombre="Dietas">150.00</Concepto>
      <Concepto Código="005" Nombre="Fee">117.22</Concepto>
    </Conceptos>
    <Total>1920.67</Total>
  </Empleado>
  ...
</Nóminas>
```

**AuditLog:**

```json
{
  "Entidad": "Closure",
  "EntidadId": 123,
  "Accion": "Export",
  "UserId": 7,
  "Timestamp": "2026-06-24T10:30:00Z",
  "CambiosJson": {
    "EstadoExport": { "ahora": "ExportadoA3Innuva" },
    "Fichero": { "ahora": "Nominas_2026-06_aprobado.xml" }
  }
}
```

### Asientos Contables Generados (A3 ERP)

**Para Nóminas (Cierre Costes):**

```
Débito:  Gasto | Nómina - Sueldo (1500.00)
Débito:  Gasto | Nómina - Visitas (150.00)
Débito:  Gasto | Nómina - Km (3.45)
Débito:  Gasto | Nómina - Dietas (150.00)
Débito:  Gasto | Nómina - Fee (117.22)
─────────────────────────────────
Crédito: CxP Empleado | Nómina por pagar (1920.67)

Centro Costo: 025888 (asignado al servicio)
Análisis: Proyecto / Cliente / Empleado
```

**Para Facturación (Cierre Facturación):**

```
Débito:  CxC Cliente | Factura emitida (5000.00)
─────────────────────────────────
Crédito: Ingreso | Servicio de field (5000.00)

Centro Costo: 025888
Análisis: Cliente / Proyecto
```

**Reconciliación:**
- **Nómina:** Suma cierres costes = Suma asientos débito nómina
- **Facturación:** Suma cierres facturación = Suma asientos crédito ingresos
- **Margen:** Facturación − Costes = Margen del período

---

## Resumen Final

### Flujo End-to-End

```
1. SYNC (FASE 1)
   ├─ Traer datos de 6+ APIs externas
   ├─ Aplicar idempotencia (SHA-256)
   └─ Persistir en Staging{Sistema}

2. PREPARE (FASE 2)
   ├─ Validar maestros (User, Service, Concept)
   ├─ Mapear staging a RowAdapter
   └─ Crear Closure vacío

3. CALCULATE (FASE 3)
   ├─ Pasada 1: Conceptos base
   ├─ Pasada 2: Conceptos fee (ConceptRef)
   └─ Generar ClosureLines + CalculationLog + Alertas

4. OVERRIDE (FASE 4)
   ├─ Agregar líneas manuales
   └─ Auditar cambios

5. APPROVAL (FASE 5)
   ├─ 5 pasos: Validación → Grupo → FICO → Dirección → Aprobado
   └─ Registrar ApprovalHistory

6. EXPORT (FASE 6)
   ├─ A3 INNUVA (Nóminas)
   ├─ A3 ERP (Asientos contables)
   └─ Descargar XML + Auditar

7. REPORTING (FASE 7)
   ├─ Dashboard KPIs
   ├─ Informes nativos
   ├─ AuditLog
   └─ CalculationLog (trazabilidad)
```

### Puntos Críticos

- **Idempotencia:** Hash SHA-256 en staging (previene duplicados)
- **Trazabilidad:** CalculationLog inmutable por cada línea
- **Auditoría:** AuditLog en cada cambio (usuario, timestamp, antes/ahora)
- **Validación:** Alertas bloqueantes/advertencias en 5+ puntos del flujo
- **Flexibilidad:** Fórmulas AST permitie composición ilimitada (Number, Variable, Aggregate, BinaryOp, Modifier, Tramos, ConceptRef)

### Estados Finales Posibles

| Estado | Significado | Reversible |
|--------|-----------|-----------|
| **Aprobado** | Listo para exportar | Sí (rechazar en cualquier paso) |
| **Rechazado** | Retorna a Abierto para recalcular | Sí (reintentar desde PHASE 3) |
| **Exportado** | Nóminas enviadas a A3 | Sí (regenear XML) |

---

**Fin del documento.**

Para preguntas o ampliaciones: revisar `docs/ARQUITECTURA.md`, `docs/MOTOR_CALCULO.md`, `docs/INTEGRACIONES.md` en el repositorio.
