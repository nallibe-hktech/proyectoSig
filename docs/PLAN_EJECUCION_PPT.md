# PLAN DE EJECUCIÓN — Cambios PPT 10/06/2026

> **Fecha:** 2026-06-12
> **Fuentes:** `CAMBIOS_PPT_10062026.md` (análisis del PPT) + `ESTADO_ACTUAL.md` (fotografía del código).
> **Decisiones del cliente (2026-06-12):**
> 1. **Naming:** clases en inglés `Service` (convención del repo); rutas API `/api/services` y frontend `/services`; labels de UI en español ("Servicio").
> 2. **Migración EF Core:** **con preservación de datos** (rename de tablas/columnas + reescritura `proyectoId→servicioId`), NO recrear desde seed.
> 3. **Alcance autorizado por sesión:** se ejecuta por fases con aprobación previa. Este documento es el plan; no se ha tocado código todavía.
>
> **Regla transversal (CLAUDE.md):** fidelidad nominal, YAGNI, integraciones cliente solo lectura, build verde tras cada fase (máx. 3 iteraciones), registrar decisiones autónomas en `SUPOSICIONES_CRITICAS.md` y bloqueos en `BLOQUEANTES.md`.

---

## 0. Principios de ejecución

1. **Vertical, no big-bang.** Cada fase deja el sistema compilando y testeando (backend `dotnet build` + `dotnet test`; frontend `ng build`).
2. **Backend antes que frontend** dentro de cada fase (el contrato API manda).
3. **Ramas:** una rama por ola (`feat/ppt-01-service-rename`, `feat/ppt-02-period-states`, …). PR por ola.
4. **Solo lo DECIDIDO.** Los bloques §3 del PPT (Facturación, Contabilidad, Config Presupuesto/Factura, Errores nómina/facturación, Traspaso CECOs, parametrización fina de Conceptos) **NO se implementan** hasta validación FICO/FF/Eladio. Se dejan como stubs documentados si algo los referencia.
5. **Tests primero donde exista cobertura.** Hay `SIG.Tests` (Unit + Integration). Cada renombrado debe actualizar tests y mantenerlos verdes.

---

## OLA 1 — Cambio estructural #1: eliminar `Project`, renombrar `Action→Service`

> **Riesgo:** Alto. **Bloquea:** todo lo demás. **Estrategia DB:** migración con preservación de datos.

### Modelo objetivo
```
ANTES:  Client → Project → Action → Concept
AHORA:  Client → Service → Concept
```
`Service` = el antiguo `Action` renombrado, que ABSORBE de `Project`:
- vínculo directo a `Client` (Action ya tiene `ClientId` → se conserva).
- CECOs: `ProjectCostCenter` → `ServiceCostCenter` (nueva relación Service↔CostCenter).
- Usuarios: `ActionUser` → `ServiceUser` (ya existe en Action; se conserva renombrado).
- `Closure.ProjectId` → `Closure.ServiceId`.
- `TarifaProyecto` → `TarifaServicio` (FK a Service).
- `PresupuestoProyecto` → `PresupuestoServicio` (FK a Service).
- `Concept.ProjectId` (global/por-proyecto) → `Concept.ServiceId`.
- `CeleroServiceMapping.ProjectId` → `ServiceId`; `CeleroMissionMapping.ActionId` → `ServiceId`.

> **Decisión de modelado abierta (registrar en SUPOSICIONES_CRITICAS):** al colapsar Project en Service, cada Action heredará el `ClientId` de su Project (ya lo tiene), y los CECOs/Usuarios/Cierres/Tarifas/Presupuestos que colgaban del Project se **replican hacia cada Service hijo** durante la migración de datos (1 Project con N Actions → N Services, cada uno recibe copia de los CECOs/usuarios del Project, y los Closures se reparten por la FK existente). **⚠️ Punto crítico de la migración de datos:** hoy `Closure` cuelga de Project (1 closure por project×period). Con N actions por project, hay que decidir si el closure pasa a UNO de los services o se mantiene a nivel agregado. → **Ver §1.6 (bloqueante potencial).**

