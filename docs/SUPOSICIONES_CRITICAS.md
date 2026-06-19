# SUPOSICIONES CRÍTICAS — SIG-es

Decisiones autónomas tomadas ante ambigüedad no bloqueante. Cada una se puede revertir si el cliente lo corrige. Ver diseño en `docs/ARQUITECTURA.md §15`.

## Ola 2 (2026-06-17)

### SUP-01 · Cecos: "el ceco en sí, no el número" (#3b)
**Ambigüedad:** el cliente pidió mostrar "el ceco en sí, no el número". `CostCenter` tiene `Codigo` (numérico/alfanumérico) y `Nombre` (descriptivo); hoy la UI muestra `"Codigo - Nombre"`.
**Decisión:** mostrar `CostCenter.Nombre` como etiqueta principal del ceco en las vistas de cálculo/cierre, en lugar del `Codigo` numérico.
**Reversible:** sí, es solo presentación. Confirmar con cliente si prefiere `Codigo`, `Nombre` o ambos.

### SUP-02 · Periodos: fechas de pago 30/15/9 (#9) — CONFIRMADO por cliente 2026-06-17
**Interpretación confirmada:** 30/15/9 son el **día del mes** de pago. Cada periodo tiene asignado un día de pago entre esos valores.
**Decisión:** campo `DiaPago` en `Period` (valores permitidos 30, 15, 9). Validación que restrinja a esos tres valores.
**Reversible:** sí.

### SUP-03 · Contratos de un día: criterio de detección (#2)
**Ambigüedad:** "contrato de un día" no estaba formalmente definido.
**Decisión:** se considera contrato de un día todo `StagingA3InnuvaContrato` con `FechaInicio == FechaFin`. La exclusión es **manual** (el usuario marca "a ignorar" + motivo); la detección automática solo lo **señala**, no lo ignora por sí sola.
**Reversible:** sí.

### SUP-04 · Incentivos manuales: mecanismo (#3a)
**Ambigüedad:** "añadir incentivos manualmente / importe personalizado" sin especificar dónde se persiste.
**Decisión:** reutilizar el scaffolding existente (`OverrideExceptionDialog` en frontend, entidad `PresupuestoServicio`) y persistir el importe manual como una línea de cierre no derivada de fórmula, con auditoría (motivo). Se concreta en la Ola 2.
**Reversible:** sí.

## PPT «Pantallas actualizadas - HK_10062026» (2026-06-18)

### SUP-05 · Incidencias de cliente (PPT slide 6) — CONFIRMADO necesario (slide 47: "h&k proponer solución")
**Ambigüedad:** el PPT pide "registrar incidencias del cliente (tipo, explicación y estado), editables y con histórico; un cliente puede tener varias", sin definir el catálogo de tipos ni los estados ni los permisos de escritura.
**Decisiones:**
- **Estados:** enum `EstadoIncidencia { Abierta, EnProceso, Resuelta }` (estado por defecto al crear: `Abierta`).
- **Tipo:** texto libre (`string`, máx. 100), sin catálogo cerrado (YAGNI: el PPT no define valores).
- **Histórico:** lo aporta el `AuditInterceptor` sobre `IAuditable` + `AuditLog` (Create/Update/Delete), no una tabla de historial dedicada.
- **Permisos:** lectura para cualquier usuario autenticado; crear/editar/eliminar solo `Administrator` (mismo criterio que la edición del propio cliente). Eliminación = soft-delete.
- **API:** anidada `api/clients/{clientId}/incidencias` (patrón Tarifas/Presupuestos). Entidad `ClienteIncidencia`, migración `AddClienteIncidencia`.
**Reversible:** sí. A validar en uso real (disclaimer slide 1); ajustar estados/tipos/roles si el cliente lo pide.

