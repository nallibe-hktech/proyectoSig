# Reconciliación CECO ↔ TravelPerk — hallazgos

**Fecha:** 2026-06-23 · **Actualizado:** 2026-06-25 (SIG confirma la regla + entrega el maestro completo)
**Origen:** revisión de la documentación del cliente + repo, tras detectar que casi ningún `Cost object` de TravelPerk casaba con el maestro de CECOs.

## ✅ CONFIRMADO POR SIG (2026-06-25)

Las tres preguntas abiertas quedan resueltas por el cliente:

1. **Las 4 cifras de TravelPerk son los 4 primeros dígitos del CECO de 6**, y vale para **todos** los proyectos (ya no solo el caso cruzado). Ej: `0103` → `010301`/`010302`.
2. **Todas las subcuentas que comparten prefijo son del mismo cliente/proyecto.** Convención de subcuenta: **`01` = PERS. CAMPO**, **`02` = OPERACIONES**. Un mismo prefijo de 4 NUNCA corresponde a clientes distintos ⇒ el prefijo de 4 es suficiente para imputar.
3. **SIG entregó el listado maestro completo** (6 díg + nombre). Cargado en `backend/seed-cecos-reales.local.sql` (local, no versionado).

Las "inferencias" de la sección de abajo quedan **validadas**; se conservan como histórico. Detalles operativos pendientes (no bloquean la regla) en "Acciones pendientes".

> **Gobierno del dato:** este documento está **anonimizado**. Los nombres reales de cliente y los CECOs reales NO se versionan; viven solo en `backend/seed-cecos-reales.local.sql` (gitignored vía `*.local.sql`). Aquí se usan placeholders (Cliente A, B…) y códigos de ejemplo con el formato real.

> **Nota de rigor:** se separa lo **leído literalmente** (HECHO) de lo **inferido** (INFERENCIA). Nada inferido debe presentarse a SIG como conclusión cerrada; va como pregunta a confirmar.

## Resumen ejecutivo

El maestro de CECOs del repo (seed) solo tiene códigos de ejemplo, ninguno correspondiente a un proyecto de TravelPerk → por eso "no casa nada". La **regla de correspondencia más plausible** está deducida del cruce documental, pero **solo está cruzada del todo en un caso (Cliente A)**; el resto se apoya en un patrón de formato consistente y requiere confirmación de SIG. El `TravelPerkCecoResolver` ya implementa esa regla de forma defensiva (si no hay match único, marca `CECO_NO_MAESTRO` en vez de adivinar).

## HECHOS (leídos literalmente, sin interpretación)

1. El **fichero de ejemplo de descarga TravelPerk** (hoja `report`) trae 8 `Cost object` distintos, **con su nombre escrito en el propio campo**, en formato `NNNN_NOMBRE` (4 cifras + nombre de cliente), más 1 línea **sin** `Cost object` (la cuota `Subscription fee`).
2. Las **plantillas de Pagos por Proyecto** tienen una columna literal **`CECO DESTINO (PROYECTO)`** con código de **6 cifras + nombre** (formato `NNNNNN NOMBRE`).
3. En los **libros de gastos del cliente** existen varios códigos de 6 cifras. (Su columna de descripción es texto libre de gasto, NO un nombre de cliente fiable.)
4. El seed versionado solo tiene CECOs de ejemplo (formato `0NNNSS`).
5. Modelo de datos (hoja **Entidades** del libro de cierres): *"Un Ceco contiene uno o varios Proyectos"*, *"Proyecto pertenece a un Ceco"*, origen del Ceco = **A3 Innuva** ("Ceco contable del contrato").
6. Hoja **Conceptos x Proyecto**: "Viajes Travel Perk" figura como concepto de **Facturación** para 2 clientes (columna Pago vacía).

## CRUCE VERIFICADO (dos señales independientes: número Y nombre)

- **Solo Cliente A:** su `Cost object` de 4 cifras (TravelPerk) coincide en **número y nombre** con su CECO de 6 cifras de la plantilla de Pagos. Ejemplo de la forma: `0NNN_CLIENTE-A` ↔ `0NNNSS CLIENTE-A`. Este caso sí es sólido.
- **Formato `0`+proyecto(3)+subcuenta(2):** confirmado por 4 códigos con nombre en las plantillas.

## INFERENCIAS (NO confirmadas por documento — requieren SIG)

- **Regla general "4 díg TP = 4 primeros del CECO de 6":** se apoya en **un solo caso totalmente cruzado (Cliente A)** + el formato consistente. Muy plausible, **no probada** para cada proyecto.
- **Asociación de los demás códigos a su cliente:** el nombre lo dice TravelPerk en su propio campo; que el 6 díg concreto sea de ese cliente **no está confirmado en fuente independiente** (la descripción de los libros es texto libre, no fiable).
- **Subcuentas del mismo cliente:** que dos CECOs de 6 díg que empiezan igual (p. ej. `0NNN01`/`0NNN02`) pertenezcan al mismo cliente/servicio es **inferencia**, no dato. Es la pregunta nº2 a SIG.
- **Dos clientes** (de los 8 del fichero) **no tienen** CECO de 6 díg localizable en ningún fichero. Cualquier valor sería suposición.
- **Por qué en la prueba inicial "solo casó uno":** no verificado (no se dispone del detalle de esa prueba).

