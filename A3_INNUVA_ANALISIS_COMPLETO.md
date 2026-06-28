# A3 Innuva — Análisis Completo de Flujo de Datos y Cálculo de Nóminas

**Documento Base:** FLUJO_DATOS_SIG_ES_COMPLETO.md (Secciones 3.1, 4, 9-10)  
**Fecha:** 2026-06-27  
**Status:** LISTO PARA IMPLEMENTACIÓN

---

## 1. FLUJO DE DATOS: APIs → Tablas Staging → Cálculos → Excel

```
FUENTES EXTERNAS
│
├─ Wolters Kluwer A3 INNUVA (OAuth REST)
│  └─ Lee: Conceptos, Salario Base/Neto, Empleados, IRPF, Contratos
│     → StagingA3InnuvaContrato
│
├─ PayHawk (REST API)
│  └─ Lee: Gastos (dietas, KM, taxi, comida, hotel, otros)
│     → StagingPayHawkGasto
│
├─ Bizneo (REST API)
│  └─ Lee: Empleados maestro, Ausencias (bajas, permisos, vacaciones)
│     → StagingBizneoEmpleado + StagingBizneoAbsencia
│
├─ Intratime (REST API)
│  └─ Lee: Fichajes (entrada, salida, horas calculadas)
│     → StagingIntratimeFichaje
│
├─ Celero One (PostgreSQL AlloyDB)
│  └─ Lee: Visitas de campo, Clientes, Servicios
│     → StagingCeleroVisita
│
├─ TravelPerk (REST API — Fase 2)
│  └─ Lee: Viajes corporativos (vuelos, hoteles)
│     → StagingTravelPerk
│
└─ SGPV (JSON interno)
   └─ Lee: Visitas legacy
      → StagingSgpv
│
└─ Mediapost (Excel manual)
   └─ Lee: Logística, m³, tarifas
      → Integrado en cálculos
│
▼
┌─────────────────────────────────────────────┐
│     MOTOR DE CÁLCULO (2 PASADAS)            │
├─────────────────────────────────────────────┤
│ PASADA 1: Conceptos base (independientes)   │
│ • Sueldo directo de A3                      │
│ • Visitas × tarifa (Celero)                 │
│ • KM × 0.23 (PayHawk, menos franquicia)     │
│ • Dietas (PayHawk)                          │
│ • Horas - Ausencias (Intratime - Bizneo)    │
│ • Tarifa escalonada (Intratime)             │
│ • Etc.                                      │
│                                             │
│ PASADA 2: Conceptos fee (ConceptRef)        │
│ • Fee 6.5% sobre suma conceptos base        │
│ • Comisión sobre ventas                     │
│ • Etc.                                      │
└─────────────────────────────────────────────┘
│
└─ Salida: ClosureLines + CalculationLog + Alertas
│
▼
┌─────────────────────────────────────────────┐
│     FLUJO DE APROBACIONES (5 PASOS)         │
├─────────────────────────────────────────────┤
│ 1. PendienteValidacion → BACKOFFICE         │
│ 2. PendienteGrupo → GESTOR REGIONAL         │
│ 3. PendienteFICO → CONTROL FINANCIERO       │
│ 4. PendienteDireccion → DIRECCIÓN           │
│ 5. Aprobado → LISTO PARA EXPORTAR           │
└─────────────────────────────────────────────┘
│
▼
┌─────────────────────────────────────────────┐
│    EXPORTACIÓN A WOLTERS KLUWER             │
├─────────────────────────────────────────────┤
│ POST /api/a3innuva/sync/nominas-calculadas  │
│ {                                           │
│   "empresaId": 1,                           │
│   "periodoId": 202306,                      │
│   "empleados": [                            │
│     {                                       │
│       "nif": "12345678A",                   │
│       "nombre": "Juan García",              │
│       "conceptos": {                        │
│         "Sueldo": 1500.00,                  │
│         "Visitas": 150.00,                  │
│         "KM": 72.45,                        │
│         "Dietas": 150.00,                   │
│         "Fee": 117.22                       │
│       }                                     │
│     }                                       │
│   ]                                         │
│ }                                           │
└─────────────────────────────────────────────┘
```

---

## 2. DATOS QUE NECESITAMOS DE CADA API

### A. Wolters Kluwer A3 INNUVA (OAuth)

**Endpoint:** `https://api.wolterskluwer.es/a3innuva/v1`

**Datos que TRAEMOS (lectura):**

| Campo | Tipo | Origen | Descripción |
|-------|------|--------|-------------|
| **EmpleadoId** | string | `/api/companies/{id}/employees` | ID único en WK |
| **NIF** | string | `/api/companies/{id}/employees` | Número identificación fiscal |
| **Nombre** | string | `/api/companies/{id}/employees` | Nombre completo |
| **SalarioBase** | decimal | `/api/companies/{id}/employees/{emp}/pactedsalary` | Salario mensual base |
| **SalarioNeto** | decimal | `/api/companies/{id}/employees/{emp}/pactedsalary` | Salario neto (tras IRPF/SS) |
| **IRPF%** | decimal | `/api/companies/{id}/employees/{emp}/irpfdata` | % retención IRPF |
| **CotizacionSS%** | decimal | `/api/companies/{id}/employees/{emp}/irpfdata` | % cotización SS |
| **FechaInicio** | DateOnly | `/api/companies/{id}/employees/{emp}/hiringdates` | Contrato inicia |
| **FechaFin** | DateOnly | `/api/companies/{id}/employees/{emp}/hiringdates` | Contrato finaliza (si aplica) |
| **Conceptos** | array | `/api/companies/{id}/employees/{emp}/concepts` | Lista códigos de concepto |
| **Descendientes/Ascendientes** | array | `/api/companies/{id}/employees/{emp}/irpfdata/descendants` | Para cálculo IRPF |

