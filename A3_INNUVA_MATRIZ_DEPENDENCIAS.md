# A3 Innuva — Matriz de Dependencias Completa

**Versión:** 2.0 | **Fecha:** 2026-06-27 | **Basado en:** FLUJO_DATOS_SIG_ES_COMPLETO.md § 6

---

## 1. TABLA COMPLETA: Concepto × Fuente × Cálculo

```
┌────────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Concepto | Tipo     │ Fuente Staging         │ Filtros           │ Cálculo/Agregado    │ Imputación   │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Sueldo Base │ Pago │ StagingA3InnuvaContrato │ Período vigencia  │ Directo (SalarioBase)│ Empleado     │
│           │        │ → campo SalarioBase     │                   │                     │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Visitas de │ Pago   │ StagingCeleroVisita    │ tipoVisita=2,     │ Count(Visitas) ×    │ Empleado     │
│ campo tipo │        │ → Count registro       │ estado=completada │ 50€                 │              │
│ 2          │        │                        │ Período           │ = 3 × 50 = 150€    │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Visitas    │ Pago   │ StagingCeleroVisita    │ tipoVisita=3,     │ Count(Visitas) ×    │ Empleado     │
│ auditoría  │        │ → Count registro       │ estado=completada │ 80€                 │              │
│ tipo 3     │        │                        │ Período           │ = 2 × 80 = 160€    │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Kilometraje│ Pago   │ StagingCeleroVisita    │ Período           │ Modifier[Franquicia │ Empleado     │
│            │        │ → Sum(km)              │ zona opcional     │ threshold=300]      │              │
│            │        │                        │ estado=completada │ × 0.23€/km          │              │
│            │        │                        │                   │ = (315-300)×0.23    │              │
│            │        │                        │                   │ = 3.45€             │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Dietas     │ Pago   │ StagingPayHawkGasto    │ categoria=dieta   │ Sum(Importe)        │ Empleado     │
│            │        │ → Sum(Importe)         │ Período           │ = 150€              │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Horas      │ Pago   │ StagingIntratimeFichaje│ Período           │ Sum(HorasCalculadas)│ Empleado     │
│ trabajadas │        │ → Sum(HorasCalculadas) │ Salida NOT NULL   │ = 160 h             │ (informativo)│
│            │        │                        │                   │ (se usa para dtos)  │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Ausencias  │ Pago   │ StagingBizneoAbsencia  │ tipo IN           │ Sum(Horas)          │ Empleado     │
│ (negativo) │        │ → Sum(Horas)           │ (baja,permiso)    │ × TarifaHora        │ (descuento)  │
│            │        │                        │ Período           │ = -8 × 18.5 = -148€ │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Tarifa por │ Pago   │ StagingCeleroVisita    │ zona="A" ó "B",   │ Count(Visitas) ×    │ Empleado     │
│ zona A     │        │ → Count registro       │ Período           │ 75€                 │              │
│            │        │                        │ estado=completada │ = 3 × 75 = 225€    │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Tarifa     │ Pago   │ StagingIntratimeFichaje│ Período           │ Tramos:             │ Empleado     │
│ escalonada │        │ → Sum(HorasCalculadas) │                   │ 1ª h: 90€           │              │
│ horas      │        │                        │                   │ 2ª+: 37€/h          │              │
│            │        │                        │                   │ 3h → 1×90+2×37=164€ │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Fee sobre  │ Pago   │ ConceptRef (Pasada 2)  │ Pasada 1 completa │ Sum(ImportesBase)   │ Empleado     │
│ conceptos  │        │ → Referencias           │ conceptIds=[]     │ × 6.5%              │              │
│ base       │        │                        │ (todos)           │ 1803.45 × 0.065     │              │
│            │        │                        │                   │ = 117.22€           │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Refactación│ Factura│ Presupuesto o Galán    │ Período           │ Coste × (1+Margen%) │ Cliente      │
│ cliente    │        │ (Fase 2)               │                   │ 1000 × 1.15 = 1150€ │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Cuota      │ Factura│ Presupuesto (maestro)  │ Período vigencia  │ Fijo                │ Cliente      │
│ servicio   │        │                        │                   │ = 5000€             │              │
│ mensual    │        │                        │                   │                     │              │
├────────────────────────────────────────────────────────────────────────────────────────────────────────┤
│ Sobrefactu │ Factura│ Mediapost               │ Período           │ m³ × Tarifa × (1+%) │ Cliente      │
│ ra logística│       │ (Excel manual)          │                   │ 50 × 2.5 × 1.1      │              │
│            │        │                        │                   │ = 137.50€           │              │
└────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. MAPA DE ORIGEN POR TIPO DE DATO

```
╔════════════════════════════════════════════════════════════════╗
║               DATOS DE ENTRADA (Staging)                      ║
╚════════════════════════════════════════════════════════════════╝

