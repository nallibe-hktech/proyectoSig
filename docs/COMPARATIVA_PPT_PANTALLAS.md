# Comparativa PPT «Pantallas actualizadas - HK_10062026» vs. estado del proyecto

**Fecha análisis:** 2026-06-18
**Fuente:** `Pantallas actualizadas - HK_10062026 (formato unificado) (1).pptx` (47 slides, 30 capturas + comentarios de feedback de H&K sobre las pantallas actuales)
**Objetivo:** identificar, pantalla por pantalla, qué cambios del PPT ya están hechos y qué falta.

> El PPT NO es un diseño nuevo: son **comentarios/correcciones sobre las pantallas que ya existen**. Por tanto el análisis se hace a nivel de cada petición concreta, no de "pantalla sí/no".

**Leyenda:** ✅ hecho · ⚠️ parcial · ❌ falta · 🔍 verificar en runtime · 🏗️ requiere reunión/definición externa

---

## 1. DASHBOARD (slides 3-4)

### 1.1 Resumen Ejecutivo (slide 3) — ✅ HECHO 2026-06-18 (ver SUP-07)
| Petición | Estado | Nota |
|---|---|---|
| Margen mostrado es **bruto** (no neto/DB); etiquetar como vista operativa | ⚠️ | Margen ya es € bruto; el KPI muestra ahora coste real + margen € (queda como vista operativa) |
| Junto a «MARGEN PROMEDIO» y %, mostrar **coste real** de lo facturado | ✅ | Sublínea "Coste real € XK · Margen € YK · Obj. 25%" en el KPI de margen |
| Distinguir «cierre completado» de **costes** vs **facturación** | ✅ | 4 contadores nuevos en `DashboardKpisDto`; KPIs desdoblados Costes/Facturación |
| «Facturación por cliente»: ordenar → primero importe €, luego margen | ✅ | Leyenda del donut muestra importe € y luego margen |
| En filtros añadir «servicio» (antes «acción») | ✅ | Selector de servicio en cabecera; backend filtra KPIs y mis-servicios por `serviceId` |
| Confirmar que el filtro de período se aplica a todos los gráficos | ✅ | KPIs/mis-servicios reciben periodId; avisos/alertas son globales por diseño |

### 1.2 Facturación y Margen (slide 4) — ✅ HECHO 2026-06-18
| Petición | Estado | Nota |
|---|---|---|
| Todos los importes en la misma unidad: miles de € (K) | ✅ | KPIs y desglose por cliente en K |
| Eliminar gráfico «Margen vs Objetivo» e integrar objetivo en el KPI de margen | ✅ | Gauge eliminado; objetivo % en el KPI de margen (placeholder, SUP-07) |
| Eliminar «Objetivos del período» e integrarlo en el KPI de facturación total | ✅ | Panel eliminado; objetivo € en el KPI de facturación (placeholder, SUP-07) |
| Alertas **interactivas**: redirigir al pulsar | ✅ | Ya eran clicables (redirigen al servicio del cierre); se mantiene |

---

## 2. CLIENTE (slide 6)
| Petición | Estado | Nota |
|---|---|---|
| Mismos filtros que el dashboard y con los mismos nombres | ❌ | Depende de 1.1 (filtros dashboard) |
| Mostrar los **cecos** a los que pertenece (no solo la cantidad) | ⚠️ | Modelo ServiceCostCenter existe; falta exponer cecos del cliente |
| Al seleccionar cliente, navegar al detalle de sus servicios | ✅ | `client-detail` lista servicios |
| Añadir datos que no vienen de Celero (**rol, teléfonos, emails**); alta en Celero, app completa metadatos | ⚠️ | Client tiene campos Contacto; revisar que cubra rol/teléfonos/emails como metadatos editables |
| Añadir **resumen de su facturación** | ❌ | Falta |
| Estado cliente solo activo/inactivo («en revisión» es de servicios) | ✅ | `EstadoCliente` Activo/Inactivo |
| **Registrar incidencias del cliente** (tipo, explicación, estado), editables, con histórico, varias por cliente | ✅ | **HECHO 2026-06-18**: entidad `ClienteIncidencia` + API `api/clients/{id}/incidencias` + sección en `client-detail`. Estados Abierta/EnProceso/Resuelta; histórico vía AuditLog. Ver SUP-05 |

