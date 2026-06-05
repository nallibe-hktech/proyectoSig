# AVANCES DEL PROYECTO SIG-ES
## 5 de Junio de 2026 — Semana 2 de Desarrollo

---

## RESUMEN GENERAL

| Métrica | Valor | Nota |
|---------|-------|------|
| **Duración total** | 4-5 semanas (27 mayo - 15/20 julio) | Estimado |
| **Tiempo transcurrido** | 2 semanas | Equivale a 40-50% del cronograma |
| **Backend completado** | 85% | Core arquitectura, servicios, APIs |
| **Frontend completado** | 75% | Angular 18, Material, componentes principales |
| **Integraciones completadas** | 40% | Bizneo + Celero OK, resto bloqueados |
| **Motor de cálculo** | 95% | Funcional pero necesita datos reales |
| **Exportación Excel** | 100% | A3 Innuva + A3 ERP, tests E2E OK |

**Status Actual:** ⚠️ FUNCIONAL CON DATOS FICTICIOS | Bloqueado sin credenciales externas

---

## TRABAJO COMPLETADO (SEMANAS 1-2)

### ✅ BACKEND (85% COMPLETADO)

#### Arquitectura & Infraestructura
- [x] Clean Architecture implementada (Domain, Application, Infrastructure, API)
- [x] Entity Framework Core 10 con migraciones
- [x] SQL Server schema completo (33 tablas)
- [x] Soft-delete global filters con ownership
- [x] AuditLog completo e inmutable
- [x] Timestamps (createdAt/updatedAt) automáticos

#### Controllers & APIs (13 controllers)
- [x] AuthController — Login, refresh, logout
- [x] ClientsController — CRUD clientes + sync Celero
- [x] ProjectsController — CRUD proyectos + usuarios asignados
- [x] ActionsController — CRUD acciones + conceptos asociados
- [x] ConceptsController — CRUD conceptos + formulación
- [x] PeriodsController — CRUD períodos + recalcular
- [x] ClosuresController — Cierres integrales + líneas
- [x] ApprovalsController — Flujo multi-rol (Gestor→FICO→Dirección)
- [x] UsersController — CRUD usuarios + asignaciones
- [x] AdminControllers — CECOs, departamentos, roles
- [x] HealthController — Health checks
- [x] CeleroVisitasController — Sync de visitas
- [x] OtherControllers — Integraciones varias

#### Servicios Core (19 servicios)
- [x] **AuthService** — JWT generation, password hashing (BCrypt)
- [x] **ClientService** — Gestión clientes, validaciones
- [x] **ProjectService** — Proyectos, CECOs multi-select, usuarios
- [x] **ActionService** — Acciones, conceptos asociados
- [x] **ConceptService** — Conceptos, cálculo de jerarquía (global→acción→empleado)
- [x] **ClosureService** — Cierres, líneas de cálculo
- [x] **ApprovalService** — Flujo de aprobaciones, rechazos, devoluciones
- [x] **PeriodService** — Períodos, estados, transiciones
- [x] **UserService** — Usuarios, roles, departamentos
- [x] **AdminServices** — CECOs, departamentos
- [x] **CalculationDataLoader** — Carga de datos para motor
- [x] **ExportService** — Exports A3 Innuva (.xls) y A3 ERP (.xlsx)
- [x] **CeleroVisitaService** — Sincronización Celero
- [x] **CurrentUserService** — Contexto usuario actual
- [x] **DashboardCalcSyncAudit** — KPIs dashboard
- [x] **TarifaPresupuestoServices** — Tarifas y presupuestos por proyecto

#### Motor de Cálculo (95% COMPLETADO)
- [x] **FormulaParser** — Parsea JSON de fórmulas `{tipo, valor, entidad, operacion}`
- [x] **CalculationEngine** — Ejecuta cálculos, resuelve variables, aplica operaciones
- [x] **VariableResolver** — Resuelve desde Visitas, Horas Bizneo, Horas Intratime, Gastos PayHawk, Tarifas
- [x] **CalculationContext** — Contexto de datos para cálculos
- [x] **FormulaNode** — Nodos de fórmulas (número, variable, operación)
- [x] Soporta operaciones: Suma, Cuenta, Promedio, +, -, ×, /, %
- [x] Jerarquía de alcance: global → proyecto → acción → empleado
- [x] Tests unitarios OK (FormulaParserTests, CalculationEngineTests)
- ⚠️ **FALTA:** Validación con datos reales de SIG