TIPO              │ SISTEMA        │ TABLA STAGING              │ CAMPOS CRÍTICOS
─────────────────┼────────────────┼───────────────────────────┼──────────────────────
EMPLEADO          │ A3 Innuva      │ StagingA3InnuvaContrato   │ NIF, SalarioBase, 
                  │ (maestro)      │                           │ FechaInicio/Fin, IRPF%
                  │                │                           │
HORAS TRABAJADAS  │ Intratime      │ StagingIntratimeFichaje   │ Entrada, Salida,
                  │                │                           │ HorasCalculadas
                  │                │                           │
AUSENCIAS         │ Bizneo         │ StagingBizneoAbsencia     │ Horas, Tipo (baja,
                  │ (RRHH)         │                           │ permiso, vacaciones)
                  │                │                           │
GASTOS DIETA      │ PayHawk        │ StagingPayHawkGasto       │ Importe, Categoria
TAXI/HOTEL/COMIDA │                │                           │ (dieta/taxi/hotel)
OTROS             │                │                           │
                  │                │                           │
VISITAS CAMPO     │ Celero         │ StagingCeleroVisita       │ km, horas, tipoVisita,
KM VISITAS        │ (PostgreSQL)   │                           │ zona, estado, 
ZONA              │                │                           │ numeroVisita
                  │                │                           │
VIAJES            │ TravelPerk     │ StagingTravelPerk         │ Importe, Tipo
(Fase 2)          │                │ (Pending)                 │ (doméstico/intl)
                  │                │                           │
DATOS LEGACY      │ SGPV           │ StagingSgpv               │ visitaIdExterno,
                  │ (JSON local)   │                           │ importe
```

---

## 3. FLUJO: Staging → Cálculo → Salida

```
ENTRADA (Staging)
│
├─ StagingA3InnuvaContrato
│  ├─ NIF → User (validación)
│  ├─ SalarioBase → Concepto "Sueldo"
│  ├─ IrpfPct → Multiplicador neto
│  └─ CotizacionSsPct → Descuento SS
│
├─ StagingPayHawkGasto
│  ├─ Categoria="dieta" → Concepto "Dietas" (Sum Importe)
│  ├─ Categoria="km" → Fallback a Celero
│  ├─ Categoria="taxi/hotel/comida" → Conceptos "Gastos"
│  └─ Fecha → Filtro período
│
├─ StagingBizneoAbsencia
│  ├─ Horas → Descuento horas (Ausencias negativas)
│  ├─ Tipo → Filtro (baja vs permiso vs vacaciones)
│  └─ Fecha → Filtro período
│
├─ StagingIntratimeFichaje
│  ├─ HorasCalculadas → Agregado Sum para conceptos
│  ├─ (Salida − Entrada)/3600 si null
│  └─ Fecha implícita en período
│
├─ StagingCeleroVisita
│  ├─ km → Concepto "Km" (Sum con Franquicia)
│  ├─ tipoVisita → Filtro (concepto distinto por tipo)
│  ├─ zona → Filtro tarifa por zona
│  ├─ numeroVisita → Descuento 2ª visita
│  ├─ nocturnidad/pernocta → Multiplicadores
│  └─ estado → Filtro (completada only)
│
└─ StagingSgpv / Mediapost (Legacy)
   └─ Datos fallback si falta fuente principal

        ↓ MOTOR 2-PASADAS

PASADA 1: Conceptos Base (independientes)
│
├─ EvaluarFórmula(Concepto.FormulaJson, staging)
├─ → ClosureLine[ConceptoId] = Importe calculado
└─ → Guardar en ImportesPrevios[ConceptoId]

PASADA 2: Conceptos Fee (ConceptRef)
│
├─ EvaluarFórmula(Concepto.FormulaJson, ImportesPrevios)
└─ → ClosureLine[ConceptoId fee] = Suma × Factor

        ↓ SALIDA (Excel Descargable)