## Estado del código (no requiere cambios)

`TravelPerkCecoResolver.ResolverServiceId` resuelve en cascada: (1) código exacto, (2) prefijo numérico exacto, (3) el CECO maestro empieza por el prefijo (caso 4→6 díg). En cada nivel exige **un único Servicio**; si hay ambigüedad o no existe, devuelve `null` y la línea se marca `CECO_NO_MAESTRO`. Cubierto por `TravelPerkCecoResolverTests` (incluye el caso 4→6 díg y la ambigüedad). **No se adivina nada.**

## Solución aplicada en dev (2026-06-23)

- **Seed versionado** (`DataSeeder.SeedCostCentersAsync`): CECOs ficticios con el formato `0NNNSS` y comentario que documenta la regla. Sin datos reales del cliente.
- **Script local** `backend/seed-cecos-reales.local.sql` (gitignored vía `*.local.sql`, **no se versiona ni se envía a nadie**): carga CECOs/servicios reales para que el fichero real impute end-to-end **en la BD de dev local**. Incluye los dos códigos NO confirmados **marcados explícitamente como ASUMIDOS**; son solo para probar localmente, no para producción ni para enviar.

## Acciones pendientes

1. ~~Confirmar con SIG la regla y la ambigüedad de subcuentas y obtener el listado maestro.~~ ✅ **HECHO (2026-06-25).**
2. **Cargar los CECOs reales ↔ Servicio.** ✅ en dev local vía `seed-cecos-reales.local.sql` (catálogo completo). Para prod, la fuente "viva" definitiva = A3 Innuva (cada contrato trae su Ceco), hoy bloqueado por el 403 de permisos WK y porque el DTO de nóminas actual **no mapea el campo Ceco**.
3. ~~Decidir el trato de los CECOs estructurales/internos de SIG.~~ ✅ **IMPLEMENTADO (2026-06-25).** Los CECOs estructurales (departamentos de SIG, sin Servicio de cliente: Dirección, Comercial, Finanzas, RRHH, BI, Marketing, Admin Temporal) se reconocen como **gasto interno SIG** (igual que el `0423`), no como `CECO_NO_MAESTRO`. Mecánica: el maestro contiene esos CECOs sin vincular a Servicio; `TravelPerkCecoResolver.EsCecoInternoSig` casa por código/prefijo contra ellos (`ICostCenterRepository.GetInternalSigCecoCodesAsync` = CECOs sin Servicio). El dashboard clasifica "Gasto SIG" = `ServiceId == null && ErrorProcesamiento == null`. Suite 468/468 verde.
4. **Aclarar conflicto de plantillas vs maestro:** las plantillas de Pagos por Proyecto asignaban `010401→JDE`, `010901→MOLINS`, `023301→DAIKIN`, mientras el maestro SIG dice `SALES FORCE`, `SALES TACTICAL`, `NEW BUSINESS SALES SERVICES`. Solo Granini (`010301`) coincide en ambas. ¿Códigos antiguos / otra numeración / clientes movidos?
5. **Typos en el listado recibido** (a confirmar): la 2ª línea de COTY figura como `010201` (debería ser `010202`) y la 2ª de SALES SERVICES como `023101` (debería ser `023102`); el resto de pares respeta `01`/`02`. El seed local asume la corrección.

## Pregunta a SIG (solo hechos + lo que pedimos confirmar)

> **Asunto: TravelPerk — a qué proyecto/CECO va cada gasto**
>
> Estamos cargando los gastos de TravelPerk y necesitamos confirmar cómo imputarlos.
>
> En TravelPerk cada gasto trae un código de proyecto de **4 cifras** (lo que pone el fichero). En vuestra contabilidad el CECO tiene **6 cifras**.
>
> 1. ¿Es correcto que **las 4 cifras de TravelPerk son el principio del CECO de 6** y las 2 últimas son la subcuenta? Solo lo hemos podido verificar del todo con un cliente; queremos confirmar que vale para todos.
>
> 2. Vemos proyectos con **varias subcuentas** (códigos de 6 cifras que empiezan igual). ¿Esas subcuentas son **siempre del mismo cliente/proyecto**, o un mismo código de 4 cifras de TravelPerk puede corresponder a **clientes distintos**? (TravelPerk solo nos da las 4 cifras, así que si pudieran ser clientes distintos no sabríamos a cuál cargar.)
>
> 3. ¿Nos podéis pasar la **lista completa de CECOs activos (6 cifras + nombre)**? No hemos localizado el CECO de 2 de los proyectos; sobre uno de ellos, ¿es un cliente o algo interno de SIG (como el `0423` de la cuota)?
>
> Los gastos cuyo proyecto no podamos identificar con seguridad los dejaremos **marcados como pendientes de imputar**, no los adivinamos. El `Subscription fee` (sin proyecto) ya lo imputamos a SIG (`0423`), como confirmasteis.
