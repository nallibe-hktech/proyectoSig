# SIG-ES: ANÁLISIS COMPLETO — Estado Actual vs Especificación

**Fecha**: 2026-06-15  
**Generado desde**: COMPLETE_SPECIFICATION.md + engram memory + git history  
**Estado del Proyecto**: Backend 85%, Frontend 75%

---

## 1. RESUMEN EJECUTIVO

**Posición**: SIG-ES está en semana 2 de ~5 semanas. **Motor de cálculo funcional**, **cierre de periodos operativo**, pero **datos del cliente no disponibles** bloquean 35% del trabajo restante.

| Aspecto | Estado | Bloqueo |
|---------|--------|--------|
| **Entidades de datos** | ✅ 95% | No |
| **Autenticación JWT** | ✅ 100% | No |
| **Roles y permisos** | ✅ 80% | Parcial |
| **Motor de cálculo** | ✅ 90% | No |
| **Cierre y aprobaciones** | ✅ 85% | No |
| **Integraciones externas** | ⚠️ 60% | SÍ - Falta credenciales/datos de cliente |
| **Export A3 Innuva** | ✅ 85% | No |
| **Alertas y validaciones** | ⚠️ 70% | Parcial |
| **Frontend UI** | ⚠️ 75% | No |
| **Tests (unit + E2E)** | ✅ 75% | No |

---

## 2. ESTADO DETALLADO POR COMPONENTE

### 2.1 ENTIDADES DE DATOS ✅ 95% IMPLEMENTADAS

**Implementadas en Domain/Entities**:
- ✅ User (USUARIO)
- ✅ Role (ROL)
- ✅ Client (CLIENTE)
- ✅ Project / Service (PROYECTO — renamed from Proyecto to Servicio)
- ✅ Action (ACCIÓN)
- ✅ Concept (CONCEPTO)
- ✅ Period (PERIODO)
- ✅ Closure (CIERRE)
- ✅ ClosureLine (LINEA DE CIERRE)
- ✅ Approval (APROBACIÓN CIERRE)
- ✅ ApprovalHistory (HISTÓRICO DE APROBACIONES)
- ✅ AuditLog (AUDITORÍA)
- ✅ CalculationLog (LOG DE CÁLCULOS)
- ✅ Resource / Empleado (RECURSO)
- ✅ Contract (CONTRATO)
- ✅ Visit (VISITA)
- ✅ CostCenter / Ceco (CECO)
- ✅ Department (DEPARTAMENTO)
- ✅ Expense (GASTO)
- ✅ Travel (VIAJE)

**Staging tables** (para integraciones):
- ✅ StagingCelero
- ✅ StagingBizneo
- ✅ StagingIntratime
- ✅ StagingPayhawk
- ✅ StagingInnuva

**Falta**:
- `Center` (CENTRO) — En especificación pero NO en código. **REQUIERE IMPLEMENTACIÓN**.

---

### 2.2 AUTENTICACIÓN Y ROLES ✅ 80% IMPLEMENTADO

**Implementado**:
- ✅ JWT Bearer Token (BCrypt + System.IdentityModel.Tokens.Jwt)
- ✅ Custom claims mapping (`JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()`)
- ✅ Login endpoint (POST `/api/auth/login`)
- ✅ Logout endpoint (POST `/api/auth/logout`)
- ✅ Refresh endpoint (POST `/api/auth/refresh`)
- ✅ RefreshToken entity with rotation

**Roles implementados**:
- ✅ Administrador
- ✅ Dirección
- ✅ FICO
- ✅ RRHH
- ✅ Facilitador
- ✅ Interlocutor
- ✅ Gestor
- ✅ Backoffice
- ✅ Auxiliar

**Permission matrix**:
- ⚠️ **Parcial** — Roles existen en BD, pero **validación en controllers/services NO COMPLETA**. 
  - Falta enforcement de permisos a nivel servicio (ej: "RRHH no puede ver Facturas")
  - Falta validación de scope (Usuario global vs. proyecto-específico)
  - El DI pattern está en lugar pero no todas las validaciones están implementadas