**Tabla Staging:** `StagingA3InnuvaContrato`

```csharp
public class StagingA3InnuvaContrato
{
    public int Id { get; set; }
    public string EmpleadoId { get; set; }        // WK ID
    public string NIF { get; set; }               // Mapeo a User.NIF
    public string Nombre { get; set; }
    public decimal SalarioBase { get; set; }
    public decimal SalarioNeto { get; set; }
    public decimal IrpfPct { get; set; }          // Porcentaje
    public decimal CotizacionSsPct { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string PayloadJson { get; set; }       // Payload completo (idempotencia)
    public string Hash { get; set; }              // SHA-256(PayloadJson) — unique
    public bool FlagProcesado { get; set; }       // = false (aún no consumido para cálculo)
    public DateTime FechaSincronizacion { get; set; }
}
```

---

### B. PayHawk (REST API)

**Endpoint:** `https://api.payhawk.com/v2`  
**Auth:** BearerToken

**Datos que TRAEMOS:**

| Campo | Tipo | Descripción | Filtro para Nómina |
|-------|------|-------------|------------------|
| **GastoIdExterno** | string | ID único en PayHawk | — |
| **UserId** | int | Mapeo a User.Id por NIF | — |
| **ServiceId** | int | Proyecto/servicio | Filtro si aplica por servicio |
| **Fecha** | DateOnly | Fecha del gasto | **Período de nómina** |
| **Importe** | decimal | Cantidad en EUR | Suma para cálculos |
| **Categoria** | string | Tipo gasto | **CRÍTICA para filtros** |
| **Descripcion** | string | Texto libre | Auditoría |

**Categorías Soportadas:**
- `dieta` → Dietas (manutención)
- `km` → Kilómetros a tarifa variable
- `taxi` → Transporte local
- `hotel` → Pernocta
- `comida` → Comidas de cliente
- `otro` → Gastos diversos

**Tabla Staging:** `StagingPayHawkGasto`

```csharp
public class StagingPayHawkGasto
{
    public int Id { get; set; }
    public string GastoIdExterno { get; set; }
    public int? UserIdMapeo { get; set; }         // Mapeo por NIF
    public int? ServiceId { get; set; }
    public DateOnly Fecha { get; set; }
    public decimal Importe { get; set; }
    public string Categoria { get; set; }         // dieta, km, taxi, hotel, comida, otro
    public string? Descripcion { get; set; }
    public string PayloadJson { get; set; }
    public string Hash { get; set; }              // Unique constraint
    public bool FlagProcesado { get; set; }
    public DateTime FechaSincronizacion { get; set; }
}
```

**Uso en Cálculos:**

```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Sum",
    "field": "Importe",
    "source": {
      "type": "Source",
      "entity": "StagingPayHawkGasto",
      "filters": [{ "field": "Categoria", "op": "Eq", "value": "dieta" }]
    }
  },
  "right": { "type": "Number", "value": 1 }
}
```

---

### C. Bizneo (REST API)

**Endpoint:** `https://api.bizneo.com/v2`  
**Auth:** ApiKey

**Datos que TRAEMOS:**

| Campo | Tipo | Descripción | Uso |
|-------|------|-------------|-----|
| **EmpleadoIdExterno** | string | ID en Bizneo | — |
| **NIF** | string | Mapeo a User.NIF | **CRÍTICO** |
| **Nombre** | string | Nombre completo | Validación |
| **Departamento** | string | Dpto RRHH | Agrupación |
| **RegistroIdExterno** | string | ID ausencia | — |
| **Fecha** | DateOnly | Día de ausencia | **Período de nómina** |
| **Horas** | decimal | Horas perdidas | Descuento de total horas |
| **Tipo** | string | Tipo ausencia | **FILTRO** |

**Tipos de Ausencia:**
- `baja` → Baja médica (descontar salario)
- `permiso` → Permiso retribuido (descontar horas)
- `vacaciones` → Vacaciones (descontar horas)
- `excedencia` → Sin paga

**Tablas Staging:**

```csharp
public class StagingBizneoEmpleado
{
    public int Id { get; set; }
    public string EmpleadoIdExterno { get; set; }
    public string NIF { get; set; }               // Mapeo a User.Id
    public string Nombre { get; set; }
    public string? Departamento { get; set; }
    public string PayloadJson { get; set; }
    public string Hash { get; set; }
    public bool FlagProcesado { get; set; }
    public DateTime FechaSincronizacion { get; set; }
}

public class StagingBizneoAbsencia
{
    public int Id { get; set; }
    public string RegistroIdExterno { get; set; }
    public int? UserIdMapeo { get; set; }         // Por NIF
    public int? ServiceId { get; set; }           // Si aplica
    public DateOnly Fecha { get; set; }
    public decimal Horas { get; set; }            // 8 = día completo
    public string Tipo { get; set; }              // baja, permiso, vacaciones, excedencia
    public string PayloadJson { get; set; }
    public string Hash { get; set; }
    public bool FlagProcesado { get; set; }
    public DateTime FechaSincronizacion { get; set; }
}
```

**Uso en Cálculos:**

```json
{
  "type": "BinaryOp",
  "op": "Sub",
  "left": {
    "type": "Aggregate",
    "op": "Sum",
    "field": "Horas",
    "source": { "type": "Source", "entity": "StagingIntratimeFichaje", "filters": [] }
  },
  "right": {
    "type": "Aggregate",
    "op": "Sum",
    "field": "Horas",
    "source": {
      "type": "Source",
      "entity": "StagingBizneoAbsencia",
      "filters": [{ "field": "Tipo", "op": "In", "value": ["baja", "permiso"] }]
    }
  }
}
```

---

### D. Intratime (REST API)

**Endpoint:** `https://api.intratime.es/v1`  
**Auth:** ApiToken

