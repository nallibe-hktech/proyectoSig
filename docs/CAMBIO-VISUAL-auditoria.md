# Cambio visual: auditoría

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-auditoria.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-F01** — AuditLog completo en transacción
- **RF-F02** — Consulta AuditLog paginado con filtros

## Componente Angular afectado

- `frontend/src/app/features/audit/` (ruta lazy-loaded bajo `/auditoria`).
- Incluye: `AuditLogComponent` con tabla paginada, filtros, y vista de detalle de cambios.

## Endpoints relacionados (sin cambio)

- `GET /api/audit` — listado paginado con filtros (`?usuarioId=&entidad=&accion=&desde=&hasta=`)
- Acceso solo para roles: Administrator, Auditor

## Elementos UI identificados en el diseño

- **Panel de filtros** (expansión vertical o barra horizontal):
  - Selector de Usuario
  - Selector de Entidad (Usuario, Proyecto, Concepto, Aprobación, etc.)
  - Selector de Acción (Crear, Actualizar, Eliminar, Aprobar, Rechazar)
  - Filtro de fecha: Desde / Hasta (datepicker)
  - Botón "Buscar" + "Limpiar filtros"
- **Tabla de auditoría** (`mat-table`):
  - Columnas: Fecha/Hora, Usuario, Entidad, ID Entidad, Acción, IP
  - Color coding por acción: Crear (verde), Actualizar (azul), Eliminar (rojo), Aprobar (verde), Rechazar (rojo)
- **Detalle de cambios** (expandable row o modal):
  - Vista diff de OldValue vs NewValue (JSON formateado)
  - Sintaxis resaltada para los cambios
- **Paginación**: número total de registros, selector de página

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- El diff de cambios debe ser legible: mostrar solo los campos que cambiaron, no el objeto completo.
- Los filtros deben permitir combinaciones (ej: todas las acciones de un usuario en una fecha).
- Color coding: Create `#70AD47`, Update `#1F4E78`, Delete `#D32F2F`, Approve `#2E5C8A`, Reject `#FFC107`.

## Notas para Frontend

- Aplicar `data-testid="audit-table"`, `data-testid="audit-row-{auditId}"`.
- Aplicar `data-testid="audit-filters"`, `data-testid="audit-search"`, `data-testid="audit-clear"`.
- Las filas expandibles deben mostrar el diff en un formato legible (no raw JSON).
- Validar accesibilidad WCAG 2.1 AA.
