# CONTEXT SIG-ES — Plataforma Operativa Integral
> Documento de contexto para el agente Arquitecto. Utilizar como base para generar `ARQUITECTURA.md`.
> Fecha: Mayo 2026 | Estado: Fase 0 → Inicio Desarrollo

---

## 1. VISIÓN Y ALCANCE DEL PROYECTO

**Nombre:** Plataforma Operativa SIG ES y BBDD Unificada
**Cliente final:** SIG Europe
**Partner/Sponsor:** h&k consulting
**Inicio real del proyecto:** 8 de abril de 2026
**Objetivo:**
Centralizar y automatizar los procesos operativos y financieros de SIG Europe, reemplazando flujos manuales en Excel por una plataforma web integrada que consolide datos de 9 sistemas fuente, automatice el cálculo de pagos y facturación por proyecto/empleado, implemente flujos de aprobación multi-rol y ofrezca visibilidad 360° vía Power BI.

**Problema actual:**
- Gestión manual de cierres de pago/facturación en Excel
- Datos dispersos en 9 sistemas: Celero, Bizneo, Intratime, Payhawk, A3 Innuva, A3 ERP, Galán, Mediapost, TravelPerk
- Falta de trazabilidad y auditoría de cambios
- Procesos de aprobación ad-hoc sin flujo estructurado

---

## 2. STAKEHOLDERS Y ROLES FUNCIONALES

| Rol | Responsable(s) | Función en el sistema |
|-----|---------------|----------------------|
| Product Owner / Dirección | Eladio / Sergio | Prioriza, decide y valida |
| Responsable Operaciones | Yoana / Martha | Procesos de visitas y campañas |
| Responsable Finanzas/Pagos | Lourdes / Lara / Yoana / Martha | Lógica de pagos y facturación |
| Responsable RRHH/Dietas | Lourdes / Lara | Tiempos, dietas, kilometraje |
| Responsable IT/Seguridad | Eladio / Silvia | Integraciones, AD, permisos |
| Responsable BI/Reporting | Eladio / Silvia | KPIs, Power BI |
| Contacto técnico h&k | Silvia López | Interlocutor técnico principal |

### Roles de usuario en la plataforma:
- **Administrador:** Acceso total. Gestión de maestros, usuarios, configuración.
- **Gestor de Proyecto:** Visualiza y edita sus proyectos/acciones asignados, inicia cierres.
- **Backoffice:** Consolida datos, gestiona conceptos y periodos.
- **FICO (Finance Control):** Aprueba cierres desde perspectiva financiera.
- **Dirección:** Aprobación final, visualización global de KPIs.
- **Interlocutor (usuario campo):** Empleado de campo cuya actividad se captura desde Celero.
- **Auditor:**  solo lectura del AuditLog y CalculationLog.
- **Lectura:**  consulta de información operativa, sin acciones de aprobación.

---

## 3. ARQUITECTURA FUNCIONAL — MÓDULOS

### 3.1 Navegación principal
```
├── Dashboard
├── Clientes
├── Proyectos
├── Acciones
├── Conceptos
├── Periodos
├── Aprobaciones
├── Contabilidad
├── Informes
└── Administración
    ├── CECOs
    ├── Departamentos
    ├── Roles
    └── Usuarios
```

### 3.2 Módulo: Dashboard
- KPIs por período seleccionado: Cierres completados, Pendientes aprobación, Facturación total, Margen promedio
- Panel "Mis Proyectos": Proyecto, Estado, Coste bruto, Facturación
- Resumen global: Coste total, Ingresos, Margen
- Panel de alertas: Proyectos pendientes aprobación FICO, períodos bloqueados, cierres completados
- Filtro por período (selector dinámico) + botón "Recalcular"

### 3.3 Módulo: Clientes
- CRUD de clientes (fuente: sincronización desde Celero + manual)
- Campos: clientId, clientVatNr (NIF), clientName, clientNotes, dirección completa, datos contacto
- Relación: 1 cliente → N proyectos

### 3.4 Módulo: Proyectos (ServiceType en Celero)
- CRUD de proyectos
- Campos: idProyecto (auto), Estado (Activo/Inactivo), Cliente, Proyecto (nombre), CECO(s) (multi-select), Teléfono, Email, Nombre contacto, Interlocutor/a, Usuarios asignados
- Filtros: texto libre, cliente, estado
- Vistas: listado, detalle, formulario nuevo/editar
- Ejemplo real: Amex Shop Small (id:32), Granini GPVs (id:18)