#### Autenticación & Seguridad (95% COMPLETADO)
- [x] JWT Bearer tokens con expiración configurable
- [x] RBAC con 9 roles: Admin, GestorProyecto, Backoffice, FICO, Dirección, Interlocutor, Auditor, Facilitador, Auxiliar.
- [x] Ownership filters — Gestor ve solo sus proyectos
- [x] Password hashing con BCrypt
- [x] Azure AD infraestructura (SSO fase 2)
- [x] Middleware de manejo de excepciones
- [x] ValidationFilter para DTOs

#### Base de Datos (95% COMPLETADO)
- [x] Tablas core: Usuarios, Clientes, Proyectos, Acciones, Conceptos, Períodos, Cierres, Líneas
- [x] Tablas staging: StagingCeleroVisita, StagingBizneoHora, StagingIntratimeFichaje, StagingPayHawkGasto, etc.
- [x] Tablas analíticas: BI_ClosureDetail, BI_ProjectMargin, BI_EmployeeProductivity
- [x] Índices en PK, FK, búsquedas frecuentes
- [x] Vistas analíticas para Power BI
- [x] 3 migraciones completadas
- ⚠️ **FALTA:** Datos reales en tablas productive

#### Exportación Excel (100% COMPLETADO)
- [x] **A3 Innuva (.xls)** — Nóminas, agrupa por UserId, mapea ColumnaA3
- [x] **A3 ERP (.xlsx)** — Facturas, líneas con VAT (21% España, 0% intra-EU)
- [x] Validación datos antes de export
- [x] Histórico de envíos
- [x] Tests E2E con Playwright
- [x] Manejo de soft-delete en navegaciones (documented pattern)

#### Testing Backend (80% COMPLETADO)
- [x] **Unit Tests:** FormulaParser, CalculationEngine, Services
- [x] **Integration Tests:** ApprovalFlow, ClientService, ClosureService, Ownership, Auth
- [x] **E2E Tests:** Exports Excel con datos
- [x] CustomWebApplicationFactory + SQLite in-memory
- [x] xUnit + FluentAssertions
- ⚠️ **FALTA:** Tests contra APIs reales (PayHawk, Intratime, etc.)

---

### ✅ FRONTEND (75% COMPLETADO)

#### Arquitectura Angular 18
- [x] Standalone components (sin módulos)
- [x] App routing con guards
- [x] Interceptores HTTP (Auth, error handling)
- [x] Guards: AuthGuard, RoleGuard
- [x] Servicios: Auth, API (clients, projects, actions, concepts, periods, approvals, closures)
- [x] Tema Angular Material (claro/oscuro)
- [x] SCSS con variables de tema

#### Componentes Shell & Layout (95% COMPLETADO)
- [x] **Shell** — Sidebar navegación, navbar, footer
- [x] **Login** — Formulario login, demo mode toggle, modo oscuro
- [x] **Breadcrumbs** — Navegación contextual
- [x] **Theme toggle** — Claro/oscuro con Material tokens

#### Dashboard (70% COMPLETADO)
- [x] KPIs por período: Cierres completados, Pendientes aprobación, Facturación, Margen
- [x] Período selector (dropdown dinámico)
- [x] Panel "Mis Proyectos" con estado/coste/facturación
- [x] Resumen global: Coste total, ingresos, margen
- [x] Panel alertas básico
- [x] Botón "Recalcular"
- [x] Charts SVG (pie, bar) con tema responsive
- ⚠️ **FALTA:** Validación datos reales, gráficas con margen por cliente