**Falta**:
- Validación de "ownership" por proyecto en servicios de lectura/edición
- Filtrado de vistas por rol global vs. proyecto-específico

---

### 2.3 MOTOR DE CÁLCULO ✅ 90% IMPLEMENTADO

**Conceptos soportados** (de las 10 especificadas):

| Tipo | Descripción | Estado | Notas |
|------|-------------|--------|-------|
| 1 | Cantidad fija mensual | ✅ | Simple |
| 2 | Conteo visitas × cantidad fija | ✅ | Con Celero mapping |
| 3 | Conteo días con actividad | ✅ | Con Celero mapping |
| 4 | Kilómetros × coste/km | ✅ | Con Payhawk, WARNING si >€0.25/km |
| 5 | Conteo Entity-A × Entity-B | ✅ | Genérico |
| 6 | Suma Entity-A × Entity-B | ✅ | Genérico |
| 7 | Porcentaje de Entidad | ✅ | Fee support |
| 8 | Porcentaje fijo de cantidad variable | ✅ | Fee support |
| 9 | Horas × cantidad incremental | ⚠️ | **Parcial** — Soporta Bizneo pero escalado incremental NO TESTADO |
| 10 | Tarifa/hora × horas estimadas | ✅ | Con Dyson, DJI, Kobo, etc. |

**Implementación**:
- ✅ `CalculationEngine.cs` — Motor completo en Application/Calculation
- ✅ Concepto AST evaluation
- ✅ Staging data in-memory loading
- ✅ Multi-period aggregation
- ✅ Formula snapshots en CalculationLog (trazabilidad)

**Falta**:
- **Escalado incremental (Tipo 9) NO TESTADO** en entorno productivo
- **Data examples para validación** — No tenemos datos reales de cliente para validar que Tipo 1-8 funcionen con TODOS los proyectos (Molins, Granini, JDE, etc.)

---

### 2.4 CIERRE DE PERIODOS Y APROBACIONES ✅ 85% IMPLEMENTADO

**Workflow de aprobación** (5 pasos especificados):
1. Gestor → FICO → Dirección (workflow principal)
   - ✅ Implementado en `ApprovalService`
   - ✅ ApprovalHistory registra cada transición
   - ⚠️ **Validación de permisos por rol** — Parcial (ver sección 2.2)

**Cierre de periodo**:
- ✅ Period.Abierto → Cerrado → Bloqueado
- ✅ Cierre crea Closure + ClosureLines
- ✅ Recalculation endpoint (`POST /api/closures/{id}/recalcular`)
- ✅ Soft delete + Global query filter

**Falta**:
- Confirmación explícita de usuario en interface para "permitir cierre a pesar de advertencias"
- Audit trail completo de cambios en ClosureLine (editadas por FICO)

---

### 2.5 ALERTAS Y VALIDACIONES ⚠️ 70% IMPLEMENTADO

**Bloqueantes (5 especificadas)**:

| Error | Implementado | Estado |
|-------|--------------|--------|
| Contratos solapados en Innuva | ⚠️ | Lógica existe pero **NO VALIDADA con datos reales** |
| NIF mismatch | ✅ | Validación de reconciliación |
| Missing key fields | ✅ | Validación por entity |
| Activity sin contrato | ⚠️ | Parcial — Checks para Celero, falta para otros |
| Cost center no en master | ✅ | CECO validation en entidades |

**Advertencias (4 especificadas)**:

| Advertencia | Implementado | Estado |
|-------------|--------------|--------|
| Contrato sin actividad | ⚠️ | Parcial |
| Alto coste de km (>€0.25/km) | ✅ | Implementado en Payhawk processor |
| Gastos negativos | ✅ | Detecta e-flags |
| Pagos insuficientes | ⚠️ | Lógica falta detalle |

**Special rule — One-day contracts**:
- ⚠️ **NO IMPLEMENTADO** — Sistema no distingue entre:
  - Separaciones (un día, sin pago real) → Ignorar
  - Contratos legítimos (un día, con actividad real) → Incluir
  - **CRÍTICO PARA INNUVA RECONCILIATION**

