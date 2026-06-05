# ESTADO REAL DEL PROYECTO SIG-ES
**Fecha:** 5 de junio de 2026 | **Semana:** 2 de 4-5 semanas | **Entrega:** ~15-20 de julio de 2026

---

## 1. RESUMEN EJECUTIVO

### ✅ COMPLETADO
- **Backend Core:** 85% — Controllers, Services, DTOs, Migrations, Clean Architecture en lugar
- **Frontend Core:** 75% — Angular 18, Material Design, componentes principales, routing
- **Base de datos:** 95% — Schema SQL Server completo, soft-delete, índices, vistas analíticas
- **Cálculo de pagos:** 80% — Motor de cálculo formulado (JSON parser, variable resolver)
- **Autenticación:** 90% — JWT + Azure AD ready (no completamente integrado)
- **Auditoría:** 95% — AuditLog completo, soft-delete, trazabilidad
- **Export Excel:** 100% — A3 Innuva (.xls) y A3 ERP (.xlsx) + tests E2E

### ⚠️ EN PROGRESO / PARCIAL
- **Integraciones API:** 40% — Bizneo y Celero OK, Intratime/PayHawk/TravelPerk stubs, otros bloqueados
- **Dashboard:** 70% — KPIs básicos, período selector, alertas iniciales
- **Aprobaciones:** 80% — Flujo multi-rol, FICO/Dirección, rechazos
- **Operaciones UI:** 60% — Proyectos, Acciones, Conceptos, pendiente detalle completo
- **Power BI Integration:** 0% — Vistas SQL OK, embeddings pendientes
- **Datos reales:** 5% — Seed data ficticia solamente, cero datos en vivo

### ❌ NO INICIADO / CRÍTICO BLOQUEADO
- **Integraciones** A3 Nómina, A3 ERP (requieren OAuth2 Conectia)
- **Integraciones** Galán, Mediapost (sin credenciales)
- **Integraciones** PayHawk, Intratime (credenciales expiradas/inválidas)
- **Tests E2E en vivo** — Sin data real, sin Celero conectada a prod
- **Documentación API** — Swagger presente pero no validado
- **Deployment Azure** — Configuración pendiente

---

## 2. ANÁLISIS DETALLADO POR MÓDULO

### **MÓDULO: DASHBOARD**
| Componente | Estado | Notas |
|-----------|--------|-------|
| Layout shell | ✅ 100% | Sidebar, navbar, tema claro/oscuro OK |
| KPIs período | ⚠️ 70% | Selector de período OK, cálculos básicos OK, falta desglose por cliente |
| Panel "Mis Proyectos" | ⚠️ 60% | Listado OK, coste/facturación pendiente |
| Resumen global | ⚠️ 50% | Coste total OK, márgenes aún no calculados |
| Panel de alertas | ⚠️ 40% | Estructura OK, lógica de alertas pendiente |
| Botón recalcular | ✅ 100% | Funcional, dispara motor de cálculo |

**Urgencia:** MEDIA — Necesita datos reales para validar visualización

---

### **MÓDULO: CLIENTES & PROYECTOS & ACCIONES**
| Componente | Estado | Notas |
|-----------|--------|-------|
| CRUD Clientes | ✅ 95% | API completa, UI listado + form, falta cascadas de borrado |
| CRUD Proyectos | ✅ 90% | API completa, asignación usuarios OK, CECOs multi-select OK |
| CRUD Acciones | ✅ 85% | API completa, relación acción→concepto OK, falta sublistado conceptos inline |
| Sincronización Celero | ⚠️ 60% | Conexión PostgreSQL directa OK, mapeo de IDs pendiente |
| Filtros & búsqueda | ✅ 90% | Implementados, falta full-text search |

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

#### **Intratime** ❌ BLOQUEADO
- **Problema:** Token inválido o expirado
- **Requerimiento:** Contactar soporte Intratime para credenciales válidas
- **Stub en código:** `IntratimeClient.cs` presente pero sin implementación real

#### **PayHawk** ❌ BLOQUEADO
- **Problema:** Falta Account ID del portal PayHawk
- **Requerimiento:** Cliente debe proporcionar Account ID y API Key
- **Stub:** `PayHawkClient.cs` presente pero vacío

#### **TravelPerk** ❌ BLOQUEADO
- **Problema:** Falta API Key
- **Requerimiento:** Cliente debe obtener de portal TravelPerk
- **Stub:** `TravelPerkClient.cs` presente

#### **SGPV** ⚠️ PENDIENTE ESPECIFICACIÓN
- **Problema:** Necesita aclaración del formato JSON de exportación
- **Estado:** Tabla staging OK (`StagingSgpvProducto`), sync buttons OK
- **Bloqueador:** Cliente debe proveer esquema exacto de datos esperados

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
2. **PayHawk** — Account ID y API Key del portal
3. **TravelPerk** — API Key del portal
4. **A3 Nómina (Conectia)** — Client ID / Client Secret OAuth2 ⚠️ **CRÍTICO**
5. **A3 ERP (Conectia)** — Client ID / Client Secret OAuth2 ⚠️ **CRÍTICO**
6. **Galán** — Documentación de API o acceso SFTP
7. **Mediapost** — Documentación y credenciales de acceso
8. **SGPV** — Especificación exacta del formato JSON de exportación esperado

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

- [ ] Llamadas reales a APIs bloqueadas (A3, PayHawk, Intratime, TravelPerk)
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
| **3** (10-14 jun) | Integración real PayHawk + Intratime; Validación flujo aprobación con FICO; Tests E2E aprobaciones | Creds PayHawk/Intratime |
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
  - [ ] PayHawk — Account ID + API Key
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

- [ ] Esperar credentials; integrar APIs reales
- [ ] Validar E2E aprobaciones con datos ficticios
- [ ] Preparar Azure deployment
- [ ] Revisar datos seed para que sean realistas

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