**Datos que TRAEMOS:**

| Campo | Tipo | Descripción | Cálculo |
|-------|------|-------------|---------|
| **FichajeIdExterno** | string | ID único | — |
| **UserId** | int | Mapeo a User | **CRÍTICO** |
| **Entrada** | DateTime | Marca entrada | Validación: siempre presente |
| **Salida** | DateTime | Marca salida | Validación: > Entrada |
| **HorasCalculadas** | decimal | (Salida-Entrada)/3600 | Si null: calcular |

**Tabla Staging:** `StagingIntratimeFichaje`

```csharp
public class StagingIntratimeFichaje
{
    public int Id { get; set; }
    public string FichajeIdExterno { get; set; }
    public int? UserIdMapeo { get; set; }         // Por NIF
    public DateTime Entrada { get; set; }
    public DateTime? Salida { get; set; }         // Null = jornada abierta
    public decimal? HorasCalculadas { get; set; } // (Salida-Entrada)/3600
    public string PayloadJson { get; set; }
    public string? Extra { get; set; }            // JSON: tipo jornada, etc.
    public string Hash { get; set; }
    public bool FlagProcesado { get; set; }
    public DateTime FechaSincronizacion { get; set; }
}
```

**Validaciones:**
- Si `Salida` nula → se asume jornada abierta, **NO se contabiliza**
- Si `HorasCalculadas` nula → calcular: `(Salida - Entrada).TotalHours`
- Aplicar `DateTime.SpecifyKind(..., DateTimeKind.Utc)` para timezone consistency

**Uso en Cálculos:**

```json
{
  "type": "BinaryOp",
  "op": "Mul",
  "left": {
    "type": "Aggregate",
    "op": "Sum",
    "field": "HorasCalculadas",
    "source": { "type": "Source", "entity": "StagingIntratimeFichaje", "filters": [] }
  },
  "right": { "type": "Variable", "variableId": 4 }  // TarifaHora = 18.5
}
```

---

### E. Celero One (PostgreSQL AlloyDB)

**Conexión:** PostgreSQL directo (Google Cloud IAM)

**Datos que TRAEMOS:**

| Campo | Tipo | Descripción | Crítico |
|-------|------|-------------|---------|
| **VisitaIdExterno** | string | ID único Celero | — |
| **ResourceNif** | string | NIF GPV → User.NIF | **SÍ** |
| **ServiceName** | string | Nombre servicio | Mapeo a Service |
| **MissionName** | string | Punto de venta | Extra info |
| **Fecha** | DateOnly | Fecha visita | **Período** |
| **PayloadJsonRaw** | string | JSON crudo | Ver abajo |

**Campos en PayloadJson:**

```json
{
  "tipoVisita": 2,           // 1=estándar, 2=reposición, 3=auditoría
  "puntoMontado": "Premium", // Premium, Regular, Básico
  "zona": "A",               // Zona geográfica (A, B, C, etc.)
  "km": 45.3,                // Kilómetros recorridos
  "horas": 2.5,              // Horas dedicadas
  "importe": 250.00,         // Tarifa prenegociada
  "categoria": "visita_venta",
  "idQuestion": "Q001",      // ID pregunta Celero
  "respuesta": "Sí",         // Respuesta a pregunta
  "estado": "completada",    // completada, fallida, cancelada
  "numeroVisita": 1,         // 1ª, 2ª, 3ª visita del mes
  "nocturnidad": false,      // Si > 22:00
  "pernocta": false,         // Si requiere pernoctación
  "extra": { ... }           // Campos adicionales
}
```

**Tabla Staging:** `StagingCeleroVisita`

```csharp
public class StagingCeleroVisita
{
    public int Id { get; set; }
    public string VisitaIdExterno { get; set; }
    public string ResourceNif { get; set; }      // → User.NIF mapeo
    public string ServiceName { get; set; }      // → Service.Nombre mapeo
    public string? MissionName { get; set; }
    public DateOnly Fecha { get; set; }
    public string PayloadJsonRaw { get; set; }   // JSON crudo (ParseJson para campos)
    public string Hash { get; set; }
    public bool FlagProcesado { get; set; }
    public DateTime FechaSincronizacion { get; set; }
}

// Helper para acceso a campos
public class CeleroVisitaPayload
{
    public int TipoVisita { get; set; }
    public string? PuntoMontado { get; set; }
    public string? Zona { get; set; }
    public decimal Km { get; set; }
    public decimal Horas { get; set; }
    public decimal Importe { get; set; }
    public string? Estado { get; set; }
    public int NumeroVisita { get; set; }
    public bool Nocturnidad { get; set; }
    public bool Pernocta { get; set; }
}
```

**Uso en Cálculos:**

```json
[
  {
    "name": "Visitas tipo 2 × 5€",
    "formula": {
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
  },
  {
    "name": "Km × 0.23 (menos franquicia 300)",
    "formula": {
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
  },
  {
    "name": "Días activos × 150€",
    "formula": {
      "type": "BinaryOp",
      "op": "Mul",
      "left": {
        "type": "Aggregate",
        "op": "Count",
        "distinct": "Fecha",
        "source": { "type": "Source", "entity": "VisitasCelero", "filters": [] }
      },
      "right": { "type": "Number", "value": 150 }
    }
  }
]
```

---

### F. TravelPerk (REST API — Fase 2)

**Endpoint:** `https://api.travelperk.com/v2`  
**Auth:** BearerToken  
**Status:** Pending implementation

**Datos que TRAEMOS (futura):**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| **ViajeIdExterno** | string | ID en TravelPerk |
| **UserId** | int | Mapeo a User |
| **Importe** | decimal | Costo total vuelo+hotel |
| **Fecha** | DateOnly | Inicio viaje |
| **Tipo** | string | Doméstico / Internacional |