---

### 2.6 INTEGRACIONES EXTERNAS ⚠️ 60% IMPLEMENTADO

**Status por sistema**:

| Sistema | Protocolo | Lectura | Escritura | Datos Recibidos | Bloqueo |
|---------|-----------|---------|-----------|-----------------|--------|
| **Celero** | PostgreSQL directo | ✅ | ✅ | ✅ 260+ visitas | No |
| **Bizneo** | OAuth2 HTTP | ✅ | ❌ | ⚠️ Sin datos reales | SÍ — Credenciales falta |
| **Intratime** | Basic Auth HTTP | ✅ | ❌ | ⚠️ Sin datos reales | SÍ — Credenciales falta |
| **Payhawk** | OAuth2 HTTP | ✅ | ❌ | ⚠️ Sin datos reales | SÍ — Credenciales falta |
| **A3 Innuva** | SFTP / HTTP | ⚠️ | ❌ | ❌ No configurado | SÍ — Credenciales falta |
| **TravelPerk** | OAuth2 HTTP | ✅ | ❌ | ❌ No configurado | SÍ — Credenciales falta |

**Implementado**:
- ✅ Named HttpClient pattern con retry policies
- ✅ Staging table persistence pattern
- ✅ DataProcessorService (staging → productive)
- ✅ Celero full integration (PostgreSQL read + mapping)
- ✅ Payhawk structure + expense parsing
- ✅ HTTP auth (OAuth2, Basic)

**Falta**:
- **Credenciales de cliente para Bizneo, Intratime, Payhawk, A3 Innuva, TravelPerk**
- Datos de prueba para validar integration tests
- Reconciliación automática de NIF entre sistemas
- Detección de inconsistencias entre fuentes

---

### 2.7 EXPORT A3 INNUVA ✅ 85% IMPLEMENTADO

**Formato A3 NOM** (29 columnas especificadas):
- ✅ Structure: Headers (rows 1-5) + Data (rows 8+)
- ✅ Columns A-H: Pre-row metadata
- ✅ Columns I-Y: Payment breakdown

**Implementado**:
- ✅ `ExcelExporter.cs` → ClosedXML (.xls + .xlsx)
- ✅ Mapping de conceptos a columnas I-Y
- ✅ Period-specific export
- ✅ Multi-concept aggregation

**Falta**:
- **Validación con archivo ejemplo real** — No tenemos datos de prueba del cliente
- Reconciliación de datos en Innuva post-export
- Deduction application (taxes, social security) — **A3 system lo hace, no nuestra app**
- Invoice export format (diferente de payroll)

---

### 2.8 FRONTEND ⚠️ 75% IMPLEMENTADO

**Componentes completados**:
- ✅ Dashboard (KPIs, period overview)
- ✅ Login/Logout
- ✅ Navigation (navbar con lazy-load)
- ✅ Clients CRUD
- ✅ Projects CRUD
- ✅ Actions CRUD
- ✅ Concepts CRUD (con editor de fórmulas)
- ✅ Users CRUD
- ✅ Periods CRUD
- ✅ Closures (create, list, detail)
- ✅ Approvals (panel + workflow)
- ✅ Audit log viewer
- ✅ Celero visitas mapping
- ✅ Integraciones (Bizneo, Intratime, PayHawk, Galán, Mediapost dashboards)

**Status Angular**:
- ✅ Angular 21 + TypeScript strict mode
- ✅ RxJS 7.8 con OnDestroy cleanup
- ✅ Angular Material UI
- ✅ Lazy-loaded feature modules
- ✅ HTTP interceptors (JWT)

**Falta**:
- **Tests E2E con Playwright** — Framework existe pero NO CORRIENDO
- Real data from client integrations para validar vistas
- Export button wiring para A3 files
- Dark mode / accessibility (no especificado pero buena práctica)

---

### 2.9 TESTING ✅ 75% IMPLEMENTADO

**Backend**:
- ✅ xUnit 2.9.3 framework setup
- ✅ WebApplicationFactory para integration tests
- ✅ NSubstitute para mocking
- ✅ FluentAssertions
- ✅ Unit tests para servicios principales
- ⚠️ Integration tests: **Parcial** — No tenemos BD de prueba con datos reales