NIF | Nombre | Concepto1 | Concepto2 | ... | TOTAL | IRPF | SS | NETO
────────────────────────────────────────────────────────────────────────
12345A | Juan | 1500 | 150 | 3.45 | ... | 1920.67 | 364.93 | 121.86 | 1433.88
```

---

## 4. TABLA: Filtros Aplicados por Concepto

```
CONCEPTO              │ FILTRO 1          │ FILTRO 2           │ FILTRO 3          │ FILTRO 4
──────────────────────┼───────────────────┼────────────────────┼───────────────────┼─────────
Sueldo                │ (directo A3)      │ —                  │ —                 │ —
Visitas tipo 2        │ tipoVisita=2      │ estado=completada  │ Período           │ —
Visitas tipo 3        │ tipoVisita=3      │ estado=completada  │ Período           │ —
KM                    │ (todos)           │ estado=completada  │ Período           │ —
Dietas                │ categoria=dieta   │ Período            │ —                 │ —
Ausencias descuento   │ tipo IN (baja, permiso)  │ Período     │ —                 │ —
Tarifa zona A         │ zona="A"          │ estado=completada  │ Período           │ —
Tarifa escalonada     │ (todos)           │ Período            │ —                 │ —
Fee                   │ ConceptRef (Pasada 1)    │ —             │ —                 │ —
```

---

## 5. TABLA: Validaciones por Concepto

```
CONCEPTO       │ VALIDACIÓN             │ TIPO ALERTA   │ ACCIÓN
───────────────┼────────────────────────┼───────────────┼──────────────────────────
Sueldo         │ Existe contrato vigente│ Bloqueante    │ Rechazar si NIT no existe
               │ en período             │               │
───────────────┼────────────────────────┼───────────────┼──────────────────────────
Visitas        │ Aggregate devuelve 0   │ Advertencia   │ Crear alerta "Sin visitas"
               │                        │               │
───────────────┼────────────────────────┼───────────────┼──────────────────────────
KM             │ Sum(km) < Franquicia   │ Advertencia   │ KM = 0 (normal si < 300)
               │                        │               │
───────────────┼────────────────────────┼───────────────┼──────────────────────────
Dietas         │ Aggregate devuelve 0   │ Advertencia   │ Crear alerta "Sin dietas"
               │                        │               │
───────────────┼────────────────────────┼───────────────┼──────────────────────────
Horas netas    │ Ausencias > Horas total│ Bloqueante    │ Datos inconsistentes
               │                        │               │
───────────────┼────────────────────────┼───────────────┼──────────────────────────
Fee            │ Pasada 1 debe estar    │ Bloqueante    │ Validar ordem de pasadas
               │ completa               │               │
───────────────┼────────────────────────┼───────────────┼──────────────────────────
TOTAL          │ Suma líneas = Importe  │ Bloqueante    │ Rechazar si no cuadra
               │ cierre                 │               │
```

---

## 6. TABLA: Transformación Staging → RowAdapter → Salida

```
STAGING (Crudo)                │ ROWADAPTER (Intermedio)    │ CLOSURELINE (Salida)
───────────────────────────────┼────────────────────────────┼──────────────────────
StagingCeleroVisita {          │ RowAdapter {               │ ClosureLine {
  VisitaIdExterno: "CEL123"    │   UserId: 5 (por NIF)      │   ClosureId: 1
  ResourceNif: "12345678A"     │   ServiceId: 10            │   ConceptoId: 2
  Fecha: 2026-06-05            │   Fecha: 2026-06-05        │   Importe: 150.00
  PayloadJsonRaw: {            │   Visitas: 1               │   EsOverride: false
    km: 45.3,                  │   Km: 45.3,                │   CalculationLog: {...}
    tipoVisita: 2,             │   Horas: 2.5,              │ }
    zona: "A",                 │   Extra: {
    estado: "completada"       │     tipoVisita: 2,         
  }                            │     zona: "A"
}                              │   }
                               │ }
```

---

## 7. Matriz: Conceptos × Pasadas

```
                    │ PASADA 1           │ PASADA 2
                    │ (Base)             │ (Fee/Dependientes)
