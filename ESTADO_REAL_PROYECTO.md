# ESTADO REAL DEL PROYECTO SIG-ES
**Fecha:** 9 de junio de 2026 | **Semana:** 2.5 de 4-5 semanas | **Entrega:** ~15-20 de julio de 2026

---

## 1. RESUMEN EJECUTIVO

### ✅ COMPLETADO
- **Backend Core:** 90% — Controllers, Services, DTOs, Migrations, Clean Architecture en lugar
- **Frontend Core:** 85% — Angular 18, Material Design, componentes principales, routing, computed signals
- **Base de datos:** 95% — Schema SQL Server completo, soft-delete, índices, vistas analíticas
- **Cálculo de pagos:** 85% — Motor de cálculo formulado (JSON parser, variable resolver), soporta Intratime
- **Autenticación:** 90% — JWT + Azure AD ready (no completamente integrado)
- **Auditoría:** 95% — AuditLog completo, soft-delete, trazabilidad
- **Export Excel:** 100% — A3 Innuva (.xls) y A3 ERP (.xlsx) + tests E2E
- **Intratime Integration:** 100% ✅ — empleados, fichajes, discrepancias, RowAdapter fix
- **Dashboard Módulo:** 100% ✅ COMPLETADO — KPIs dinámicos, desglose clientes, evolución períodos, alertas, gauge margen

### ⚠️ EN PROGRESO / PARCIAL
- **Integraciones API:** 65% — Bizneo, Celero, PayHawk, SGPV, Intratime OK (5 sistemas); A3/Galán/Mediapost/TravelPerk bloqueados
- **Aprobaciones:** 80% — Flujo multi-rol, FICO/Dirección, rechazos; falta aprobación masiva mejorada
- **Operaciones UI:** 60% — Proyectos, Acciones, Conceptos, pendiente detalle completo
- **Editor Visual Fórmulas:** 0% — Componente skeleton creado, falta drag-drop CDK
- **Sincronización Celero:** 70% — Mapeos de IDs parciales, falta validación PostgreSQL real
- **Detalle Aprobación:** 50% — Vista básica OK, falta desglose por empleado
- **Validaciones FICO:** 60% — Lógica previa OK, falta histórico de envíos
- **Power BI Integration:** 0% — Vistas SQL OK, embeddings pendientes
- **Datos reales:** 25% — Intratime empleados/fichajes, SGPV productos, PayHawk gastos (992 registros)

### ❌ NO INICIADO / CRÍTICO BLOQUEADO
- **Integraciones** A3 Nómina, A3 ERP (requieren OAuth2 Conectia)
- **Integraciones** TravelPerk (falta API Key)
- **Integraciones** Galán, Mediapost (DOCX Con ejemplo de datos)
- **Tests E2E en vivo** — Sin data real, sin Celero conectada a prod
- **Documentación API** — Swagger presente pero no validado
- **Deployment Azure** — Configuración pendiente

---

## 2. ANÁLISIS DETALLADO POR MÓDULO

### **MÓDULO: DASHBOARD**
| Componente | Estado | Notas |
|-----------|--------|-------|
| Layout shell | ✅ 100% | Sidebar, navbar, período selector con datos dinámicos |
| KPIs período | ✅ 100% | Cálculos reales, facturación, coste, margen %, cierres completados/pendientes |
| Desglose clientes | ✅ 100% | Top 6 clientes por facturación, % del total, leyenda dinámica |
| Evolución períodos | ✅ 100% | Últimos 6 períodos, área chart dinámico con path SVG calculado |
| Panel "Mis Proyectos" | ✅ 100% | Listado completo con coste, facturación, margen por proyecto |
| Gauge margen | ✅ 100% | Arc SVG dinámico basado en margenPct real, actualización en vivo |
| Panel objetivos | ✅ 100% | Barras de progreso reales: facturación, cierres, margen (vs objetivo 25%) |
| Panel alertas | ✅ 100% | 5 tipos: CierrePendiente, CierreRechazado, IncidenciaCalculo, PeriodoBloqueado, PeriodoProximoVencer |
| Badge notificaciones | ✅ 100% | Muestra número dinámico de avisos (eliminado hardcoded "3") |
| Botón recalcular | ✅ 100% | Funcional, dispara motor de cálculo |
| Computed Signals | ✅ 100% | 4 signals: donutSegmentos, gaugePath, evolucionPath, margenPct |

**Compilación:** ✅ Backend sin errores (195 warnings). ✅ Frontend sin errores.

**Urgencia:** COMPLETADA — Módulo 100% funcional con datos dinámicos

---