#### Módulo Clientes (95% COMPLETADO)
- [x] Listado + búsqueda + paginación
- [x] Formulario crear/editar
- [x] Detalle cliente
- [x] CRUD operacional
- ⚠️ **FALTA:** Cascada de borrado visual

#### Módulo Proyectos (90% COMPLETADO)
- [x] Listado + filtros (cliente, estado)
- [x] Formulario crear/editar
- [x] Detalle proyecto
- [x] Asignación de usuarios (multi-select)
- [x] CECOs multi-select
- [x] Sincronización Celero button
- [x] CRUD operacional
- ⚠️ **FALTA:** Sublistado de acciones en detalle

#### Módulo Acciones (85% COMPLETADO)
- [x] Listado + filtros (proyecto, estado)
- [x] Formulario crear/editar
- [x] Detalle acción
- [x] Gestión de conceptos (añadir, quitar, duplicar)
- [x] CRUD operacional
- ⚠️ **FALTA:** Sublistado de conceptos inline en detalle

#### Módulo Conceptos (80% COMPLETADO)
- [x] Listado + filtros (tipo: Pago/Factura)
- [x] Formulario crear/editar
- [x] Detalle concepto
- [x] Editor de fórmulas (JSON visible, no builder visual)
- [x] Tipos: Pago, Factura
- [x] Alcance: global, proyecto, acción, empleado
- [x] CRUD operacional
- ⚠️ **FALTA:** Builder visual drag-drop

#### Módulo Períodos (95% COMPLETADO)
- [x] Listado + filtros
- [x] Estados: Abierto, EnCalculo, Calculado, EnRevision, Aprobado, Bloqueado
- [x] Creación/edición períodos
- [x] Botón "Recalcular" (dispara motor)
- [x] CRUD operacional

#### Módulo Cierres & Aprobaciones (80% COMPLETADO)
- [x] Listado cierres con filtros (período, cliente, proyecto, estado)
- [x] Detalle cierre con líneas de cálculo
- [x] Flujo aprobación: Gestor→Backoffice→FICO→Dirección
- [x] Botones: Aprobar, Rechazar (con comentarios)
- [x] Aprobación masiva (checkbox multi-select)
- [x] Histórico aprobaciones
- ⚠️ **FALTA:** Desglose líneas por concepto/empleado; exceptions handling UI

#### Módulo Usuarios & Roles (90% COMPLETADO)
- [x] Listado usuarios + filtros (rol, departamento)
- [x] Formulario crear/editar
- [x] Detalle usuario
- [x] Asignación de roles (multi-select)
- [x] Asignación de departamentos (multi-select)
- [x] Asignación de clientes/proyectos/acciones
- [x] CRUD operacional

#### Módulo Administración (85% COMPLETADO)
- [x] CECOs (CRUD)
- [x] Departamentos (CRUD)
- [x] Roles (vista/edit permisos)
- ⚠️ **FALTA:** Integraciones (botones sync para APIs)

#### Auditoría (90% COMPLETADO)
- [x] AuditLog viewer
- [x] Filtros: Usuario, Entidad, Acción, Rango fechas
- [x] Tabla con Usuario, Entidad, ID, Acción, Cambios (before/after), Timestamp

#### Testing Frontend (40% COMPLETADO)
- [x] Smoke tests (componentes cargan sin errores)
- [x] Tests unitarios: AuthService, ApiHelpers
- [x] E2E visuales (Cypress smoke)
- ⚠️ **FALTA:** E2E completo (login→approval workflow) con datos reales

---

### ✅ INTEGRACIONES (40% COMPLETADO)

#### Celero One ✅ EN CURSO (70%)
- [x] Conexión directa PostgreSQL AlloyDB establecida
- [x] CeleroPostgresClient implementado
- [x] Sincronización de clientes/proyectos
- [x] Sincronización de visitas
- [x] Botón sync en UI
- ⚠️ **FALTA:** Validación contra datos reales PROD

