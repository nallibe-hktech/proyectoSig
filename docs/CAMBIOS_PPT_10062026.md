# CAMBIOS DERIVADOS DEL PPT "Pantallas actualizadas - HK_10062026"

> **Fuente:** `Pantallas actualizadas - HK_10062026 (formato unificado).pptx` (44 diapositivas)
> **Naturaleza:** ronda de revisión de pantallas con SIG Europe. Cada pantalla incluye comentarios de cambios del cliente.
> **Fecha de análisis:** 2026-06-12
> **Objetivo:** separar cambios de BACKEND vs FRONTEND, distinguir lo **decidido** de lo **pendiente de validación**, y definir un plan de migración priorizado para `C:\dev\SIG-es`.

---

## 0. Aviso global del documento (Slide 1)

> *"Todos los comentarios deberán validarse cuando tengamos la posibilidad de utilizar todas las pantallas con su flujo y aplicando casos reales. Sin esa visión no podemos asegurar que las pantallas estén completas."*

**Implicación:** ningún cambio es contractualmente cerrado hasta validar con flujo real. No obstante, hay un subconjunto **decidido y de bajo riesgo** que puede aplicarse ya (ver §3 y §5). Los bloques marcados "pendiente de revisar / falta FICO / falta FF" NO se implementan (ver §4).

---

## 1. CAMBIO ESTRUCTURAL CRÍTICO — Eliminar "Proyecto" y renombrar "Acción"→"Servicio"

**Slides 7 y 9. Decisión firme. Es el cambio de mayor impacto del documento y bloquea al resto.**

### Modelo de datos
- **ANTES:** `Cliente → Proyecto → Acción → Concepto`
- **AHORA:** `Cliente → Servicio → Concepto`
  - Desaparece el concepto **"Proyecto"** del modelo.
  - **"Acción" se renombra a "Servicio"** en toda la aplicación (alineado con Celero, donde *tipo de servicio = servicio*, no proyecto).
  - Trasladar la nomenclatura **departamento / cliente / servicio** a todas las pantallas y filtros.

### Impacto en el código actual (`C:\dev\SIG-es`)

| Capa | Artefacto actual | Acción |
|------|------------------|--------|
| Domain | `Project`, `ProjectCostCenter`, `ProjectUser` (Entities.cs) | **Eliminar** `Project`; el vínculo Cliente↔Servicio pasa a ser directo. Las relaciones de CECO/Usuario migran a `Service`. |
| Domain | `Action`, `ActionConcept`, `ActionUser` | **Renombrar** a `Service`, `ServiceConcept`, `ServiceUser`. |
| Domain | `EstadoProyecto`, `EstadoAccion` (Enums.cs) | Eliminar `EstadoProyecto`; renombrar `EstadoAccion`→`EstadoServicio`. |
| Domain | `TarifaProyecto`, `PresupuestoProyecto` | Reasociar a `Service` (revisar si pasan a `TarifaServicio` / `PresupuestoServicio`). |
| API | `ProjectsController.cs` | **Eliminar.** |
| API | `ActionsController.cs` | **Renombrar** a `ServicesController` + rutas `/api/servicios`. |
| Application | DTOs/Services/Validators con `Project`/`Action` | Renombrar en cascada. |
| Frontend | `features/projects` | **Eliminar** feature, ruta y entrada de menú. |
| Frontend | `features/actions` | **Renombrar** a `features/services`, rutas, labels, `data-testid`. |
| Frontend | filtros con "acción" | Renombrar etiqueta a **"servicio"** en todas las pantallas. |

> ⚠️ **Migración EF Core:** requiere migración con renombrado de tablas/columnas y FKs. Planificar script de datos (los `proyectoId` existentes deben resolverse a `servicioId`).

**Clasificación:** 🟦 FRONT + 🟥 BACK (ambos, masivo). **Estado:** ✅ DECIDIDO.

---

## 2. CAMBIOS POR PANTALLA (clasificados BACK / FRONT)