### 3.5 Módulo: Acciones (Servicios en Celero)
- Representan las acciones/campañas dentro de un proyecto
- Campos: serviceId, Acción (nombre), Proyecto, Estado, CECO, Cliente, Departamento, Interlocutor
- Sub-listado: Conceptos asociados a la acción (con Ver/Editar/Quitar/Duplicar)
- Funcionalidad especial: Añadir Concepto existente, Nuevo Concepto directo desde Acción
- Ejemplo: Amex Shop Small (id:112), Granini GPVs (id:27), Amex New (id:118)

### 3.6 Módulo: Conceptos (Motor de Cálculo)
- Definición de fórmulas de pago/facturación aplicadas en periodos
- Campos: idConcepto (auto), Concepto (nombre), Tipo (Pago / Factura), Desde (fecha), Hasta (fecha), Aplica a (Acciones/Usuarios, multi-select), Cálculo (formulación)
- **Motor de Cálculo / Formulación:** Compuesto por:
  - `Número`: valor numérico fijo o decimal
  - `Variable`: entidad de origen (Visitas Celero, Horas Bizneo, Horas Intratime, Gastos Payhawk, etc.)
  - `Operación`: Suma, Cuenta, %, +, -, ×, /
  - `Entidad`: Gastos Payhawk, Visitas, Horas Bizneo, Horas Intratime
  - Variables custom basadas en preguntas de visita en Celero
- Jerarquía de aplicación: global → proyecto → acción → empleado específico
- Ejemplos reales:
  - C143: "Nota de gastos pago" — Pago — Suma de Gasto
  - C78: "Nota de gastos facturación" — Factura — Suma de Gasto
  - C59: "Pago por visita" — Pago — Conteo de Visita × 18 (2025-09-07 → 2026-12-31)
  - "Sueldo Base" — Pago
  - "Bonus por Visita" — Pago
- Pantalla Detalle de cálculo: muestra datos de entrada, operación, resultado, origen datos, fecha importación

### 3.7 Módulo: Periodos
- Gestión de períodos de cierre (mensual)
- Estados: Abierto, En cálculo, Calculado, En revisión, Aprobado, Bloqueado
- Acción "Recalcular" aplica todos los conceptos activos al período seleccionado
- Genera el "cierre integral" con detalle por empleado y proyecto

### 3.8 Módulo: Aprobaciones
- Flujo multi-rol de aprobación de cierres calculados
- Filtros: Período, Cliente, Proyecto
- **Pendientes de aprobación:** listado con checkbox multi-select → "Aprobar seleccionados"
  - Columnas: Período, Cliente, Proyecto, Coste, Facturación
- **Registros aprobados:** histórico con Aprobado por + Fecha
- **Detalle de aprobación por proyecto:** Coste total, Facturación total, Margen + desglose por concepto/empleado/importe
- Acciones en detalle: Editar/Borrar/Ver por línea de concepto
- Campo de comentarios libre
- Botones: [✅ Aprobar] [❌ Rechazar]
- Ejemplos: Amex Shop Small Mayo 2026 (Coste: €15K, Factura: €20.5K)

### 3.9 Módulo: Contabilidad
- Generación de ficheros de salida para A3 Innuva (nóminas) y A3 ERP (facturas)
- Validación previa al envío
- Histórico de envíos y estados

### 3.10 Módulo: Informes
- Integración con Power BI (dashboards embebidos)
- Dimensiones analíticas: margen por proyecto, productividad, costes, comparativas
- Vistas analíticas SQL para consumo de Power BI

### 3.11 Módulo: Administración
- **CECOs (Centros de Costos):** CRUD, sincronización con Celero
- **Departamentos:** CRUD
- **Roles:** Gestión de roles y permisos
- **Usuarios:** NIF, Nombre, Apellidos, Email, Contraseña, Rol(es) (multi), Departamento(s) (multi), Asignaciones (Clientes/Proyectos/Acciones multi-select)
  - Ejemplo: Silvia Garzon (44888962X) — Rol: Interlocutor — Dept: Sales Training
  - Ejemplo: Tomas Martin (55556668F) — Rol: Interlocutor — Dept: Sales Force