**Frontend**:
- ✅ Jasmine 6.2 + Karma 6.4 setup
- ⚠️ Unit tests: Mínimos (skipTests: true en schematicsAngular)
- ⚠️ E2E Playwright: **Instalado pero NO CORRIENDO**

**Falta**:
- Test coverage reports (SonarQube analysis)
- CI/CD pipeline validation
- Load/stress testing para motor de cálculo (RNF-01: <30s para 1000 líneas)

---

## 3. DATOS QUE FALTAN DEL CLIENTE

### 3.1 BLOQUEANTES CRÍTICOS

#### 🔴 Credenciales de acceso a sistemas externos

| Sistema | Requerido para | Status |
|---------|----------------|--------|
| **Bizneo** | Integration tests, employee data sync | ❌ No recibidas |
| **Intratime** | Time tracking sync, hour-based calculations | ❌ No recibidas |
| **Payhawk** | Expense reconciliation, km cost validation | ❌ No recibidas |
| **A3 Innuva** | Payroll export, contract reconciliation | ❌ No recibidas |
| **TravelPerk** | Travel booking reconciliation | ❌ No recibidas |

**Impacto**: 
- No podemos validar integraciones
- No tenemos datos reales para calcular
- Integration tests están "mocked" (riesgoso para producción)

---

#### 🔴 Datos de ejemplo por tipo de cálculo

**Especificación tiene 10 tipos de cálculo. Necesitamos para CADA tipo**:
- ✅ Tipo 1: Cantidad fija mensual — NO TENEMOS EJEMPLO
- ✅ Tipo 2: Conteo visitas — TENEMOS (Celero, ~260 visitas)
- ❌ Tipo 3: Conteo días — NO TESTADO
- ✅ Tipo 4: Kilómetros — NO TENEMOS EJEMPLO PAYHAWK
- ❌ Tipo 5-8: Combinaciones — NO TESTADAS CON DATOS REALES
- ❌ Tipo 9: Escalado incremental — **NO TESTADO CON BIZNEO**
- ⚠️ Tipo 10: Tarifa/hora estimada — SOLO DYSON

**Proyectos listados en especificación sin datos de prueba**:
Molins, Granini, JDE, Morrison, Apple RST, Kobo, Coty, ITC, Inpost, etc.

---

#### 🔴 Definición de "One-day contracts" para cada proyecto

**Problema**: Contratos de 1 día en Innuva pueden ser:
1. Separaciones (sin pago real) → IGNORAR
2. Trabajo legítimo (con actividad) → INCLUIR

**Sin datos del cliente**, no podemos distinguir automáticamente.

**Necesario**:
- Ejemplos de ambos tipos
- Regla de negocio por proyecto (algunos permiten, otros no)
- Criterio de validación (ej: "si hay visitas en Celero, incluir")

---

#### 🔴 Mapping de Cost Centers (CECOS) a master table

**Especificación dice**: "Must match to master table; mismatches are BLOCKING errors"

**Sin datos del cliente**:
- No tenemos lista de CECOs válidos
- No podemos validar datos que llegan de Celero, Payhawk, A3 Innuva
- No podemos poblar la tabla de referencia

**Necesario**:
- Excel/CSV con todos los CECOs válidos
- Cuál es el CECO por defecto si falta
- Si CECO se puede auto-crear o debe existir

---

### 3.2 BLOQUEANTES SECUNDARIOS

#### ⚠️ Mapeado de roles por proyecto

**Especificación**: "Project-scoped users" solo ven proyectos asignados, "Global users" ven todo.

**Sin datos del cliente**:
- No sabemos quién es global vs. proyecto-específico
- No tenemos usuarios de prueba
- No podemos testear permisos de lectura por scope

---

#### ⚠️ Definición de "Approval workflow" por proyecto/cliente

**Especificación dice**: Workflow de 5 pasos (Gestor → FICO → Director)