### **MÓDULO: CLIENTES & PROYECTOS & ACCIONES**
| Componente | Estado | Notas |
|-----------|--------|-------|
| CRUD Clientes | ✅ 95% | API completa, UI listado + form, falta cascadas de borrado |
| CRUD Proyectos | ✅ 90% | API completa, asignación usuarios OK, CECOs multi-select OK |
| CRUD Acciones | ✅ 85% | API completa, relación acción→concepto OK, falta sublistado conceptos inline |
| Sincronización Celero | ⚠️ 60% | Conexión PostgreSQL directa OK, mapeo de IDs pendiente |
| Filtros & búsqueda | ✅ 90% | Implementados, falta full-text search |

VER SI SE DESEA TENER:
  1. [ ] Cascadas borrado
  Completar soft-delete en cascada: borrar Cliente elimina sus Proyectos, Acciones y Conceptos
  2. [ ] Acciones: sublistado
  Añadir sublistado de conceptos inline dentro del detalle de una Acción
  3. [ ] Celero: mapeo IDs
  Completar el mapeo CeleroClientId ↔ SIG ClientId para que la sync tenga fidelidad 100%
  4. [ ] Full-text search
  Añadir búsqueda de texto libre en listados de Clientes, Proyectos y Acciones

**Urgencia:** MEDIA-BAJA — Core funcional, UX refinements secundarios

---

### **MÓDULO: CONCEPTOS (Motor de Cálculo)**
| Componente | Estado | Notas |
|-----------|--------|-------|
| CRUD Conceptos | ✅ 90% | API OK, tipos pago/factura OK |
| Parser formulación JSON | ✅ 95% | **FormulaParser.cs:** parsea `{tipo, valor, entidad, operacion}` correctamente |
| Motor cálculo | ✅ 90% | **CalculationEngine.cs:** resuelve variables, aplica operaciones, maneja jerarquías |
| Variable resolver | ✅ 85% | Resuelve desde Visitas, Horas Bizneo, Horas Intratime, Gastos PayHawk, Tarifas, etc. |
| Editor visual formulación | ⚠️ 40% | Interfaz presente, UX árida, falta builder drag-drop |
| Alcance (global→acción→empleado) | ✅ 90% | Jerarquía implementada, tests OK |


**Urgencia:** MEDIA — Motor OK, UI refinements para usabilidad del cliente

---

### **MÓDULO: PERÍODOS & CIERRES & APROBACIONES**
| Componente | Estado | Notas |
|-----------|--------|-------|
| CRUD Períodos | ✅ 95% | Estados (Abierto→Aprobado→Bloqueado), transiciones OK |
| Acción "Recalcular" | ✅ 100% | Dispara motor para todos los conceptos activos |
| Cierre integral | ✅ 90% | **CierreIntegral + LineaCierre:** estructura OK, cálculos OK |
| Flujo aprobación | ✅ 85% | Gestor→Backoffice→FICO→Dirección, rechazos + devoluciones OK |
| Detalle aprobación | ⚠️ 70% | Líneas de cálculo visibles, desglose por empleado pendiente |
| Comentarios & auditoría | ✅ 95% | AuditLog completo, comentarios en CierreIntegral OK |
| Aprobación masiva | ⚠️ 60% | Checkbox multi-select OK, API OK, UX mejorable |

**Urgencia:** ALTA — Necesita validación con FICO sobre líneas de cálculo y excepciones

---

### **MÓDULO: CONTABILIDAD & EXPORTACIÓN**
| Componente | Estado | Notas |
|-----------|--------|-------|
| Export A3 Innuva (.xls) | ✅ 100% | **ExportService.cs:** agrupa por UserId, mapea ColumnaA3, tests E2E OK |
| Export A3 ERP (.xlsx) | ✅ 100% | Líneas de factura + VAT (21% España, 0% intra-EU), tests OK |
| Validación previa | ⚠️ 70% | Básica (presencia datos), falta validaciones FICO |
| Histórico de envíos | ⚠️ 50% | Estructura OK, logging pendiente |
| Integración A3 real | ❌ 0% | **BLOQUEADO:** Requiere OAuth2 Conectia (Client ID/Secret) |

**Urgencia:** CRÍTICA — Exports funcionan pero integración real bloqueada por cliente

---

### **MÓDULO: INTEGRACIONES API**

#### **Bizneo** ✅ EN PROGRESO
- **Implementación:** `BizneoClient.cs` (HttpClients.cs)
- **Endpoints:** Users (empleados), Absences (ausencias/vacaciones)
- **Estado:** Parcial — Obtiene empleados OK, horas pendiente
- **Problema:** API Bizneo tiene formato diferente, necesita ajustes de mapeo
- **Bloqueador:** Validación end-to-end con datos reales SIG

