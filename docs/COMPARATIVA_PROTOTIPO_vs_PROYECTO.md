# Comparativa COMPLETA — Prototipo PDF + PPT de feedback vs. Proyecto

**Fecha análisis:** 2026-06-19
**Autor:** Revisión pantalla por pantalla (no se omite ninguna)

## Fuentes revisadas (las tres, íntegras)

| Fuente | Contenido | Naturaleza |
|---|---|---|
| **A. PPT feedback** — `Pantallas actualizadas - HK_10062026 (formato unificado) (2).pptx` | **47 slides** (30 capturas + comentarios de H&K) | Correcciones/peticiones sobre las pantallas existentes |
| **B. Prototipo PDF** — `Prototipo SIG - Pantallas.pdf` | **28 pantallas mockup** (1 imagen/pantalla, 29 págs.) | Diseño objetivo definitivo maquetado a 1440px (18-jun-2026) |
| **C. Proyecto** — `frontend/src/app` | **50+ rutas/componentes** (`app.routes.ts` verificado) | Lo construido a día de hoy |

> Las dos fuentes de cliente son **complementarias**: el **PPT (A)** dice *qué cambiar*; el **Prototipo PDF (B)** muestra *cómo debe quedar*. El detalle del PPT está en `docs/COMPARATIVA_PPT_PANTALLAS.md` (slides 1-47). Este documento añade el **prototipo PDF** y cierra el triángulo A↔B↔C.

**Leyenda:** ✅ existe y alineado · ⚠️ existe pero incompleto vs prototipo · ❌ no existe · 🔶 discrepancia/decisión a confirmar

---

## 1. Arquitectura de información (menú lateral)

**Menú del prototipo (B) — canónico:**
- **PRINCIPAL**: Dashboard · Clientes · **Incidencias** · Servicios · Conceptos · Periodos · Aprobaciones · **Contabilidad** · Informes
- **ADMINISTRACIÓN**: Usuarios · Roles · CECOs · Departamentos · Auditoría
- **CONFIGURACIÓN**: Config. Presupuesto · Config. Factura · Errores Nómina/Pagos · Errores Facturación · Traspaso CECOs

**Menú del proyecto (C):** Dashboard · Clientes · Servicios · Conceptos · Periodos · Aprobaciones · Alertas · Contratos un-día · Auditoría · Sync · integraciones (Galán, Mediapost, Bizneo, Intratime, PayHawk, Celero) · Reports · Forecast · Variables · CECOs · Departamentos · Roles · Usuarios.

**Diferencias de IA (lo que el prototipo pide y el menú actual NO refleja):**
- ❌ **Incidencias** como entrada de 1er nivel (hoy vive dentro de la ficha de cliente).
- ❌ **Contabilidad** como entrada de 1er nivel (no existe pantalla).
- ❌ **Config. Presupuesto**, **Config. Factura**, **Errores Nómina/Pagos**, **Errores Facturación**, **Traspaso CECOs** como bloque «CONFIGURACIÓN» (hoy: presupuestos viven en sub-tab de servicio; errores unificados en `/alertas`; las otras tres no existen).

---

## 2. Tabla maestra — las 28 pantallas del Prototipo (B) ↔ PPT (A) ↔ Proyecto (C)