---

## 3. PROYECTOS → se elimina (slide 7)
| Petición | Estado | Nota |
|---|---|---|
| Eliminar pantalla y concepto «proyecto» | ✅ | Hecho (Ola 1, migración RenameProjectActionToService) |
| Modelo Cliente → Servicios | ✅ | Hecho |
| Trasladar nomenclatura departamento/cliente/servicio al resto | ✅ | Mayormente hecho; 🔍 repasar textos residuales |

---

## 4. SERVICIOS (antes Acciones) (slide 9)
| Petición | Estado | Nota |
|---|---|---|
| «Acción» → «SERVICIO» en toda la app | ✅ | Hecho (Ola 1) |
| En la tabla de servicios añadir columnas **departamento** (de Celero), **facturación** y **margen**, con los mismos filtros que el dashboard | ❌ | `services-list` no tiene esas columnas/filtros |
| Dentro de cada servicio, **seleccionar de desplegable** los usuarios (interlocutores, gestores, bboo…), predefinidos por cliente y editables | ⚠️ | Existe `ServiceUser` (N-N) y endpoints; falta UI de desplegable/asignación |
| Edición del servicio limitada a su estado (activo/inactivo) | 🔍 | Verificar que `service-form` no permita editar más de lo permitido |

---

## 5. CONCEPTOS (slide 11)
| Petición | Estado | Nota |
|---|---|---|
| **Separar en dos ventanas independientes** pago y facturación | ⚠️ | Hoy una pantalla `/concepts` con dos secciones; piden dos ventanas independientes |
| Conceptos de pago: predefinidos de Celero, autocompletados, editables solo por ciertos roles, con opción de añadir/modificar cálculos | ⚠️ | Create admin-only + editor de fórmula existen; falta "predefinidos/autocompletados de Celero" |
| Cálculo basado en preguntas/respuestas del cuestionario de Celero + visita, con lógica parametrizada (nivel de detalle por visita / tipo mueble) | ❌ 🏗️ | "Falta trasladar esa parametrización" — requiere reunión |
| Diccionario de **equivalencias con conceptos de nómina (Innuva)** | ❌ | Falta (¿campo ligado a cada concepto?) |
| Un concepto = un cálculo + **filtro/pretratamiento personalizable por cliente** (ej. descontar primeros X km, máx €/día) | ❌ | Falta el pretratamiento parametrizable por cliente |
| Conceptos de facturación: parten de los de pago, agrupables, multiplicador/% sobre coste, tarifa predefinida o reglas por casuística | ⚠️ 🏗️ | Parcial; requiere reunión |
| Revisar filtro «Hasta» duplicado | ⚠️ | Bug UI a corregir |

---

## 6. PERIODOS (slide 13)
| Petición | Estado | Nota |
|---|---|---|
| Diferenciar estado de cierre de nóminas (costes) del de facturación | ✅ | Hecho (Ola 3b: CierreCostes + CierreFacturacion) |
| Campo **fecha de pago** (día 30 fin de mes vs día 15 mes vencido) | ✅ | `Period.DiaPago` |
| Facturación con límite de cierre el día 9 | ⚠️ | `DiaPago` admite 9; verificar lógica/regla |
| Cierre **por servicio**, no global; mostrar estado por servicio y aprobar por servicio | ✅ | Cierre por servicio implementado |
| Interlocutor puede aprobar o **bloquear** el cierre; validación previa de gastos | ⚠️ | Flujo de aprobación existe; revisar acción "bloquear" + validación previa |
| Eliminar campos «ámbito» y «responsable» | 🔍 | Verificar en `period-form` |
| Estados del proceso: Abierto, **Revisado**, **Revisado con alertas**, Bloqueado, Cerrado | ⚠️ | Enum actual `EstadoPeriodo = {Abierto, Cerrado, Bloqueado}`. Faltan "Revisado" y "Revisado con alertas" |

---

## 7. APROBACIONES — PAGOS (slides 15-17)