### 1.1 Domain (`SIG.Domain`)
**`Entities.cs`:**
- [ ] Eliminar `Project`, `ProjectCostCenter`, `ProjectUser`.
- [ ] Renombrar `Action`→`Service`, `ActionConcept`→`ServiceConcept`, `ActionUser`→`ServiceUser`.
- [ ] `Service`: quitar `ProjectId/Project`; conservar `ClientId`, `DepartmentId?`; añadir `ICollection<ServiceCostCenter>`.
- [ ] Nuevo `ServiceCostCenter { ServiceId, CostCenterId }`.
- [ ] `Service`: añadir `Interlocutor*`, `FechaAlta` (heredados de Project, si se quieren conservar a nivel servicio) — **YAGNI: solo si la UI los usa; ver §2.3 PPT (edición servicio limitada a estado)** → probablemente NO migrar interlocutor a Service; el interlocutor pasa a ser un `ServiceUser` con rol. Registrar decisión.
- [ ] `Concept`: `ProjectId?`→`ServiceId?`.
- [ ] `Closure`: `ProjectId`→`ServiceId`.
- [ ] `TarifaProyecto`→`TarifaServicio` (`ProjectId`→`ServiceId`); `PresupuestoProyecto`→`PresupuestoServicio`.
- [ ] `User`: `ProjectUsers` fuera; `ActionUsers`→`ServiceUsers`.
- [ ] `Client`: `Projects`→`Services`.

**`Enums.cs`:**
- [ ] Eliminar `EstadoProyecto`.
- [ ] `EstadoAccion`→`EstadoServicio { Activo, Inactivo }` (alinear género: el PPT §2.3 dice activo/inactivo).

### 1.2 Infrastructure (`SIG.Infrastructure`)
- [ ] `AppDbContext.cs`: DbSets — quitar Project/ProjectCostCenter/ProjectUser; renombrar Action*→Service*; añadir ServiceCostCenter; Tarifa/Presupuesto renombrados.
- [ ] `Configurations.cs`: reescribir configs (ToTable `services`, `service_concepts`, `service_users`, `service_cost_centers`, `tarifas_servicio`, `presupuestos_servicio`); FKs Closure→service, Concept→service, Celero*→service; query filters IsDeleted; índice único `closures(service_id, period_id)` (ver §1.6).
- [ ] `Repositories.cs`: quitar `ProjectRepository`; `ActionRepository`→`ServiceRepository`; ajustar `HasActionsOrClosuresAsync`/`HasClosuresAsync`; métodos `ListProjectIdsForUserAsync`→`ListServiceIdsForUserAsync` (UserRepository, ownership).
- [ ] `DependencyInjection.cs`: actualizar registros (quitar IProjectRepository/IProjectService; IActionService→IServiceService; Tarifa/Presupuesto).
- [ ] `DataSeeder.cs`: eliminar `SeedProjectsAsync`; `SeedActionsAsync`→`SeedServicesAsync` (asignan Client directo + CECOs + usuarios); ajustar closures/tarifas/presupuestos al nuevo FK; truncate order.
- [ ] **Migración EF Core (preservación de datos)** — ver §1.5.

### 1.3 Application (`SIG.Application`)
- [ ] **DTOs:** eliminar `Project*` (ListItem/Detail/Create/Update); `Action*`→`Service*`; `ConceptDetailDto.ActionIds`→`ServiceIds`; `Closure*Dto.ProjectId/ProjectNombre`→`ServiceId/ServiceNombre`; `ApprovalPanelItemDto.ProjectId/Nombre`→`ServiceId/Nombre`; `MiProyectoDto`→`MiServicioDto`; `TarifaProyectoDto`→`TarifaServicioDto`, `PresupuestoProyectoDto`→`PresupuestoServicioDto`; `CeleroVisita*.ProjectId/ActionId`→`ServiceId`.
- [ ] **Interfaces:** `IProjectService` fuera; `IActionService`→`IServiceService`; `ITarifa/IPresupuestoProyectoService`→`*ServicioService`; ajustar `IConceptService`, `IClosureService`, `IDashboardService`, `IApprovalService`, `IUserRepository`.
- [ ] **Services:** eliminar `ProjectService`; `ActionService`→`ServiceService`; ajustar `ClosureService.ComputeLinesAsync` (conceptos por service), `DashboardService` (GetMisProyectos→GetMisServicios), `ConceptService` (serviceId), `Tarifa/PresupuestoProyectoService`→`*ServicioService`, `ApprovalService`.
- [ ] **Validators:** renombrar `Action*Validator`→`Service*Validator`; quitar `Project*Validator`; ajustar refs a ProjectId.