**Tabla Staging:** `StagingTravelPerk`

---

### G. SGPV (JSON interno)

**Archivo:** `SGPV_Productos.json` en workspace

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

**Tabla Staging:** `StagingSgpv`

---

## 3. ORDEN DE SINCRONIZACIÓN (Crítico)

```
ORDEN SECUENCIAL ESTRICTO:
═════════════════════════

1️⃣ A3 INNUVA (Primero)
   └─ Obtener: Empleados, Contratos, Salarios, IRPF
   └─ Mapeo: NIF → User (validar existencia)
   └─ Duración: ~5-10 min

2️⃣ BIZNEO (Segundo)
   ├─ Obtener: Empleados (maestro), Ausencias
   ├─ Mapeo: NIF → User (actualizar datos RRHH)
   └─ Duración: ~2-5 min

3️⃣ INTRATIME (Tercero)
   ├─ Obtener: Fichajes (período completo)
   ├─ Cálculo: HorasCalculadas si null
   └─ Duración: ~3-5 min

4️⃣ PAYHAWK (Cuarto)
   ├─ Obtener: Gastos (dietas, km, etc. período completo)
   └─ Duración: ~2-5 min

5️⃣ CELERO (Quinto)
   ├─ Obtener: Visitas + Servicios (período completo)
   ├─ Extra: Preguntas Celero + respuestas para variables
   └─ Duración: ~5-10 min

6️⃣ TRAVELPERK (Sexto — Fase 2)
   ├─ Obtener: Viajes (período completo)
   └─ Duración: ~1-3 min

7️⃣ SGPV (Último)
   ├─ Cargar JSON local
   └─ Duración: <1 min
```

**¿Por qué este orden?**
- A3 Innuva primero: es el "maestro" de empleados + contratos
- Bizneo segundo: valida empleados RRHH + ausencias
- Intratime tercero: proporciona horas totales (base para descuentos)
- PayHawk cuarto: gastos dependientes de empleado validado
- Celero quinto: visitas más complejas (requiere contexto de empleado+servicio)
- TravelPerk sexto: datos menos frecuentes
- SGPV último: legacy, solo llenar huecos

---

## 4. CÁLCULOS PREVIOS (ANTES DE ENVIAR A WK)

### Motor de Cálculo: 2 Pasadas