### 3.12 Módulo: Auditoría y Trazabilidad
- Log completo de cambios: Usuario, Entidad, ID, Acción (Crear/Actualizar/Eliminar/Aprobar), Cambios, Fecha/Hora
- Filtros: Usuario, Entidad, Acción
- Retención configurable

---

## 4. ENTIDADES DE DATOS (DATA MODEL)

### 4.1 Entidades Core

```
Cliente
  - id (PK)
  - celeroClientId (FK externo)
  - nif / vatNr
  - nombre
  - notas
  - direccion (objeto)
  - contacto (telefono, email, nombreContacto)
  - activo (bool)
  - createdAt / updatedAt

Proyecto (ServiceType en Celero)
  - id (PK)
  - celeroServiceTypeId (FK externo)
  - clienteId (FK → Cliente)
  - nombre
  - estado (Activo | Inactivo)
  - cecos (array FK → CECO)
  - departamentoId (FK)
  - interlocutor
  - telefono / email / nombreContacto
  - usuariosAsignados (many-to-many → Usuario)
  - createdAt / updatedAt

Accion (Service en Celero)
  - id (PK)
  - celeroServiceId (FK externo)
  - proyectoId (FK → Proyecto)
  - clienteId (FK → Cliente)
  - nombre
  - estado (Activo | Inactivo)
  - cecos (array FK)
  - departamentoId (FK)
  - interlocutor
  - conceptos (many-to-many → Concepto)
  - createdAt / updatedAt

Concepto
  - id (PK)
  - nombre
  - tipo (Pago | Factura)
  - desde (date)
  - hasta (date, nullable = indefinido)
  - aplicaA (array de {tipo: 'accion'|'usuario'|'global', refId})
  - calculo (JSON: array de {tipo: 'numero'|'variable'|'operacion', valor, entidad?})
  - activo (bool)
  - createdAt / updatedAt / createdBy

Periodo
  - id (PK)
  - año (int)
  - mes (int)
  - estado (Abierto | EnCalculo | Calculado | EnRevision | Aprobado | Bloqueado)
  - fechaCalculo
  - fechaCierre
  - createdAt / updatedAt

CierreIntegral
  - id (PK)
  - periodoId (FK → Periodo)
  - proyectoId (FK → Proyecto)
  - accionId (FK → Accion, nullable)
  - estado (EnRevision | Aprobado | Rechazado)
  - costeTotal (decimal)
  - facturacionTotal (decimal)
  - margen (decimal)
  - lineas (FK → LineaCierre[])
  - aprobadoPor (FK → Usuario, nullable)
  - fechaAprobacion (nullable)
  - comentarios (text)
  - createdAt / updatedAt

LineaCierre
  - id (PK)
  - cierreId (FK)
  - conceptoId (FK → Concepto)
  - empleadoId (FK → Empleado, nullable)
  - importe (decimal)
  - datosEntrada (JSON: valores usados en el calculo)
  - origenDatos (sistema, fechaImportacion)

Usuario
  - id (PK)
  - nif (unique)
  - nombre / apellidos
  - email (unique)
  - passwordHash
  - roles (array FK → Rol)
  - departamentos (array FK → Departamento)
  - asignaciones (array: {tipo: 'cliente'|'proyecto'|'accion', refId})
  - activo (bool)
  - createdAt / updatedAt

CECO (Centro de Costo)
  - id (PK)
  - codigo (string, unique)
  - nombre
  - activo

Departamento
  - id (PK)
  - nombre
  - activo

Rol
  - id (PK)
  - nombre (Administrador | GestorProyecto | Backoffice | FICO | Direccion | Interlocutor)
  - permisos (array string)

AuditLog
  - id (PK)
  - usuarioId (FK)
  - entidad (string: Usuario | Proyecto | Concepto | Aprobacion | etc.)
  - entidadId (string)
  - accion (Crear | Actualizar | Eliminar | Aprobar | Rechazar)
  - cambios (JSON: {before, after})
  - fechaHora (datetime)
  - ip (string)
```

---

## 5. INTEGRACIONES CON SISTEMAS EXTERNOS