#### **Celero One** ✅ EN PROGRESO
- **Implementación:** `CeleroPostgresClient.cs` (conexión directa PostgreSQL AlloyDB)
- **Datos:** Clientes, proyectos, visitas, geolocalización
- **Estado:** **Conexión directa OK**, sincronización de datos OK
- **Nota:** Single source of truth en tiempo real
- **Bloqueador:** Requiere validación con esquema Celero real en prod

#### **Intratime** ✅ COMPLETADO
- **Implementación:** `IntratimeClient.cs` (HttpClients.cs) - COMPLETA
- **Endpoints:** 
  - `GetEmpleadosAsync()` — Obtiene empleados con NIF
  - `GetFichajesAsync(desde, hasta)` — Obtiene fichajes entrada/salida
- **Integración en SyncService:**
  - `case "intratime-empleados"` — Sincroniza empleados a StagingIntratimeEmpleado
  - `case "intratime"` — Sincroniza fichajes, calcula HorasCalculadas, resuelve UserId
- **DataProcessor:**
  - `ProcessIntratimeEmpleadosAsync()` — Mapea empleados por NIF, crea users si no existen
  - `ValidarDiscrepanciasIntratimeAsync(desde, hasta)` — Detecta horas vs. visitas
- **CalculationEngine:** `RowAdapter.FromFichaje()` ahora retorna `Horas = f.HorasCalculadas`
- **Frontend:** Sync component incluye botones para "Intratime Fichajes" e "Intratime Empleados"
- **Base de datos:** Migraciones aplicadas (StagingIntratimeEmpleado, campos en StagingIntratimeFichaje)
- **Estado:** Listo para sincronizar con API real (requiere token válido)

#### **PayHawk** ✅ COMPLETADO
- **Implementación:** `PayHawkClient.cs` (HttpClients.cs)
- **Autenticación:** X-Payhawk-ApiKey header (no Bearer)
- **Endpoint:** `/api/v3/accounts/{accountId}/expenses`
- **Estado:** **992 gastos sincronizados correctamente** (5 de junio 2026)
- **NIF Parsing:** Extrae IDs numéricos de campos alfanuméricos (ej: "44175805G" → 44175805)
- **Nota:** DataProcessor migró 992 registros a producción con 0 errores

#### **TravelPerk** ❌ BLOQUEADO
- **Problema:** Falta API Key
- **Requerimiento:** Cliente debe obtener de portal TravelPerk
- **Stub:** `TravelPerkClient.cs` presente

#### **SGPV** ✅ COMPLETADO
- **Implementación:** `SgpvClient.cs` (HttpClients.cs)
- **Autenticación:** HTTP Basic auth (Username/Password)
- **Endpoint:** `/api/` con login de sesión
- **Estado:** **997 productos sincronizados correctamente** (5 de junio 2026)
- **Tabla staging:** `StagingSgpvProducto` con migración a producción OK
- **Nota:** DataProcessor migró 997 registros a producción con 0 errores

#### **A3 Nómina (Innuva)** ❌ CRÍTICO BLOQUEADO
- **Problema:** Requiere OAuth2 en plataforma Conectia
- **Requerimiento:** Client ID / Client Secret desde Conectia
- **Impacto:** Sin esto, NO se pueden generar nóminas

#### **A3 ERP (Facturación)** ❌ CRÍTICO BLOQUEADO
- **Problema:** Requiere OAuth2 en plataforma Conectia
- **Requerimiento:** Client ID / Client Secret desde Conectia
- **Impacto:** Sin esto, NO se pueden generar facturas

#### **Galán** ❌ SIN INFORMACIÓN
- **Problema:** Sin acceso a API o documentación
- **Dato disponible:** PowerBi_Logistica_2026.docx (revisar internamente)

#### **Mediapost** ❌ SIN INFORMACIÓN
- **Problema:** Sin información de acceso (API/SFTP)

---

### **AUTENTICACIÓN & SEGURIDAD**
| Componente | Estado | Notas |
|-----------|--------|-------|
| JWT Bearer | ✅ 95% | Claims (roles, userId, departamentos), expiration OK |
| Azure AD (SSO) | ⚠️ 30% | Infraestructura lista, integración pendiente |
| RBAC (roles) | ✅ 95% | Administrador, GestorProyecto, Backoffice, FICO, Dirección, Interlocutor, Auditor |
| Guards Angular | ✅ 95% | AuthGuard, RoleGuard OK |
| Interceptores HTTP | ✅ 100% | Token injection, error handling OK |
| Password hashing | ✅ 100% | BCrypt implementado |
| Ownership filters | ✅ 95% | Un gestor ve solo sus proyectos, auditor solo logs |