### 7.1 Vista y detalle (slide 15)
| Petición | Estado | Nota |
|---|---|---|
| Vista global en **matriz** (empleados en filas, conceptos en columnas) | ✅ | **HECHO 2026-06-18**: matriz empleados×conceptos en el detalle del cierre (desde las líneas), con totales |
| Ir al detalle por día, empleado/servicio y concepto | ⚠️ | Existe `calculation-detail`; falta detalle por día |
| Al pulsar concepto/persona/servicio abrir detalle por día separado por concepto | ⚠️ | Falta |
| Filtros que ocupen poco espacio | UI | — |
| Vista detalle por día por persona; otra vista de visitas por persona en distintos servicios | ❌ | Faltan ambas vistas |
| Cada línea con campo de **pago y facturación adicional** + comentario justificativo del ajuste | ⚠️ | Existe override/incentivo con motivo; falta el ajuste por línea pago+facturación |

### 7.2 Conceptos (slide 16)
| Petición | Estado | Nota |
|---|---|---|
| Orden de conceptos: salario bruto, incentivos, visitas extra, suplidos no cotizables, dietas no PayHawk, km no PayHawk | ❌ | Ordenación fija a implementar |
| Al situarse sobre cada concepto, ir al detalle (ej. salario = 15 visitas × 10 € = 150 €) | ⚠️ | Parcial vía `calculation-detail` |
| Añadir importe dejando registro de qué/quién/motivo | ✅ | Override/Incentivo (`EsManual`, `ImporteOriginal`, `MotivoManual`) |
| Cada persona aparece **tantas veces como llamamientos/contratos** tenga (info de Innuva), sumando pagos al llamamiento | ❌ | Falta multiplicidad por contrato; contratos en staging A3Innuva |
| Revisar utilidad del botón «rechazar» | review | — |

### 7.3 Totales y flujo (slide 17)
| Petición | Estado | Nota |
|---|---|---|
| Revisar si la tabla (repite la superior) es necesaria | review | — |
| **Desdoblar** columna «Total» en total salario y total gastos | ❌ | Falta |
| Flujo: Cálculo automático (no debe salir) → Revisión operaciones (FF) → Aprobación FICO → (Dirección, prob. no) → Cierre FICO | ✅ | Hecho (Ola 3a: Grupo→FICO, sin Dirección) |

---

## 8. APROBACIONES — FACTURACIÓN (slide 19)
| Petición | Estado | Nota |
|---|---|---|
| Sustituir «recurso» por **«Categoría de factura»** (ej. Gastos de personal, servicios de campo) | ❌ | No existe categoría de factura |
| Conceptos con valores precargados y otros manuales (facturas de proveedores, fase 1) | ❌ | Falta |
| Contemplar concepto extra a facturar o importe a abonar | ❌ | Falta |
| Campo **comentario interno** (para el aprobador FICO) | ❌ | Falta |
| Campo **comentario externo** (para la factura) | ❌ | Falta |
| Campo **PO del servicio** (lo da el cliente) | ❌ | Falta |
| Campo **ID del servicio** (lo da el cliente; exclusivo RSTs Apple) | ❌ | Falta |
| Pantalla inferior (detalle borrador): mostrar los conceptos que suman en la categoría | ❌ | Falta |
| Ver todas las facturas emitidas a un cliente según filtro de fechas | ❌ | Falta |

---

## 9. CONTABILIDAD / ENVÍO (slide 21) — *bloque a revisar por Lourdes*
| Petición | Estado | Nota |
|---|---|---|
| Selector de **Servicio** que se selecciona y envía a pago o facturación | ❌ | No hay pantalla de contabilidad/envío dedicada (solo `ExportsController`) |
| Tener en cuenta costes de servicios externos que aplican a facturación (logística) | ❌ 🏗️ | Falta |
| Costes de logística como coste de proyecto, diferenciando pagado a proveedores vs refacturado; cálculo de márgenes | ❌ 🏗️ | Falta |
| **Visualizar el fichero** generado antes de enviar (botón "Visualizar fichero") | ❌ | Export existe; falta preview |
| Unidad mínima de envío = proyecto (paquete completo), encapsulado por servicio | 🔍 | Verificar |
| Dos destinos: A3 (nóminas/pagos) y facturas; errores por línea | ⚠️ | Endpoints `a3-innuva` / `a3-erp` existen; falta errores por línea |
| Botón de envío por Cliente/Servicio/Mes + botón de selección MASIVOS | ❌ | Falta |
| Filtros: DPTO, CLIENTE, SERVICIO, FECHAS, ESTADO | ❌ | Falta |