────────────────────┼────────────────────┼─────────────────────
Sueldo              │ ✓ Se calcula       │ —
Visitas             │ ✓ Se calcula       │ —
KM                  │ ✓ Se calcula       │ —
Dietas              │ ✓ Se calcula       │ —
Fee 6.5%            │ —                  │ ✓ Usa importes Pasada 1
Tarifa escalonada   │ ✓ Se calcula       │ —
Ausencias           │ ✓ Se calcula       │ —
Refacturación       │ (Factura, no Pago) │ —
Comisión ventas     │ —                  │ ✓ Usa bases Pasada 1
```

**Nota:** ConceptRef SOLO se evalúa después de Pasada 1 (depende de ImportesPrevios).

---

## 8. Matriz: Campos Agregables por Tabla Staging

```
TABLA                      │ CAMPO AGGREGABLE  │ AGREGADO SOPORTADO │ EJEMPLO
───────────────────────────┼──────────────────┼────────────────────┼──────────────
StagingPayHawkGasto        │ Importe          │ Sum, Count, Min     │ Sum(Importe)
                           │                  │ Max, Avg           │ = 150€
───────────────────────────┼──────────────────┼────────────────────┼──────────────
StagingIntratimeFichaje    │ HorasCalculadas  │ Sum, Min, Max, Avg  │ Sum(Horas)
                           │                  │                    │ = 160 h
───────────────────────────┼──────────────────┼────────────────────┼──────────────
StagingBizneoAbsencia      │ Horas            │ Sum, Min, Max, Avg  │ Sum(Horas)
                           │                  │                    │ = 8 h
───────────────────────────┼──────────────────┼────────────────────┼──────────────
StagingCeleroVisita        │ km               │ Sum, Min, Max, Avg  │ Sum(km) = 315
                           │ horas            │ Count (registros)   │ Count = 3
                           │ (cualquier campo)│ Distinct(Fecha)     │ Distinct = 5
───────────────────────────┼──────────────────┼────────────────────┼──────────────
StagingA3InnuvaContrato    │ (directo, no agr)│ —                  │ SalarioBase
                           │                  │                    │ = 1500€
```

---

## 9. Matriz: Modificadores Aplicables

```
MODIFICADOR    │ LÓGICA                    │ USO TÍPICO        │ EJEMPLO
───────────────┼───────────────────────────┼───────────────────┼─────────────────
Min (suelo)    │ Si inner < threshold      │ Cantidad mínima    │ Min[250](100)
               │ → threshold               │ facturación        │ = 250
               │                           │                    │
Max (techo)    │ Si inner > threshold      │ Cantidad máxima    │ Max[2000](2500)
               │ → threshold               │ (caps)             │ = 2000
               │                           │                    │
FloorZero      │ Si inner < threshold      │ Umbral sin piso    │ FloorZero[50](40)
               │ → 0                       │ (no negativo)      │ = 0
               │                           │                    │
Franquicia     │ inner − threshold (mín 0) │ Primeros X no      │ Franquicia[300](315)
               │                           │ cuentan            │ km = 15
```

---

## 10. Ejemplo Completo: Juan García (Junio 2026)

```
ENTRADA (Staging)
═════════════════
StagingA3InnuvaContrato:
  NIF: "12345678A"
  SalarioBase: 1500.00
  IrpfPct: 19.0
  CotizacionSsPct: 6.35

StagingCeleroVisita (3 registros):
  Visita 1: 05-jun, tipoVisita=2, km=45, estado=completada
  Visita 2: 10-jun, tipoVisita=2, km=50, estado=completada
  Visita 3: 20-jun, tipoVisita=2, km=220, estado=completada
  Total: 3 visitas, 315 km

StagingPayHawkGasto:
  Dieta 1: 05-jun, categoria=dieta, 30.00
  Dieta 2: 12-jun, categoria=dieta, 30.00
  Dieta 3: 19-jun, categoria=dieta, 30.00
  Dieta 4: 26-jun, categoria=dieta, 30.00
  Dieta 5: 30-jun, categoria=dieta, 30.00
  Total: 150.00

StagingIntratimeFichaje (muchos registros):
  HorasCalculadas total: 160 h
  (entrada/salida diarios)

StagingBizneoAbsencia:
  Baja médica 01-jun: 8 horas
  Total: 8 h

MOTOR CÁLCULO
═════════════

