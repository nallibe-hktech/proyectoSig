# Descripción funcional del proyecto

Plataforma SIG — Sistema Integrado de Gestión Operativa y Financiera

Cliente: SIG ES (Service Innovation Group España), Mayo 2026.

SIG ES es una empresa de servicios en punto de venta (personal de campo, visitas, implantaciones, formaciones) que actualmente gestiona sus cierres mensuales de pagos al personal y facturación a clientes con un mosaico de Excel manuales. Esta plataforma centraliza esos procesos en una única aplicación web con motor de cálculo configurable, flujo de aprobación multi-rol, audit log completo y generación automática de ficheros de salida para nómina y ERP.

STACK (decidido por el cliente, vinculante para el Arquitecto):
- Backend: .NET con Clean Architecture (Domain/Application/Infrastructure/API) + Entity Framework Core
- Base de datos: PostgreSQL (Power BI se conectará vía conector nativo PostgreSQL)
- Frontend: Angular SPA con Material 3
- Autenticación: JWT propio con BCrypt (NO Azure AD en MVP)
- Mono-tenant (una sola organización: SIG ES)
- UI en español, código e identificadores en inglés

ALCANCE FUNCIONAL COMPLETO (lo que sigue es el brief literal del cliente — el Arquitecto debe procesarlo íntegro):

## 1. RESTRICCIONES DEL CLIENTE

- Backend .NET + EF Core, Clean Architecture
- PostgreSQL (Power BI conectado por conector nativo)
- Angular SPA Material 3
- JWT propio con BCrypt (Azure AD queda en Fase 2)
- Mono-tenant. Los "Client" del dominio son clientes de SIG (empresas a las que SIG presta servicios), NO tenants
- UI español, identificadores inglés

## 2. REQUISITOS FUNCIONALES (RF)

- RF-A01 Login email+password → access token + refresh token
- RF-A02 Logout invalida refresh tokens del usuario
- RF-A03 Refresh: intercambiar refresh válido por nuevo access
- RF-B01 Dashboard: KPIs del período activo (cierres completados, pendientes, facturación total, margen)
- RF-B02 Dashboard: panel de avisos automáticos (cierres pendientes, períodos bloqueados, errores de sync)
- RF-B03 Dashboard: tabla "Mis proyectos" filtrada por ownership del usuario autenticado
- RF-C01 CRUD Client con búsqueda paginada
- RF-C02 CRUD Project (relaciones N:M con CostCenter y User)
- RF-C03 CRUD Action (pertenece a Project y Client; N:M con Concept y User)
- RF-C04 CRUD Concept con tipo (Pago/Factura), validez temporal (Desde/Hasta), y fórmula de cálculo
- RF-C05 CRUD User con NIF único, multi-rol y asignaciones jerárquicas a Cliente/Proyecto/Acción
- RF-C06 CRUD Role, Department, CostCenter (admin)
- RF-C07 CRUD Period con cierre y reapertura (admin)
- RF-D01 Crear/recalcular Closure de Project+Period, generando ClosureLine vía motor de cálculo
- RF-D02 Panel aprobaciones con filtros (Período, Cliente, CECO, Estado, Recurso, Departamento, Tipo, Concepto)
- RF-D03 Vista "Pendientes": proyectos pendientes para el usuario según su rol
- RF-D04 Detalle de aprobación: KPIs (coste total, facturación, margen), tabla conceptos, comentarios
- RF-D05 Aprobar cierre: avanza al siguiente paso del flujo secuencial
- RF-D06 Rechazar con motivo: reinicia cadena al paso correspondiente y graba ApprovalHistory
- RF-D07 Detalle de cálculo de ClosureLine: snapshot fórmula, inputs, resultado, origen de datos
- RF-E01 Sincronización con sistemas externos (POST /api/sync/{system})
- RF-E02 Exportar ficheros A3 Innuva (nómina) sobre cierre aprobado
- RF-E03 Exportar ficheros A3 ERP (facturación) sobre cierre aprobado
- RF-F01 AuditLog completo: cambios de entidades, login/logout, exportaciones, recálculos
- RF-F02 Consulta AuditLog (solo Auditor/Administrador)
- RF-F03 Vistas SQL en schema "bi" para Power BI
- RF-G01 Filtrado por ownership en repositorio: un Gestor solo ve sus proyectos
- RF-G02 Soft delete con Global Query Filter en maestros