---

## 10. INFORMES (slide 23) — ✅ HECHO 2026-06-18 (nativo, sin Power BI; ver SUP-08)
| Petición | Estado | Nota |
|---|---|---|
| Filtros (dpto, cliente, servicio, año) | ✅ | Filtros nativos en `/reports` |
| Inicio por departamento → drill down a cliente → servicio y drill out | ✅ | Tab "Resultado": árbol expandible dpto→cliente→servicio |
| Facturación, margen, costes | ✅ | Columnas en el drill-down (real, del año) |
| Comparativa previsión de ventas vs facturación + margen bruto | ✅ | Tab "Previsión vs Real" (Forecast vs cierres), métrica Ventas/Margen, por dpto/cliente/mes |
| Informe de solo forecast de ventas y margen | ✅ | Ya cubierto por pantalla `/forecast` |
| Power BI | ✅ | **Eliminado** (el cliente no lo usa); página sustituida + migración `DropBiSchema` |
| Informe de productividad | ❌ | Pendiente (no definido) |
| Informe de traspaso entre cecos | 🏗️ | Bloqueado (ver Traspaso cecos) |

---

## 11. USUARIOS (slide 25)
| Petición | Estado | Nota |
|---|---|---|
| Roles separados aunque permisos similares | ⚠️ | Modelo Role existe |
| El rol determina el acceso a clientes/servicios (global o por servicio) | ⚠️ | Revisar visibilidad por servicio |
| Un usuario puede tener varias asignaciones (cliente y servicios) | ⚠️ | `ServiceUser` existe; falta UI de asignación múltiple |

---

## 12. ROLES (slide 27)
| Petición | Estado | Nota |
|---|---|---|
| Añadir **permiso para ver auditorías** | ❌ | Tabla de roles muestra Pagos/Facturaciones/Usuarios/Roles; falta Auditorías |
| Asignación Ceco→Acción desde Celero | info | Se determina en Celero |
| Tabla de roles: visualización, validación, edición y creación de usuarios/roles | ⚠️ | Parcial |
| Revisar asignación de permisos de edición | 🔍 | — |

---

## 13. CECOS (slide 29) / DEPARTAMENTOS (slide 31)
| Petición | Estado | Nota |
|---|---|---|
| Cecos: edición mínima e informativa (se sincronizan de Celero) | ⚠️ | Hoy hay CRUD completo; debería ser informativo/solo lectura |
| Departamentos: **eliminar "Usuarios del departamento"** (solo analítica); dejar solo clientes vinculados | ❌ | Falta |
| Departamentos: eliminar campos innecesarios (uso informativo) | ❌ | Falta |
| Reducir ventana global y ampliar el detalle | UI | — |
| En el detalle, incluir el **ceco** al que están imputados los usuarios | ❌ | Falta |

---

## 14. AUDITORÍA (slide 33)
| Petición | Estado | Nota |
|---|---|---|
| Columna con botón «detalle» de lo gestionado | ⚠️ | `audit` tiene detalle expandible; revisar que cubra la petición |
| Registrar todas las acciones (aprobaciones, ediciones, rechazos) + acceso al detalle | ✅ | `AuditLog` con Old/New value, action, timestamp, IP |

---

