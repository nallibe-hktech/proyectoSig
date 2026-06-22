# Validación del Motor de Cálculo (Pagos / Facturación)

> **Documento de validación técnica** — destinado a revisión por el equipo.
> **Fecha:** 2026-06-21 · **Rama:** `fix/motor-idquestion-flags-excepcion`
> **Alcance:** motor de cálculo de Pagos y Facturación (`backend/SIG.Application/Calculation`).
> **Resultado:** motor `Unit/Calculation` **77/77** · suite completa **395/395** (0 fallos, 0 saltados).

---

## 1. Resumen ejecutivo

El motor de cálculo es el componente que, dado un **Concepto** (con su fórmula) y un **objetivo de cálculo** (servicio + período + recurso opcional), produce el importe de una línea de Pago o de Facturación. Su lógica está especificada en el Excel maestro del cliente **`CierresIntegralesSIG`**, hoja **`Pagos - Facturación`** (catálogo `TIPO CONCEPTO` + `FILTROS`).

Esta validación confirma, mediante tests automatizados y verificables, que:

1. El motor implementa **el catálogo canónico al completo** (10 tipos de concepto + 5 filtros + idQuestion + flags de excepción) — **1:1, cada entrada con test**.
2. Cuatro casos reales de cliente se reproducen **al céntimo** contra cifras impresas en el Excel/proformas (Inpost, Granini, Dyson, Cosmética).
3. **No quedan gaps de motor abiertos.** Lo que aún falta es *parametrización* (números que pone el cliente) e *ingesta de datos de origen*, no capacidad de cálculo.

Los resultados son **auditables**: cada ejecución deja un fichero TRX (estándar de Visual Studio) en `backend/TestResults/`.

---

## 2. Qué es el motor (arquitectura)

El motor evalúa **fórmulas representadas como un árbol (AST) en JSON**, almacenadas en cada `Concept.FormulaJson`. El flujo es:

```
Concept.FormulaJson ──► FormulaParser ──► AST (FormulaNode)
                                              │
CalculationTarget (servicio+período) ─► ICalculationDataLoader ─► CalculationContext (datos staging)
                                              │
                                   CalculationEngine.EvaluateAsync
                                              │
                                              ▼
                              CalculationResult (importe + inputs + sistema origen + incidencias)
```

### Componentes (`backend/SIG.Application/Calculation/`)

| Fichero | Responsabilidad |
|---|---|
| `Nodes/FormulaNode.cs` | Define los **8 tipos de nodo** del AST |
| `FormulaParser.cs` | Deserializa el JSON a AST y **valida** la estructura |
| `FormulaNodeJsonConverter.cs` | Conversor JSON polimórfico (formato nuevo con `type` + retro-compat) |
| `CalculationEngine.cs` | **Evalúa** el AST recursivamente → importe |
| `CalculationContext.cs` | Adapta los datos de staging a filas (`RowAdapter`) y aplica filtros |
| `VariableResolver.cs` | Resuelve una `Variable` a un número (incl. idQuestion de Celero) |
| `CalculationResult.cs` | Resultado: importe redondeado a 2 decimales + trazabilidad + incidencias |

### Los 8 nodos del AST

| Nodo | Significado de negocio |
|---|---|
| `Number` | Cantidad fija (p. ej. cuota mensual) |
| `Variable` | Valor parametrizado; puede resolverse desde la respuesta real de Celero (idQuestion) |
| `Source` | Fuente de datos (Celero, PayHawk, Bizneo, Intratime, SGPV, Tarifas) |
| `Aggregate` | Agregación sobre la fuente: `Sum` / `Count` (+`distinct`) / `Min` / `Max` |
| `BinaryOp` | Operación entre dos sub-árboles: `Add` / `Sub` / `Mul` / `Div` / `Pct` |
| `Modifier` | Filtro/tope sobre un resultado: `Min` / `Max` / `FloorZero` / `Franquicia` |
| `Tramos` | Tarifa incremental por tramos (1ª hora a X, siguientes a Y) |
| `ConceptRef` | "Fee sobre conceptos": referencia importes de otros conceptos del mismo cierre |

---

## 3. Metodología de validación — tipos de test

Se han usado **tres tipos de prueba complementarios**, cada uno cubriendo un riesgo distinto:

### 3.1 Tests unitarios de parser (`FormulaParserTests`)
Verifican que el JSON de fórmula se **deserializa y valida** correctamente, y que las fórmulas malformadas lanzan `FormulaInvalidException` con mensaje claro. Aíslan el riesgo de "entrada inválida".