```
PASADA 1: Conceptos Base (Independientes)
═════════════════════════════════════════

Estos cálculos NO dependen de otros conceptos.

┌─────────────────────────────────────────────────────────────────┐
│ 1. SUELDO BASE                                                  │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago                                                      │
│ Origen: A3 Innuva (directo)                                    │
│ Fórmula: Number({SalarioBase})                                 │
│ Entrada: StagingA3InnuvaContrato.SalarioBase                   │
│ Salida: ClosureLine.Importe = 1500.00                          │
│ Ejemplo: 1500.00 EUR                                           │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 2. VISITAS DE CAMPO (por tipo)                                 │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago                                                      │
│ Origen: Celero (Count + filtro TipoVisita)                     │
│ Fórmula:                                                        │
│ {                                                               │
│   "type": "BinaryOp",                                          │
│   "op": "Mul",                                                 │
│   "left": {                                                    │
│     "type": "Aggregate",                                       │
│     "op": "Count",                                             │
│     "source": {                                                │
│       "type": "Source",                                        │
│       "entity": "VisitasCelero",                               │
│       "filters": [{ "field": "tipoVisita", "op": "Eq", ...}]  │
│     }                                                           │
│   },                                                            │
│   "right": { "type": "Number", "value": 50 }                  │
│ }                                                               │
│ Entrada: StagingCeleroVisita (filtered)                        │
│ Cálculo: Count × 50 = 3 × 50 = 150.00 EUR                     │
│ Nota: Crear concepto por tipo (2→5€, 3→8€, etc.)              │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 3. KILÓMETROS (con franquicia)                                 │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago                                                      │
│ Origen: Celero (Sum km − 300 franquicia)                       │
│ Fórmula:                                                        │
│ {                                                               │
│   "type": "BinaryOp",                                          │
│   "op": "Mul",                                                 │
│   "left": {                                                    │
│     "type": "Modifier",                                        │
│     "kind": "Franquicia",                                      │
│     "threshold": 300,                                          │
│     "inner": {                                                 │
│       "type": "Aggregate",                                     │
│       "op": "Sum",                                             │
│       "field": "km",                                           │
│       "source": {                                              │
│         "type": "Source",                                      │
│         "entity": "VisitasCelero"                              │
│       }                                                         │
│     }                                                           │
│   },                                                            │
│   "right": { "type": "Number", "value": 0.23 }               │
│ }                                                               │
│ Entrada: StagingCeleroVisita.km                                │
│ Cálculo: (315 − 300) × 0.23 = 15 × 0.23 = 3.45 EUR           │
│ Si Total km < 300: devuelve 0 (Modifier con Franquicia)       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 4. DIETAS (suma gastos categoría "dieta")                      │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago                                                      │
│ Origen: PayHawk (Sum Importe, filtro categoria=dieta)          │
│ Fórmula:                                                        │
│ {                                                               │
│   "type": "Aggregate",                                         │
│   "op": "Sum",                                                 │
│   "field": "Importe",                                          │
│   "source": {                                                  │
│     "type": "Source",                                          │
│     "entity": "StagingPayHawkGasto",                           │
│     "filters": [{ "field": "Categoria", "op": "Eq", "dieta"}] │
│   }                                                             │
│ }                                                               │
│ Entrada: StagingPayHawkGasto (Categoria='dieta')               │
│ Cálculo: 150.00 EUR (suma gastos dieta)                        │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 5. HORAS TRABAJADAS NETAS (Horas − Ausencias)                  │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago (para cálculo posterior de costo por hora)          │
│ Origen: Intratime − Bizneo                                     │
│ Fórmula:                                                        │
│ {                                                               │
│   "type": "BinaryOp",                                          │
│   "op": "Sub",                                                 │
│   "left": {                                                    │
│     "type": "Aggregate",                                       │
│     "op": "Sum",                                               │
│     "field": "HorasCalculadas",                                │
│     "source": { "type": "Source", "entity": "Intratime" }      │
│   },                                                            │
│   "right": {                                                   │
│     "type": "Aggregate",                                       │
│     "op": "Sum",                                               │
│     "field": "Horas",                                          │
│     "source": {                                                │
│       "type": "Source",                                        │
│       "entity": "StagingBizneoAbsencia",                       │
│       "filters": [{ "field": "Tipo", "op": "In", ... }]        │
│     }                                                           │
│   }                                                             │
│ }                                                               │
│ Entrada: StagingIntratimeFichaje.HorasCalculadas −             │
│          StagingBizneoAbsencia.Horas                            │
│ Cálculo: (160 − 8) = 152 horas                                 │
│ Nota: No es salario directo, sino dato para otros cálculos     │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 6. TARIFA ESCALONADA (p.ej., Intratime horas)                  │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago                                                      │
│ Origen: Intratime (Tramos)                                     │
│ Fórmula:                                                        │
│ {                                                               │
│   "type": "Tramos",                                            │
│   "tramos": [                                                  │
│     { "hasta": 1, "precio": 90 },    // 1ª hora 90 EUR         │
│     { "hasta": null, "precio": 37 }  // siguientes 37 EUR      │
│   ],                                                            │
│   "inner": {                                                   │
│     "type": "Aggregate",                                       │
│     "op": "Sum",                                               │
│     "field": "HorasCalculadas",                                │
│     "source": { "type": "Source", "entity": "Intratime" }      │
│   }                                                             │
│ }                                                               │
│ Entrada: StagingIntratimeFichaje.HorasCalculadas               │
│ Cálculo: Si 3 horas → 1×90 + 2×37 = 164 EUR                   │
│ Nota: "hasta" null significa "infinito" (último tramo)         │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│ 7. OTROS GASTOS (taxi, hotel, comida, etc.)                    │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago                                                      │
│ Origen: PayHawk (por categoría)                                │
│ Fórmula: Sum(Importe, categoria=taxi/hotel/comida/otro)        │
│ Entrada: StagingPayHawkGasto (Categoria específica)            │
│ Cálculo: Suma por categoría (igual a dietas)                   │
└─────────────────────────────────────────────────────────────────┘

SUBTOTAL PASADA 1: 
═════════════════
Sueldo:        1500.00
Visitas:        150.00
KM:               3.45
Dietas:         150.00
────────────────────────
TOTAL:         1803.45 EUR


PASADA 2: Conceptos Fee (Dependientes de Pasada 1)
═══════════════════════════════════════════════════

Estos cálculos DEPENDEN de importes calculados en Pasada 1.

┌─────────────────────────────────────────────────────────────────┐
│ FEE 6.5% SOBRE TOTAL NÓMINA BASE                               │
├─────────────────────────────────────────────────────────────────┤
│ Tipo: Pago                                                      │
│ Origen: ConceptRef (referencias conceptos base)                │
│ Fórmula:                                                        │
│ {                                                               │
│   "type": "BinaryOp",                                          │
│   "op": "Mul",                                                 │
│   "left": {                                                    │
│     "type": "ConceptRef",                                      │
│     "conceptIds": []  // vacío = todos conceptos base           │
│   },                                                            │
│   "right": { "type": "Number", "value": 0.065 }               │
│ }                                                               │
│ Entrada: Importes previos {ConceptoId → Importe}              │
│ Disponible en CalculationTarget.ImportesPrevios                │
│ Cálculo: 1803.45 × 0.065 = 117.22 EUR                         │
│ Nota: Se evalúa DESPUÉS de Pasada 1                            │
└─────────────────────────────────────────────────────────────────┘

TOTAL NÓMINA FINAL:
═══════════════════
Sueldo:        1500.00
Visitas:        150.00
KM:               3.45
Dietas:         150.00
Fee 6.5%:       117.22
────────────────────────
TOTAL:         1920.67 EUR
```

---

## 5. CAMPOS FINALES EN NÓMINA CALCULADA (Excel Descargable)

**Nota:** Este Excel NO se envía directamente a Wolters Kluwer. Es para **DESCARGA MANUAL** del usuario antes de enviarlo manualmente a WK.

### Estructura de filas y columnas:

```
╔═══════════════════════════════════════════════════════════════════════════════════════════════════════════╗
║                            NÓMINA CALCULADA — PERÍODO JUNIO 2026                                         ║
╚═══════════════════════════════════════════════════════════════════════════════════════════════════════════╝

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ ENCABEZADO                                                                                              │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Empresa:           SIG ES (A3 Innuva ID: 1)                                                             │
│ Período:           Junio 2026 (202306)                                                                  │
│ Fecha Inicio:      2026-06-01                                                                           │
│ Fecha Fin:         2026-06-30                                                                           │
│ Fecha Generación:  2026-06-27 10:30:00                                                                  │
│ Usuario Generador: nallibe-hktech                                                                       │
│ Estado:            Aprobado (PendienteFICO)                                                             │
└─────────────────────────────────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ TABLA DE EMPLEADOS Y CONCEPTOS                                                                          │
├────────────┬──────────────┬──────────┬──────────┬──────────┬──────────┬──────────┬────────┬─────────────┤
│ NIF        │ Nombre       │ Sueldo   │ Visitas  │ Km       │ Dietas   │ Fee 6.5% │ TOTAL  │ IRPF %      │
├────────────┼──────────────┼──────────┼──────────┼──────────┼──────────┼──────────┼────────┼─────────────┤
│ 12345678A  │ Juan García  │ 1500.00  │ 150.00   │ 3.45     │ 150.00   │ 117.22   │ 1920.67│ 19.0% → 364.93│
│ 87654321B  │ María López  │ 1800.00  │ 200.00   │ 0.00     │ 175.00   │ 130.03   │ 2305.03│ 19.0% → 437.95│
│ 11223344C  │ Pedro Martín │ 1600.00  │ 100.00   │ 8.90     │ 125.00   │ 104.86   │ 1938.76│ 19.0% → 368.36│
├────────────┼──────────────┼──────────┼──────────┼──────────┼──────────┼──────────┼────────┼─────────────┤
│ TOTAL      │ 3 empleados  │ 4900.00  │ 450.00   │ 12.35    │ 450.00   │ 352.11   │ 6164.46│ 1171.24     │
└────────────┴──────────────┴──────────┴──────────┴──────────┴──────────┴──────────┴────────┴─────────────┘

┌─────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ DETALLES POR EMPLEADO (Pestañas separadas o secciones)                                                  │
├─────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ EMPLEADO: Juan García (12345678A)                                                                       │
│                                                                                                         │
│ ┌─ DATOS A3 INNUVA ──────────────────────┐                                                             │
│ │ Salario Base (contrato):    1500.00    │                                                             │
│ │ Salario Neto WK:            1500.00    │                                                             │
│ │ IRPF %:                     19.0%      │                                                             │
│ │ Cotización SS %:            6.35%      │                                                             │
│ │ Fecha inicio contrato:      2024-01-15 │                                                             │
│ │ Fecha fin contrato:         (vigente)  │                                                             │
│ └────────────────────────────────────────┘                                                             │
│                                                                                                         │
│ ┌─ DATOS CELERO (VISITAS) ────────────────────────────────────────────────────────────────────────────┐│
│ │ Tipo 2 (Reposición):  3 × 50€ = 150.00                                                    │││
│ │ Detalle:  Visita 1 (zona A, 05-jun), Visita 2 (zona B, 10-jun), Visita 3 (zona A, 20-jun)││
│ │ Km totales: 45.3 km                                                                       │││
│ │ Km descuento (franquicia 300): 0 km (inferior a franquicia)                                │││
│ │ Km facturables: 0 × 0.23€ = 0.00 (pero ver línea separada)                                │││
│ └──────────────────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                                         │
│ ┌─ DATOS PAYHAWK (GASTOS) ────────────────────────────────────────────────────────────────────────────┐│
│ │ Dietas (manutención):    150.00€  (5 días × 30€)                                          │││
│ │ Km por gasto:            0.00€    (no registrados en PayHawk, usar Celero)                │││
│ │ Taxi:                    0.00€                                                             │││
│ │ Hotel:                   0.00€                                                             │││
│ │ Comida cliente:          0.00€                                                             │││
│ │ Otros:                   0.00€                                                             │││
│ └──────────────────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                                         │
│ ┌─ DATOS INTRATIME (FICHAJES) ────────────────────────────────────────────────────────────────────────┐│
│ │ Horas totales:           160.00 h                                                           │││
│ │ Ausencias (Bizneo):      -8.00 h  (1 baja médica)                                          │││
│ │ Horas netas:             152.00 h                                                           │││
│ │ Tarifa por hora:         18.50€   (Variable TarifaHora)                                    │││
│ │ Costo horas:             152 × 18.50 = 2816.00€  (NO va a nómina, es para contabilidad)   │││
│ └──────────────────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                                         │
│ ┌─ DATOS BIZNEO (AUSENCIAS) ──────────────────────────────────────────────────────────────────────────┐│
│ │ Baja médica (1-jun):     -8.00 h                                                            │││
│ │ Permiso retribuido:      0 h                                                                │││
│ │ Vacaciones:              0 h                                                                │││
│ │ Total ausencias:         -8.00 h                                                            │││
│ └──────────────────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                                         │
│ ┌─ CÁLCULO NÓMINA ────────────────────────────────────────────────────────────────────────────────────┐│
│ │                                                                                             │││
│ │ Concepto 1 (Sueldo Base):                          1500.00€  (A3 Innuva)                   │││
│ │ Concepto 2 (Visitas tipo 2):                        150.00€  (3 × 50€ Celero)              │││
│ │ Concepto 3 (Kilometraje):                             3.45€  ((315-300) × 0.23 Celero)    │││
│ │ Concepto 4 (Dietas):                                150.00€  (PayHawk)                     │││
│ │ ─────────────────────────────────────────────────────────────────                         │││
│ │ Subtotal (PASADA 1):                              1803.45€                                 │││
│ │                                                                                             │││
│ │ Concepto 5 (Fee 6.5% sobre base):                  117.22€  (1803.45 × 0.065)              │││
│ │ ─────────────────────────────────────────────────────────────────                         │││
│ │ TOTAL NÓMINA BRUTA:                               1920.67€                                 │││
│ │                                                                                             │││
│ │ Retención IRPF (19.0%):                           -364.93€                                 │││
│ │ Cotización SS (6.35%):                            -121.86€                                 │││
│ │ ─────────────────────────────────────────────────────────────────                         │││
│ │ TOTAL NÓMINA NETA (a pagar):                      1433.88€                                 │││
│ └──────────────────────────────────────────────────────────────────────────────────────────────┘│
│                                                                                                         │
│ ┌─ VALIDACIONES ──────────────────────────────────────────────────────────────────────────────────────┐│
│ │ ✓ Empleado con contrato vigente en período                                                 │││
│ │ ✓ Conceptos aplicables: 5 (todos vigentes)                                                 │││
│ │ ✓ Datos de todas las fuentes disponibles                                                   │││
│ │ ⚠ Aviso: Km PayHawk no disponibles (usar Celero)                                           │││
│ │ ✓ Totales cuadran: 1803.45 + 117.22 = 1920.67                                              │││
│ └──────────────────────────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

### Campos de la tabla principal:

```
COLUMNA: NIF
├─ Tipo: string (13 caracteres)
├─ Origen: User.NIF
├─ Ejemplo: "12345678A"
└─ Validación: Debe coincidir con A3 Innuva