## 15. CONFIGURACIÓN PRESUPUESTO (slide 35) — *pendiente de revisar*
| Petición | Estado | Nota |
|---|---|---|
| Presupuesto inicial vs ejecutado (márgenes y desviaciones) | ⚠️ | `PresupuestoServicio` existe (sub-tab del servicio); falta comparación |
| Filtro activo/inactivo/todos | ❌ | Falta |
| Pantalla con presupuestos según filtro (dpto/cliente/acción/fechas) | ❌ | Falta pantalla de listado de presupuestos con filtros |
| Duplicar presupuestos | ❌ | Falta |
| Entrar al presupuesto seleccionado | ⚠️ | — |
| Botón retroceder | UI | — |
| Campo nº de «personas de campo necesarias» (sustituye forecast GPP) | ❌ | Falta |
| Segunda fila: margen operativo real + desviación; partidas mensuales | ❌ | Falta |
| Definir campos del botón «añadir Partida» | 🏗️ | A definir |
| Eliminar cuadros «márgenes objetivo» y «Vigencia y Ámbito» (duplican) | ❌ | Falta |

---

## 16. FORECAST VENTAS Y GPP (slide 36) — ✅ HECHO 2026-06-18 (ver SUP-06)
| Petición | Estado | Nota |
|---|---|---|
| Introducir forecast de ventas (importe manual por mes) | ✅ | Tab "Forecast" en detalle de servicio: rejilla 12 meses con ventas/margen/nº personas. Granularidad servicio+mes |
| Forecast modificable en presente y futuro, no en meses cerrados | ✅ | Meses anteriores al actual bloqueados (UI) y rechazados en backend (409 `period_closed`) |
| Ventana resumen forecast de ventas (filtros; tabla dpto/clientes × meses + total) | ✅ | `/forecast` tab Ventas: pivote filas dpto+cliente, columnas meses, totales. Filtros año/dpto/cliente/servicio |
| Ventana resumen forecast de GPP (nº personas; misma tabla) | ✅ | `/forecast` tab GPP (nº personas) |
| (slide 35 vs 36) Contradicción GPP presupuesto vs forecast | ⚠️ | Seguido slide 36; pendiente confirmar con cliente (SUP-06) |

---

## 17. CONFIGURACIÓN FACTURA (slide 37-38)
| Petición | Estado | Nota |
|---|---|---|
| 2º tipo de etiqueta para agrupar conceptos de un mismo servicio en distintas facturas | ❌ | **No existe configuración de factura** (grep sin resultados) |
| Categoría como campo texto libre (no precargado; ej. abono manual) | ❌ | Falta |
| Definir bloque de conceptos disponibles del cliente | ❌ 🏗️ | "No queda claro" — a definir |

---

## 18. ERRORES NÓMINAS / PAGOS (slide 40) — ✅ HECHO 2026-06-18 (ver SUP-09)
| Petición | Estado | Nota |
|---|---|---|
| Diferenciar bloqueantes (impiden cierre) de avisos (descartables) | ✅ | `ClosureAlerta` Bloqueante/Advertencia |
| Ejemplos: recursos sin contrato, DNIs no localizados, km fuera de rango, conceptos manuales sin observación | ⚠️ 🏗️ | Validaciones parciales; **Eladio** enviará la lista definitiva de códigos |
| Pantalla con **varias pestañas** por bloques para resolver errores en grupo | ✅ | `/alertas` ("Alertas de desviación"): tab "Por bloques" (agrupa por código) + tab "Listado" |
| Mantener filtros | ✅ | Filtros tipo/origen/estado/búsqueda |
| Pantalla de datos consolidados de operaciones | ❌ | Pendiente (infra-especificado) |
| Pantalla de datos consolidados de Innuva | ❌ | Pendiente (infra-especificado) |

---

## 19. ERRORES FACTURACIÓN (slide 42) — ✅ HECHO 2026-06-18
| Petición | Estado | Nota |
|---|---|---|
| Redefinir como **«Alertas de desviación»**; analizar conceptos de facturación igual que nómina | ✅ | Pantalla renombrada; filtro de origen Nómina/Facturación |
| Posible vista única ERRORES NÓMINA/FACTURACIÓN | ✅ | `/alertas` unifica ambos orígenes |

---

## 20. TRASPASO CECOS (slides 43-46)
| Petición | Estado | Nota |
|---|---|---|
| Proceso/explicación del traspaso entre cecos | 🏗️ | Eladio enviará informe; coordinar actualización masiva en Celero |
| Pantalla de **resumen para traspaso contable** (ceco origen → cecos destino) | ❌ | **No existe** (grep sin resultados) |
| Neteo de líneas inversas (origen↔destino) → mostrar una sola línea contable | ❌ | Falta lógica de neteo |