### 2.1 Dashboard (Slides 3-4) — 🟦 FRONT (mayoría) + algo de BACK
- Margen mostrado es **margen bruto** (no neto/DB): vista operativa. *(sin cambio de cálculo, solo etiquetado)*. **FRONT.**
- Junto a "MARGEN PROMEDIO" mostrar también el **coste real de lo facturado**. **FRONT** (dato ya disponible).
- "Cierre completado" se refiere al **cierre de costes**: añadir etiqueta equivalente para **cierre de facturación** (distinguir ambos). **FRONT** (depende de §2.5 separación de estados). **BACK** para exponer ambos estados.
- "Facturación por cliente": ordenar columnas como la visión global → **€ primero, margen después**. **FRONT.**
- Unificar todos los importes a **miles de euros (K)**. **FRONT.**
- Eliminar gráfico **"Margen vs Objetivo"** (duplica margen promedio) → mostrar el objetivo como valor dentro del recuadro de margen promedio. **FRONT.**
- Eliminar gráfico **"Objetivos del período"** → integrarlo en el recuadro de facturación total. **FRONT.**
- Filtros: añadir **"servicio"** (antes "acción"). **FRONT.**
- Confirmar que el **filtro de período se aplica a todos los gráficos**. **FRONT** (verificación).
- **Alertas interactivas:** al pulsar cada mensaje, redirigir a la ventana correspondiente (p. ej. pagos pendientes → ventana de aprobación). **FRONT** (routing) + **BACK** (deep-link: la alerta debe traer el id/contexto destino).

**Estado:** ✅ DECIDIDO.

### 2.2 Cliente (Slide 6) — 🟥 BACK + 🟦 FRONT
- Pantalla informativa: añadir los **mismos filtros que el dashboard** (mismos nombres). **FRONT.**
- Mostrar **el/los CECOs** del cliente (no solo la cantidad). **FRONT** (+ BACK si el dato no se proyecta hoy).
- Al seleccionar cliente, **navegar al detalle de sus servicios**. **FRONT.**
- Añadir **datos que no vienen de Celero** (rol, teléfonos, emails): el alta es en Celero; la app solo completa metadatos. **BACK** (campos locales sobre entidad sincronizada) + **FRONT** (formulario).
- Añadir **resumen de facturación** del cliente. **BACK** (agregado) + **FRONT.**
- Estado del cliente limitado a **activo / inactivo** ("en revisión" aplica a servicios, no al cliente). **BACK** (enum) + **FRONT.**
- **NUEVO: Incidencias de cliente** — registrar incidencias (tipo, explicación, estado), editables, **con histórico**, N por cliente. **BACK** (nueva entidad + CRUD + endpoints) + **FRONT** (pestaña/sección en ficha).

**Estado:** ✅ DECIDIDO.

### 2.3 Servicios (antes Acciones) (Slide 9) — 🟦 FRONT + 🟥 BACK
- "Acción" → **"SERVICIO"** en toda la app (ver §1). **BACK + FRONT.**
- En la tabla de servicios añadir columnas **departamento** (viene de Celero), **facturación** y **margen**, con los mismos filtros que el dashboard. **BACK** (proyección de datos) + **FRONT.**
- Dentro de cada servicio, **seleccionar de un desplegable los usuarios** (interlocutores, gestores, backoffice…), predefinidos por el cliente y editables. **FRONT** + **BACK** (`ServiceUser`).
- La **edición del servicio se limita a su estado** (activo / inactivo). **FRONT** (restricción UI) + **BACK** (validación).

**Estado:** ✅ DECIDIDO.

### 2.4 Conceptos (Slide 11) — 🟥 BACK (mayoría)
- **Separar en dos ventanas/entidades independientes:** conceptos de **PAGO** y de **FACTURACIÓN**. **BACK** (modelo/endpoints) + **FRONT** (2 pantallas).
- **Conceptos de pago:** predefinidos de Celero, autocompletados, editables solo por **ciertos roles**, con opción de añadir/modificar cálculos. **BACK** (RBAC + cálculo).
- **Cálculo basado en preguntas/respuestas del cuestionario de Celero**: la visita finalizada o el detalle dentro de la visita como concepto, + lógica parametrizada → coste total de la visita para ese servicio. Importa el **nivel de detalle** (concepto "por visita" toma visitas del servicio; concepto "tipo mueble" necesita el detalle de lo hecho dentro de la visita). **BACK** (motor de cálculo). ⚠️ **REQUIERE REUNIÓN** (ver §4).
- **Diccionario de equivalencias con conceptos de nómina (Innuva):** ¿campo ligado a cada concepto? **BACK** (nuevo campo de mapeo en `Concept`).
- **Un concepto = un cálculo** (sumar gastos, contar visitas…) **+ opcional filtro/pretratamiento** de datos antes de calcular, personalizable por cliente. Ejemplos: descontar primeros X km (Payhawk; primeros 50–100 km no se pagan), máximo de gasto/día (20 €/día). **BACK** (motor: pre-filtros configurables).
- **Conceptos de facturación:** parten de los de pago, pueden **agruparse, aplicar multiplicador o % sobre el coste, tarifa predefinida o reglas por casuística**. **BACK.** ⚠️ **REQUIERE REUNIÓN.**
- Revisar el filtro **"Hasta" duplicado**. **FRONT** (bug).