| # PDF | Pantalla prototipo | Slide PPT | Ruta proyecto | Estado | Qué falta vs prototipo |
|---|---|---|---|---|---|
| 1/28 | **Login (Acceso)** | — | `/login` `LoginComponent` | ⚠️ | Prototipo muestra botón **Microsoft / Azure AD SSO** + "recordarme" + "olvidaste contraseña". Proyecto usa JWT propio con credenciales demo. (Regla global: JWT por defecto; SSO solo si se confirma) |
| 2/28 | **Dashboard — Resumen Ejecutivo** | 3-4 | `/dashboard` | ✅ | Alineado (Ola 2): filtros dpto/cliente/servicio/período, KPIs Facturación/Margen+coste real/Cierre Nóminas/Cierre Facturación/Pend. Aprobación, evolución, facturación por cliente, alertas interactivas. Objetivo € sigue placeholder (SUP-07) |
| 3/28 | **Clientes** | 6 | `/clients` `ClientsListComponent` + `client-detail` | ⚠️ | Falta en la ficha: **CECOs del cliente** (chips), **metadatos** (rol contacto, teléfono, email, condiciones pago, dirección facturación), **resumen de facturación** (acumulada, margen bruto, última factura, pendiente cobro), **usuarios vinculados** con quitar/+añadir, botón **Duplicar**, aviso "incidencia abierta → Ver incidencias" |
| 4/28 | **Incidencias** (listado) | 6 | ❌ **No existe como pantalla** | ❌ | Existe la entidad `ClienteIncidencia` + API, pero **solo como sección dentro de `client-detail`**. El prototipo la quiere como **pantalla de 1er nivel** con filtros (cliente/tipo/estado), tabla, panel de detalle + histórico |
| 5/28 | **Incidencias · Nueva incidencia** (dialog) | 6 | ⚠️ (alta inline en client-detail) | ⚠️ | Falta el **dialog dedicado** (cliente, tipo, descripción, fecha apertura, origen/responsable, estado inicial Pendiente/En proceso/Resuelta) |
| 6/28 | **Servicios** | 9 | `/services` `services-list` + `service-detail` | ⚠️ | Faltan **columnas departamento / facturación / margen** en la tabla + filtros del dashboard; en detalle faltan **conceptos asociados** (asociar existente) y **usuarios del servicio** (asignar de desplegable) |
| 7/28 | **Conceptos de Pago** | 11 | `/concepts` `concepts-list` + `formula-editor` | ⚠️ | Prototipo: **dos pestañas** Pago/Facturación, editor inline, campo **EQUIVALENCIA NÓMINA · INNUVA**, selector **APLICA A** (servicios), **jerarquía Global/Servicio/Empleado**, columna **Hasta**. Faltan: diccionario equivalencias Innuva, jerarquía de aplicación, pretratamiento por cliente |
| 8/28 | **Periodos** | 13 | `/periods` `periods-list` | ⚠️ | Prototipo: panel detalle con **5 estados** (Abierto/Revisado/Revisión con alertas/Bloqueado/Cerrado), columnas **Cierre Nóminas vs Facturación**, fechas de pago (grupo A día 30 / B día 15 / emisión día 9), **cierre por servicio** con OK/Alerta, **Validación previa de gastos**, **Aprobar/Bloquear**. Proyecto: enum solo `{Abierto, Cerrado, Bloqueado}` y lista sin panel detalle rico |
| 9/28 | **Aprobaciones — Pagos** | 15-17 | `/approvals` + `closure-detail` | ⚠️ | Prototipo: **matriz dinámica** filas=empleado (agrupado por acción) × columnas=conceptos (salario bruto, pago visitas, km, dietas, incentivos, visitas extra, total), detalle vertical por concepto, **flujo Cálculo→Revisión→FICO→Dirección→Cierre**. Proyecto tiene matriz en closure-detail pero falta el orden fijo de conceptos, desdoblar Total salario/gastos, multiplicidad por contrato |
| 10/28 | **Aprobaciones · Facturación** | 19 | ⚠️ `/cierres-facturacion` | ⚠️/❌ | Prototipo: **Borrador de Facturación** editable (coste pagos → facturación borrador con override por línea), **historial de versiones** del borrador, **Enviar a aprobación**. Faltan campos del PPT slide 19: Categoría de factura, comentario interno/externo, PO, ID servicio. La UI de borrador editable no existe así |
| 11/28 | **Contabilidad** | 20-21 | ❌ **No existe** (solo `ExportsController` backend + `/sync`) | ❌ | Pantalla nueva: cierres aprobados listos para contabilizar, **Generar y enviar a A3** (A3 Innuva nóminas / A3 ERP facturas), destinos, **historial de envíos con errores por línea**, formato fichero. Falta selector servicio→pago/facturación, preview fichero, logística, masivos |
| 12/28 | **Informes** | 23 | `/reports` `ReportsComponent` | 🔶 | **DISCREPANCIA**: el prototipo muestra **Power BI embebido** (tabs Margen/Coste/Facturación/Productividad/Visión 360 + "Abrir en Power BI") marcado *"pendiente de confirmar con SIG si será Power BI o pantallas propias"*. El proyecto **eliminó Power BI** y construyó informes nativos drill-down (SUP-08, migración `DropBiSchema`, "el cliente NO usa Power BI"). **Hay que reconciliar esta decisión con H&K** |
| 13/28 | **Usuarios** | 25 | `/users` `users-list` + `user-detail` | ⚠️ | Prototipo: panel detalle con **ASIGNACIONES múltiples** (cliente+servicio, quitar/+añadir). Proyecto tiene `ServiceUser` (N-N) pero **falta la UI de asignación múltiple** |
| 14/28 | Usuarios · Filtro abierto (Rol) | 25 | `/users` (estado) | ✅ | Variante de estado: dropdown de filtro Rol. Filtros existen |
| 15/28 | Usuarios · Resultado filtrado | 25 | `/users` (estado) | ✅ | Variante de estado: resultado filtrado. Existe |
| 16/28 | Usuarios · Página 2 | 25 | `/users` (estado) | ✅ | Variante de estado: paginación. Existe (`mat-paginator`) |
| 17/28 | **Roles** | 27 | `/roles` `roles-list` | ⚠️ | Prototipo: **matriz de permisos** (Pagos, Facturaciones, **Ceco, Departamento, Cliente, Servicio**, Usuarios, Roles, **Auditorías**) + columna **ámbito Vista Global/Servicio** + usuarios con rol. Proyecto: falta **permiso Auditorías**, ámbito por servicio, y la rejilla detallada |
| 18/28 | **CECOs** | 29 | `/cost-centers` `cost-centers-list` | ⚠️ | Prototipo: **solo lectura/informativo** ("Sincronizado desde A3 / Celero"), filtros globales dpto/cliente/servicio/período, detalle con **servicios que imputan a este CECO**. Proyecto: hoy **CRUD completo** (debería degradarse a informativo) |
| 19/28 | **Departamentos** | 31 | `/departments` `departments-list` | ⚠️ | Prototipo: detalle con **clientes vinculados** + **usuarios del departamento "SOLO INFORMATIVO"** con **CECO imputado** + responsable + cód. analítica. (Matiz: el PPT pedía *eliminar* usuarios del departamento; el prototipo los conserva marcados "solo informativo"). Falta CECO imputado por usuario |
| 20/28 | **Auditoría** | 33 | `/audit` `AuditComponent` | ✅/⚠️ | Prototipo: log con fecha/hora/usuario/tipo acción/cliente/proyecto/acción/recurso/entidad/tipo. Proyecto cubre el registro; **revisar botón "detalle"** por registro (slide 33) |
| 21/28 | Auditoría · Filtro abierto (Tipo acción) | 33 | `/audit` (estado) | ✅ | Variante de estado: filtro tipo acción |
| 22/28 | Auditoría · Resultado filtrado | 33 | `/audit` (estado) | ✅ | Variante de estado: resultado filtrado |
| 23/28 | Auditoría · Página 2 | 33 | `/audit` (estado) | ✅ | Variante de estado: paginación |
| 24/28 | **Configuración de Presupuesto** | 35 | ✅ `/config-presupuesto` `ConfigPresupuestoComponent` | ✅ | **HECHO 2026-06-19** (SUP-13). Pantalla dedicada: partidas por acción (Anual/Total acción), presupuesto/consumido **manual** (el prototipo dice "sin origen de datos"), restante/avance calculados, KPIs, margen objetivo (manual) vs real (de cierres) + desviación. Entidad `PartidaPresupuesto` + `Service.MargenObjetivoPct`. **Sin "nº personas de campo"** (el mockup no lo muestra → cierra SUP-06; forecast GPP se mantiene aparte). Sub-tab viejo superado. 376/376 tests |
| 25/28 | **Configuración de Factura** | 37-38 | ✅ `/config-factura` `ConfigFacturaComponent` | ✅ | **HECHO 2026-06-19** (SUP-12). Categorías por cliente que agrupan conceptos de facturación (entidad `CategoriaFactura`, API `api/clients/{id}/categorias-factura`, UI con selector de cliente + KPIs + editor + panel "conceptos disponibles"). Un concepto ≤1 categoría/cliente (409 si se reutiliza). Seed ilustrativo anónimo; contenidos reales pendientes de validar con SIG (lo dice el propio prototipo). 362/362 tests |
| 26/28 | **Errores Nómina / Pagos** | 40 | ⚠️ `/alertas` (unificado) | ⚠️ | Prototipo: pantalla dedicada **Validación del cierre de pagos** con severidad **Bloqueante/Aviso/Info**, recurso/acción/CECO/tipo error/detalle, **bloquea "Generar fichero nómina"** si hay bloqueantes. Proyecto: `/alertas` cubre tipos Bloqueante/Advertencia pero **no la pantalla de validación de cierre con generación de fichero** |
| 27/28 | **Errores de Facturación — Desviación vs presupuesto** | 42 | ⚠️ `/alertas` (origen Facturación) | ⚠️/❌ | Prototipo: **desviación facturado vs presupuesto** por acción (presupuesto/facturado/desviación/desv.%/estado), **umbral configurable ±x%**, "sin presupuesto". Proyecto: `/alertas` no tiene la **lógica de desviación vs presupuesto** ni umbral; falta también alerta facturado<coste (slide 42) |
| 28/28 | **Traspaso entre CECOs** | 43-46 | ❌ **No existe** | ❌ | Pantalla nueva: tabla CECO origen→destino con importes Bruto/Gastos, **neteo de líneas inversas** (origen↔destino en una sola línea contable), filtros, totales, informe. No existe nada (bloqueado: Eladio enviará proceso) |

