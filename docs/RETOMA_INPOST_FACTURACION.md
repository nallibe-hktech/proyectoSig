# Punto de retoma — Facturación INPOST (motor de cálculo)

> **Fecha de cierre de sesión:** 2026-06-20
> **Próxima sesión:** 2026-06-21
> **Ámbito:** validar/ampliar el motor de cálculo del proyecto para el caso de facturación de INPOST
> (lockers), a partir del análisis de la carpeta FACTURACIÓN, la plantilla de pagos y el flujograma.
> **Regla seguida:** nada inventado ni supuesto; cada conclusión está trazada a documentación real.

---

## 1. Objetivo de esta línea de trabajo
Comprobar si el motor de cálculo de `C:\dev\SIG-es` sirve para la facturación real de los clientes
(muestreados: Future, Granini, Dyson, **Inpost**) y, en concreto, resolver el caso Inpost, que es el
único con una regla no trivial.

---

## 2. Qué hemos CONSEGUIDO

### 2.1 Verificación del motor (contra código, no contra doc)
- El motor tiene 8 nodos reales (`Number, Variable, Source, Aggregate(+distinct), BinaryOp, Modifier, Tramos, ConceptRef`) — `FormulaNodeJsonConverter.cs`.
- Fuentes de datos: `VisitasCelero, GastosPayHawk, HorasBizneo, HorasIntratime, VisitasSgpv, TarifasServicio` — `CalculationContext.cs`.
- Doble cierre `CierreCostes` / `CierreFacturacion`; salidas `ExportA3InnuvaAsync` (nóminas) y `ExportA3ErpAsync` (facturas).
- **Discrepancias doc↔código detectadas:** "Proyecto" se renombró a `Service`; aprobación es de **3 pasos** (`Grupo→Fico→SystemExports`), no 5; el esquema `bi` de Power BI fue **eliminado** (la doc dice lo contrario).

### 2.2 Golden tests creados (12 nuevos, suite Calculation 71/71 verde)
| Archivo | Tests | Qué prueba |
|---|---|---|
| `backend/SIG.Tests/Unit/Calculation/InpostFacturacionGoldenTests.cs` | 2 | Franja de minutos reproduce factura real (1.009,68 €) + que el nodo `Tramos` NO la modela |
| `backend/SIG.Tests/Unit/Calculation/FacturacionRealGoldenTests.cs` | 4 | Future (1.931,92), Granini km×0,24 (837,12), Dyson costes (31.624,05) y margen (46.640,41) |
| `backend/SIG.Tests/Unit/Calculation/ExcepcionesInpostGoldenTests.cs` | 5 | Fallida, fallida=mismo coste, cancelación condicional, 2ª/3ª visita, pernocta |
| `backend/SIG.Tests/Unit/Calculation/FacturaInpostEndToEndGoldenTest.cs` | 1 | **Factura mensual Inpost completa end-to-end = 1.406,34 €** |

### 2.3 Revisión documental EXHAUSTIVA
Leídos: 19 hojas del Excel maestro `CierresIntegralesSIG (9).xlsx`, los docs de `C:\dev\SIG-es\docs`,
los 4 specs grandes de la raíz, el prototipo PDF (28 pantallas) y 2 .docx. Hallazgo: **4 de mis 5
"preguntas abiertas" ya estaban respondidas** en la doc (sobre todo hoja `Detalles PagosFact_IL` —
entrevista de Esmeralda Rodríguez — y hoja `CeleroOne`).

### 2.4 Revisión de la ingesta de Celero
Localizada la cadena exacta: `CeleroPostgresClient.cs` (SQL) → `CeleroVisitaDto` (DTO) →
`DashboardCalcSyncAudit.cs:334` (PayloadJson) → `RowAdapter`. **Hoy el payload NO trae duración ni estado.**

---

## 3. HALLAZGOS CLAVE

1. **Franja ≠ Tramos.** Inpost cobra por **franja de tiempo** (busca la banda de minutos → tarifa de esa banda, por provincia), NO por tramos incrementales. El nodo `Tramos` del motor sirve para **Molins** (1ª hora/resto), no para Inpost. La doc (`MOTOR_CALCULO.md:124`) los confunde.
2. **El motor SÍ puede calcular Inpost** con los nodos actuales (suma de `Count(franja)×tarifa`); demostrado al céntimo. Un nodo `TarifaPorFranja` sería una mejora opcional (no imprescindible).
3. **Modelo oficial de Inpost** (entrevista Esmeralda, `Detalles PagosFact_IL` fila 3): factura = visitas tarifadas por **tiempo + provincia** + logística MDP refacturada (+30% margen, +8% admin desde abril 2026). Excepciones: fallida = mismo coste; cancelada en ruta = se cobra; cancelada sin salir = no; 2ª visita por error SIG = no factura / por petición cliente = sí; pernocta = modalidad nueva con costes de estancia.
4. **Celero SÍ tiene la duración de la visita** (`CeleroOne`: `realDuration`, `visitDuration`, `visitStartedAt/visitFinishedAt`, `serviceDefaultDurationMin`=30) — pero el proyecto **no la ingesta** todavía.