**Pero qué pasa si**:
- Cliente X solo tiene 2 pasos (Gestor → FICO)
- Cliente Y tiene custom step (Compliance review)
- Algunos proyectos requieren aprobación adicional

**Sin datos del cliente**: No podemos configurar variaciones.

---

#### ⚠️ Fórmulas exactas para conceptos de incentivos

**Especificación lista**:
- Incentivos mensuales (Granini, JDE, Apple BA, ITC)
- Incentivos trimestrales (Granini, Daikin)

**Pero NO especifica**:
- ¿Cómo se calculan? (% de ventas, amount fijo, etc.)
- ¿Cuáles son las condiciones? (mínimo de visitas, etc.)
- ¿Cómo se aplican en el modelo AST?

---

#### ⚠️ "Trasvaso entre CECOs" (Transfer rule)

**Hallado en image**: "Traspaso entre Cecos.jpg" muestra un diagrama

**Pero NO especifica**:
- ¿Es requerido para MVP?
- ¿Cuál es la lógica exacta?
- ¿Cuándo se aplica? (al cierre, al cálculo, etc.)
- ¿Quién lo autoriza?

---

## 4. QUÉ LE FALTA A LA HERRAMIENTA

### 4.1 FUNCIONALIDAD CRÍTICA PARA MVP

#### ❌ Center entity (CENTRO)

**Especificación lo requiere**: "Establishment where visit is performed"

**Impacto**: Sin Centers, no podemos mapear Celero visitas correctamente.

**Implementación necesaria**:
- Entity: `Center` en Domain
- Controller: GET/POST `/api/centers`
- Mapping en Celero integration
- UI CRUD en frontend

**Esfuerzo**: ~2-3 horas

---

#### ❌ Reconciliación automática de NIF

**Especificación dice**: "PRIMARY KEY para reconciliación es NIF"

**Implementado**: ⚠️ Parcial — Existe validación pero no reconciliación inteligente

**Necesario**:
- Service que: Busque empleado por NIF en todas las fuentes
- Reporte inconsistencias (mismo NIF, distinto nombre, etc.)
- Sugiera correcciones

**Esfuerzo**: ~4-5 horas

---

#### ❌ Filtrado por "ownership" (OwnerId)

**Especificación**: "RF-G01: Filtrado por ownership"

**Estado**: Implementado a nivel entity, pero **NO VALIDADO en servicios**

**Necesario**:
- Validar que usuario no pueda ver/editar proyectos no asignados
- Exception si usuario intenta acceder a cierre de otro usuario
- Tests para cada endpoint

**Esfuerzo**: ~3-4 horas

---

#### ⚠️ Enforcement de permisos por rol

**Especificación**: Role matrix con 9 roles y permisos específicos

**Estado**: Roles existen, pero validación NO COMPLETA

**Falta**:
- RRHH NO puede ver Facturas (pero puede ver Pagos)
- Backoffice solo ve (no valida ni edita)
- Auxiliar solo ve (nunca edita)
- Validation attributes o authorization filters en controllers

**Esfuerzo**: ~5-6 horas

---

### 4.2 INTEGRACIONES PARCIALMENTE COMPLETAS

#### ⚠️ Bizneo integration — Solo lectura

**Especificación**: Sync de empleados + horas

**Implementado**: 
- ✅ HttpClient pattern
- ✅ Staging table
- ⚠️ SIN DATOS REALES

**Necesario**:
- Credenciales del cliente
- Test data para validar hour calculations
- Integration test con BD de prueba

**Esfuerzo**: ~6-8 horas (bloqueado por cliente)

---

#### ⚠️ Intratime integration — Solo lectura

**Especificación**: Sync de clock in/out

**Implementado**:
- ✅ HttpClient pattern
- ✅ Staging table
- ⚠️ SIN DATOS REALES

**Necesario**:
- Credenciales
- Schema mapping para rows in Intratime vs. Closure calculation
- Validation logic para detectar clock in/out without matching visit

**Esfuerzo**: ~6-8 horas (bloqueado por cliente)

---

#### ⚠️ Payhawk integration — Solo lectura

**Especificación**: Expenses, meals, mileage, travel