**Estado:** ⚠️ PARCIAL — la estructura (separar pago/facturación, pre-filtros, diccionario Innuva, filtro duplicado) está decidida; la **parametrización fina del cálculo por nivel de detalle** requiere reunión.

### 2.5 Periodos (Slide 13) — 🟥 BACK (modelo + máquina de estados)
- **Diferenciar el estado del cierre de nóminas (costes) del cierre de facturación.** **BACK** (dos estados separados).
- Añadir campo **"fecha de pago"**: unos cobran el día 30 (fin de mes), otros el día 15 (mes vencido); en ambos casos se calcula sobre la actividad del **mes natural**. La **facturación tiene límite de cierre el día 9** de cada mes. **BACK.**
- **Cierre por servicio, no global:** no se cierra hasta que todos los periodos están cerrados; mostrar estado de cierre **por servicio** y **aprobar por servicio**. **BACK** (granularidad) + **FRONT.**
- El **interlocutor puede aprobar o bloquear** el cierre; **validación previa de gastos** antes de cerrar. **BACK.**
- **Eliminar** los campos "ámbito" y "responsable" de esta pantalla. **FRONT.**
- **Estados del proceso:** `Abierto, Revisado, Revisado con alertas, Bloqueado (definir casos), Cerrado (lo cierra Finanzas/FICO)`. **BACK** (ampliar enum). ⚠️ "Bloqueado: definir en qué casos" pendiente.

> El enum actual `EstadoPeriodo { Abierto, Cerrado, Bloqueado }` **se queda corto**. Requiere rediseño a doble dimensión (costes vs facturación) + nuevos estados.

**Estado:** ⚠️ PARCIAL — estados y separación costes/facturación decididos; "casos de Bloqueado" pendiente.

### 2.6 Aprobaciones — Vista y detalle (Slide 15) — 🟦 FRONT (mayoría) + 🟥 BACK
- **Vista global en matriz:** empleados en filas, conceptos en columnas. **FRONT** + **BACK** (endpoint matriz).
- **Drill-down** por día, empleado/servicio y concepto. Al pulsar concepto/persona/servicio → detalle por día separado por concepto. **FRONT** + **BACK.**
- **Filtros que ocupen poco espacio** (máximo a la zona de revisión). **FRONT.**
- Vista adicional: **detalle por día de trabajo de cada persona** que ha trabajado. **FRONT** + **BACK.**
- Vista adicional: **visitas realizadas por una persona en los distintos servicios**. **FRONT** + **BACK.**
- **Por cada línea de detalle:** campo de **pago adicional**, **facturación adicional** y **comentario justificativo** para ajustes (visitas/trabajos/rutas excepcionales) hechos por interlocutor/gestor. **BACK** (`ClosureLine` + campos de ajuste) + **FRONT.**

**Estado:** ✅ DECIDIDO.

### 2.7 Pagos / Aprobaciones — Conceptos (Slide 16) — 🟥 BACK
- Mostrar conceptos en este **orden fijo:** salario bruto, incentivos, visitas extra, suplidos no cotizables, dietas no PayHawk, km no PayHawk. **FRONT** (orden) + **BACK** (categorización).
- Al situarse sobre cada concepto, ir al **detalle** (ej.: salario = 15 visitas × 10 € = 150 €). **FRONT** + **BACK.**
- **Añadir un importe** dejando registro de **qué se añadió, quién y motivo**. **BACK** (auditoría de ajuste) + **FRONT.**
- **Cada persona aparece tantas veces como llamamientos/contratos tenga** (info de contrato desde **Innuva**), sumando los pagos al llamamiento correspondiente. **BACK** (granularidad de línea por contrato — integración Innuva).
- Revisar la **utilidad del botón "rechazar"** en esta ventana. **FRONT** (decisión UX pendiente).

**Estado:** ✅ DECIDIDO (salvo utilidad de "rechazar").