---

## RESUMEN — qué falta por bloques de trabajo

### A) Ya cerrado (Olas 1/2/3a/3b)
- Eliminación de «proyecto», renombrado Acción→Servicio, jerarquía Cliente→Servicio→Concepto.
- Split de cierres: **CierreCostes** vs **CierreFacturacion**, cierre por servicio.
- Flujo de aprobación **Grupo→FICO** (sin Dirección).
- Override/incentivo con registro (quién/motivo).
- `Period.DiaPago`, alertas Bloqueante/Advertencia, auditoría base, contratos de un día (ignorar con motivo).

### B) Pendiente — features NUEVAS de mayor tamaño (no existe nada)
1. ~~**Incidencias de cliente** (entidad + CRUD + histórico + UI) — slide 6.~~ ✅ **HECHO 2026-06-18**.
2. ~~**Forecast de ventas y GPP** (alta por mes, resúmenes, no editar meses cerrados) — slide 36.~~ ✅ **HECHO 2026-06-18**.
3. **Configuración de factura** (categorías, etiquetas de agrupación, campos texto) — slides 37-38, 19.
4. **Pantalla de Contabilidad/Envío** (selector servicio, preview fichero, masivos, filtros, logística) — slide 21.
5. **Traspaso entre cecos** (resumen contable + neteo) — slides 43-46.
6. ~~**Errores Nóminas/Facturación con pestañas** + "Alertas de desviación" — slides 40, 42.~~ ✅ **HECHO 2026-06-18** (faltan solo las vistas "datos consolidados operaciones/Innuva", infra-especificadas).
7. ~~**Informes con drill-down** dpto→cliente→servicio (hoy solo enlaces BI) — slide 23.~~ ✅ **HECHO 2026-06-18** (nativo, sin Power BI).
8. ~~**Vista matriz de Aprobaciones** (empleados×conceptos) — slide 15.~~ ✅ **HECHO 2026-06-18** (matriz en detalle de cierre; detalle-por-día/visitas-por-persona quedan como ampliación).

### C) Pendiente — ajustes/mejoras sobre pantallas existentes (medianos)
- ~~Dashboard: coste real, desdoblar cierres, eliminar gráficos duplicados, K, filtro servicio, alertas — slides 3-4.~~ ✅ **HECHO 2026-06-18** (objetivo queda placeholder, SUP-07).
- Servicios: columnas departamento/facturación/margen + filtros + desplegable de usuarios — slide 9.
- Conceptos: separar en dos ventanas, diccionario equivalencias nómina (Innuva), pretratamiento por cliente, fix filtro "Hasta" duplicado — slide 11.
- Periodos: estados "Revisado" / "Revisado con alertas", acción bloquear, eliminar campos ámbito/responsable — slide 13.
- Aprobaciones pagos: orden de conceptos, desdoblar Total (salario/gastos), multiplicidad por contrato/llamamiento — slides 16-17.
- Cliente: cecos del cliente, metadatos (rol/teléfonos/emails), resumen de facturación — slide 6.
- Roles: permiso de auditorías — slide 27.
- Departamentos: quitar "usuarios del departamento", añadir ceco imputado en detalle — slide 31.
- Cecos: pasar a solo lectura/informativo — slide 29.
- Presupuestos: listado con filtros, duplicar, comparación inicial vs ejecutado, nº personas de campo, limpiar cuadros duplicados — slide 35.

### D) Bloqueado / requiere reunión o input externo (🏗️)
- Parametrización de cálculo de conceptos desde cuestionario Celero (nivel de detalle por visita / tipo de elemento) — slide 11.
- Reglas de conceptos de facturación (agrupación, multiplicadores, casuísticas) — slide 11.
- Lógica de logística en contabilidad (Lourdes) — slide 21.
- Lista definitiva de errores (Eladio) — slide 40.
- Proceso de traspaso de cecos (Eladio) + códigos masivos Celero (Lourdes) — slides 43-44.