### 3.2 Tests unitarios de motor (`CalculationEngineTests`)
Verifican el **resultado numérico** de cada primitiva y combinación, usando un **`FakeDataLoader`** (datos en memoria, sin base de datos). Aíslan el riesgo de "cálculo incorrecto". Son deterministas y rápidos (~90 ms los 77).

### 3.3 Golden / characterization tests (5 ficheros `*GoldenTests.cs`)
Anclan el resultado a un **número real impreso en la documentación del cliente** (Excel maestro, proformas, facturas), no a una cifra inventada. Aíslan el riesgo de "el motor calcula algo coherente pero que no coincide con la operativa real". Cada test cita su fuente documental en el comentario de cabecera.

### 3.4 (Contexto) Tests de integración (`SIG.Tests/Integration`)
No prueban el motor de forma aislada, pero sí el **sistema completo** alrededor (controllers de cierres, flujo de aprobación, persistencia). Requieren PostgreSQL. Se incluyen en el conteo global (395) para dar la foto completa.

> **Por qué unit con datos en memoria:** el motor es lógica pura (no depende de infraestructura), por lo que se valida con dobles de prueba (`FakeDataLoader`). Esto hace los 77 tests **100 % reproducibles en cualquier máquina sin BD**.

---

## 4. Cobertura del catálogo canónico (hoja `Pagos - Facturación`)

La especificación del motor es el catálogo `TIPO CONCEPTO` + `FILTROS` del Excel. Cada entrada está implementada **y testeada**:

| Catálogo `TIPO CONCEPTO` | Nodo del motor | Test que lo ancla | Estado |
|---|---|---|---|
| Cantidad fija mensual | `Number` | `Evaluate_Number_DevuelveValor` | ✅ |
| Conteo de Visitas × Cantidad fija | `Count` × `Number` | `Evaluate_BonusVisitaEstandar_x5` | ✅ |
| Conteo de días con actividad × Cantidad fija | `Count distinct Fecha` × N | `Evaluate_CountDistinctFecha` | ✅ |
| Suma de Kilómetros × Coste/Km | `Sum(Km)` × N | `Granini_KilometrajePorTarifa` | ✅ |
| Conteo de Entidad-A × Entidad-B | `Count` × `Count` | `Evaluate_ConteoEntidadAxEntidadB` | ✅ |
| Suma de Entidad-A × Entidad-B | `Sum` × `Sum` | `Evaluate_SumaEntidadAxEntidadB` | ✅ |
| Porcentaje de Entidad | `Pct` | `Evaluate_BinaryPct` / `RefacturacionGastosPct` | ✅ |
| Porcentaje fijo de cantidad variable | `ConceptRef` | `Evaluate_ConceptRef_SumaImportesPreviosYAplicaFee` | ✅ |
| Conteo de horas × cantidad incremental (1ª/resto) | `Tramos` | `Evaluate_Tramos_PrimeraHoraYSiguientes` | ✅ |
| *idQuestion de Celero → variable | `Variable` + idQuestion | `Evaluate_VariableConIdQuestion_*` | ✅ |

### Filtros (columna `FILTROS` del catálogo)

| Filtro | Nodo | Test | Estado |
|---|---|---|---|
| Cantidad mínima (suelo) | `Modifier Min` | `Evaluate_ModifierMin_AplicaSuelo` | ✅ |
| Cantidad máxima (techo) | `Modifier Max` | `Evaluate_ModifierMax_AplicaTecho` | ✅ |
| Rendimiento mínimo (si < X → 0) | `Modifier FloorZero` | `Evaluate_ModifierFloorZero_*` | ✅ |
| Franquicia (primeros X no cuentan) | `Modifier Franquicia` | `Evaluate_ModifierFranquicia_RestaPrimerosX` | ✅ |

### Flags de excepción de la visita

Extraídos del `PayloadJson` de Celero y tipados en `RowAdapter` (`Estado`, `NumeroVisita`, `Nocturnidad`, `Pernocta`):

| Regla de excepción | Test | Estado |
|---|---|---|
| Visita fallida → mismo coste / no factura | `Fallida_MismoCoste_*` / `Fallida_NoSeFactura_*` | ✅ |
| Cancelación → no factura si no salió a ruta | `Cancelacion_NoFacturaSiNoSalioARuta_*` | ✅ |
| Nocturnidad → recargo +50 % | `Evaluate_RecargoNocturnidad_*` | ✅ |
| 2ª/3ª visita → tarifa distinta | `SegundaTerceraVisita_*` / `Evaluate_SegundaVisita_*` | ✅ |
| Pernocta → tarifa especial | `Pernocta_AplicaTarifaEspecial_*` | ✅ |

---

## 5. Inventario de tests (77 en `Unit/Calculation`)