| Sistema | Tipo datos | Tipo conexión | Periodicidad | Dirección |
|---------|-----------|---------------|--------------|-----------|
| **Celero One** | Operativa: clientes, proyectos, acciones, visitas | REST API live → Google AlloyDB (PostgreSQL/AlloyDB) | Tiempo real / bajo demanda | Lectura |
| **Bizneo** | Fichajes, imputación de horas | REST API | Diario / bajo demanda | Lectura |
| **Intratime** | Fichajes entrada/salida | REST API | Diario / bajo demanda | Lectura |
| **Payhawk** | Gastos, dietas, kilometraje | REST API | Diario / bajo demanda | Lectura |
| **A3 Innuva** | Nóminas (output) | Fichero (.txt/.csv, formato A3) | Al cierre aprobado | Escritura |
| **A3 ERP** | Facturas (output) | Fichero (.txt/.csv, formato A3) | Al cierre aprobado | Escritura |
| **Galán** | Field service management | API/SFTP | Diario | Lectura |
| **Mediapost** | Logística/distribución | API/SFTP | Diario | Lectura |
| **TravelPerk** | Gestión de viajes | REST API | Diario / bajo demanda | Lectura |

### Nota especial Celero:
Celero One usa Google AlloyDB (PostgreSQL compatible). La conexión es **live connection directa** (sin ETL/replicación). Garantiza "single source of truth" en tiempo real. Puerto: estándar PostgreSQL. Requiere credenciales Google Cloud IAM.

---

## 6. STACK TECNOLÓGICO

```
Backend:
  - Runtime:       .NET Core 8+
  - Arquitectura:  Clean Architecture + Vertical Slice (por módulo)
  - ORM:           Entity Framework Core 8
  - DB:            SQL Server 2022 (Azure SQL)
  - Auth:          JWT Bearer + Azure AD (SSO opcional)
  - Messaging:     Azure Service Bus (eventos de integración)
  - Logging:       Serilog → Azure Application Insights
  - Testing:       xUnit + Moq + FluentAssertions
  - Docs:          Swagger/OpenAPI 3.0

Frontend:
  - Framework:     Angular 18+ (Standalone Components)
  - UI Library:    Angular Material 18
  - Estado:        NgRx o Signals (decisión fase 1)
  - Styling:       SCSS + Angular Material theming
  - Testing:       Jasmine/Karma + Cypress (E2E)
  - Build:         Angular CLI + Nx (monorepo opcional)

Base de datos:
  - Principal:     SQL Server 2022 (Azure SQL Managed Instance)
  - Celero:        Google AlloyDB (PostgreSQL) — lectura directa
  - Cache:         Redis (Azure Cache for Redis) — opcional fase 2

Infraestructura (Azure):
  - Hosting BE:    Azure App Service (o AKS en fase avanzada)
  - Hosting FE:    Azure Static Web Apps
  - DB:            Azure SQL
  - Storage:       Azure Blob Storage (ficheros A3, adjuntos, diseños)
  - Bus:           Azure Service Bus
  - Auth:          Azure AD B2C / Azure AD
  - Monitoring:    Azure Application Insights + Azure Monitor
  - CI/CD:         GitHub Actions o Azure DevOps
  - Secrets:       Azure Key Vault

Diseño:
  - Herramienta:   Penpot (self-hosted: http://host.docker.internal:9001)
  - Paleta:        Primary #1F4E78, Light #2E5C8A, Dark #163A52
                   Success #70AD47, Warning #FFC107, Danger #D32F2F
  - Tipografía:    Segoe UI / Roboto / system-ui
  - Estilo:        Corporate clean — sidebar dark, content light

Reporting:
  - Power BI Premium (embebido o workspace)
  - Vistas SQL analíticas en Azure SQL
  - Refresh: diario / bajo demanda

AI & Orquestación (desarrollo):
  - n8n (self-hosted: http://host.docker.internal:5678)
  - Agentes Claude vía SDK (http://host.docker.internal:8888)
  - Penpot: diseños de referencia para agentes
```

---

## 7. FLUJOS DE APROBACIÓN