### SUP-06 · Forecast de ventas y GPP (PPT slide 36)
**Ambigüedad:** el PPT define el resumen (filas dpto+cliente, columnas mes) y lista "acción/servicio" como filtro, pero no fija la granularidad de captura; además **se contradice** sobre GPP.
**Decisiones:**
- **Granularidad = servicio + mes.** Es la única que produce las filas por **departamento** del resumen (el dpto vive en el `Service`, no en el `Client`) y soporta el filtro por servicio que pide el PPT. Agrega hacia arriba a cliente y dpto. Un registro por (ServiceId, Anio, Mes), índice único.
- **Campos:** `VentasPrevistas` (€), `MargenPrevisto` (€, slide 23: "previsión de ventas y margen bruto"), `PersonasCampo` (GPP).
- **Mes cerrado = mes natural anterior al actual** (no editable). Se rechaza con 409 `period_closed`. (No se ató a `Period.Estado` porque el forecast es a futuro y esos periodos aún no existen.)
- **Permisos:** lectura autenticado; escritura `Administrator,Backoffice` (igual que Tarifas/Presupuestos). Resumen solo lectura.
- **UI:** editor como tab "Forecast" en el detalle de servicio (rejilla de 12 meses, meses cerrados bloqueados) + pantalla `/forecast` con tabs Ventas/GPP, filtros (año, dpto, cliente, servicio) y tabla pivote con totales.

**CONTRADICCIÓN PENDIENTE de confirmar con cliente (slide 35 vs 36):** el slide 35 (presupuesto) propone meter el nº de personas de campo en el presupuesto "y así eliminar el forecast de GPP", mientras el slide 36 (pantalla dedicada de forecast) pide explícitamente un forecast de GPP mensual con tabla resumen. **Se ha seguido el slide 36** (pantalla dedicada y más detallada). Si el cliente confirma el slide 35, habría que mover GPP al presupuesto y retirar la columna/tab GPP del forecast.
**Reversible:** sí.

### SUP-07 · Dashboard: origen del "objetivo" (PPT slides 3-4)
**Ambigüedad:** el slide 4 pide eliminar los gráficos "Margen vs Objetivo" y "Objetivos del período" y mostrar el **objetivo** como valor dentro de los KPIs de la parte superior. Pero el origen del objetivo no está definido en el modelo (hoy eran placeholders hardcodeados: 400K facturación, 25% margen).
**Decisión:** se han eliminado ambos gráficos y el objetivo se muestra como valor en los KPIs (facturación: `objetivoFacturacionK=400`; margen: `objetivoMargenPct=25`), **manteniéndolo como placeholder** hasta que el cliente defina su origen. Candidato natural: enlazarlo al **Forecast** (ventas/margen previstos, [[feature-forecast-ventas-gpp]]) o a `PresupuestoServicio`.
**Otros ajustes del dashboard aplicados (no ambiguos):** coste real junto al margen, desdoblar "cierre completado" en costes/facturación (4 contadores nuevos en `DashboardKpisDto`), importes en K, "Facturación por cliente" con importe € antes que margen, filtro de servicio (backend `serviceId` en KPIs y mis-servicios). Las alertas ya eran interactivas (redirigen al servicio del cierre).
**Reversible:** sí.

### SUP-08 · Informes nativos, NO Power BI (PPT slide 23) — CONFIRMADO por cliente 2026-06-18
**Contexto:** el scaffolding inicial asumió Power BI para reporting (página `/reports` informativa + schema `bi` con vistas `bi.v_*`). El cliente confirmó que **no usa Power BI**.
**Decisiones:**
- Reporting **nativo en la app**: pantalla `/reports` (Informes) reconstruida con `IReportsService` + endpoints `api/reports/resultado` (drill-down dpto→cliente→servicio: facturación/coste/margen, reusa el emparejado coste+factura del dashboard) y `api/reports/prevision-vs-real` (Forecast vs cierres, por dpto/cliente/mes, métrica Ventas o Margen).
- **Eliminado el andamiaje Power BI**: página informativa sustituida y migración `DropBiSchema` borra el schema `bi` y sus vistas (ningún código las consumía).
- El informe "solo forecast de ventas y margen" del slide 23 ya está cubierto por la pantalla `/forecast` ([[feature-forecast-ventas-gpp]]).
- **Pendiente** del slide 23 (no bloqueante): informe de productividad y el de traspaso entre cecos (este último bloqueado, ver traspaso cecos).
**Reversible:** sí (Down de la migración recrea el schema).