## 3. REQUISITOS NO FUNCIONALES (RNF)

- RNF-01 Recálculo de cierre con hasta 1000 líneas < 30 segundos en hardware desarrollo
- RNF-02 AuditLog se escribe en la MISMA transacción que la operación auditada
- RNF-03 Concurrencia optimista en Closure y ClosureLine vía rowVersion
- RNF-04 Trazabilidad: cada ClosureLine enlaza con CalculationLog con snapshot exacto de fórmula
- RNF-05 API stateless. UserId sale del JWT, NUNCA del body
- RNF-06 Errores devuelven ProblemDetails con código semántico
- RNF-07 Idempotencia en sincronizaciones: segunda carga con mismos datos no duplica

## 4. MODELO DE DATOS (fuente única de verdad)

### Maestros

- **User**: Id, NIF (único), nombre, apellidos, mail (único), passwordHash, estado
- **Role**: Id, nombre, descripción
- **UserRole**: N:M User-Role
- **Department**: Id, nombre
- **CostCenter**: Id, código (ej "035501"), nombre
- **Client**: Id (espejo clientId externo), nombre, NIF, dirección, ciudad, provincia, país, CP, datos contacto
- **Project**: Id, nombre, ClientId, estado, interlocutor (nombre,mail,tel), fechaAlta. N:M CostCenter, N:M User
- **Action**: Id (serviceId externo), nombre, ProjectId, ClientId, DepartmentId (nullable), estado. N:M Concept, N:M User
- **Concept**: Id, nombre, tipo (Pago/Factura), fechaDesde, fechaHasta, fórmula (JSON). N:M Action, N:M User
- **Variable**: Id, nombre, questionIdExterno (referencia Celero), mapeoValores (JSON respuesta→valor)
- **Period**: Id, nombre ("Marzo 2026"), fechaInicio, fechaFin, estado (Abierto/Cerrado/Bloqueado)

### Transaccionales

- **Closure**: Id, ProjectId, PeriodId, costeTotal, facturacionTotal, margen, estado, fechaCreacion, comentarios, rowVersion
- **ClosureLine**: Id, ClosureId, ConceptId, UserId (nullable), importe, datosEntrada (JSON), tipo (Pago/Factura), rowVersion

### Aprobación

- **Approval**: Id, ClosureId, RoleId, UserId, estado (Pendiente/Aprobado/Rechazado), motivo, fecha
- **ApprovalHistory**: INMUTABLE. Cada transición deja fila con quién, cuándo, motivo

### Auditoría

- **AuditLog**: Id, UserId, entityType, entityId, action (Create/Update/Delete/Login/Logout/Export/Recalc), oldValue (JSON), newValue (JSON), timestamp, ip
- **CalculationLog**: Id, ClosureLineId, ConceptId, fórmulaSnapshot (JSON), inputs (JSON), resultado, timestamp, sistemaOrigen

### Staging (una tabla por sistema externo: Celero, Bizneo, Intratime, PayHawk, TravelPerk)

- Cada una: payload raw, hash, fechaUltimaSincronizacion, flagProcesado, errorProcesamiento (nullable)

## 5. ROLES Y FLUJO DE APROBACIÓN

7 roles: Administrator, Direction, Fico, Backoffice, ProjectManager, Auditor, Reader.
Multi-rol por usuario (UserRole N:M).
Flujo secuencial: ProjectManager (cierra) → Backoffice (valida) → Fico (financiero) → Direction (firma) → Sistema (exports).
Rechazo: reabre cierre en paso anterior, graba ApprovalHistory, notifica responsable.

## 6. MOTOR DE CÁLCULO (CRÍTICO)