### 1.4 API (`SIG.API`)
- [ ] Eliminar `ProjectsController.cs`.
- [ ] `ActionsController.cs`→`ServicesController.cs`, `[Route("api/services")]`; params `projectId?`→ (filtro por client/department).
- [ ] `TarifasController` y `PresupuestosController`: re-anclar a `[Route("api/services/{serviceId:int}/tarifas|presupuestos")]`.
- [ ] `CeleroVisitasController`/`Update`: mapeo a `ServiceId` (quitar ProjectId/ActionId).
- [ ] Revisar `ClosuresController`, `ApprovalsController`, `DashboardController` por params/DTO renombrados.

### 1.5 Migración EF Core con preservación de datos
**Enfoque:** una migración manual `RenameProjectActionToService` que use `RenameTable`/`RenameColumn` (preservan datos) + SQL de reescritura para los colapsos. Orden:
1. `RenameTable actions → services`; `action_concepts → service_concepts`; `action_users → service_users`; `tarifas_proyecto → tarifas_servicio`; `presupuestos_proyecto → presupuestos_servicio`.
2. `RenameColumn action_id → service_id` en `service_concepts`, `service_users`, `celero_mission_mappings`.
3. **Reasignar FKs que colgaban de Project hacia Service** (paso de datos, ver §1.6): poblar `closures.service_id`, `concepts.service_id`, `tarifas_servicio.service_id`, `presupuestos_servicio.service_id`, `celero_service_mappings.service_id` a partir del `project_id` antiguo + relación project→actions.
4. Crear tabla `service_cost_centers` y copiar `project_cost_centers` expandido por service.
5. `DropForeignKey`/`DropColumn project_id` en services/concepts/closures/tarifas/presupuestos/celero_*; `DropTable project_users, project_cost_centers, projects`.
6. Recrear índices únicos sobre `service_id`.
- [ ] Generar `AppDbContextModelSnapshot` consistente.
- [ ] Probar `dotnet ef database update` sobre copia de la BD dev (la de `:5432` nativa, NO Docker :5433 — ver memoria local-postgres-setup).
- [ ] Script idempotente (lección aprendida fix-migracion-estado-mapeo-duplicada).

### 1.6 ⚠️ BLOQUEANTE POTENCIAL — semántica del Closure al colapsar Project→Service
Hoy: 1 Closure por **(Project, Period)**. El PPT §2.5 pide **cierre por SERVICIO**. Pero un Project tiene N Actions(→Services). Opciones:
- **A)** Cada Closure existente se asocia al **primer/único Service** del Project (si la mayoría de Projects tienen 1 Action útil) y los demás Services arrancan sin closures históricos.
- **B)** Replicar el Closure a cada Service del Project (duplica históricos, distorsiona agregados).
- **C)** Mantener Closure a nivel agregado temporalmente y crear cierre-por-servicio como entidad nueva en Ola 2.
→ **Recomendación:** Opción A para la migración histórica + el PPT §2.5 (cierre por servicio) se implementa de verdad en **Ola 2**. **Requiere confirmación del cliente** antes de ejecutar la migración de datos. Registrar en `BLOQUEANTES.md` hasta validar.

### 1.7 Frontend
- [ ] Eliminar `features/projects` (list/form/detail/tarifas/presupuestos) + ruta + entrada menú "Projects".
- [ ] `features/actions`→`features/services`; rutas `/services/**`; labels "Servicio"; `data-testid`; "SRV-{id}" se conserva.
- [ ] `models/enums.ts`: quitar `EstadoProyecto`; `EstadoAccion`→`EstadoServicio`.
- [ ] `models/dtos.ts`: Project*/Action*→Service*; renombres en cascada (Closure, Approval, MiProyecto→MiServicio, Tarifa/Presupuesto).
- [ ] `core/api`: `ProjectService` fuera; `ActionService`→`ServiceApiService`; `TarifaService`/`PresupuestoService` re-anclados.
- [ ] Dashboard: "Mis Proyectos"→"Mis Servicios"; tabla Closures col "Proyecto"→"Servicio"; Approvals filtro "proyecto"→"servicio"; Audit col "Proyecto"→"Servicio".
- [ ] Celero-visitas: mapeo a Service.
- [ ] e2e/visual tests: actualizar selectores y textos.

### 1.8 Criterios de aceptación Ola 1
- `dotnet build` + `dotnet test` verdes; `ng build` verde.
- `dotnet ef database update` aplica sobre BD dev sin pérdida (conteos de closures/conceptos coherentes).
- No queda ninguna referencia a `Project`/`Action`/`proyecto`/`acción` salvo en históricos de migración y este doc.
- Menú sin "Projects"; "Actions"→"Servicios".