### SUP-09 · Errores/Alertas de desviación y matriz de aprobaciones (PPT slides 40, 42, 15)
**Errores Nómina/Facturación (slides 40, 42):** se reutiliza el sistema existente `ClosureAlerta` (Bloqueante/Advertencia). La pantalla `/alertas` se renombra a **"Alertas de desviación"** y se enriquece con **filtros** (tipo, origen Nómina/Facturación, estado, búsqueda) y una **vista por bloques** (agrupada por código de error) para resolverlos en grupo. Cubre la "vista única ERRORES NÓMINA/FACTURACIÓN" del slide 42.
- **Pendiente (no bloqueante):** las pantallas de "datos consolidados de operaciones" y "datos consolidados de Innuva" (slide 40) quedan fuera por estar infra-especificadas; y la **lista definitiva de códigos de error** la enviará Eladio (slide 40) — el sistema ya funciona con los códigos que genere `IClosureValidationService`.

**Matriz de aprobaciones (slide 15):** se añade al **detalle del cierre** una **matriz empleados × conceptos** (filas usuarios, columnas conceptos, celdas importe, totales) construida en el front desde `CierreDetailDto.Lines` — sin backend nuevo. El "campo de pago/facturación adicional + comentario justificativo" por línea ya existe (override/incentivo). El **detalle por día** y las vistas "trabajo por persona/visitas por persona" quedan como ampliación posterior (requieren datos por día que hoy no están en la línea de cierre).
**Reversible:** sí.

### SUP-10 · Motor de cálculo — lógica de Pagos-Facturación del Excel "CierresIntegralesSIG"
El Excel entregado por el cliente es la **especificación funcional del motor de cálculo** ya existente (cada "TIPO CONCEPTO" = una fórmula `Concept.FormulaJson`; "Pagos" → `CierreCostes`, "Facturación" → `CierreFacturacion`). Se amplía el motor con las primitivas que faltaban, **de forma aditiva y compatible** con las fórmulas ya almacenadas:

- **`ModifierNode`** (`Min`/`Max`/`FloorZero`/`Franquicia`): cubre los FILTROS del Excel (cantidad mínima/suelo, máxima/techo, rendimiento-umbral mínimo → 0, y franquicia "los primeros X no contabilizan").
- **`TramosNode`**: tarifa incremental por tramos acumulativos (1ª hora/módulo a un precio, siguientes a otro).
- **`AggregateNode.Distinct`**: con `Count`, cuenta valores únicos de un campo → "conteo de días con actividad".
- **`ConceptRefNode`**: "fee sobre conceptos" / "% fijo de cantidad variable" = referencia a la suma de importes de otros conceptos del mismo cierre. Se resuelve en una **2ª pasada** del cierre (`CierreServiceBase.ComputeLinesAsync`), con los importes base en `CalculationTarget.ImportesPrevios`.

**Decisiones autónomas y límites asumidos:**
1. **Logística (Galán/MDP) NO se modela como fuente de datos nueva**: los staging de Galán/Mediapost no traen coste ni m³ (la logística va "según tarifas/PWBI"). Se modela como `Tarifa`/gasto + margen con `BinaryOp` (`Mul`/`Pct`). Si en el futuro llega el coste estructurado, se añadirá como `Source`.
2. **Segmentación de visitas por tipo/zona/mueble/km**: como `StagingCeleroVisita` no tiene esas columnas, `RowAdapter.FromVisita` las extrae del **`PayloadJson`** (claves conocidas `tipoVisita`, `puntoMontado`, `zona`, `km`, `horas`, `importe`, `categoria`) y vuelca el resto en un diccionario `Extra` filtrable. Es el mecanismo del "idQuestion de Celero → variable".
3. **Fee sobre conceptos = solo sobre conceptos base** (2 pasadas). Un fee sobre otro fee NO está soportado (no aparece en el Excel); documentado como límite.
4. **Datos de cliente**: se crean **conceptos de EJEMPLO con datos anónimos** en el seeder (uno por primitiva nueva). Las tarifas/fees/horas reales del Excel las introduce el cliente por la UI — **cero PII/valores reales en el repo**.
5. **Eficiencia**: `ComputeLinesAsync` pasa a evaluar **cada concepto una sola vez** (antes lo hacía dos: línea + log); el log se construye del mismo resultado.