COLUMNA: Nombre Empleado
├─ Tipo: string
├─ Origen: User.Nombre + User.Apellidos
├─ Ejemplo: "Juan García"
└─ Auditoría: Importante para verificación manual

COLUMNA: [Cada Concepto]
├─ Tipo: decimal (2 decimales)
├─ Origen: ClosureLine.Importe (ConceptoId = X)
├─ Validación: >= 0 (excepto overrides negativos)
└─ Nota: Una columna por cada Concepto del período

COLUMNA: TOTAL
├─ Tipo: decimal
├─ Cálculo: Suma de todas las columnas de concepto
├─ Ejemplo: 1920.67
└─ Validación: Debe cuadrar con suma ClosureLines

COLUMNA: IRPF % → Importe
├─ Tipo: decimal
├─ Cálculo: TOTAL × (IRPF% de A3 Innuva)
├─ Ejemplo: 1920.67 × 19% = 364.93
└─ Origen: StagingA3InnuvaContrato.IrpfPct

COLUMNA: Cotización SS % → Importe
├─ Tipo: decimal
├─ Cálculo: TOTAL × (CotizacionSS% de A3 Innuva)
├─ Ejemplo: 1920.67 × 6.35% = 121.86
└─ Origen: StagingA3InnuvaContrato.CotizacionSsPct

COLUMNA: NETO (a pagar)
├─ Tipo: decimal
├─ Cálculo: TOTAL − IRPF − CotizacionSS
├─ Ejemplo: 1920.67 − 364.93 − 121.86 = 1433.88
└─ Nota: Este es el monto que realmente se paga al empleado
```

---

## 6. SECUENCIA DE SINCRONIZACIÓN DETALLADA

```csharp
// Pseudo-código del orden exacto
public async Task SincronizarNominasCompleto(int periodoId, CancellationToken ct)
{
    var periodo = await _db.Periods.FirstAsync(p => p.Id == periodoId, ct);
    
    if (periodo.Estado != EstadoPeriod.Abierto)
        throw new PeriodNotOpenException("Período debe estar abierto");

    try
    {
        // PASO 1: A3 INNUVA (Maestro)
        _logger.LogInformation("Sincronizando A3 Innuva...");
        var contratosA3 = await _a3InnuvaClient.GetContratosAsync(
            empresaId: 1,
            periodo: new DateOnly(2026, 6, 1),
            ct);
        
        foreach (var contrato in contratosA3)
        {
            var hash = ComputeSHA256(JsonConvert.SerializeObject(contrato));
            var exists = await _db.StagingA3InnuvaContrato
                .AnyAsync(s => s.Hash == hash, ct);
            
            if (!exists)
            {
                var staging = new StagingA3InnuvaContrato
                {
                    EmpleadoId = contrato.EmpleadoId,
                    NIF = contrato.NIF,
                    Nombre = contrato.Nombre,
                    SalarioBase = contrato.SalarioBase,
                    SalarioNeto = contrato.SalarioNeto,
                    IrpfPct = contrato.IrpfPct,
                    CotizacionSsPct = contrato.CotizacionSsPct,
                    FechaInicio = contrato.FechaInicio,
                    FechaFin = contrato.FechaFin,
                    PayloadJson = JsonConvert.SerializeObject(contrato),
                    Hash = hash,
                    FlagProcesado = false,
                    FechaSincronizacion = DateTime.UtcNow
                };
                _db.StagingA3InnuvaContrato.Add(staging);
            }
        }
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation($"✓ A3 Innuva: {contratosA3.Count} contratos sincronizados");

        // PASO 2: BIZNEO (RRHH)
        _logger.LogInformation("Sincronizando Bizneo...");
        var empleadosBizneo = await _bizneoClient.GetEmpleadosAsync(ct);
        var ausenciasBizneo = await _bizneoClient.GetAbsencesAsync(
            periodo.FechaInicio,
            periodo.FechaFin,
            ct);
        
        // (Sincronizar empleados y ausencias en tablas staging)
        _logger.LogInformation($"✓ Bizneo: {empleadosBizneo.Count} empleados, " +
                              $"{ausenciasBizneo.Count} ausencias");

        // PASO 3: INTRATIME (Fichajes)
        _logger.LogInformation("Sincronizando Intratime...");
        var fichajes = await _intratimeClient.GetFichajesAsync(
            periodo.FechaInicio,
            periodo.FechaFin,
            ct);
        
        // (Sincronizar en StagingIntratimeFichaje)
        _logger.LogInformation($"✓ Intratime: {fichajes.Count} fichajes");

        // PASO 4: PAYHAWK (Gastos)
        _logger.LogInformation("Sincronizando PayHawk...");
        var gastos = await _payHawkClient.GetGastosAsync(
            periodo.FechaInicio,
            periodo.FechaFin,
            ct);
        
        // (Sincronizar en StagingPayHawkGasto)
        _logger.LogInformation($"✓ PayHawk: {gastos.Count} gastos");

        // PASO 5: CELERO (Visitas)
        _logger.LogInformation("Sincronizando Celero...");
        var visitas = await _celeroClient.GetVisitasAsync(
            periodo.FechaInicio,
            periodo.FechaFin,
            ct);
        
        // (Sincronizar en StagingCeleroVisita)
        _logger.LogInformation($"✓ Celero: {visitas.Count} visitas");

        // PASO 6: TRAVELPERK (Futura)
        // _logger.LogInformation("Sincronizando TravelPerk...");
        // var viajes = await _travelPerkClient.GetViagesAsync(...);
        
        // PASO 7: SGPV (Local JSON)
        _logger.LogInformation("Cargando SGPV...");
        var sgpvData = await LoadSgpvJsonAsync(ct);
        _logger.LogInformation($"✓ SGPV: {sgpvData.Count} registros");

        // TRIGGER: Crear cierres y calcular
        _logger.LogInformation("Disparando motor de cálculo...");
        await _closureService.CreateAndCalculateBatchAsync(periodoId, ct);
        
        _logger.LogInformation("✓ Sincronización completa");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error en sincronización");
        throw;
    }
}
```

---

## 7. DATOS QUE NO VIENEN DE WK (Pero son CRÍTICOS)

| Categoría | Dato | Sistema | Uso en Nómina | Obligatorio |
|-----------|------|--------|---------------|-----------|
| **Movimiento** | Kilómetros | Celero | Concepto "KM × 0.23€" | SÍ |
| **Movimiento** | KM por gasto | PayHawk | Fallback a Celero | NO |
| **Tiempo** | Horas trabajadas | Intratime | Base para descuentos | SÍ |
| **Tiempo** | Ausencias | Bizneo | Descuento de horas | SÍ |
| **Tiempo** | Vacaciones | Bizneo | Descuento de horas | SÍ |
| **Reembolsos** | Dietas | PayHawk | Concepto "Dietas" | SÍ |
| **Reembolsos** | Taxi/Hotel/Comida | PayHawk | Conceptos varios | NO |
| **Visitas** | Nº visitas | Celero | Concepto "Visitas × tarifa" | SÍ |
| **Visitas** | Tipo visita | Celero | Filtro para concepto | SÍ |
| **Visitas** | Zona geográfica | Celero | Filtro tarifa por zona | NO |
| **Logística** | m³ transportados | Mediapost/Galán | Concepto "Logística" | NO |
| **Logística** | Tarifa logística | Configuración | Multiplicador | NO |
| **Viajes** | Vuelos/Hoteles | TravelPerk | Concepto "Viajes" (Fase 2) | NO |
| **Preguntas** | Respuestas Celero | Celero | Variables por idQuestion | NO |
| **Datos maestros** | Empleados | Bizneo | Validación + actualización RRHH | NO |
| **Datos maestros** | Contratos | Bizneo | Validación vigencia | NO |

---

## 8. VALIDACIONES CRÍTICAS ANTES DE ENVIAR A WK

```
CHECKLIST PRE-EXPORTACIÓN A WOLTERS KLUWER
═══════════════════════════════════════════