| Fichero | Tipo | Nº | Qué cubre |
|---|---|---:|---|
| `FormulaParserTests.cs` | Unit (parser) | 14 | Parseo/validación de cada nodo y errores de fórmula |
| `CalculationEngineTests.cs` | Unit (motor) | 44 | Las 8 primitivas, filtros, tramos, idQuestion, flags, agregado×agregado, filtros implícitos/explícitos, snapshot e inputs |
| `InpostFacturacionGoldenTests.cs` | Golden | 2 | Inpost por franjas de minutos = **1.009,68 €**; demuestra que `Tramos` NO modela Inpost |
| `FacturacionRealGoldenTests.cs` | Golden | 4 | Future (1.931,92), Granini km (837,12), Dyson costes (31.624,05) y margen (46.640,41) |
| `ExcepcionesInpostGoldenTests.cs` | Golden | 5 | Fallida (ambos sentidos), cancelación condicional, 2ª/3ª visita, pernocta |
| `FacturaInpostEndToEndGoldenTest.cs` | Golden | 1 | Factura mensual Inpost completa end-to-end = **1.406,34 €** |
| `CosmeticaCatalogoGoldenTests.cs` | Golden | 7 | Cosmética: pago 11,92 €/h y factura 55 €/h por horas pactadas (47,68/23,84/59,60 y 220/110/275) + regla "menos tiempo = 100 % pactado" |
| **Total** | | **77** | |

### Casos anclados a cifra real del Excel (golden)

| Cliente | Magnitud | Cifra ancla (impresa) | Fuente documental |
|---|---|---|---|
| **Inpost** | Factura por franjas | 1.009,68 € · end-to-end 1.406,34 € | Proforma 12 + entrevista `Detalles PagosFact_IL` |
| **Granini** | Kilometraje | 3.487,997 km × 0,24 = 837,12 € | Hoja GRANINI / entrevista |
| **Dyson** | Total costes / margen | 31.624,05 € / 46.640,41 € | Hoja DB1 (presupuesto/quote) |
| **Cosmética** | Pago y factura por hora | 11,92 €/h y 55 €/h × horas pactadas | Hoja `Pagos - Facturación`, bloque "PAGO MERCH COSMETIX" |

---

## 6. Filtros de contexto cubiertos

Además de las fórmulas, el motor aplica **filtros implícitos** sobre los datos (también testeados):

- **Período**: excluye filas fuera del rango `[FechaInicio, FechaFin]` del cierre — `Evaluate_FiltroImplicitoPeriodo_*`.
- **Servicio**: excluye filas de otro servicio — `Evaluate_FiltroImplicitoProyecto_*`.
- **Recurso** (opcional): si se pasa `recursoId`, filtra solo sus filas — `Evaluate_RecursoId_*`.

Y **filtros explícitos** en la fuente (operadores `Eq`, `Neq`, `Gt`, `Gte`, `Lt`, `Lte`, `In`), incluyendo segmentación por atributos del `PayloadJson` (tipo de visita, zona, etc.) — `Evaluate_CountConFiltro_*`, `Evaluate_FiltroTipoVisitaDesdePayload_*`, `Evaluate_FiltroIn_*`.

### Trazabilidad y robustez
- El resultado incluye `SistemaOrigen` (PayHawk / Celero / Bizneo / … / Mixto) e `InputsJson` con los parámetros usados — `Evaluate_DevuelveInputsJson_*`, `Evaluate_DevuelveSnapshotDeFormula_*`.
- Casos límite con **incidencia** en vez de excepción: dataset vacío (`EmptyDataset`), división por cero (`DivisionByZero`), fee sin conceptos previos (`SinConceptosPrevios`).

---

## 7. Qué NO es alcance del motor (y por qué)

Para evitar malentendidos, se documenta explícitamente lo que **no** calcula el motor (es decisión de diseño, no una carencia):

| Elemento | Dónde se resuelve | Evidencia documental |
|---|---|---|
| Adelantos / anticipos / embargos | **Líneas manuales** del cierre (migración `AddManualLineFields`); los registra FICO | Entrevista: *"Embargos los registra FICO"*, *"adelantos… se descuentan en el cierre de nóminas"* |
| Salario fijo / incentivos | Línea manual / dato de contrato | Entrevistas (Granini, Daikin, etc.) |
| Política de horas extra (prorrateo vs redondeo) | **Parametrización del cliente** | El Excel lo marca en `Pendientes_para_parametrizar`: *"queda como excepción a confirmar y documentar"* |
| Tarifas reales por tipo/zona/acción | Parametrización por UI (sin PII en repo) | Catálogo + `gobierno-dato-cliente` |
| Flujo de aprobación Grupo→FICO | Servicios de cierre + tests de integración | `SIG.Tests/Integration/ApprovalFlowTests.cs` |