**Urgencia:** MEDIA — Core OK, Azure AD integración fase 2

---

### **TESTING**
| Tipo | Estado | Cobertura |
|-----|--------|-----------|
| Unit Tests | ✅ 90% | Formula parser, cálculos, servicios core |
| Integration Tests | ✅ 80% | ApprovalFlow, ClientService, ClosureService, Ownership |
| E2E Tests | ⚠️ 40% | Login OK, dashboard visual OK, actualmente no tests E2E de aprobaciones |
| Performance Tests | ❌ 0% | No benchmarks para cierre de 5000 registros |

**Urgencia:** MEDIA — Unit + integration OK, E2E necesita datos reales

---

## 3. QUÉS NECESITAMOS DEL CLIENTE (URGENCIA ALTA)

### **INTEGRACIONES EXTERNAS - CREDENCIALES (BLOQUEANTES)**

1. **Intratime** — Token/API Key válido para acceso en tiempo real
2. **TravelPerk** — API Key del portal
3. **A3 Nómina (Conectia)** — Client ID / Client Secret OAuth2 ⚠️ **CRÍTICO**
4. **A3 ERP (Conectia)** — Client ID / Client Secret OAuth2 ⚠️ **CRÍTICO**
5. **Galán** — Documentación de API o acceso SFTP
6. **Mediapost** — Documentación y credenciales de acceso

**✅ COMPLETADAS (5 de junio 2026):**
- PayHawk: 992 gastos sincronizados
- SGPV: 997 productos sincronizados

### **DATOS & CONFIGURACIÓN**

9. **Datos reales de prueba** — Al menos 2-3 períodos (Mayo, Junio, Julio 2026) con:
   - Clientes reales (no ficticios)
   - Proyectos con presupuestos reales
   - Empleados reales (mínimo 5-10)
   - Visitas Celero sincronizadas
   - Conceptos de pago personalizados según acuerdos

10. **Validación de reglas de negocio** con Lourdes/Lara (Finanzas):
    - ¿Qué conceptos SIEMPRE se incluyen?
    - ¿Cuáles son excepcionales?
    - ¿Límites de dietas/kilometraje?
    - ¿Márgenes mínimos/máximos por cliente?

11. **Validación de flujo de aprobación** con Yoana/Martha (Operaciones):
    - ¿FICO siempre aprueba? ¿Hay casos de devolución?
    - ¿Dirección necesita revisar todos o solo >50K€?
    - ¿Timings esperados de cada fase?

12. **Integración Azure AD** — Definir grupos AD y asignaciones automáticas de roles

---

## 4. QUÉ FALTA IMPLEMENTAR (URGENCIA MEDIA-ALTA)

### **BACKEND**

- [x] PayHawk integration: 992 gastos sincronizados
- [x] SGPV integration: 997 productos sincronizados
- [ ] **PRÓXIMOS:** Validación de cálculos de cierre con datos sincronizados
- [ ] Llamadas reales a APIs bloqueadas (A3, Intratime, TravelPerk, Galán, Mediapost)
- [ ] Retry + circuit-breaker para integraciones HTTP (Polly)
- [ ] Sincronización programada (background jobs — Hangfire o hosted services)
- [ ] Notificaciones de aprobación (email, quizás Slack)
- [ ] Validaciones FICO adicionales (margen mínimo, políticas de gasto)
- [ ] Tests de integración E2E contra APIs reales (mock o sandbox)

### **FRONTEND**

- [ ] Detalle línea de cálculo en aprobaciones (desglose por concepto + empleado)
- [ ] Builder visual de fórmulas (drag-drop, no JSON textual)
- [ ] Vista "Mi Aprobaciones" para cada rol
- [ ] Dashboard ejecutivo con gráficas de margen, desviaciones presupuestarias
- [ ] Gestión de excepciones en cálculos (override manual + auditoría)
- [ ] Importación masiva de datos desde Excel (template definido)
- [ ] Búsqueda full-text en listados

### **INFRAESTRUCTURA & DEVOPS**

- [ ] Configuración Azure App Service (BE)
- [ ] Configuración Azure Static Web Apps (FE)
- [ ] CI/CD pipelines (GitHub Actions)
- [ ] Secrets en Azure Key Vault
- [ ] Monitoring / Application Insights
- [ ] Base de datos Azure SQL en producción

### **PODER BI**