### 7.1 Flujo estándar de cierre
```
1. Backoffice inicia período → estado: Abierto
2. Sistema importa datos de integraciones → estado: EnCalculo
3. Motor de cálculo aplica conceptos → estado: Calculado
4. Gestor de Proyecto revisa → estado: EnRevision
5. FICO aprueba → estado: Aprobado por FICO
6. Dirección aprueba (si requerido) → estado: Aprobado Final
7. Sistema genera ficheros A3 Innuva + A3 ERP
8. Contabilidad confirma recepción → estado: Cerrado
```
### 7.2 Flujo de aprobación (secuencial multi-rol)
- **Gestor de proyecto:** cierra el período de su proyecto.
- **Backoffice:** valida los datos y los cálculos. Si encuentra incidencias puede editar y devolver al gestor.
- **FICO:** aprueba financieramente. Puede devolver con motivo (vuelve a Backoffice o al Gestor según el motivo).
- **Dirección:** aprueba finalmente y firma.
- **Sistema:** genera automáticamente los ficheros A3 Innuva (nómina) y A3 ERP (facturación).

Cualquier devolución reinicia la cadena en el punto correspondiente y queda registrada en ApprovalHistory con motivo.

### 7.3 Flujo de rechazo
```
En cualquier paso aprobador:
- Rechazar con comentario → vuelve al estado anterior
- El iniciador del paso recibe notificación
- AuditLog registra rechazo con comentario
```

### 7.4 Aprobación masiva
- Check-box "Seleccionar todos" + "Aprobar seleccionados" para múltiples proyectos del mismo período

---

## 8. APIs REQUERIDAS (Endpoints clave)

### Autenticación
```
POST   /api/auth/login
POST   /api/auth/refresh
POST   /api/auth/logout
```

### Clientes
```
GET    /api/clientes?search=&page=&size=
GET    /api/clientes/{id}
POST   /api/clientes
PUT    /api/clientes/{id}
DELETE /api/clientes/{id}
POST   /api/clientes/sync-celero     (sincroniza desde Celero)
```

### Proyectos
```
GET    /api/proyectos?clienteId=&estado=&search=&page=&size=
GET    /api/proyectos/{id}
POST   /api/proyectos
PUT    /api/proyectos/{id}
DELETE /api/proyectos/{id}
POST   /api/proyectos/sync-celero
```

### Acciones
```
GET    /api/acciones?proyectoId=&clienteId=&estado=&search=&page=&size=
GET    /api/acciones/{id}
POST   /api/acciones
PUT    /api/acciones/{id}
DELETE /api/acciones/{id}
POST   /api/acciones/{id}/conceptos/{conceptoId}    (asociar concepto)
DELETE /api/acciones/{id}/conceptos/{conceptoId}    (desasociar)
```

### Conceptos
```
GET    /api/conceptos?tipo=&search=&page=&size=
GET    /api/conceptos/{id}
POST   /api/conceptos
PUT    /api/conceptos/{id}
DELETE /api/conceptos/{id}
POST   /api/conceptos/{id}/duplicate
GET    /api/conceptos/{id}/variables     (variables disponibles para formulación)
```

### Periodos
```
GET    /api/periodos?año=&mes=&estado=
GET    /api/periodos/{id}
POST   /api/periodos
PUT    /api/periodos/{id}/estado
POST   /api/periodos/{id}/calcular       (lanza motor de cálculo)
POST   /api/periodos/{id}/recalcular
```

### Cierres / Aprobaciones
```
GET    /api/cierres?periodoId=&proyectoId=&estado=&page=&size=
GET    /api/cierres/{id}
GET    /api/cierres/{id}/detalle         (líneas de cálculo detalladas)
POST   /api/cierres/aprobar              (batch: array de cierreIds)
POST   /api/cierres/{id}/aprobar
POST   /api/cierres/{id}/rechazar        (body: {comentario})
```

### Contabilidad / Exportación
```
POST   /api/contabilidad/{periodoId}/generar-a3innuva
POST   /api/contabilidad/{periodoId}/generar-a3erp
GET    /api/contabilidad/historial?page=&size=
```

### Usuarios
```
GET    /api/usuarios?rol=&departamento=&estado=&search=&page=&size=
GET    /api/usuarios/{id}
POST   /api/usuarios
PUT    /api/usuarios/{id}
DELETE /api/usuarios/{id}
PUT    /api/usuarios/{id}/asignaciones
```

### Maestros
```
GET    /api/cecos
POST   /api/cecos
PUT    /api/cecos/{id}
GET    /api/departamentos
POST   /api/departamentos
GET    /api/roles
GET    /api/integraciones/sync-status    (estado de cada integración)
POST   /api/integraciones/{sistema}/sync (trigger manual)
```

