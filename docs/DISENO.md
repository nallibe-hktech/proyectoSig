# Diseño de Pantallas · SIG Plataforma de Cierres

> Fuente de verdad: mockups Penpot + ARQUITECTURA.md.
> Fecha: Junio 2026 | Versión: 1.0

---

## 1. Layout general

```
┌──────────────────────────────────────────────────────────┐
│ AppBar (sticky, 64px) // PRIMARY bg, white text          │
│ [☰] [SIG Logo] [⋯] [Período ▼] [Usuario] [👤]          │
├──────────────┬───────────────────────────────────────────┤
│ Sidenav      │ Main Content                              │
│ (256px)      │                                           │
│              │  .sig-page                                │
│ OPERATIVO    │  ┌─── page__header (title + actions) ──┐  │
│  Dashboard   │  │   [Breadcrumbs]                      │  │
│  Clients     │  │   <h1>Title</h1>    [btn] [btn]     │  │
│  Servicios   │  └─────────────────────────────────────┘  │
│  Concepts    │  ┌─── filters ──────────────────────────┐  │
│  Variables   │  │   [search] [filter1] [filter2]       │  │
│  Periods     │  └─────────────────────────────────────┘  │
│  Approvals   │                                           │
│  Closures    │  ┌─── mat-table / cards ───────────────┐  │
│  Reports     │  │   [datos paginados]                 │  │
│ ─────────── │  └─────────────────────────────────────┘  │
│ ADMIN       │                                           │
│  CostCenters│  ┌─── paginator ─────────────────────────┐  │
│  Departments│  │   [⏮] [⏪] [Page 1 of N] [⏩] [⏭]   │  │
│  Roles      │  └─────────────────────────────────────┘  │
│  Users      │                                           │
│  Audit Log  │                                           │
│  Sync       │                                           │
│  Visitas    │                                           │
└──────────────┴───────────────────────────────────────────┘
```

---

## 2. Pantallas por módulo

### 2.1 Login (`/login`)

| Atributo | Valor |
|----------|-------|
| Ruta | `/login` |
| Shell | No (layout público independiente) |
| Componente | `LoginComponent` |
| Guard | `AllowAnonymous` |

**Layout:** Centro vertical+horizontal, card con logo SIG + formulario email/password + botón "Iniciar sesión". Enlace "¿Olvidó su contraseña?" (placeholder, no implementado).

**Estados:**
- **Default:** Formulario vacío, botón deshabilitado
- **Validando:** Spinner en botón, feedback de error inline
- **Error:** Mensaje "Credenciales incorrectas" en snackbar
- **Éxito:** Redirección a `/dashboard`

**data-testid:** `input-email`, `input-password`, `btn-login`, `login-error`

---

### 2.2 Dashboard (`/dashboard`)

| Atributo | Valor |
|----------|-------|
| Ruta | `/dashboard` |
| Roles | Todos |
| Componente | `DashboardComponent` |

**Layout:** Fila de 4 tarjetas KPI (Cierres completados, Pendientes, Facturación total, Margen). Panel "Mis proyectos" (tabla resumen). Panel de alertas (cards de warning/info). Selector de período en AppBar.

**Estados:**
- **Carga:** 4 esqueletos KPI + skeleton table
- **Vacío:** Empty state "No hay datos para el período seleccionado" + CTA "Ir a Periodos"
- **Error:** Snackbar "Error al cargar dashboard. Intente de nuevo."
- **Éxito:** KPIs numéricos, tabla con datos, alertas visibles

**Componentes:** `sig-kpi-card`, `mat-table`, `sig-state-badge`, `sig-skeleton`

---

### 2.3 Clients (`/clients`)

| Atributo | Valor |
|----------|-------|
| Rutas | `/clients`, `/clients/nuevo`, `/clients/:id`, `/clients/:id/editar` |
| Roles | Administrator, ProjectManager, Backoffice |
| Componentes | `ClientsList`, `ClientForm`, `ClientDetail` |

**Listado:** Search bar + tabla (Nombre, NIF, Ciudad, Proyectos, Acciones). Paginación 25 items. Botón "Nuevo Client" flotante o en header.