**Implementado**:
- ✅ HttpClient pattern
- ✅ Staging table
- ⚠️ SIN DATOS REALES

**Falta**:
- Parsing de expense categories mapeadas a Conceptos
- Validation de km cost (WARNING si >€0.25/km) — IMPLEMENTADO pero NO TESTADO
- Travel booking integration (separado de expenses)

**Esfuerzo**: ~6-8 horas (bloqueado por cliente)

---

#### ⚠️ A3 Innuva integration — Ninguna

**Especificación**: SFTP upload de export, SFTP download de contratos

**Implementado**: ❌ NADA

**Falta**:
- SFTP client configuration
- Innuva file parser (contracts → staging table)
- Innuva export builder (closure → A3NOM.xls)
- NIF reconciliation logic

**Esfuerzo**: ~10-12 horas

---

### 4.3 VALIDACIONES NO IMPLEMENTADAS

#### ⚠️ One-day contract filter logic

**Especificación lo menciona** pero NO IMPLEMENTADO.

**Necesario**:
```csharp
// Service to distinguish between:
if (contract.Days == 1) {
    // Is there activity in Celero for this employee in this period?
    if (HasVisits(employeeId, period)) {
        // Include in payment
    } else {
        // Likely separation — ignore
    }
}
```

**Esfuerzo**: ~2-3 horas

---

#### ⚠️ Cost center master table validation

**Especificación**: "CECO mismatches are BLOCKING errors"

**Implementado**: ⚠️ Entity validation existe, pero NO master table check

**Necesario**:
- Table: `CostCenterMaster` (CECO, description, valid_from, valid_to)
- Validator que rechace CECOs no en master
- Configuration page para admins para actualizar master

**Esfuerzo**: ~3-4 horas

---

#### ❌ Contract overlap detection

**Especificación**: "Two contracts for same resource cannot overlap"

**Implementado**: ⚠️ Lógica existe pero SIN DATOS REALES para testear

**Necesario**:
- Unit tests con datos de solapamiento
- Integration test con BD de prueba

**Esfuerzo**: ~2-3 horas (una vez tengas datos Innuva)

---

### 4.4 FUNCIONALIDAD "NICE TO HAVE" (NO MVP)

- [ ] Dark mode
- [ ] Accessibility (WCAG AA)
- [ ] Multi-language support (actualmente Spanish-only, OK)
- [ ] Power BI / BI views (mencionadas como RF-F03 pero "Power BI directo")
- [ ] Mobile app
- [ ] Offline mode

---

## 5. DATOS DEL CLIENTE QUE YA TENEMOS

✅ **Especificación funcional completa**: 79 RF (requisitos funcionales) definidos  
✅ **Especificación técnica completa**: 7 RNF (requisitos no-funcionales)  
✅ **Entidades y workflows**: 20+ entidades mapeadas  
✅ **Cálculo ejemplos**: Tipo 1-10 definidos  
✅ **Stack decidido**: .NET 10, Angular 21, PostgreSQL  

⚠️ **Celero data**: ~260 visitas reales (read-only, sin edición)  
⚠️ **Estructuras de integración**: Staging tables para Bizneo, Intratime, Payhawk (pero sin datos)  

❌ **NADA de datos reales de cliente** para validar:
- Cálculos de salario
- Trasvaso entre CECOs
- Fórmulas de incentivos
- Valores de CECO master
- Contratos de prueba
- Ejemplos de "one-day contracts"
- Credenciales para APIs externas

---

## 6. PLAN RECOMENDADO PARA COMPLETAR MVP

### Semana 3 (Actual)
**Prioridad ALTA** (no bloqueadas por cliente):

- [ ] **Center entity** — 2-3 horas
  - Entity, migrations, controller, UI

- [ ] **Ownership filtering** — 3-4 horas
  - Authorization on services
  - Tests

- [ ] **Role permission enforcement** — 5-6 horas
  - Authorization attributes
  - Tests por role

- [ ] **One-day contract filter** — 2-3 horas
  - Service logic
  - Unit tests