> El motor evalúa **un concepto por fórmula**; la factura/cierre de un cliente es la **composición** de varios conceptos del catálogo (mapeo cliente↔concepto en la hoja `Conceptos x Proyecto` del Excel).

---

## 8. Limitaciones conocidas / pendientes (no de motor)

1. **Ingesta de Celero**: la duración real de visita (`realDuration`), el estado (`visitStatus`) y la provincia aún **no llegan al `PayloadJson`**. El motor ya sabe usarlos (los golden de Inpost lo demuestran inyectándolos), pero falta enriquecer el DTO/SQL de ingesta. Detalle en `docs/RETOMA_INPOST_FACTURACION.md §4.3`.
   - Ojo: el SQL filtra hoy `WHERE status = 'done'`, que descarta fallidas/canceladas que Inpost sí necesita. Cambiarlo afecta a todos los clientes → hacerlo configurable.
2. **Tarifas oficiales de Inpost** (franjas + pernocta): están en un `.pptx` en SharePoint que no está en disco. Los números usados en los golden son **provisionales** (proforma 12), claramente etiquetados.
3. **Parámetros por cliente**: horas pactadas por acción, %fee, tarifas por zona — los introduce el cliente; no son cálculo.

---

## 9. Cómo reproducir la verificación

Requisitos: .NET SDK 10 (`global.json` fija la versión). Para la suite completa, PostgreSQL en `localhost:5432` (BD `sig_plataforma_test`, usuario `postgres`).

```bash
cd backend

# (A) Solo el motor de cálculo — NO requiere base de datos
dotnet test SIG.Tests/SIG.Tests.csproj \
  --filter "FullyQualifiedName~SIG.Tests.Unit.Calculation" \
  --logger "trx;LogFileName=MotorCalculo_Resultados.trx" \
  --results-directory ./TestResults
# Esperado: Superado: 77, Total: 77

# (B) Suite completa (Unit + Integration) — requiere PostgreSQL
dotnet test SIG.Tests/SIG.Tests.csproj \
  --logger "trx;LogFileName=SuiteCompleta_Resultados.trx" \
  --results-directory ./TestResults
# Esperado: Superado: 395, Total: 395
```

### Artefactos generados (auditables)
- `backend/TestResults/MotorCalculo_Resultados.trx` — 77/77 (solo motor).
- `backend/TestResults/SuiteCompleta_Resultados.trx` — 395/395 (Unit + Integration).

Los `.trx` se abren en Visual Studio (Test Explorer → Open) o en cualquier visor TRX; cada test aparece con `outcome="Passed"`. El bloque `<Counters .../>` del XML resume el total.

---

## 10. Resultados

| Suite | Tests | Resultado | Tiempo aprox. |
|---|---:|---|---|
| Motor (`Unit/Calculation`) | 77 | **77 ✅ / 0 ❌ / 0 omitidos** | ~0,1 s |
| Suite completa (Unit + Integration) | 395 | **395 ✅ / 0 ❌ / 0 omitidos** | ~70 s |

Desglose de los 395: 77 motor + 122 servicios (mocks) + 155 integración (API real + Postgres) + 41 validators/otros.

---

## 11. Conclusión

El motor de cálculo está **validado de forma exhaustiva y verificable** contra su especificación real (catálogo del Excel `CierresIntegralesSIG`):

- **Cobertura funcional completa**: los 10 tipos de concepto + 5 filtros + idQuestion + flags de excepción están implementados y cada uno tiene test.
- **Validación con realidad operativa**: 4 clientes reproducidos al céntimo contra cifras impresas (Inpost, Granini, Dyson, Cosmética).
- **Sin gaps de motor abiertos**: lo pendiente es parametrización del cliente e ingesta de datos de origen, no capacidad de cálculo.
- **Reproducible y auditable**: 77/77 sin BD, 395/395 con BD; artefactos TRX adjuntables.

---

### Anexo — Fuentes documentales consultadas
- Excel maestro `CierresIntegralesSIG (9).xlsx`: hojas `Pagos - Facturación` (catálogo), `Lógicas Pagos-Facturación` (modelos por cliente), `Detalles PagosFact_IL` (entrevistas), `CuadroDetallesPagosFact`, `Conceptos x Proyecto`, `CeleroOne`, `Entidades`.
- Carpeta `FACTURACIÓN` del cliente (proformas / facturas reales: Inpost, Future, Granini, Dyson).
- Documentos del repo: `docs/MOTOR_CALCULO.md`, `docs/ARQUITECTURA.md`, `docs/RETOMA_INPOST_FACTURACION.md`, `docs/SUPOSICIONES_CRITICAS.md`.