### 2.8 Pagos / Aprobaciones — Totales y flujo (Slide 17) — 🟥 BACK
- Revisar si la **tabla inferior es necesaria** (repite info superior). **FRONT.**
- **Desdoblar columna "Total"** en dos: **total salario** y **total gastos**. **BACK** (agregados) + **FRONT.**
- **Flujo de aprobación DECIDIDO:**
  1. **Cálculo** → automático por el sistema, **NO debe mostrarse**.
  2. **Revisión por Operaciones (FF).**
  3. **Aprobación FICO.**
  4. **Dirección** → *"seguramente no será necesario"*.
  5. **Cierre FICO.**

> El enum `ApprovalStep` actual debe alinearse a: `RevisionOperaciones → AprobacionFICO → (Direccion opcional) → CierreFICO`. Eliminar el paso de cálculo del flujo visible.

**Estado:** ✅ DECIDIDO (Dirección por confirmar).

### 2.9 Facturación (Slides 18-19) — ⚠️ PENDIENTE (revisar Lourdes/FICO)
> Bloque marcado *"a revisar por Lourdes. Comentarios inconclusos. Falta revisión y comentarios de FiCo."*

Comentarios preliminares (NO implementar hasta validar):
- Sustituir "recurso" por **"Categoría de factura"** (según config. facturación: gastos de personal, servicios de campo, etc.).
- Conceptos de categoría con **valores precargados** vs **manuales** (facturas de proveedores, al menos fase 1).
- Posibles **conceptos extra a facturar** o **importes a abonar**.
- Campo de **comentario interno** (para el aprobador FICO) y **comentario externo** (para incluir en la factura).
- Campo con la **PO del servicio**.
- En el detalle borrador, mostrar los **conceptos que suman en la categoría** (ej. servicios de campo: salario bruto, incentivos, visitas extras).
- Ver **todas las facturas emitidas a un cliente** según filtro de fechas.

**Estado:** ⚠️ PENDIENTE DE FICO. **Naturaleza:** mayormente BACK (modelo de facturación) + FRONT.

### 2.10 Contabilidad (Slides 20-21) — ⚠️ PENDIENTE (revisar Lourdes/FICO) + 🟥 BACK
> Bloque marcado *"a revisar por Lourdes. Falta FICO."*
- Falta **selector para servicio** que se selecciona y envía a pago o facturación. **FRONT** + **BACK.**
- Tener en cuenta **costes de servicios externos** que aplican a facturación (p. ej. **logística**). **BACK.**
- Incluir **costes de logística como coste de proyecto/servicio**, diferenciando lo **pagado a proveedores** vs lo **refacturado al cliente**; asegurar cálculo de **márgenes**. **BACK.**
- Revisar con H&K la implementación de la logística.

**Estado:** ⚠️ PENDIENTE DE FICO.

### 2.11 Informes (Slide 23) — 🟨 MIXTO (Power BI / vistas SQL)
- Filtros comunes (dpto, cliente, servicio, rango fechas, mes, año). **FRONT.**
- De inicio mostrar info **por departamento** y luego **drill-down** a cliente → servicio, y **drill-out** viceversa. **FRONT** + **BACK** (vistas).
- OK mostrar facturación, margen, costes.
- Añadir **previsión de ventas y margen bruto por mes/cliente**; informe que compare **previsión vs facturación** (por dpto, cliente, mes) e ídem para margen bruto. **BACK** (vistas SQL forecast). *(comentar con facilitadores)*.
- Informe solo del **forecast** de ventas y margen por dpto/cliente/mes. **BACK.**
- Informes de facturación, costes, márgenes y productividad; incluir **informe de traspaso entre CECOs**. **BACK** (vistas).
- *(Propuesta H&K, no en petición original):* validación y priorización por las facilitadoras; **roles separados aunque tengan permisos similares**.

**Estado:** ⚠️ PARCIAL — forecast pendiente de comentar con facilitadores.

### 2.12 Usuarios (Slide 25) — 🟥 BACK + 🟦 FRONT
- **Roles separados aunque tengan permisos similares** (facilita gestión futura). **BACK** (modelo de roles).
- El **rol determina el acceso** a clientes/proyectos/acciones (visibilidad global o por proyecto/servicio). **BACK** (RBAC + ownership).
- Un usuario puede tener **varias asignaciones** (cliente y servicios). **BACK** + **FRONT.**