---

## OLA 2 — Modelo de cierre y aprobación (#2 + #3)

> **Riesgo:** Alto/Medio. **Depende de:** Ola 1. **Estado PPT:** §2.5 PARCIAL (casos "Bloqueado" pendientes), §2.8 DECIDIDO (Dirección por confirmar).

### #2 — Estados de Period + cierre por servicio (§2.5)
- [ ] Rediseñar a **doble dimensión**: estado de **cierre de costes (nóminas)** vs **cierre de facturación**, separados.
- [ ] Ampliar enum de estado del proceso: `Abierto, Revisado, RevisadoConAlertas, Bloqueado, Cerrado`. (`Bloqueado`: casos por definir → dejar el estado pero documentar "casos pendientes" en BLOQUEANTES).
- [ ] Campo **"fecha de pago"** (día 30 fin de mes / día 15 mes vencido; cálculo siempre sobre mes natural). Límite de cierre de facturación: día 9.
- [ ] **Cierre por servicio** (no global): nueva granularidad — estado de cierre por (Service, Period); aprobar por servicio; el periodo no cierra hasta que todos sus servicios estén cerrados.
- [ ] Interlocutor puede aprobar/bloquear; validación previa de gastos antes de cerrar.
- [ ] Frontend Periods: **eliminar campos "ámbito" y "responsable"** (verificar plantilla); mostrar estado de cierre por servicio.
- [ ] Migración EF (nuevos campos/estados; tabla de cierre-por-servicio si se modela aparte).

### #3 — Flujo de aprobación alineado (§2.8)
- [ ] Realinear `ApprovalStep` a: `RevisionOperaciones → AprobacionFICO → (Direccion opcional) → CierreFICO`.
- [ ] El paso de **Cálculo** queda fuera del flujo visible (automático, no se muestra).
- [ ] Ajustar `ClosureService.ApproveAsync/RejectAsync`, `StepToRole`, seed de Approvals, frontend (closures texto "5 pasos", approvals flujo de pasos).
- [ ] "Dirección seguramente no necesario" → opcional/configurable. Registrar como suposición.

---

## OLA 3 — Conceptos pago/facturación + motor de pre-filtros (#7)

> **Riesgo:** Medio. **Estado PPT:** §2.4 PARCIAL (estructura decidida; parametrización fina del cálculo por nivel de detalle REQUIERE REUNIÓN — NO implementar esa parte).

- [ ] **Separar Conceptos en dos entidades/ventanas:** conceptos de **Pago** y de **Facturación** (hoy solo discriminados por `TipoConcepto`). Decidir: ¿dos entidades o una con discriminador reforzado + dos pantallas? → recomendación: mantener `Concept` con `Tipo` pero **dos pantallas/CRUD separados** y reglas distintas (mínimo cambio de modelo, máximo cambio de UI/validación). Registrar.
- [ ] **Pre-filtros/pretratamiento configurables** por cliente antes de calcular: descontar primeros X km (Payhawk), máximo €/día (20€). → nuevo modelo `ConceptPreFilter` o campo JSON en Concept + soporte en `CalculationEngine` (nuevo paso de pretratamiento sobre `SourceNode` rows).
- [ ] **Diccionario de equivalencias Innuva:** nuevo campo de mapeo en `Concept` (concepto→campo de nómina Innuva).
- [ ] **Conceptos de facturación derivados de pago:** agrupar, multiplicador, % sobre coste, tarifa, reglas por casuística. ⚠️ **REQUIERE REUNIÓN** → implementar solo el andamiaje (campos/relación), no la lógica fina.
- [ ] Frontend: 2 pantallas (pago/facturación); **bug filtro "Hasta" duplicado** (§2.4) → corregir.

---

## OLA 4 — Incidencias de cliente (#4)

> **Riesgo:** Bajo. **Estado PPT:** §2.2 DECIDIDO. **No depende de Ola 1** (pero conviene tras el rename para no duplicar trabajo de UI de cliente).

- [ ] Nueva entidad `ClientIncidencia { Id, ClientId, Tipo, Explicacion, Estado, ...histórico }` (N por cliente, editable, con histórico → tabla `client_incidencia_history` o auditoría).
- [ ] CRUD + endpoints `api/clients/{id}/incidencias`.
- [ ] Frontend: pestaña/sección en ficha de cliente.
- [ ] Migración EF.