PASADA 1:
  1. Sueldo: 1500.00 (directo A3)
  2. Visitas: Count=3 × 50€ = 150.00
  3. KM: (315 − 300 Franquicia) × 0.23 = 15 × 0.23 = 3.45
  4. Dietas: Sum=150.00
  
  ImportesPrevios = {1→1500, 2→150, 3→3.45, 4→150}
  Subtotal = 1803.45

PASADA 2:
  5. Fee: 1803.45 × 0.065 = 117.22

SALIDA (Excel)
══════════════
NIF:        12345678A
Nombre:     Juan García
Sueldo:     1500.00
Visitas:    150.00
KM:         3.45
Dietas:     150.00
Fee:        117.22
─────────────────────
TOTAL:      1920.67
IRPF 19%:   364.93
SS 6.35%:   121.86
─────────────────────
NETO:       1433.88
```

---

## 11. Dependencias: De qué Depende Cada Concepto

```
Sueldo
  ├─ A3 Innuva (lectura directa)
  └─ Período (validación vigencia)

Visitas
  ├─ Celero (StagingCeleroVisita.Count)
  ├─ Filtro tipoVisita (depende de pregunta definición)
  └─ Período

KM
  ├─ Celero (StagingCeleroVisita.km)
  ├─ Franquicia (variable configuración)
  └─ Período

Dietas
  ├─ PayHawk (StagingPayHawkGasto.Importe)
  ├─ Filtro categoria=dieta
  └─ Período

Ausencias
  ├─ Bizneo (StagingBizneoAbsencia.Horas)
  ├─ Filtro tipo (baja, permiso, etc.)
  └─ Período

Fee 6.5%
  ├─ Pasada 1 completada (Sueldo + Visitas + KM + Dietas)
  ├─ ConceptRef (Referencias)
  └─ Factor 6.5% (variable)

IRPF/SS
  ├─ A3 Innuva (porcentajes)
  ├─ Total Bruto calculado
  └─ Período

Neto
  ├─ Total Bruto
  ├─ IRPF calculado
  └─ SS calculado
```

---

## 12. Cadena de Errores: Si Falta Un Campo

```
SI FALTA...                    → RESULTADO          → TIPO ALERTA
────────────────────────────────────────────────────────────────
SalarioBase (A3)               → Concepto no         │ Bloqueante
                               │ evaluable (null)    │
────────────────────────────────────────────────────────────────
NIF válido                     → No mapear a User    │ Bloqueante
                               │ → Skip empleado     │
────────────────────────────────────────────────────────────────
Visitas (Celero 0)             → 0 × 50 = 0         │ Advertencia
                               │ (normal)            │
────────────────────────────────────────────────────────────────
KM (Celero < 300)              → (0) × 0.23 = 0     │ Normal
                               │ (franquicia)        │
────────────────────────────────────────────────────────────────
Dietas (PayHawk 0)             → Sum = 0            │ Advertencia
                               │ (sin dietas)        │
────────────────────────────────────────────────────────────────
Horas Intratime                → No calcular         │ Bloqueante
                               │ ausencias           │
────────────────────────────────────────────────────────────────
Período abierto                → No calcular         │ Bloqueante
                               │ nada                │
────────────────────────────────────────────────────────────────
Pasada 1 incompleta            → Fee = null          │ Bloqueante
                               │ (ConceptRef error)  │
```

---

## 13. Resumen: 3 Reglas Críticas

```
REGLA 1: IDEMPOTENCIA
═══════════════════════
Cada fila staging tiene Hash = SHA-256(PayloadJson)
Única constraint (Hash) previene duplicados
Si mismos datos llegan 2 veces → se ignora 2ª


REGLA 2: ORDEN SINCRONIZACIÓN
══════════════════════════════
1. A3 Innuva (define empleados/contratos)
2. Bizneo (valida empleados, agrega ausencias)
3. Intratime (total horas base)
4. PayHawk (gastos dependientes de empleado)
5. Celero (visitas complejas)
6. TravelPerk (viajes, Fase 2)
7. SGPV (fallback legacy)

No invertir orden → errores de mapeo


REGLA 3: IMPORTESPACIOS PREVIOS (Pasada 2)
═════════════════════════════════════════════
ConceptRef SOLO disponible después de Pasada 1
Pasa como parámetro a EvaluarFórmula:
  EvaluarFórmula(formula, staging, rows, importesPrevios)
                                        ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

Sin esto: ConceptRef = null → error Bloqueante
```

---

**Documento completo de referencia para desarrollo e implementación.**