---

## 3. PPT (A) — slides de portada/cierre sin pantalla propia

| Slide | Contenido | Estado |
|---|---|---|
| 1 | Aviso: "todo debe validarse con flujos y casos reales" | ℹ️ Nota |
| 2,5,8,10,12,14,18,22,24,26,28,30,32,34,37,39,41,43 | Portadas de sección (solo título) | ℹ️ Separadores |
| 7 | **Proyectos — se elimina** | ✅ Hecho (Ola 1: Acción→Servicio, sin "proyecto") |
| 47 | Checkpoints y próximos pasos (compromisos SIG/H&K) | ℹ️ Plan |

> Los slides 3-46 con contenido funcional están mapeados 1:1 en la tabla §2 y, en su detalle de peticiones, en `docs/COMPARATIVA_PPT_PANTALLAS.md`.

---

## 4. Pantallas del Proyecto (C) que NO están en el prototipo

Son herramientas internas/de integración/staging (el prototipo es la app operativa de cara a SIG/H&K). **No sobran**, pero conviene saber que el cliente no las ha maquetado:

| Ruta | Componente | Por qué no está en el prototipo |
|---|---|---|
| `/_smoke` | SmokeComponent | Test técnico de tema M3 |
| `/variables` | VariablesListComponent | Mapeos Celero→valor (back-office técnico) |
| `/sync` | SyncComponent | Sincronización manual de integraciones |
| `/celero-visitas`, `/celero-mapeos` | Celero* | Staging/validación de datos Celero |
| `/galan`, `/mediapost`, `/bizneo`, `/intratime`, `/payhawk` | *DashboardComponent | Dashboards de integración (datos crudos) |
| `/alertas` | AlertsListComponent | Unifica los dos "Errores" del prototipo (26/27) |
| `/contratos-un-dia` | ContratosUnDiaComponent | Gestión contratos 1 día (info Innuva) |
| `/forecast` | ForecastResumenComponent | Forecast ventas/GPP (slide 36) — 🔶 ver §5 |
| `/calculations/:id` | CalculationDetailComponent | Detalle inmutable de cálculo (drill-down) |
| `/cierres-costes`, `/cierres-facturacion` | Closures* | Motor de cierres (alimenta Aprobaciones/Contabilidad) |