Evalúa fórmula configurable por Concept contra datos del período → importe de ClosureLine.

Primitivas: Números, Variables (mapean questionId Celero a valor numérico), Entidades origen (Gastos PayHawk, Visitas Celero, Horas Bizneo, Horas Intratime), Operaciones (+ − × ÷ Suma Cuenta % Min Max), Filtros (período, proyecto, usuario, estado).

Decisiones vinculantes:
- Evaluación en C# en el momento del cálculo. NO se traduce a SQL dinámico ni se evalúa en cliente.
- CalculationLog persiste COPIA COMPLETA de la fórmula en JSON (snapshot), no referencia al Concept actual. Reproducible aunque la fórmula del Concept se edite después.
- Editar Concept.formula NO recalcula cierres pasados. Solo cálculos futuros o recálculos explícitos.
- Datos faltantes: línea creada con importe=0 y flag incidencia (visible en panel Backoffice).

Tipos de lógica expresables: tarifa fija, unidades con tramos, horas×tarifa con rate card, mensualidad+variable, cost-plus, suma de gastos directos/viajes, anualidad+pago por sesión, conteo de eventos.

## 7. INTEGRACIONES EXTERNAS (simuladas con datos sintéticos en MVP)

DECISIÓN CLAVE: como no hay credenciales reales, el sistema funciona end-to-end con DATOS SINTÉTICOS COHERENTES (no stubs vacíos).

Sistemas MVP: Celero (CRM), Bizneo (RRHH), Intratime (fichajes), PayHawk (gastos).
Fase 2: TravelPerk, SGPV.

Implementación:
- Interfaz Application/Interfaces/Integrations/ I{Sistema}Client
- Productiva Infrastructure/Integrations/Http/{Sistema}Client.cs (HttpClient tipado, NO se invoca en MVP)
- MVP: Infrastructure/Integrations/Fake/{Sistema}FakeClient.cs con Bogus + semilla fija (20260101)
- Registro DI condicionado a IConfiguration["Integrations:UseFake"]=="true" (true en Development y Testing)
- Tablas staging rellenadas con datos coherentes con maestros
- POST /api/sync/{system} funciona: invoca IFakeClient, regenera staging, devuelve resumen
- Idempotencia: hash de payload, no duplica si coincide. Lógica en Application, común real/fake
- Tests integración mockean I{Sistema}Client vía DI en CustomWebApplicationFactory

Salidas MVP: A3 Innuva (XML/EDI nómina), A3 ERP (facturación). Formato exacto TODO; estructura documentada en docs/EXPORTS.md. Botón UI + endpoint + log en AuditLog operativos.

## 8. PANTALLAS

Navegación lateral con dos bloques:
- **Operativo**: Dashboard, Clients, Projects, Actions, Concepts, Periods, Approvals, Closures, Reports
- **Administración**: CostCenters, Departments, Roles, Users

Cada maestra: listado paginado + filtros + búsqueda + acciones / formulario crear-editar / detalle.

Pantallas específicas: Dashboard, Editor de fórmula del Concept (interfaz visual con cajas Número/Variable/Operación/Entidad/Filtro — responsabilidad del Designer), Editor de Variable ("Si respuesta a [questionId] es [texto] = [valor]"), Panel de aprobaciones, Detalle de aprobación por proyecto, Detalle de cálculo (auditoría con fórmula snapshot+inputs+resultado), Confirmación de eliminación (modal con conteo de dependientes).

## 9. ENDPOINTS (orientativos — el Arquitecto define verbos/DTOs/validaciones exactos)

- /api/auth/{register,login,refresh,logout}
- /api/clients, /api/projects, /api/actions, /api/concepts, /api/users, /api/roles, /api/departments, /api/costcenters, /api/periods
- /api/closures (crear/recalcular/listar), /api/closures/{id}/approve, /api/closures/{id}/reject
- /api/approvals (panel)
- /api/dashboard (KPIs período activo)
- /api/calculations/{closureLineId}
- /api/audit (solo Auditor/Administrator)
- /api/sync/{system} (solo Administrator)
- /api/exports/a3-innuva/{closureId}, /api/exports/a3-erp/{closureId} (solo cierres aprobados)