**Estado:** ✅ DECIDIDO.

### 2.13 Roles (Slide 27) — 🟥 BACK
- Falta **permiso para ver auditorías**. De CECO a servicio ya se asignan desde Celero (se determinan ahí). **BACK** (nuevo permiso).
- Tabla de roles: visualización, validación, edición y creación de usuarios/roles. **FRONT** + **BACK.**
- Añadir permiso para **auditorías** y revisar asignación de permisos de edición. **BACK.**

**Estado:** ✅ DECIDIDO.

### 2.14 CECOs (Slide 29) — 🟦 FRONT (menor)
- Quitar el dato que "no aplica en esta vista" (abajo ya se ven las acciones/servicios a las que imputa cada CECO). **FRONT.**
- CECOs y departamentos **se sincronizan desde Celero**; edición mínima e informativa. **BACK** (read-only desde Celero) + **FRONT.**

**Estado:** ✅ DECIDIDO.

### 2.15 Departamentos (Slide 31) — 🟦 FRONT
- **Eliminar "USUARIOS DEL DEPARTAMENTO"** (solo a efectos de analítica/estadística); mantener solo los **clientes vinculados**. **FRONT** (+ BACK si se quita proyección).
- Eliminar campos innecesarios; mantener solo datos relevantes (uso informativo/analítica).
- **Reducir la ventana global y ampliar la del detalle.** **FRONT.**
- En el detalle, los usuarios de departamento deben incluir el **CECO al que están imputados**. **FRONT** + **BACK** (dato).

**Estado:** ✅ DECIDIDO.

### 2.16 Auditoría (Slide 33) — 🟥 BACK + 🟦 FRONT
- Añadir columna con botón **"detalle"** para ver lo gestionado en cada registro. **FRONT** + **BACK** (exponer before/after).
- Registrar **todas las acciones** (aprobaciones, ediciones, rechazos) y acceder al detalle de cada registro (trazabilidad). **BACK.**

**Estado:** ✅ DECIDIDO.

---

## 3. BLOQUES PENDIENTES — NO IMPLEMENTAR (sin validación)

Marcados explícitamente en el PPT como *"pendiente de revisar / comentarios inconclusos / falta revisión FICO o FF / no validada"*:

| Slide | Bloque | Estado | Responsable |
|-------|--------|--------|-------------|
| 18-19 | **Facturación** | Comentarios inconclusos | Lourdes + FICO |
| 20-21 | **Contabilidad** | Comentarios inconclusos | Lourdes + FICO |
| 34-35 | **Configuración Presupuesto** | Pendiente de revisar | Facilitadores (FF) |
| 36-37 | **Configuración Factura** | Pendiente de revisar | Facilitadores (FF) |
| 38-39 | **Errores Nóminas/Pagos** | Lista entregada; Eladio la actualizará con ejemplos | Eladio + FF |
| 40-41 | **Errores Facturación** | Definir como "Alertas de desviación"; posible vista única ERRORES NÓMINA/FACTURACIÓN | FF |
| 42-43 | **Traspaso CECOs** | **No validada**; Eladio enviará proceso e indicaciones | Eladio |
| 11 | Parametrización fina del cálculo de Conceptos (nivel de detalle visita/mueble) | Requiere reunión dedicada | SIG + H&K |

### Notas de detalle de los bloques pendientes (para no perder contexto)
- **Errores Nóminas (S39):** diferenciar **errores bloqueantes** (impiden el cierre) de **avisos** (justificables y descartables). Ejemplos: recursos sin contrato, DNIs no localizados, kilometrajes fuera de rango, conceptos manuales sin observación. Añadir inconsistencias de relaciones.
- **Errores Facturación (S41):** mismo tratamiento que nómina; analizar conceptos de facturación; posible **vista única "Alertas de desviación"**.
- **Presupuesto (S35):** introducir presupuesto inicial y compararlo con lo ejecutado (márgenes y desviaciones).
- **Config Factura (S37):** consultar con facilitadoras la **agrupación de conceptos** en facturación y su utilidad antes de implementar.
- **Traspaso CECOs (S43):** coordinar la **actualización masiva del centro de coste en Celero** (códigos de Lourdes).

---

## 4. CHECKPOINTS Y PRÓXIMOS PASOS (Slide 44)

**Por parte de SIG:**
- Compartir la **matriz de lógicas de negocio**.
- Entregar **flujogramas detallados**.
- Confirmar la **definición de estados de cierre**.