1. DATOS INCOMPLETOS
   ☐ ¿Todos los empleados (de A3) tienen fichajes en Intratime?
   ☐ ¿Todos los empleados tienen contrato vigente en período?
   ☐ ¿Hay empleados sin NIF válido?
   ☐ ¿Hay conceptos aplicables al período?

2. CÁLCULOS CORRECTOS
   ☐ Total nómina > 0
   ☐ Suma ClosureLines = Total cierre
   ☐ IRPF + SS < Total bruto
   ☐ Fee 6.5% calculado correctamente (Pasada 2)
   ☐ Franquicias aplicadas correctamente

3. ALERTAS BLOQUEANTES
   ☐ No hay alertas con TipoAlerta=Bloqueante sin confirmar
   ☐ Líneas sin empleado asignado (si tipo Pago): confirmadas
   ☐ División por cero: NO existe

4. AUDITORÍA
   ☐ AuditLog registra cada cambio
   ☐ CalculationLog completo para cada línea
   ☐ ApprovalHistory documentada (5 pasos)
   ☐ Usuario y timestamp en cada acción

5. ESTADO APROBACIÓN
   ☐ Closure.Estado == Aprobado (paso 5 completado)
   ☐ Dirección autorizó (último paso)
   ☐ Fecha aprobación registrada

6. INTEGRIDAD DATOS
   ☐ Período no está Bloqueado
   ☐ Sin corrupción de datos (verificar hashes)
   ☐ Sincronizaciones completadas sin errores
   ☐ Sin duplicados en staging (constraint hash respetado)
```

---

## 9. RESUMEN EJECUTIVO

### Flujo Resumido:

1. **Sincronizar** 7 APIs en orden estricto (A3 → Bizneo → Intratime → PayHawk → Celero → TravelPerk → SGPV)
2. **Preparar** datos: validar empleados, servicios, períodos; crear cierres vacíos
3. **Calcular** en 2 pasadas: conceptos base (independientes) + conceptos fee (ConceptRef)
4. **Validar** alertas bloqueantes; descartar incidencias
5. **Override** líneas excepcionales con motivo auditable
6. **Aprobar** en 5 pasos: Validación → Grupo → FICO → Dirección → Aprobado
7. **Exportar** Excel descargable (NO directo a WK)
8. **Enviar** manualmente a Wolters Kluwer vía API PostNominasCalculadasAsync

### Campos finales en Excel:

```
NIF | Nombre | [Concepto 1] | [Concepto 2] | ... | TOTAL | IRPF | SS | NETO
```

### Orden crítico de sincronización:

```
A3 Innuva → Bizneo → Intratime → PayHawk → Celero → TravelPerk → SGPV
```

### Datos NO de WK (obligatorios):

- **KM** (Celero, Celero.km)
- **Ausencias** (Bizneo, Bizneo.Ausencia.Horas)
- **Horas trabajadas** (Intratime, Intratime.HorasCalculadas)
- **Dietas** (PayHawk, PayHawk.Importe con Categoria="dieta")
- **Visitas** (Celero, Celero.Count)

---

**Fin del análisis. Listo para implementación.**