Todos excepto /auth/register y /auth/login requieren JWT. UserId y Roles salen del JWT.

## 10. PRIORIDADES

- **MVP**: todo lo de RF/RNF y secciones 2/3, integraciones MVP de sección 7 con datos sintéticos.
- **Fase 2 (EXCLUIDO)**: TravelPerk, SGPV, Azure AD/Entra ID, webhooks tiempo real, notificaciones email.

## 11. FUERA DE ALCANCE EXPLÍCITO

- Power BI embeddings (solo vistas SQL en schema bi)
- Azure AD/Entra ID en MVP
- App móvil/PWA
- Multi-idioma (solo español)
- Multi-tenant
- Editor visual del motor más allá de las primitivas listadas
- Formato definitivo A3 (TODO contractual)
- Notificaciones push/email
- Workflow editor visual (flujo de 5 pasos hardcoded)
- Versionado histórico de maestras más allá de AuditLog

## 12. SEED OBLIGATORIO, EXHAUSTIVO Y DEMOABLE

Arranque automático en primer arranque (Seed:AutoRun=true en Development) + endpoint POST /api/dev/regenerar-seed (solo Development/E2E, protegido por IHostEnvironment + feature flag — JAMÁS en Production).

### Volúmenes exactos

- 3 Clients: Alpha Foods (alimentación), Beta Cosmetics (cosmética), Gamma Retail (gran distribución)
- 8 Projects (3 Alpha, 3 Beta, 2 Gamma)
- 20-25 Actions (2-4 por Project)
- 4 CostCenters: "025888 - Operaciones campo", "035501 - GPV España", "035502 - GPV Portugal", "041200 - Formación"
- 4 Departments: Operaciones, Backoffice, Finanzas, Dirección
- 7 Roles (los listados)
- 15 Users (detalle abajo)
- 8 Concepts (detalle abajo)
- 3 Variables: PuntoMontado (Q12 Sí=1 No=0), TipoVisita (Estándar=1 Premium=2), ZonaBonus (A=1.5 B=1.2 C=1.0)
- 5 Periods: Nov 2025, Dic 2025, Ene 2026, Feb 2026, Mar 2026. Marzo abierto, Febrero pdte. aprobación final, anteriores cerrados.
- ~50 Closures (1 por Project×Period aplicable), estados distribuidos
- ~600 ClosureLines (8-18 por Closure)
- Approval + ApprovalHistory: cada Closure con historial completo
- AuditLog: mínimo 100 entradas (creaciones, ediciones, logins, exportaciones)
- CalculationLog: 1 por ClosureLine

### Usuarios (password en Development: Demo#2026!)

- admin@sig.local — Admin SIG — Administrator
- direccion@sig.local — Carmen Ruiz — Direction — Dirección
- fico@sig.local — Javier López — Fico — Finanzas
- backoffice1@sig.local — Laura Sánchez — Backoffice
- backoffice2@sig.local — Pedro Martín — Backoffice
- pm.alpha@sig.local — María García — ProjectManager — Operaciones (los 3 de Alpha)
- pm.beta@sig.local — David Pérez — ProjectManager (los 3 de Beta)
- pm.gamma@sig.local — Sara Gómez — ProjectManager (los 2 de Gamma)
- pm.multi@sig.local — Alex Torres — ProjectManager (mixtos, ownership cruzado)
- auditor@sig.local — Inés Romero — Auditor — Finanzas
- reader@sig.local — Luis Vega — Reader — Operaciones
- gpv1..gpv4@sig.local — 4 empleados de campo (sin roles aprobación, son "recursos" en ClosureLine.UserId)

### Conceptos seed

