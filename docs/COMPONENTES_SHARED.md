# Componentes Compartidos — SIG · Plataforma de Cierres

> Catálogo de componentes reutilizables del sistema de diseño.
> Versión: 1.0 | Fecha: Mayo 2026

---

## 1. `app-page-header`

Selector: `<app-page-header>`

Propósito: Encabezado estándar de página con título, breadcrumbs y acciones.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `title` | `string` | — | Título de la página |
| `breadcrumbs` | `BreadcrumbItem[]` | `[]` | Array de {label, route?} |
| `icon` | `string` | — | Icono opcional antes del título |

| Output | Tipo | Descripción |
|--------|------|-------------|
| — | — | — |

`data-testid`: `page-header-{title}`

---

## 2. `app-stat-card`

Selector: `<app-stat-card>`

Propósito: Tarjeta KPI con valor, etiqueta y tendencia. Usada en Dashboard.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `label` | `string` | — | Etiqueta upper (ej: "CIERRES COMPLETADOS") |
| `value` | `string` | — | Valor principal (ej: "12", "€450K") |
| `trend` | `'up' \| 'down' \| 'neutral'` | `'neutral'` | Dirección de tendencia |
| `trendText` | `string` | — | Texto de tendencia (ej: "+2 vs mes ant.") |
| `accent` | `string` | `#1F4E78` | Color de barra lateral |
| `icon` | `string` | — | Icono decorativo |

`data-testid`: `kpi-{label}`

---

## 3. `app-data-table`

Selector: `<app-data-table>`

Propósito: Tabla de datos reutilizable con paginación, ordenación y estados.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `columns` | `ColumnDef[]` | — | Definición de columnas: {key, label, sortable?, width?, format?} |
| `data` | `any[]` | `[]` | Datos a mostrar |
| `total` | `number` | `0` | Total de registros (para paginación) |
| `page` | `number` | `1` | Página actual |
| `pageSize` | `number` | `25` | Registros por página |
| `loading` | `boolean` | `false` | Muestra skeleton mientras carga |
| `emptyMessage` | `string` | `'No hay datos'` | Mensaje cuando no hay datos |
| `selectable` | `boolean` | `false` | Muestra checkbox multi-select |

| Output | Tipo | Descripción |
|--------|------|-------------|
| `pageChange` | `number` | Cambio de página |
| `sortChange` | `{key, direction}` | Cambio de ordenación |
| `rowClick` | `any` | Click en fila |
| `selectionChange` | `any[]` | Cambio en selección |

Estados: loading (skeleton), empty (empty state), error, datos.
`data-testid`: `tbl-{entity}`

---

## 4. `app-filter-bar`

Selector: `<app-filter-bar>`

Propósito: Barra de filtros reutilizable con campos configurables.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `filters` | `FilterDef[]` | `[]` | Definición de filtros: {key, label, type: 'text'|'select'|'date', options?} |
| `values` | `Record<string, any>` | `{}` | Valores actuales |
| `total` | `number` | `0` | Total de registros (badge) |

| Output | Tipo | Descripción |
|--------|------|-------------|
| `search` | `Record<string, any>` | Emite al hacer clic en Filtrar |
| `clear` | `void` | Emite al limpiar filtros |

`data-testid`: `filter-bar`

---

## 5. `app-budget-progress`

Selector: `<app-budget-progress>`

Propósito: Barra de progreso presupuestario (coste vs presupuesto).

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `actual` | `number` | `0` | Valor actual |
| `budget` | `number` | `0` | Presupuesto |
| `label` | `string` | — | Etiqueta |

`data-testid`: `budget-progress`

---

## 6. `app-empty-state`

Selector: `<app-empty-state>`

Propósito: Estado vacío para listas y búsquedas sin resultados.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `icon` | `string` | `'inbox'` | Icono a mostrar |
| `title` | `string` | `'Sin datos'` | Título del estado vacío |
| `message` | `string` | — | Mensaje descriptivo |
| `actionLabel` | `string` | — | Texto del CTA (opcional) |
| `actionRoute` | `string` | — | Ruta del CTA (opcional) |

| Output | Tipo | Descripción |
|--------|------|-------------|
| `action` | `void` | Click en CTA (si no hay route) |

`data-testid`: `empty-state`

---

## 7. `app-confirm-dialog`

Selector: N/A (servicio `MatDialog`)

Propósito: Diálogo de confirmación genérico.

| Input (vía data) | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `title` | `string` | `'Confirmar'` | Título del diálogo |
| `message` | `string` | `'¿Estás seguro?'` | Mensaje de confirmación |
| `confirmLabel` | `string` | `'Confirmar'` | Texto botón confirmar |
| `cancelLabel` | `string` | `'Cancelar'` | Texto botón cancelar |
| `type` | `'danger' \| 'warning' \| 'info'` | `'info'` | Tipo semántico |

`data-testid`: `confirm-dialog`

---

## 8. `app-toast-notification`

Selector: Servicio `MatSnackBar`

Propósito: Notificación toast semántica.

| Método | Descripción |
|--------|-------------|
| `success(message)` | Toast verde |
| `error(message)` | Toast rojo |
| `warning(message)` | Toast amarillo |
| `info(message)` | Toast azul |

CSS classes: `snack-success`, `snack-error`, `snack-warning`, `snack-info`

---

## 9. `app-amount-display`

Selector: `<app-amount-display>`

Propósito: Muestra importes monetarios formateados con moneda.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `value` | `number` | `0` | Importe |
| `currency` | `string` | `'EUR'` | Código de moneda |
| `showSign` | `boolean` | `false` | Muestra signo +/- |
| `mono` | `boolean` | `true` | Usa fuente Roboto Mono |

`data-testid`: `amount-{suffix}`

---

## 10. `app-category-badge`

Selector: `<app-category-badge>`

Propósito: Badge de categoría con color semántico.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `tipo` | `'Pago' \| 'Factura' \| 'Activo' \| 'Inactivo' \| string` | — | Tipo/categoría |
| `estado` | `string` | — | Estado del flujo |

`data-testid`: `badge-{tipo}-{estado}`

---

## 11. `app-breadcrumbs`

Selector: `<app-breadcrumbs>`

Propósito: Breadcrumbs de navegación.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `items` | `{label, route?}[]` | `[]` | Items de breadcrumb |

`data-testid`: `breadcrumbs`

---

## 12. `app-state-badge`

Selector: `<app-state-badge>`

Propósito: Badge de estado para el flujo de aprobación.

| Input | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `estado` | `string` | — | Estado (Aprobado, Rechazado, Pendiente, etc.) |
| `paso` | `string` | — | Paso del flujo (PM, Backoffice, FICO, etc.) |

Clases CSS: `sig-badge sig-badge--{estado}`

---

## 13. `app-page-skeleton`

Selector: `<app-page-skeleton>`

Propósito: Skeleton loading placeholder para páginas.

`data-testid`: `skeleton`

---

## 14. Consejos de uso

- Todos los componentes shared son standalone
- Usar `data-testid` en cada elemento interactivo para E2E
- Los colores semánticos se definen en `styles.scss` como variables CSS
- No mezclar iconos: siempre `material-symbols-outlined`