### Dashboard & Reporting
```
GET    /api/dashboard/kpis?periodoId=
GET    /api/dashboard/alertas
GET    /api/dashboard/mis-proyectos?usuarioId=&periodoId=
GET    /api/audit-log?usuarioId=&entidad=&accion=&desde=&hasta=&page=&size=
```

---

## 9. REQUISITOS NO FUNCIONALES

| Categoría | Requisito |
|-----------|-----------|
| **Rendimiento** | Carga de páginas < 2s; cálculo de cierre completo < 30s para 5000 registros |
| **Disponibilidad** | 99.5% uptime en horario laboral (8-20h CET) |
| **Seguridad** | HTTPS obligatorio, JWT con expiración corta, RBAC estricto por módulo |
| **GDPR** | Enmascaramiento de datos sensibles (NIF, salarios) en logs; derecho al olvido |
| **Fiscal** | Trazabilidad completa de cada línea de pago/factura para auditoría fiscal |
| **Escalabilidad** | Soportar 50+ usuarios concurrentes sin degradación |
| **Auditoría** | Cada acción CRUD deja huella en AuditLog; inmutable |
| **Internacionalización** | ES_es inicial; arquitectura preparada para ES/EN/PT |
| **Accesibilidad** | WCAG 2.1 nivel AA mínimo |
| **Backup** | Daily backup Azure SQL; RPO < 24h, RTO < 4h |

---

## 10. PANTALLAS / MOCKUPS DE REFERENCIA

Pantallas definidas en Demo pantallas (xlsx) y prototipo HTML:

| Pantalla | Archivo referencia | Descripción |
|----------|-------------------|-------------|
| Dashboard | `04-DASHBOARD-VISUAL.html` | KPIs, alertas, proyectos recientes, período selector |
| Proyectos | `Demo pantallas - Proyectos` | Listado filtrable + formulario + detalle |
| Acciones | `Demo pantallas - Acciones` | Listado + detalle con sub-tabla conceptos |
| Conceptos | `Demo pantallas - Conceptos` | Listado + editor formulación (builder visual) |
| Periodos | — | Selector de período + trigger recalcular |
| Aprobaciones | `05-APROBACIONES-VISUAL.html` | Pendientes (multi-check) + aprobados + detalle |
| Usuarios | `03-USUARIOS-MANAGEMENT-VISUAL.html` | Listado + formulario con asignaciones |
| Auditoría | `SIG-PLATAFORMA-TOTAL.html` | Log con filtros |

**Paleta de colores (fiel a prototipos):**
```css
--primary:       #1F4E78;   /* Azul corporativo */
--primary-light: #2E5C8A;
--primary-dark:  #163A52;
--success:       #70AD47;   /* Verde aprobación */
--warning:       #FFC107;   /* Amarillo alerta */
--danger:        #D32F2F;   /* Rojo rechazo/error */
--light:         #E8F4F8;
--lighter:       #F5F5F5;
--border:        #D0D0D0;
--text-dark:     #1A1A1A;
--text-muted:    #666666;
```

---

## 11. WORKSPACE Y CONFIGURACIÓN DE DESARROLLO

```yaml
# Entorno local de desarrollo con agentes
workspace:        sig-es
n8n:              http://host.docker.internal:5678
agent-runner:     http://host.docker.internal:8888
penpot:           http://host.docker.internal:9001
penpot-exporter:  http://host.docker.internal:6063

# Archivos de salida esperados por agente
arquitecto:       docs/ARQUITECTURA.md
                  docs/DATA-MODEL.md
                  docs/API-SPEC.md
designer:         src/app/components/**/*.{ts,html,scss}
                  penpot-design-*.{png,svg}
backend:          src/backend/**/*.cs
                  tests/unit/**/*.cs
frontend:         src/frontend/**/*.{ts,html,scss}
                  tests/e2e/**/*.cy.ts
qa:               resultado-qa-tester.md
sonarqube:        resultado-sonarqube.md

# Frames Penpot a generar (SIG-ES específicos)
frames:
  - name: dashboard        file: penpot-design-dashboard.png
  - name: proyectos        file: penpot-design-proyectos.png
  - name: acciones         file: penpot-design-acciones.png
  - name: conceptos        file: penpot-design-conceptos.png
  - name: conceptos-calc   file: penpot-design-calculadora.png
  - name: aprobaciones     file: penpot-design-aprobaciones.png
  - name: aprobacion-det   file: penpot-design-aprobacion-detalle.png
  - name: usuarios         file: penpot-design-usuarios.png
  - name: auditoria        file: penpot-design-auditoria.png
  - name: login            file: penpot-design-login.png
```

