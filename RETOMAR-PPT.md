# 🚦 Punto de retoma — Trabajo sobre el PPT «Pantallas actualizadas - HK_10062026»

**Última actualización:** 2026-06-18
**Rama:** `feat/ola2-cambios-funcionales` · **Estado: TODO EN LOCAL, SIN COMMITEAR**
**Suite backend:** 321/321 verde · builds back y front limpios.

> Este archivo es el "empieza por aquí". Para el detalle completo:
> - `docs/COMPARATIVA_PPT_PANTALLAS.md` — comparativa pantalla por pantalla (qué hay vs qué pide el PPT).
> - `docs/SUPOSICIONES_CRITICAS.md` — decisiones tomadas (SUP-05 … SUP-09).
> - El PPT original está en la raíz: `Pantallas actualizadas - HK_10062026 (formato unificado) (1).pptx`.

---

## ⚡ Cómo arrancar la próxima vez
1. Leer este archivo + `docs/COMPARATIVA_PPT_PANTALLAS.md`.
2. Confirmar si ya se han commiteado los cambios de la sesión 2026-06-18 (si no, valorar hacerlo).
3. Elegir tarea de la sección **"FALTA"** (recomendación de orden abajo).
4. Patrón de trabajo: leer el código real antes de tocar → implementar back+front+tests → `dotnet test` + `ng build` → registrar decisiones en SUPOSICIONES → actualizar la comparativa.

---

## ✅ HECHO en la sesión 2026-06-18 (local)
1. **Incidencias de cliente** (slide 6) — entidad `ClienteIncidencia` + API `api/clients/{id}/incidencias` + sección en ficha cliente.
2. **Forecast ventas/GPP** (slide 36) — entidad `Forecast` (servicio+mes), tab en servicio + pantalla `/forecast` pivote.
3. **Ajustes Dashboard** (slides 3-4) — coste real, desdoblar cierres costes/facturación, K, filtro servicio, quitar gauge/objetivos.
4. **Informes nativos** (slide 23) — `/reports` reconstruido (drill-down + previsión vs real); **Power BI eliminado** (migración `DropBiSchema`).
5. **Alertas de desviación** (slides 40, 42) — `/alertas` con filtros + vista "por bloques".
6. **Matriz de aprobaciones** (slide 15) — matriz empleados×conceptos en detalle de cierre.

(Olas previas ya en main: eliminación de «Proyecto», Acción→Servicio, split de cierres, flujo Grupo→FICO, DiaPago, override/incentivo, alertas, contratos de un día, auditoría base.)

---

## ❌ FALTA (sin bloqueo) — orden recomendado
**Ajustes rápidos primero (sin bloqueo, bajo riesgo):**
- [ ] **Roles**: añadir permiso de **auditorías** (slide 27).
- [ ] **Cecos**: pasar a solo lectura/informativo (slide 29).
- [ ] **Departamentos**: quitar "usuarios del departamento"; añadir ceco imputado en detalle (slide 31).
- [ ] **Auditoría**: columna "detalle" por registro (slide 33).
- [ ] **Periodos**: estados **Revisado** / **Revisado con alertas**; acción bloquear; quitar campos ámbito/responsable (slide 13). *(Toca enum + máquina de estados.)*

**Medianos:**
- [ ] **Servicios**: columnas departamento/facturación/margen + filtros + desplegable de usuarios (slide 9).
- [ ] **Cliente**: cecos del cliente; metadatos rol/teléfonos/emails; resumen de facturación (slide 6).
- [ ] **Conceptos**: separar en dos ventanas; diccionario equivalencias nómina (Innuva); pretratamiento por cliente; fix filtro "Hasta" duplicado (slide 11).
- [ ] **Aprobaciones pagos**: orden fijo de conceptos; desdoblar Total salario/gastos; multiplicidad por contrato/llamamiento (slides 16-17).

**Feature nueva con dudas del propio PPT:**
- [ ] **Config Presupuesto ampliada** (slide 35, marcado "pendiente de revisar"). Parte clara construible: listado global con filtros, estado activo/inactivo, duplicar, entrar+retroceder, inicial vs ejecutado (margen real + desviación), nº personas de campo, quitar cuadros duplicados. *Requiere extender `PresupuestoServicio` (estado, nº personas, nombre/fechas) → migración.* Difuso/pendiente de definir: "partidas mensuales" y campos de "añadir partida".

---

## 🏗️ BLOQUEADO (no empezar hasta tener input)
- [ ] **Traspaso entre cecos** (slides 43-46) — Eladio enviará el proceso + Lourdes los códigos masivos.
- [ ] **Contabilidad/Envío** (slides 20-21) — logística pendiente de Lourdes/H&K (el resto sería construible si se separa).
- [ ] **Configuración de factura** (slides 37-38) — el cliente declara "no nos queda claro" el bloque de conceptos.
- [ ] **Datos consolidados Operaciones/Innuva** (slide 40) — infra-especificado.
- [ ] **Lista definitiva de códigos de error** (slide 40) — la enviará Eladio (el sistema ya funciona con los actuales).
- [ ] **Informe de productividad** (slide 23) — no definido.

---

## ⚠️ DECISIONES TOMADAS, PENDIENTES DE CONFIRMAR CON EL CLIENTE
- **SUP-06 — GPP**: contradicción slide 35 ("meter nº personas en presupuesto y eliminar forecast GPP") vs slide 36 ("forecast GPP mensual"). Se siguió el **slide 36**. Si confirman el 35 → mover GPP al presupuesto y quitar el tab GPP del forecast.
- **SUP-07 — Objetivo del dashboard**: los valores objetivo (400K facturación, 25% margen) son **placeholder**; falta definir su origen (candidato: Forecast o PresupuestoServicio).
- **SUP-08 — Reporting nativo, NO Power BI**: confirmado por el cliente; andamiaje `bi` eliminado.
- Permisos de escritura de incidencias/forecast = `Administrator`/`Backoffice` (ajustable).

---

## 📌 Recordatorios
- **El cliente NO usa Power BI** (cualquier reporting es nativo en la app).
- APIs de cliente externas (Bizneo/Celero/PayHawk/A3…) son **solo lectura**.
- Hay PII real en ficheros Galán/MDP y en el historial git (pendiente purgar) — no volcar valores reales al repo.