- [ ] **Cost center master validation** — 3-4 horas
  - Table + validator
  - Admin UI

**Subtotal**: ~15-20 horas (~2 días full-time)

---

### Semana 4 (Bloqueada por cliente)

**Esperar credenciales + datos de cliente para**:
- [ ] Bizneo full integration (6-8 horas)
- [ ] Intratime full integration (6-8 horas)
- [ ] Payhawk full integration (6-8 horas)
- [ ] A3 Innuva integration (10-12 horas)

**En paralelo** (sin bloqueos):
- [ ] Frontend E2E tests (Playwright) — 8-10 horas
- [ ] Backend integration tests con test data — 6-8 horas
- [ ] Excel export validation con ejemplos reales — 4-6 horas

**Subtotal**: ~52-60 horas (si cliente entrega datos)

---

### Semana 5 (Validación)

- [ ] End-to-end testing con datos reales
- [ ] Cierre de período pilot
- [ ] User acceptance testing
- [ ] Fixes y adjustments
- [ ] Go-live

---

## 7. CHECKLIST PARA DESBLOQUEAR PROYECTO

### DE CLIENTE NECESITAMOS:

**URGENTE** (Bloquea todo desarrollo de integración):
- [ ] Credenciales Bizneo (OAuth2)
- [ ] Credenciales Intratime (Basic auth)
- [ ] Credenciales Payhawk (OAuth2)
- [ ] Credenciales A3 Innuva (SFTP)
- [ ] Credenciales TravelPerk (OAuth2)

**MUY IMPORTANTE** (Para validación de datos):
- [ ] Archivo ejemplo de descarga Innuva (contratos)
- [ ] Archivo ejemplo A3NOM.xls descargado (para validar mapping)
- [ ] Master de CECOs válidos (Excel o CSV)
- [ ] Ejemplos de "one-day contracts" — separaciones vs. trabajo legítimo

**IMPORTANTE** (Para testing):
- [ ] 10-20 periodos de datos históricos (Celero + API sources)
- [ ] Ejemplo de cierre completo de un período (de principio a fin)
- [ ] Proyectos de prueba mapeados entre sistemas

**NICE TO HAVE** (Mejora pero no bloquea):
- [ ] Diagrama detallado de "trasvaso entre CECOs"
- [ ] Fórmulas exactas de incentivos (mensuales y trimestrales)
- [ ] Casos de uso de roles (quién aprueba qué)

---

## 8. MATRIZ DE RIESGOS

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|------------|--------|-----------|
| **Falta de datos cliente** | ALTA | CRÍTICO | Contactar cliente HOY, agenda kickoff |
| **Diferencia de schemas** (APIs externas) | MEDIA | ALTO | Validar con ejemplos reales antes de integrar |
| **Fórmulas de incentivos mal interpretadas** | MEDIA | ALTO | Revisar con FICO/Dirección del cliente |
| **Overlap contratos no detectado** | BAJA | MEDIO | Unit tests una vez tenga datos Innuva |
| **Performance <30s para 1000 líneas** | BAJA | MEDIO | Load test en semana 4 con datos reales |

---

## 9. RESUMEN FINAL

**Estado**: SIG-ES tiene **arquitectura sólida** y **85% del código implementado**.

**Bloqueante**: **DATOS DEL CLIENTE** — Sin credenciales API, esquemas reales, y datos de prueba, **no podemos completar validaciones ni ir a producción**.

**Próximos pasos inmediatos**:
1. ✅ Implementar 5 features "quick wins" sin bloqueos (~15-20 horas)
2. 📧 Contactar cliente para obtener credenciales + datos (~emergencia)
3. 🧪 Preparar data fixtures para integración tests
4. 📋 Revisar fórmulas de cálculo con FICO del cliente

**Timeline realista**:
- Semana 3: MVP funcional 95%
- Semana 4-5: Integración completa + validación (si cliente responde)
- Semana 5 fin: Go-live ready OR roadmap para fase 2

---

**Generado**: 2026-06-15  
**Documento**: Estado Actual vs Especificación Completa  
**Status**: LISTO PARA ACCIÓN