**Por parte de h&k:**
- **Adaptar pantallas eliminando "proyectos"** (→ §1).
- Implementar los **ajustes del dashboard** (→ §2.1).
- Definir el **comportamiento de las alertas** (→ §2.1).
- Proponer solución para **incidencias de cliente** (→ §2.2).
- Preparar la **estructura de servicios** (roles, filtros) (→ §2.3).
- Consolidar el **diseño de periodos y cierres** (→ §2.5).

---

## 5. PLAN DE MIGRACIÓN PRIORIZADO (lo aplicable YA)

Ordenado por relación impacto/desbloqueo. Todo lo siguiente está **DECIDIDO** en el PPT.

| # | Cambio | Capas | Riesgo | Bloquea a |
|---|--------|-------|--------|-----------|
| **1** | **Eliminar `Project` + renombrar `Action`→`Service`** en todo el stack (entidades, enums, DTOs, controllers, features, rutas, filtros). Migración EF Core. | BACK + FRONT | Alto | Todo lo demás |
| **2** | Rediseñar `EstadoPeriodo`: separar **cierre costes vs facturación** + estados `Abierto/Revisado/Revisado con alertas/Bloqueado/Cerrado` + **cierre por servicio** + campo "fecha de pago". | BACK | Alto | 3, dashboard |
| **3** | Alinear flujo de aprobación a **Operaciones → FICO → (Dirección opc.) → Cierre FICO**; quitar paso de cálculo visible (`ApprovalStep`). | BACK | Medio | Aprobaciones |
| **4** | Nueva entidad **Incidencias de cliente** (tipo/explicación/estado + histórico) + CRUD + ficha. | BACK + FRONT | Bajo | — |
| **5** | **Ajustes Dashboard** (unidades K, fusionar gráficos en recuadros, orden de columnas €/margen, alertas clicables, filtro "servicio"). | FRONT (+BACK deep-link) | Bajo | — |
| **6** | **Campos de ajuste manual por línea** en aprobaciones (pago/facturación adicional + motivo + autor) en `ClosureLine`. | BACK + FRONT | Medio | — |
| **7** | **Separar Conceptos pago/facturación** + **pre-filtros configurables** (descuento km, máx €/día) + **diccionario equivalencias Innuva** (campo en `Concept`). | BACK + FRONT | Medio | Facturación |
| **8** | **Permiso de auditoría** en roles + columna "detalle" en Auditoría (before/after). | BACK + FRONT | Bajo | — |
| **9** | Ajustes Cliente (filtros dashboard, CECOs, navegación a servicios, metadatos no-Celero, resumen facturación). | FRONT + BACK | Bajo | — |
| **10** | Ajustes Servicios (columnas dpto/facturación/margen, selector usuarios, edición solo estado). | FRONT + BACK | Bajo | depende de #1 |
| **11** | Ajustes Departamentos/CECOs (quitar usuarios dpto, reducir global/ampliar detalle, CECO imputado, read-only Celero). | FRONT | Bajo | — |

> **Recomendación de ejecución:** empezar por **#1** (renombrado estructural). Hasta no resolverlo, los cambios #5, #9, #10 generan deuda (tocan pantallas que cambian de nombre). #2 y #3 son la segunda ola (modelo de cierre/aprobación). El resto puede paralelizarse.

---

## 6. RESUMEN — clasificación global BACK vs FRONT

| Predominio | Pantallas / temas |
|------------|-------------------|
| 🟥 **BACKEND** | Eliminación Proyecto, motor de Conceptos (pago/facturación, pre-filtros, Innuva), estados de Periodos, flujo de aprobación, granularidad por contrato/llamamiento (Innuva), incidencias de cliente, logística/contabilidad, permisos/roles, vistas SQL de informes/forecast, auditoría detallada. |
| 🟦 **FRONTEND** | Maquetación dashboard (unidades, gráficos, orden), vista matriz de aprobaciones, filtros compactos, navegación/drill, renombrado nomenclatura, departamentos/CECOs, restricciones de edición UI. |
| 🟨 **MIXTO** | Informes (Power BI + vistas SQL), alertas clicables (routing + contexto), columnas calculadas en tablas (proyección + render). |

---

*Documento generado a partir del análisis de las 44 diapositivas. Los apartados §3 quedan a la espera de validación de FICO/FF/Eladio antes de pasar a implementación.*