---

## 5. Discrepancias y decisiones a confirmar con H&K

1. 🔶 **Informes: Power BI vs nativo.** El prototipo (12/28) reintroduce **Power BI embebido** como candidato ("pendiente de confirmar"). El proyecto ya decidió **nativo sin Power BI** (SUP-08, migración `DropBiSchema`, memoria "el cliente NO usa Power BI"). **Conflicto directo a cerrar antes de tocar `/reports`.**
2. 🔶 **Forecast GPP (slide 35 vs 36).** El prototipo lo resuelve metiendo **nº personas de campo en Config. Presupuesto** (24/28, vía slide 35), lo que **eliminaría el forecast GPP**. El proyecto siguió el slide 36 (pantalla `/forecast` con tab GPP). Pendiente confirmar (SUP-06).
3. 🔶 **Login SSO.** El prototipo muestra **Microsoft / Azure AD SSO**; el proyecto usa JWT propio. Confirmar si se exige Entra ID.
4. 🔶 **Departamentos — usuarios.** PPT (slide 31) pedía *eliminar* "usuarios del departamento"; el prototipo (19/28) los **conserva como "solo informativo"** con CECO imputado. Prevalece el prototipo (más reciente).
5. 🔶 **CECOs CRUD.** El prototipo los quiere **informativos (solo lectura)**; el proyecto tiene CRUD completo.

