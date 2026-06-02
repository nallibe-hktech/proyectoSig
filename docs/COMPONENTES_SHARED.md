# Componentes Compartidos SIG

> Catálogo de componentes shared reutilizables en la plataforma.
> Fecha: Junio 2026 | Versión: 1.0

---

## sig-breadcrumbs

| Atributo | Valor |
|----------|-------|
| Selector | `sig-breadcrumbs` |
| Archivo | `frontend/src/app/shared/breadcrumbs.component.ts` |
| Módulo | Angular Material: `MatIconModule` |

### Inputs

| Input | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `crumbs` | `Crumb[]` | Sí | Array de {label: string, route?: string} |

### Outputs

Ninguno.

### data-testid

- `breadcrumbs` — contenedor `<nav>`

### Estados

- Ruta activa → último crumb sin enlace, clase `.sig-breadcrumb-current`
- Crumb intermedio → enlace `<a>` + separador `chevron_right`

---

## sig-state-badge

| Atributo | Valor |
|----------|-------|
| Selector | `sig-state-badge` |
| Archivo | `frontend/src/app/shared/state-badge.component.ts` |

### Inputs

| Input | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `estado` | `EstadoClosure` | Sí | Estado del cierre |
| `paso` | `ApprovalStep` | Sí | Paso actual del flujo |

### Outputs

Ninguno.

### data-testid

- `badge-estado` — span con clase dinámica

### Variantes CSS

Ver `docs/SISTEMA_DISENO.md §6.5` — clases `sig-badge--*` según estado+paso.

---

## sig-empty-state

| Atributo | Valor |
|----------|-------|
| Selector | `sig-empty-state` |
| Archivo | `frontend/src/app/shared/empty-state.component.ts` |
| Módulos | `MatIconModule`, `MatButtonModule` |

### Inputs

| Input | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `icon` | `string` | Sí | Glifo Material Symbols |
| `title` | `string` | Sí | Título del estado vacío |
| `description` | `string` | No | Descripción opcional |
| `ctaLabel` | `string` | No | Texto del botón de acción |
| `hasFilter` | `boolean` | No | Si true, icono del botón cambia a `filter_list` |

### Outputs

| Output | Tipo | Descripción |
|--------|------|-------------|
| `ctaClick` | `void` | Click en botón de acción |

### data-testid

- `empty-state` — contenedor
- `btn-empty-cta` — botón de acción

### Estados visuales

- Sin descripción → no renderiza `<p>`
- Sin ctaLabel → no renderiza botón
- hasFilter=true → icono `filter_list` en vez de `add`

---

## sig-skeleton

| Atributo | Valor |
|----------|-------|
| Selector | `sig-skeleton` |
| Archivo | `frontend/src/app/shared/page-skeleton.component.ts` |

### Inputs

| Input | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `count` | `number` | No | Número de filas skeleton (default 5) |

### data-testid

No aplica (elementos renderizados no tienen testid).

---

## sig-confirm-dialog

| Atributo | Valor |
|----------|-------|
| Selector | `sig-confirm-dialog` |
| Archivo | `frontend/src/app/shared/confirm-dialog.component.ts` |
| Módulos | `MatDialogModule`, `MatButtonModule`, `MatIconModule` |

### Dependencia

Inyectar vía `MatDialog.open(ConfirmDialogComponent, { data: ConfirmDialogData })`.

### ConfirmDialogData

| Campo | Tipo | Requerido | Descripción |
|-------|------|-----------|-------------|
| `title` | `string` | Sí | Título del diálogo |
| `message` | `string` | Sí | Cuerpo del mensaje |
| `entityName` | `string?` | No | Nombre de la entidad a eliminar (formateado) |
| `dependencies` | `{label, count}[]?` | No | Lista de dependencias afectadas |
| `confirmLabel` | `string?` | No | Texto del botón confirmar |
| `cancelLabel` | `string?` | No | Texto del botón cancelar |
| `destructive` | `boolean?` | No | Si true, botón warn + advertencia |

### Retorno

`MatDialogRef` cierra con `true` (confirmado) o `false` (cancelado).

### data-testid

- `modal-confirmacion` — título del diálogo
- `btn-confirmar-eliminar` — botón confirmar
- `btn-cancelar-eliminar` — botón cancelar

---

## sig-pie-chart

| Atributo | Valor |
|----------|-------|
| Selector | `sig-pie-chart` |
| Archivo | `frontend/src/app/shared/pie-chart.component.ts` |

Gráfico SVG circular para KPIs de dashboard.

---

## Componentes compartidos adicionales (en features)

### app-page-header

No implementado como shared. Consiste en patrón: `.sig-page__header` + `.sig-page__title` + breadcrumbs.

### app-stat-card (sig-kpi-card)

Implementado como estilo CSS en `styles.scss` (`.sig-kpi-card`). Card con label uppercase + value grande + trend indicator.

### app-data-table (sig-table)

Implementado como estilo CSS en `styles.scss` (`.sig-table`, `.sig-table-dark-header`). No es componente Angular, son clases aplicadas a `mat-table`.

### app-filter-bar

Patrón implementado en cada listado feature (search input + selects de filtro). No hay componente shared.

### app-budget-progress

No implementado en el esqueleto actual. Pendiente para implementación futura si el módulo de presupuestos lo requiere.

### app-toast-notification

Implementado via `NotifyService` + snackbars de Angular Material con clases semánticas (`.snack-success`, `.snack-error`, `.snack-warning`, `.snack-info`).

### app-amount-display

No implementado como componente. Usar clase `.mono-num` + formato manual con `Intl.NumberFormat`.

### app-category-badge

No implementado como componente. Usar `sig-state-badge` para estados y clases `.sig-badge-*` para categorías personalizadas.

---

## data-testid — convención general

Formato: `<entidad>-<acción>` o `<entidad>-<campo>`

| Elemento | Patrón | Ejemplo |
|----------|--------|---------|
| Botón crear | `btn-nuevo-<entidad>` | `btn-nuevo-cliente` |
| Botón editar | `btn-editar-<id>` | `btn-editar-42` |
| Botón eliminar | `btn-eliminar-<id>` | `btn-eliminar-42` |
| Input búsqueda | `search-<entidad>` | `search-clientes` |
| Select filtro | `filter-<campo>` | `filter-estado` |
| Celda tabla | `cell-<columna>-<id>` | `cell-nombre-42` |
| Paginación | `paginator` | `paginator` |
| Navegación | `nav-<ruta>` | `nav-dashboard` |
| Card KPI | `kpi-<nombre>` | `kpi-cierres-completados` |