#### Bizneo ⚠️ EN CURSO (60%)
- [x] BizneoClient implementado (HTTP)
- [x] Endpoint `/users` (empleados)
- [x] Endpoint `/absences` (ausencias/vacaciones)
- [x] DTOs mapeados
- ⚠️ **FALTA:** Validación mapeo real; endpoint `/hours`

#### Intratime ❌ BLOQUEADO
- [x] IntratimeClient interfaz definida
- ⚠️ **PROBLEMA:** Token inválido o expirado
- **ACCIÓN:** Contactar soporte Intratime

#### PayHawk ❌ BLOQUEADO
- [x] PayHawkClient interfaz definida
- ⚠️ **PROBLEMA:** Falta Account ID del portal
- **ACCIÓN:** Cliente proporcionar Account ID + API Key

#### TravelPerk ❌ BLOQUEADO
- [x] TravelPerkClient interfaz definida
- ⚠️ **PROBLEMA:** Falta API Key
- **ACCIÓN:** Cliente obtener de portal TravelPerk

#### SGPV ⚠️ PENDIENTE ESPECIFICACIÓN
- [x] Interfaz ISgpvClient definida
- [x] Tabla staging StagingSgpvProducto
- [x] Botón sync en UI
- ⚠️ **PROBLEMA:** Necesita formato JSON exportación
- **ACCIÓN:** Cliente aclarar esquema esperado

#### A3 Nómina (Conectia) ❌ CRÍTICO BLOQUEADO
- [x] Interfaz IA3InnuvaClient definida
- ❌ **PROBLEMA:** Requiere OAuth2 Conectia
- **ACCIÓN:** Cliente proporcionar Client ID / Client Secret

#### A3 ERP (Conectia) ❌ CRÍTICO BLOQUEADO
- [x] Interfaz IA3ErpClient definida
- ❌ **PROBLEMA:** Requiere OAuth2 Conectia
- **ACCIÓN:** Cliente proporcionar Client ID / Client Secret

#### Galán ⚠️ SIN INFORMACIÓN
- ⚠️ **PROBLEMA:** Sin documentación API
- **ACCIÓN:** Revisar PowerBi_Logistica_2026.docx (internamente)

#### Mediapost ❌ SIN INFORMACIÓN
- ❌ **PROBLEMA:** Sin credenciales ni documentación
- **ACCIÓN:** Obtener información acceso (API/SFTP)

---

## TRABAJO BLOQUEADO (POR CLIENTE)

### 🔴 CRÍTICO - NECESARIO PARA AVANZAR

1. **A3 Nómina (Conectia) OAuth2**
   - Requerimiento: Client ID / Client Secret
   - Impacto: Sin esto, NO se pueden generar nóminas
   - Status: Contactado a cliente

2. **A3 ERP (Conectia) OAuth2**
   - Requerimiento: Client ID / Client Secret
   - Impacto: Sin esto, NO se pueden generar facturas
   - Status: Contactado a cliente

3. **Intratime - Token válido**
   - Requerimiento: API Key / Token válido
   - Impacto: Sin esto, NO se sincronizan horas trabajadas
   - Status: Contactado a cliente

4. **PayHawk - Account ID + API Key**
   - Requerimiento: Account ID, API Key del portal
   - Impacto: Sin esto, NO se sincronizan gastos
   - Status: Pendiente solicitud

5. **TravelPerk - API Key**
   - Requerimiento: API Key del portal
   - Impacto: Sinc. viajes (secundario)
   - Status: Pendiente solicitud

### 🟡 MEDIA PRIORIDAD - NECESARIO PARA DATOS REALES

6. **Datos reales para UAT**
   - Requerimiento: 2-3 períodos (mayo-julio 2026) con:
     - Clientes reales
     - Proyectos con presupuestos
     - Empleados (mín 5-10)
     - Visitas Celero sincronizadas
   - Impacto: SIN datos reales, no puedo validar correctitud
   - Status: Pendiente

7. **Validación reglas de negocio (Finanzas)**
   - Requerimiento: Con Lourdes/Lara:
     - Conceptos siempre incluidos
     - Excepciones/casos especiales
     - Límites dietas/km
     - Márgenes mín/máx por cliente
   - Impacto: Motor de cálculo sin validación
   - Status: Pendiente reunión