- Suma de gastos directos (Pago) — Suma(GastosPayHawk.importe filtro Periodo+Proyecto) — Todos
- Bonus por visita estándar (Pago) — Cuenta(VisitasCelero TipoVisita=1)×5 — Proyectos de visitas
- Bonus por visita premium (Pago) — Cuenta(VisitasCelero TipoVisita=2)×8 — Proyectos visitas premium
- Pago por horas trabajadas (Pago) — Suma(HorasBizneo.horas)×Variable.TarifaHora — Formación
- Pago por implantación completada (Pago) — Cuenta(Visitas PuntoMontado=1)×250 — Implantación
- Facturación por visita (Factura) — Cuenta(VisitasCelero)×18 — Todos visitas
- Mensualidad fija proyecto (Factura) — 1500 — Proyectos mensuales
- Refacturación gastos (Factura) — Suma(GastosPayHawk.importe)×1.15 — Todos

### Distribución de cierres (clave demo)

- 20 cerrados y aprobados (Nov-Dic-Ene)
- 8 Approved (Direction) listos para exportar
- 6 Pendiente Fico (bandeja fico@sig.local)
- 8 Pendiente Backoffice (bandejas backoffice1/2)
- 5 Pendiente ProjectManager (Marzo abierto)
- 3 Rechazados con motivo en distintos pasos

### Staging seed

- StagingCeleroVisita: ~200 visitas con respuestas (PuntoMontado, TipoVisita)
- StagingBizneoEmpleado: 15 espejo de Users con departamento y NIF
- StagingBizneoHora: ~400 registros
- StagingIntratimeFichaje: ~600 fichajes coherentes
- StagingPayHawkGasto: ~150 gastos

Todos con fechaUltimaSincronizacion reciente, flagProcesado=true, hash calculado.

Determinismo: `Randomizer.Seed = new Random(20260101)`.

## 13. IDENTIDAD VISUAL Y UX

Branding: "SIG · Plataforma de Cierres" (title, AppBar, login).

### Paleta azul marino corporativa

- Primary #1A2B4A
- Primary container #D6DFF3
- Secondary #3B82B6 (CTAs secundarios y enlaces)
- Tertiary #C9A961 (acentos KPIs, badges margen positivo)
- Error #BA1A1A (M3 estándar)
- Success #1B6E3F (semántico propio)
- Warning #A66E0D
- Surface/background: tonos cálidos neutros estándar M3

### Tipografía

- Inter (UI), Roboto Mono (campos numéricos en tablas).

### Logo

Wordmark SVG "SIG" blanco sobre #1A2B4A para AppBar + versión negativa para fondos claros. Sencillo pero intencional.

### Layout

- **Login**: página dedicada con logo, paleta corporativa, email+password con validación inline, link "¿Has olvidado tu contraseña?" (placeholder), footer "© 2026 SIG ES". Tarjeta lateral con credenciales demo (solo Development).
- **Autenticado**: AppBar con logo + usuario + avatar (Perfil/Cerrar sesión); navegación lateral colapsable agrupada en Operativo/Administración.
- Breadcrumbs en cada página.
- Selector de período persistente en AppBar (filtra el resto).

### Componentes

- Tablas: paginación servidor (10/25/50), ordenación columna, filtros header, búsqueda libre, exportación CSV, sticky header.
- Formularios: validación inline, asterisco obligatorios, mensajes específicos ("el NIF debe tener 9 caracteres"), Guardar/Cancelar alineados a la derecha.
- Modales confirmación destructivas con conteo de dependientes.
- Snackbars (no alert()).
- Estados vacíos: ilustración SVG (Material Symbols Outlined grande, opacidad reducida) + texto + CTA.
- Estados de carga: skeletons (no spinners genéricos).
- Dashboard: tarjetas KPI con número grande, etiqueta, tendencia (▲ +12% verde / ▼ -3% rojo), gráfico (ngx-charts o chart.js).

### Credenciales demo (banner Development)

Tarjeta lateral expandible con la lista de cuentas + botón "Usar este" que rellena campos. Controlada por Features:ShowDemoCredentials (true solo Development).

### Accesibilidad

- Contraste WCAG AA mínimo
- Navegación completa por teclado
- aria-label en botones-icono
- data-testid en cada elemento interactivo (requerido para Playwright E2E)