---

## 4. Qué nos FALTA

### 4.1 Artefactos externos (no están en disco; solo en SharePoint de silvia_lopez)
- **`TARIFAS INPOST DIURNAS Y CON PERNOCTA 2026.pptx`** → tabla OFICIAL de franjas + tarifas de PERNOCTA.
- **`PROFORMA 13.Inpost con detalle 30-4-26.xlsx`**.
- Conseguirlos: abrir los enlaces (celda de `Detalles PagosFact_IL` fila 3, col. adjuntos) con login propio, o pedírselos a **Silvia López / Esmeralda Rodríguez**.
- *(Disponibles en local y ya usados: `PROFORMA 12.Inpost` (tabla operativa), factura MDP `DINE26-00429` (coste base 740,36 €), `Tarifas MDP 2026.pdf`.)*

### 4.2 Decisiones de negocio abiertas (no inventables)
- **Logística 30% + 8% (hueco A3 de `ACTA_HUECOS_SESIONES_VALIDACION`):** ¿el 8% se aplica sobre el coste base o sobre coste+30%? Sin cerrar. En el test end-to-end se aplicó sobre el coste base, **etiquetado como supuesto**.
- **"Tipo de visita":** las tablas resumen dicen "tiempo, provincia y tipo"; la entrevista solo dice "tiempo y provincia". Probable que "tipo" = modalidad diurna/pernocta. Confirmar.
- **Unidades de `realDuration`** en Celero: ¿minutos, segundos, HH:MM? Verificar contra la BBDD real.

### 4.3 Cambio técnico pendiente (ingesta de Celero)
Para que las franjas operen sobre dato real, **enriquecer DTO + SQL** (NO requiere migración de BBDD ni tocar el motor, porque el cálculo lee del `PayloadJson`):
- `CeleroPostgresClient.cs:33` (SQL): añadir `realDuration` (duración), `visitStatus` (estado), `cancellationReason`, y **JOIN a POA/centro** para `addressState` (provincia).
- `IntegrationDtos.cs:3` (DTO `CeleroVisitaDto`): añadir `DuracionMinutos, Estado, Provincia, CancellationReason`.
- `CeleroPostgresClient.cs:54` (reader) y `FakeClients.cs` / `HttpClients.cs`: rellenar los campos nuevos. El `Serialize(d)` de `DashboardCalcSyncAudit.cs:334` los mete solo en el payload.
- **DECISIÓN DELICADA:** el SQL filtra hoy `WHERE v.status = 'done'` (`CeleroPostgresClient.cs:43`), que **descarta fallidas y canceladas** — pero Inpost las necesita. Relajar ese filtro **afecta a TODOS los clientes**. Hacerlo configurable o por cliente. Validar antes de implementar.

---

## 5. Estado de las 5 preguntas
| # | Pregunta | Estado |
|---|---|---|
| Q1 | Tabla de franjas oficial | Modelo ✅ / números en el `.pptx` (falta) |
| Q2 | Segmentación por provincia | Confirmada ✅ / valores en el `.pptx` |
| Q3 | ¿Tipo de visita cambia tarifa? | Ambiguo — probablemente diurna/pernocta (confirmar) |
| Q4 | Minutaje por visita | Respuesta en origen ✅ (Celero `realDuration`); falta ingestarlo |
| Q5 | Excepciones | Reglas ✅ documentadas / importes pernocta en el `.pptx` |

---

## 6. ARRANQUE PARA MAÑANA (próximos pasos, priorizados)
1. **Si llega el `.pptx` de tarifas** → meter los números oficiales (franjas + provincias + pernocta) y recalcular el end-to-end con cifras reales.
2. **Si NO llega** → seguir con el cambio técnico de ingesta de Celero (sección 4.3), empezando por dejar `status='done'` **configurable** para no romper al resto de clientes, + enriquecer DTO/SQL con `realDuration/estado/provincia`.
3. Confirmar con Finanzas/Esmeralda las 3 decisiones abiertas (4.2).
4. (Opcional) Prototipar el nodo `TarifaPorFranja` para que Inpost sea 1 concepto en vez de ~22 términos.

**Nada de esto toca al cliente ni escribe fuera del PC.** Todo el trabajo hecho son tests locales y análisis; no se ha modificado código de producción del motor.