---

## 12. Convenciones técnicas adicionales
```
Filtrado por ownership en repositorio: GetByIdAndUsuarioIdAsync cuando aplique (un gestor no debe ver cierres ajenos).
Soft delete con filtro global en EF Core.
JsonStringEnumConverter para que los enums se serialicen como string (Tipo: "Pago"/"Factura", Estado: "Activo"/"Inactivo", etc.).
ProblemDetails consistente en todas las respuestas de error.
FluentValidation para validación de DTOs.
Tests xUnit de Domain en TDD; tests de integración con WebApplicationFactory + SQLite in-memory.
Frontend: data-testid en cada elemento interactivo con convención <entidad>-<acción> o <entidad>-<campo> para los E2E.
Rutas Angular: redirect con pathMatch: 'full' siempre al principio del array.
```

---


## 13. Fases del proyecto — ESTADO ACTUAL (5 jun 2026)

**Hito:** Semana 2 de 4-5 semanas | Entrega estimada: 15-20 julio 2026

### FASES COMPLETADAS ✅

**1- PREVIA — Roles y responsabilidades:** ✅ DONE
- Responsables nombrados y comunicación activa con desarrollador

**2- PREVIA — Análisis situación actual:** ✅ DONE
- Mapeo de Excel, procesos manuales, sistemas identificados
- Documento: `1. ESTADO GENERAL DEL PROYECTO.txt`

**3- PREVIA — Definición funcional integración:** ✅ DONE
- Entidades clave modeladas (Proyecto/Acción, Visita, Usuario, Gasto)
- Identificadores únicos acordados
- 9 sistemas priorizados para integración

**4- PREVIA — Gestión de pagos:** ⚠️ PARCIAL (70%)
- Plantillas Excel digitalizadas
- Motor de cálculo implementado (FormulaParser + CalculationEngine)
- **FALTA:** Validación de reglas FICO (dietas, km, límites)
- **BLOQUEADOR:** Datos reales para testing

**5- PREVIA — Gestión de facturación:** ⚠️ PARCIAL (80%)
- Exports A3 Innuva y A3 ERP funcionales
- Cálculos de margen presentes
- **FALTA:** Integración real con A3 (OAuth2 Conectia)

**6- PREVIA — Control usuarios y seguridad:** ✅ DONE (95%)
- Roles RBAC implementados
- JWT + Azure AD ready (SSO fase 2)
- Ownership filters activos

**7- PREVIA — Reporting y analítica:** ⚠️ PARCIAL (20%)
- Vistas SQL analíticas creadas
- Power BI embeddings pendientes

### FASE ACTUAL: DESARROLLO H&K (Semana 2)

**Sprint 3-5: Integraciones + UAT + Refinamientos**
- Implementar APIs reales (PayHawk, Intratime, etc.)
- Testing E2E con datos reales
- Validación de flujos con stakeholders
- Deployment Azure staging
- Buffer final pre-go-live

---


## 14. Taxonomía de tipos de concepto (referencia para el motor de cálculo)
```
- El motor debe soportar al menos estos tipos de lógica de cálculo (vistos en el cronograma del cliente):
MENSUAL_FEE: tarifa mensual fija.
MENSUAL_IMPUTADO: importe mensual con imputación variable por proyecto.
UNIDAD/TRAMOS: cálculo por unidades con tramos escalonados.
HORAS (RATE_CARD): horas multiplicadas por tarifa según rate card.
MENSUAL+INCENTIVOS: mensualidad fija + variable por incentivos.
CONTRATO+INCENTIVOS: importe contractual + incentivos.
PRESUPUESTO: importe presupuestado fijo.
FIJO_ANUAL+SESION: anualidad + pago por sesión.
COSTE+MARGEN/FEE: cost-plus con margen o fee.
INCLUIDA_EN_TARIFA/PRESUP: incluido en la tarifa o presupuesto principal (no se cobra aparte).
MENSUAL (FIJO+VAR): mensualidad con parte fija y parte variable.
- Ejemplos de lógicas concretas: PAGO MERCH, PAGO IMPLANTACIÓN, PAGO ACTUALIZACIÓN, PAGO POR COLOCACIÓN DE MÓDULOS, PAGO VACIADO MUEBLE, PAGO ACTUALIZAR+IMPLANTAR, FACTURACIÓN CLIENTE, FACT IMPLANTACIÓN.
- Diseñar el motor de modo que estas lógicas puedan expresarse con la combinación de Variables (de Celero) + Operaciones (+, -, ×, /, Suma, Cuenta, %) + Entidades (Gastos PayHawk, Visitas, Horas Bizneo, Horas Intratime) + Filtros.
```