---

## 6. Resumen ejecutivo de gaps (orden por tamaño)

### Pantallas NUEVAS que faltaban por completo (5)
1. ✅ **Incidencias** de 1er nivel (listado + dialog nueva) — **HECHO** (ya no solo dentro de cliente).
2. ❌ **Contabilidad** (envío a A3 Innuva/ERP, historial, errores por línea, masivos). *Nota: las credenciales OAuth de A3 Innuva ya están disponibles (OneDrive `API Innuva.docx`) → desbloquea esta pantalla.*
3. ✅ **Config. Factura** (categorías de factura, mapeo conceptos por cliente) — **HECHO 2026-06-19** (SUP-12).
4. 🔒 **Traspaso CECOs** (resumen contable + neteo de líneas inversas) — *bloqueado por Eladio*.
5. ✅ **Config. Presupuesto** dedicada (partidas, desviación, márgenes) — **HECHO 2026-06-19** (SUP-13). Entrada manual (lo exige el prototipo); sin "nº personas" (cierra SUP-06).

### ⚠️ Pantallas que existen pero hay que enriquecer al nivel del prototipo (11)
Clientes (cecos/metadatos/resumen) · Servicios (columnas+usuarios) · Conceptos (2 tabs+Innuva+jerarquía) · Periodos (5 estados+validación) · Aprobaciones Pagos (matriz+orden+desdoblar Total) · Aprobaciones Facturación (borrador editable+campos PO/ID/comentarios) · Usuarios (asignación múltiple) · Roles (permiso Auditorías+ámbito) · CECOs (solo lectura) · Departamentos (CECO imputado) · Errores Nómina/Facturación (validación de cierre + desviación vs presupuesto).

### ✅ Alineadas con el prototipo (Ola 2/3 ya hechas)
Dashboard · Auditoría (registro) · base de cierres costes/facturación · flujo Grupo→FICO · alertas Bloqueante/Advertencia · incidencias (entidad+API, falta pantalla).

### 🔶 Decisiones bloqueantes a cerrar con H&K
Power BI vs nativo · Forecast GPP (presupuesto vs forecast) · Login SSO · alcance Contabilidad/logística (Lourdes) · proceso Traspaso CECOs (Eladio).
</content>
</invoke>