**Avisos** (no parte del motor): `API Innuva.docx` contiene un Client Secret OAuth real en texto plano y los ficheros del Excel traen PII/tarifas reales — no commitear.
**Reversible:** sí (todos los nodos nuevos son aditivos; las fórmulas antiguas siguen evaluando igual).

### SUP-11 · Motor — idQuestion Celero→variable (real) + flags de excepción (verificación vs Excel `(9)`)
Tras revisar el Excel `CierresIntegralesSIG (9)` punto por punto, se cierran dos huecos para alinear el motor con la spec:

1. **idQuestion de Celero → variable (gap #1).** Antes `VariableResolver` devolvía siempre el primer valor de `MapeoValoresJson` (ignoraba `QuestionIdExterno`). Ahora resuelve el valor desde la **respuesta real** de las visitas Celero del contexto: la clave del `PayloadJson` = `QuestionIdExterno`, y su contenido (`"A"`/`"Premium"`/`"Sí"`) se mapea a número vía `MapeoValoresJson`.
   - **Decisión autónoma (colapso a escalar):** un `VariableNode` produce un único número, pero una pregunta Celero tiene una respuesta por visita. Se colapsa a la **respuesta más frecuente** (mode; desempate alfabético). Si no hay respuesta en el período → fallback al valor `"Default"`, y si no existe, al primero (compatibilidad con `TarifaHora` y las fórmulas y tests previos).
   - **Límite (no implementado, YAGNI):** un factor variable **por fila** (p.ej. multiplicar cada visita por su propio ZonaBonus dentro de un `Sum`) no existe; requeriría mapear la variable dentro del `Aggregate`. Registrado como posible ampliación.

2. **Flags de excepción de la visita (gap #4).** Los flags del Excel (columna *Excepciones_Modelo*: `fallida`, `cancelacion`, `2ª/3ª visita`, `nocturnidad`, `pernocta`) ya eran filtrables vía `Extra`, pero ahora son **campos de primera clase y tipados** en `RowAdapter`: `Estado` (string), `NumeroVisita` (int), `Nocturnidad` y `Pernocta` (bool). Se extraen del `PayloadJson` (claves `estado`, `numeroVisita`/`numVisita`/`nVisita`, `nocturnidad`/`nocturna`, `pernocta`).
   - Las reglas de negocio (fallida → mismo coste; 2ª visita → 50 %; nocturnidad → +50 %) se expresan **componiendo** filtros + `BinaryOp` (`Mul`/`Pct`), no con nodos nuevos. Hay conceptos de ejemplo en el seeder y tests que lo demuestran.
   - **Bug colateral corregido:** `FormulaNodeJsonConverter` no deserializaba valores de filtro **booleanos ni arrays**, dejándolos en `null` (rompía flags booleanos y el operador `In`). Ahora soporta string/número/booleano/array. `Equal` compara booleanos y strings de forma tolerante (case-insensitive, `true/1/"sí"`).

3. **Cobertura de las primitivas "Entidad-A × Entidad-B" (tipos 5 y 6 del Excel).** El motor ya las soportaba por composición (`Mul(Aggregate, Aggregate)`); se añaden conceptos de ejemplo en el seeder y tests que las blindan (Conteo×Conteo y Suma×Suma). No es un cambio de lógica.

**No es código:** la política de horas extra de Optimising (prorrateo vs redondeo a hora completa) sigue marcada **"PTE"** por el cliente en el propio Excel — decisión de negocio abierta, no un gap del motor (ambas reglas son expresables: prorrateo con `Sum(Horas)`, redondeo redondeando las horas). Logística coste+margen, "% de entidad" (`Mul`) y tarifas por zona (un concepto por zona) ya cumplían el Excel y NO eran gaps.
**Tests:** suite completa 340/340 (motor de cálculo 59/59). **Reversible:** sí (cambios aditivos y compatibles con fórmulas almacenadas).

## Aparcados (sin decisión, requieren input del cliente)

### PARK-01 · Panel de facturas pagadas/pendientes por cliente (#5)
No existe entidad Factura/Pago ni estado de pago en el modelo. El cliente no supo definir de dónde sale el estado "pagada/pendiente". **Omitido de momento**; retomar cuando se defina el origen (entidad manual, derivado del cierre de facturación, o integración externa de solo lectura).