**Formulario:** Nombre*, NIF*, Dirección, Ciudad, Provincia, País, CP, Contacto (nombre, email, teléfono). Validación en tiempo real. Botones Guardar/Cancelar.

**Detalle:** Cabecera con nombre + NIF, información de contacto, tabla de proyectos asociados.

**Estados:** Carga → skeleton. Vacío → `sig-empty-state`. Error → snackbar. Submit → loading en botón.

---

### 2.4 Servicios (`/services`)

> Pantalla resultante del refactor PPT que unificó las antiguas pantallas Projects + Actions. La jerarquía es ahora **Cliente → Servicio → Concepto**: el Servicio cuelga directamente del Cliente y agrupa CECOs, usuarios y conceptos.

| Atributo | Valor |
|----------|-------|
| Rutas | `/services`, `/services/nuevo`, `/services/:id`, `/services/:id/editar` (más sub-rutas `tarifas\` y `presupuestos\`) |
| Roles | Administrator, ProjectManager, Backoffice |
| Componentes | `ServicesList` (`services-list.component.ts`), `ServiceForm` (`service-form.component.ts`), `ServiceDetail` (`service-detail.component.ts`) |

**Listado:** Filtros por cliente + estado + búsqueda. Tabla (Nombre, Cliente, Estado, Fecha alta, acciones).

**Formulario:** Nombre*, Cliente* (autocomplete), Departamento, Estado*, CECO(s) multi-select, Conceptos asociados (multi-select), Interlocutor (nombre, email, teléfono), Usuarios asignados (multi-select, ownership).

**Detalle:** Cabecera con nombre + badge estado. Pestañas: Información, Conceptos (sub-listado con Ver/Editar/Quitar/Duplicar + "Añadir Concepto existente"/"Nuevo Concepto"), Tarifas, Presupuestos, Cierres (si existen).

---

### 2.6 Concepts (`/concepts`)

| Atributo | Valor |
|----------|-------|
| Rutas | `/concepts`, `/concepts/nuevo`, `/concepts/:id`, `/concepts/:id/editar`, `/concepts/:id/formula` |
| Roles | Administrator, Backoffice |
| Componentes | `ConceptsList`, `ConceptForm`, `ConceptDetail`, `FormulaEditor` |

**Listado:** Filtro por tipo (Pago/Factura). Tabla (Nombre, Tipo, Desde, Hasta, Estado, Acciones).

**Formulario:** Nombre*, Tipo*, Desde*, Hasta, Aplica a (multi-select acciones/usuarios), Fórmula (builder visual).

**FormulaEditor:** Builder visual con bloques de Número, Variable, Operación. Árbol JSON editable. Vista previa del resultado.

**Detalle cálculo (lectura):** Muestra datos de entrada, operación, resultado, origen, fecha importación.

---

### 2.7 Periods (`/periods`)

| Atributo | Valor |
|----------|-------|
| Ruta | `/periods` |
| Roles | Administrator, Backoffice |
| Componentes | `PeriodsList`, `PeriodForm` |

**Listado:** Tabla (Año, Mes, Estado, Fecha cálculo, Acciones). Botones "Cerrar"/"Reabrir" por período.

**Acciones:** "Calcular" / "Recalcular" lanza el motor de cálculo. Confirm dialog antes de recalcular.

---

### 2.8 Approvals (`/approvals`, `/approvals/pendientes`)

| Atributo | Valor |
|----------|-------|
| Rutas | `/approvals`, `/approvals/pendientes` |
| Roles | ProjectManager, Backoffice, FICO, Direction |
| Componentes | `ApprovalsComponent` |

**Pendientes:** Tabla con checkbox multi-select. Columnas: Período, Cliente, Proyecto, Coste, Facturación, Margen, Estado. Botón "Aprobar seleccionados". Filtro por período.

**Aprobados:** Histórico con Aprobado por + Fecha.

**Detalle aprobación por proyecto:** KPIs (Coste total, Facturación, Margen) + desglose por concepto/empleado/importe + acciones editar/ver/borrar por línea + comentarios + botones [Aprobar] [Rechazar].

**Flujo de rechazo:** Dialog con campo de comentarios obligatorio. Confirmación.

---

### 2.9 Closures (`/closures`)

| Atributo | Valor |
|----------|-------|
| Rutas | `/closures`, `/closures/nuevo`, `/closures/:id` |
| Roles | Administrator, ProjectManager, Backoffice |
| Componentes | `ClosuresList`, `ClosureForm`, `ClosureDetail` |

**Relación:** Un cierre pertenece a un Proyecto y un Período.

**Detalle:** Cabecera con KPIs + estado + paso actual. Líneas de cierre en tabla (Concepto, Empleado, Importe, Tipo, Incidencias). Histórico de aprobaciones.

---

### 2.10 Contabilidad (`/reports` contiene export)

| Atributo | Valor |
|----------|-------|
| Ruta | `/reports` |
| Roles | Administrator, FICO, Backoffice |
| Componente | `ReportsComponent` |

**Acciones:** "Generar fichero A3 Innuva", "Generar fichero A3 ERP". Histórico de exportaciones con estado y fecha.

---

### 2.11 Audit (`/audit`)

| Atributo | Valor |
|----------|-------|
| Ruta | `/audit` |
| Roles | Administrator, Auditor |
| Componente | `AuditComponent` |

**Layout:** Filtros (Usuario, Entidad, Acción, Fechas) + tabla de log (Fecha, Usuario, Entidad, ID, Acción, Cambios). Paginación 50 items.

---

### 2.12 Admin — Cost Centers (`/cost-centers`)

CRUD simple. Tabla (Código, Nombre, Estado). Formulario inline o dialog.

### 2.13 Admin — Departments (`/departments`)

CRUD simple. Tabla (Nombre, Estado). Formulario inline o dialog.

### 2.14 Admin — Roles (`/roles`)

Fuente visual: `frontend/public/penpot-design-roles.svg` (cambio aplicado vía `docs/CAMBIO-VISUAL-roles.md`).

| Atributo | Valor |
|----------|-------|
| Rutas | `/roles` |
| Roles backend | `Administrator` (CRUD), `Auditor` (lectura) — el MVP expone únicamente `GET /api/roles`; el resto se prepara visualmente |
| Componente | `RolesListComponent` (`frontend/src/app/features/roles/roles-list.component.ts`) |
| data-testid | `roles-table`, `roles-row-{rolName}`, `roles-detail-panel`, `roles-detail-close` |

**Layout (2 columnas):** A la izquierda la tabla principal de roles; a la derecha un detail panel lateral (`width: 360px`) con la matriz completa de permisos del rol seleccionado.

**Topbar:**
- Título con `mat-icon verified_user` color `--sig-teal` + chip total `{{ roles().length }}` con badge teal.
- CTA `Nuevo Rol` (outline blue) — solo visible para `Administrator`; oculto para `Auditor`.

**Filter bar:** input búsqueda (icono `search`), selects `Vista` y `Ámbito` (Global / Proyecto), botones `Filtrar` (primary) y `Limpiar` (ghost), contador `TOTAL N` alineado a la derecha.

**Tabla principal — columnas:**
1. `TIPO DE ROL` — nombre del rol, peso 600.
2. `VISTA` — `sig-scope-badge` con dos variantes: `scope--global` (teal `#00d4c4`) y `scope--proyecto` (azul `#3b82f6`).
3. `PAGOS`, `FACTURACIONES`, `USUARIOS`, `ROLES` — texto coloreado por nivel: `Control total` (teal), `Sin permisos` (muted), `Ver / Validar / Editar` (texto primary).

Fila seleccionada: fondo `rgba(0,212,196,.06)`. Hover: `--sig-bg-hover`. Click en fila abre el detail panel.

Pie de tabla: nota fina (`--sig-text-muted`, 11px): "El permiso 'Editar' incluye Eliminar · Los datos de Ceco / Cliente / Proyecto / Acción se alimentan de Celero".

**Detail panel (sig-detail-panel):**
- Header azul (`--sig-blue`) con icono `verified_user` + título "Detalle del Rol" + botón cerrar `×`.
- Cabecera del rol: ícono cuadrado teal (radius 10) + nombre del rol (15px, 700) + `sig-scope-badge` Vista.
- **Sección "Matriz de Permisos":** lista vertical de filas (label fijo 100px + chips de permiso). Entidades cubiertas: Pagos, Facturaciones, Ceco, Departamento, Cliente, Proyecto, Acción, Usuarios, Roles. Cada permiso se renderiza como pill (`sig-perm-pill`) con clase semántica: `perm--ver` (azul), `perm--val` (amber), `perm--edit` (teal), `perm--crear` (verde), `perm--none` (rojo translúcido).
- **Sección "Usuarios con este Rol":** lista de `sig-avatar-chip` (avatar circular color asignado + nombre + email + botón `Quitar`). CTA al final `+ Añadir Usuario` (link azul).
- Footer: botón `Editar` (azul, fill) + botón `Eliminar` (rojo translúcido, icon-only). Ambos visibles solo para `Administrator`.

**Roles seed iniciales mostrados:** Administrador (Global), Direccion (Global), FICO (Global), RRHH (Global), Facilitador (Global), Interlocutor (Proyecto), Gestor (Proyecto), Backoffice (Proyecto), Auxiliar (Proyecto).

**Accesibilidad:**
- Contraste de pills sobre fondo `--sig-bg-card` ≥ 4.5:1 (validado para teal `#00d4c4`, azul `#3b82f6`, amber `#f59e0b`).
- Focus-visible heredado del global (`outline 2px solid var(--sig-blue)`).
- `aria-label` en botón cerrar del panel.

**Estados:**
- Carga: `sig-skeleton-row` repetido 6 veces en la tabla.
- Vacío: `sig-empty-state` con icono `verified_user`, texto "No hay roles configurados".
- Sin rol seleccionado: detail panel oculto (la tabla ocupa el 100%).

### 2.15 Admin — Users (`/users`)

| Atributo | Valor |
|----------|-------|
| Rutas | `/users`, `/users/nuevo`, `/users/:id`, `/users/:id/editar` |
| Roles | Administrator (CRUD), Auditor (lectura) |
| Componentes | `UsersList`, `UserForm`, `UserDetail` |

**Listado:** Tabla (NIF, Nombre, Email, Roles, Estado, Acciones). Search por NIF/nombre/email.

**Formulario:** NIF*, Nombre*, Apellidos*, Email*, Contraseña*, Rol(es) multi-select, Departamento(s) multi-select, Asignaciones (Servicios multi-select — ownership vía ServiceUser).

### 2.16 Sync (`/sync`)

Monitor de estado de integraciones. Tabla (Sistema, Última sync, Estado, Acción "Sincronizar ahora").

---

## 3. Estados globales por pantalla

| Estado | Comportamiento |
|--------|---------------|
| **Carga** | Skeleton shimmer (`.sig-skeleton`, `.sig-skeleton-row`, `.sig-skeleton-text`) |
| **Vacío** | `sig-empty-state` con icono, título, descripción, CTA opcional |
| **Error** | Snackbar semántico (`.snack-error`) + reload CTA |
| **Carga submit** | Botón deshabilitado + spinner (Angular Material `disabled` + icono giratorio) |
| **Éxito submit** | Snackbar `.snack-success` + redirección o recarga de lista |
| **Offline** | (Placeholder) Snackbar "Sin conexión. Los datos pueden no estar actualizados." |

---

## 4. Responsive

| Breakpoint | Comportamiento |
|------------|----------------|
| < 600px (mobile) | Sidenav overlay (no side), padding reducido 16px, KPI values 28px |
| 600-959px (tablet) | Sidenav side o collapsed, padding 24px |
| ≥ 960px (desktop) | Sidenav side expandido 256px, layout completo |

---

## 5. Accesibilidad

- Contraste WCAG 2.1 AA verificado en paleta
- `:focus-visible` con outline 3px `--mat-sys-primary`
- `aria-label` en todos los icon-buttons, inputs, selects
- `role="banner"` en AppBar, `role="navigation"` en sidenav
- `aria-label="Navegación principal"` en `<mat-sidenav>`
- Tablas con `aria-label` descriptivo
- Mensajes de error asociados vía `mat-error`
- Color no es único indicador (iconos + texto + badge)