---


## 15. Datos de prueba sugeridos (seed)
```
- Para los tests y para el smoke del dev:
- Clientes: Amex, Granini, Apple, Coty, Dyson, Future Cosmetics.
- Proyectos: "Amex Shop Small", "Granini GPVs", "NPI Watch 03-26", "Apple Formaciones", "Coty implantaciones", "Dyson Aspiracion Q12026".
- Usuarios con roles: 1 Administrador, 1 Dirección, 1 FICO, 1 Backoffice, 2 Gestores de proyecto, 1 Auditor.
- Períodos: "Enero 2026", "Febrero 2026", "Marzo 2026".
- Conceptos: "Nota de gastos pago" (Pago, Suma de Gasto), "Nota de gastos facturación" (Factura, Suma de Gasto), "Pago por visita" (Pago, Conteo de Visita × 18), "Bonus por visita" (Pago).
```

---


## 16. RESTRICCIONES Y NOTAS FINALES
```
- La integración con Power BI no requiere desarrollo dentro de la app: la BBDD SQL Server debe quedar diseñada de modo que Power BI pueda conectarse y leer vistas/tablas analíticas. Documentar las vistas SQL necesarias en ENVIRONMENT.md.
- La integración Azure AD para SSO se considera fase 2; en el MVP, autenticación JWT propia con BCrypt para password hash.
- La generación efectiva de ficheros A3 Innuva en XML/EDI debe quedar contractualmente definida en ARQUITECTURA.md con el formato exacto pendiente de confirmación del cliente; expone endpoint pero el formato concreto se ajusta a posteriori.
- El sistema debe operar en español (UI, mensajes, validaciones); el código backend y los identificadores en inglés. 
```

---

## 17. INSTRUCCIONES PARA EL AGENTE ARQUITECTO

Al generar `docs/ARQUITECTURA.md` debes:

1. **Definir la arquitectura Clean Architecture** con capas: Domain, Application, Infrastructure, API, para el backend .NET
2. **Diseñar el esquema SQL Server completo** con todas las tablas, PK/FK, índices recomendados
3. **Especificar cada endpoint** con método HTTP, ruta, request body DTO, response DTO, códigos HTTP, autorización por rol
4. **Describir el Motor de Cálculo** (Concepto engine): cómo se parsea la formulación JSON, cómo se resuelven variables desde cada sistema origen, estrategia de caché/memoización
5. **Diseñar la capa de integración**: patrón Adapter por sistema, estrategia de retry, idempotencia, gestión de errores
6. **Definir eventos de dominio** (DomainEvents) y su publicación vía Azure Service Bus para el AuditLog
7. **Especificar la estrategia de autenticación**: JWT + RBAC, claims, middleware Angular guards
8. **Definir estructura de proyecto Angular**: módulos lazy-loaded, servicios, interceptores HTTP, guards
9. **Documentar los DTOs** request/response para cada operación
10. **Incluir diagrama de arquitectura** en Mermaid o texto ASCII
11. **Especificar estructura de carpetas** del workspace `sig-es/` para que los demás agentes sepan dónde crear archivos


**Archivos a generar:**
- `docs/ARQUITECTURA.md` (este documento principal)
- `docs/DATA-MODEL.md` (esquema SQL + Entity definitions)
- `docs/API-SPEC.md` (todos los endpoints con DTOs)
- `docs/INTEGRACIONES.md` (detalle técnico de cada integración)
- `docs/ROLES-PERMISOS.md` (matriz completa)

---