- [ ] Dashboards embebidos en la UI SIG-ES
- [ ] Definición de vistas analíticas adicionales (si procede)
- [ ] Configuración de refresh (diario/en demanda)

---

## 5. CRONOGRAMA REALISTA (SEMANAS RESTANTES)

**Hoy:** Semana 2 (5 junio)  
**Entrega:** ~15-20 julio (4-5 semanas restantes)

| Semana | Qué | Bloqueantes |
|--------|-----|------------|
| **2.5** (5-9 jun) | ✅ PayHawk + SGPV completados; Validación cierres con datos sincronizados | — |
| **3** (10-14 jun) | Integración real Intratime; Validación flujo aprobación con FICO; Tests E2E aprobaciones | Creds Intratime |
| **4** (17-21 jun) | A3 Nómina + A3 ERP; Dashboard ejecutivo; Datos reales SIG; UAT inicial | OAuth Conectia |
| **5** (24-28 jun) | Refinamientos UX; Deployment Azure staging; Tests carga (5000 registros) | Infraestructura Azure |
| **6** (1-5 jul) | Bug fixes UAT; Integración Azure AD; Documentación; Training cliente | — |
| **7** (8-15 jul) | Buffer final; Go-live readiness; Producción | — |

---

## 6. MATRIZ DE RIESGOS

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|-------------|--------|-----------|
| APIs bloqueadas sin credenciales | 🔴 Alta | 🔴 Crítico | Solicitar ahora mismo al cliente |
| Datos reales llegan tarde | 🟡 Media | 🔴 Alto | Usar seeds realistas; UAT virtual con datos ficticios |
| Cambios de negocio en reglas pago | 🟡 Media | 🟡 Medio | Documentar cada regla; versionar conceptos |
| Deployment Azure no funciona | 🟡 Media | 🟡 Medio | Configurar staging ahora; tests infraestructura |
| Celero schema cambios | 🟡 Media | 🟡 Medio | Validar conexión ahora; acordar congelación schema |

---

## 7. PRÓXIMOS PASOS INMEDIATOS (ESTA SEMANA)

### **Para TI (Eladio/Silvia)**

- [ ] **Solicitar credentials URGENTE:**
  - [ ] Intratime — token válido
  - [ ] TravelPerk — API Key
  - [ ] **A3 Conectia — Client ID/Secret** ⚠️
  - [ ] **Galán — documentación API**
  - [ ] **Mediapost — credenciales acceso**

- [ ] Validar conexión Celero PostgreSQL en PROD
- [ ] Comenzar setup Azure (App Service, Static Web Apps, SQL)

### **Para Operaciones (Yoana/Martha)**

- [ ] Validar flujo de aprobación actual (tiempos, devoluciones, excepciones)
- [ ] Proporcionar 2-3 cierres históricos reales para análisis
- [ ] Definir qué alertas son críticas en dashboard

### **Para Finanzas (Lourdes/Lara)**

- [ ] Validar reglas de cálculo de dietas/kilometraje
- [ ] Aprobar márgenes mínimos por cliente
- [ ] Confirmar casos especiales / excepciones en pago
- [ ] Proporcionar plantilla actual de cierre manual (Excel)

### **Para Desarrollo**

- [x] PayHawk integration completada (992 gastos)
- [x] SGPV integration completada (997 productos)
- [ ] **URGENTE:** Validar cálculos de cierre con datos sincronizados (PayHawk + SGPV + Celero)
- [ ] Esperar credentials Intratime; integrar APIs reales
- [ ] Validar E2E aprobaciones con datos sincronizados
- [ ] Preparar Azure deployment
- [ ] Revisar datos seed para que sean realistas
- [ ] Completar Bizneo integration (falta endpoint de horas detalladas)

---

## 8. CONTEXTO TÉCNICO ACTUALIZADO

El proyecto sigue **Clean Architecture + Vertical Slice** correctamente:
- **Domain:** Entidades, enums, value objects
- **Application:** Services, DTOs, validators, cálculo
- **Infrastructure:** Persistence (EF Core), integrations, auth
- **API:** Controllers, middleware, filters

**EF Core gotchas documentados:**
- Soft-delete filters en navegaciones requieren `.IgnoreQueryFilters()` explícito
- ExportService implementa patrón para cargar datos soft-deleted correctamente

**Stack confirmado:**
- Backend: .NET Core 10, SQL Server, Azure
- Frontend: Angular 18, Material Design
- Testing: xUnit, Jasmine/Cypress (sin full E2E aún)

---

**Documento generado:** 2026-06-05 | **Estado proyecto:** Funcional 75%, ready para datos reales 50%