8. **Validación flujo aprobación (Operaciones)**
   - Requerimiento: Con Yoana/Martha:
     - ¿FICO siempre aprueba?
     - ¿Dirección solo > 50K€?
     - Timings esperados
   - Impacto: Flujo sin datos reales
   - Status: Pendiente reunión

---

## PRÓXIMOS PASOS (SEMANAS 3-7)

### SEMANA 3 (10-14 JUNIO) - INTEGRACIÓN & VALIDACIÓN

**Backend:**
- [ ] Solicitar credenciales URGENTE (Intratime, PayHawk, TravelPerk, A3, Galán, Mediapost)
- [ ] Implementar integraciones reales (según credenciales disponibles)
- [ ] Integrar retry + circuit-breaker (Polly)
- [ ] Tests E2E integraciones (mock/sandbox)

**Frontend:**
- [ ] Detalle línea de cálculo en aprobaciones (desglose concepto+empleado)
- [ ] Dashboard con gráficas margen por cliente
- [ ] Vista "Mis Aprobaciones" por rol
- [ ] Refinamiento UX general

**Validaciones:**
- [ ] Reunión con FICO (Lourdes/Lara) — reglas pago
- [ ] Reunión con Operaciones (Yoana/Martha) — flujo aprobación
- [ ] Testing E2E aprobaciones con datos ficticios

### SEMANA 4 (17-21 JUNIO) - A3 + POWER BI + DATOS REALES

**Backend:**
- [ ] Integración A3 Nómina (si tenemos OAuth)
- [ ] Integración A3 ERP (si tenemos OAuth)
- [ ] Sync programada (Hangfire o hosted services)
- [ ] Notificaciones de aprobación

**Frontend:**
- [ ] Power BI dashboards embebidos
- [ ] UAT inicial con datos reales
- [ ] Bug fixes según feedback

**Data:**
- [ ] Recibir datos reales SIG (2-3 períodos)
- [ ] Cargar en base datos de desarrollo
- [ ] Ejecutar cierres reales

### SEMANA 5 (24-28 JUNIO) - REFINAMIENTO & DEPLOYMENT STAGING

**Backend:**
- [ ] Optimización queries (< 30s para 5000 registros)
- [ ] Validaciones adicionales FICO
- [ ] Documentación API (Swagger)

**Frontend:**
- [ ] Builder visual de fórmulas (drag-drop)
- [ ] Importación masiva desde Excel
- [ ] Bug fixes UAT
- [ ] Documentación usuario

**Infraestructura:**
- [ ] Setup Azure App Service (BE)
- [ ] Setup Azure Static Web Apps (FE)
- [ ] Deployment staging
- [ ] CI/CD pipelines (GitHub Actions)

### SEMANA 6 (1-5 JULIO) - UAT FINAL & PRODUCCIÓN

**Testing:**
- [ ] Tests carga (5000+ registros)
- [ ] Tests integraciones en staging
- [ ] UAT completa con cliente
- [ ] Bug fixes críticos

**Infraestructura:**
- [ ] Setup Azure SQL (producción)
- [ ] Integración Azure AD (SSO)
- [ ] Secrets en Key Vault
- [ ] Monitoring App Insights

**Documentación:**
- [ ] Manuales usuario por rol
- [ ] Guía de administración
- [ ] Guía de integraciones

### SEMANA 7 (8-15 JULIO) - BUFFER & GO-LIVE

- [ ] Bug fixes finales
- [ ] Training al equipo SIG
- [ ] Checklist go-live
- [ ] Soporte post-launch (primeros días)

---

## DEPENDENCIAS CRÍTICAS (RUTA CRÍTICA)