---

## OLA 5 — Ajustes Dashboard (#5)

> **Riesgo:** Bajo. **Estado PPT:** §2.1 DECIDIDO. Mayormente FRONT.

- [ ] Unidades unificadas a **miles de € (K)** en todos los importes.
- [ ] Junto a "MARGEN PROMEDIO" mostrar **coste real de lo facturado** (dato ya disponible en `DashboardKpisDto.CosteTotal`).
- [ ] **Eliminar gráfico "Margen vs Objetivo"** → objetivo como valor dentro del recuadro de margen.
- [ ] **Eliminar gráfico "Objetivos del Período"** → integrar objetivo en recuadro de facturación total.
- [ ] "Facturación por cliente": ordenar columnas **€ primero, margen después**.
- [ ] "Cierre completado" (= cierre de costes) + etiqueta para **cierre de facturación** (depende de Ola 2; back debe exponer ambos estados).
- [ ] Filtro **"servicio"** (antes "acción"); confirmar filtro de período aplica a todos los gráficos.
- [ ] **Alertas clicables:** routing por `DashboardAvisoDto.Tipo`+`EntityId` → ventana destino (deep-link). Back ya trae `EntityId?`.
- [ ] "Mis Proyectos"→"Mis Servicios".

---

## OLA 6 — Ajustes por pantalla de bajo riesgo (#6, #8, #9, #10, #11)

> **Riesgo:** Bajo/Medio. Paralelizable tras Ola 1.

- **#6 (§2.6):** campos de **ajuste manual por línea** en `ClosureLine` (pago adicional, facturación adicional, comentario justificativo, autor) + migración + UI en aprobaciones. *(La vista matriz/drill-down completa es mayor; valorar sub-fase.)*
- **#8 (§2.16, §2.13):** permiso **"ver auditorías"** en roles (backend de roles real — hoy la tabla de roles del frontend es MOCK; decidir si se modela RBAC real) + columna **"detalle" before/after** en Auditoría (back ya guarda `OldValueJson/NewValueJson`).
- **#9 (§2.2):** Cliente — filtros del dashboard, mostrar CECOs, navegación a servicios, metadatos no-Celero, resumen de facturación, estado activo/inactivo.
- **#10 (§2.3):** Servicios — columnas departamento/facturación/margen, selector de usuarios (interlocutores/gestores/backoffice), edición limitada a estado.
- **#11 (§2.14, §2.15):** Departamentos/CECOs — quitar "usuarios del departamento", reducir ventana global/ampliar detalle, CECO imputado en detalle, read-only desde Celero.

---

## NO IMPLEMENTAR (pendiente validación — PPT §3)
Facturación (S18-19), Contabilidad/logística (S20-21), Config Presupuesto (S34-35), Config Factura (S36-37), Errores Nóminas/Pagos (S38-39), Errores Facturación (S40-41), Traspaso CECOs (S42-43), parametrización fina de cálculo de Conceptos por nivel de detalle (S11). → Esperan a FICO/FF/Eladio. Si algún cambio de las olas anteriores los roza, dejar interfaz/stub documentado, sin lógica.

---

## Secuencia recomendada y dependencias
```
Ola 1 (estructural) ──┬─> Ola 2 (cierre/aprobación) ──> Ola 5 (dashboard, parte "cierre facturación")
                      ├─> Ola 3 (conceptos)
                      ├─> Ola 4 (incidencias cliente)   [independiente]
                      ├─> Ola 5 (dashboard, resto)
                      └─> Ola 6 (ajustes pantalla)
```

## Checkpoints que requieren al cliente antes de ejecutar
1. **§1.6** — semántica del Closure al colapsar Project→Service (Opción A recomendada). **BLOQUEANTE de la migración de datos.**
2. **Ola 2** — definición de "casos de Bloqueado" y si "Dirección" es paso obligatorio u opcional.
3. **Ola 3** — confirmación de modelar Conceptos pago/facturación como dos entidades o una con dos pantallas; la lógica fina espera reunión.
4. **Ola 6/#8** — ¿se modela RBAC real en backend (hoy roles es mock en frontend) o solo se añade el permiso de auditoría visual?

---

*Próximo paso sugerido: aprobar este plan y autorizar el arranque de la Ola 1, resolviendo antes el checkpoint §1.6.*