```mermaid
A["Credenciales A3\n(Conectia OAuth)"] -->|BLOQUEADOR| B["Implementar A3\nNómina + ERP"]
C["Datos reales\nSIG (2-3 períodos)"] -->|BLOQUEADOR| D["UAT completa\ncon cliente"]
E["Credenciales\nPayHawk/Intratime"] -->|BLOQUEADOR| F["Integraciones\nreales"]
B -->|REQUIERE| G["Testing &\nRefinamientos"]
D -->|REQUIERE| G
F -->|REQUIERE| G
G -->|REQUIERE| H["Deployment\nAzure"]
H -->|REQUIERE| I["Go-Live\n15-20 Jul"]
```

---

## RIESGOS & MITIGACIÓN

| Riesgo | Prob. | Impact | Mitigación | Propietario |
|--------|-------|--------|-----------|------------|
| APIs bloqueadas sin credentials | 🔴 Alta | 🔴 Crítico | Solicitar ahora a cliente | Eladio/Silvia |
| Datos reales llegan tarde | 🟡 Media | 🔴 Alto | Usar seeds realistas; UAT ficticia pre-carga | Cliente / Dev |
| Cambios reglas pago en fase final | 🟡 Media | 🟡 Medio | Documentar cada regla; versionar conceptos | Lourdes/Lara |
| Deployment Azure no funciona | 🟡 Media | 🟡 Medio | Configurar staging ahora; infra tests | Eladio/Silvia |
| Performance < 30s para 5K registros | 🟢 Baja | 🟡 Medio | Profiling temprano; índices; caching | Dev |

---

## ENTREGABLES POR SEMANA

| Semana | Entregable | Status |
|--------|-----------|--------|
| **2** (5 jun) | Estado actual + análisis | ✅ Hoy |
| **3** | Integraciones parciales | 🔄 In progress |
| **4** | A3 + Power BI + Datos reales | ⏳ Pending |
| **5** | Staging deployment | ⏳ Pending |
| **6** | UAT final | ⏳ Pending |
| **7** | Go-live readiness | ⏳ Pending |

---

## CHECKLIST ESTA SEMANA (5-9 JUNIO)

### Para Cliente (URGENTE)

- [ ] **Solicitar credenciales por email:**
  - [ ] Intratime API Key / Token
  - [ ] PayHawk Account ID + API Key
  - [ ] TravelPerk API Key
  - [ ] **A3 Conectia Client ID / Secret** ⚠️ CRÍTICO
  - [ ] Galán documentación API
  - [ ] Mediapost credenciales

- [ ] **Agendar reuniones:**
  - [ ] Lourdes/Lara (Finanzas) — 60 min — Reglas pago/dietas/excepciones
  - [ ] Yoana/Martha (Operaciones) — 60 min — Flujo aprobación/timings
  - [ ] Eladio/Silvia (IT) — 30 min — Azure AD + credenciales

- [ ] **Preparar datos reales:**
  - [ ] Exportar 3 cierres históricos (Excel actual)
  - [ ] Listar proyectos activos con presupuestos
  - [ ] Listar empleados activos (mín 5-10)

### Para Desarrollo

- [ ] Completar E2E aprobaciones con datos ficticios
- [ ] Preparar Azure setup (templates)
- [ ] Documentar credenciales en .env (no commiteadas)
- [ ] Actualizar ESTADO_REAL_PROYECTO.md semanal

---

**Documento generado:** 5 de junio 2026  
**Próxima actualización:** 12 de junio 2026 (final semana 3)  
**Responsable:** h&k consulting + Dev team

---

## CONTACTOS CLAVE

| Rol | Nombre | Email | Teléfono | Responsabilidad |
|-----|--------|-------|----------|-----------------|
| Product Owner | Eladio / Sergio | — | — | Decisiones, priorización |
| Responsable IT/Integraciones | Silvia López | — | — | Credenciales, infraestructura |
| Responsable Finanzas | Lourdes / Lara | — | — | Reglas pago, validación |
| Responsable Operaciones | Yoana / Martha | — | — | Procesos, flujos |
| Desarrollador Backend | [h&k] | — | — | Implementación |
| Desarrollador Frontend | [h&k] | — | — | UI/UX |

---

*Este documento es VIVO y se actualiza cada semana con progreso real.*
