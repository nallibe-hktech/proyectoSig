# Cambio visual: acciones

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-acciones.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-C03** — CRUD Action (N:M Concept, N:M User)
- **RF-G01** — Filtrado por ownership

## Componente Angular afectado

- `frontend/src/app/features/actions/` (ruta lazy-loaded bajo `/acciones`).
- Incluye: `ActionsListComponent` (tabla + filtros), `ActionFormComponent`, `ActionDetailComponent` (con sub-tabla de conceptos asociados).

## Endpoints relacionados (sin cambio)

- `GET /api/actions` — listado paginado (`?proyectoId=&clienteId=&estado=&search=`)
- `GET /api/actions/{id}` — detalle
- `POST /api/actions` — crear (Administrator, Backoffice)
- `PUT /api/actions/{id}` — actualizar
- `DELETE /api/actions/{id}` — eliminar
- `POST /api/actions/{id}/conceptos/{conceptoId}` — asociar concepto a acción
- `DELETE /api/actions/{id}/conceptos/{conceptoId}` — desasociar concepto

## Elementos UI identificados en el diseño

- Tabla `mat-table` con columnas: Nombre, Proyecto, Cliente, CECO, Departamento, Estado, Acciones
- Filtros: selector de Proyecto (o texto libre), selector de Cliente, selector de Estado
- Botón "Nueva Acción"
- **Vista detalle de acción**:
  - Información general: nombre, proyecto, cliente, departamento, CECO, estado
- **Sub-tabla "Conceptos asociados"** dentro del detalle:
  - Columnas: Concepto, Tipo (Pago/Factura badge), Desde, Hasta, Acciones (Ver/Editar/Quitar/Duplicar)
  - Botón "Añadir Concepto existente" (selector modal con búsqueda)
  - Botón "Nuevo Concepto" (navega a creación de concepto con acción pre-seleccionada)
- Sección de usuarios asignados (chips/lista)

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- El detalle debe tener la sub-tabla de conceptos claramente diferenciada como sección independiente.
- Los botones de asociar/desasociar conceptos deben ser intuitivos.
- Al añadir concepto existente, el modal debe permitir búsqueda y filtro por tipo (Pago/Factura).

## Notas para Frontend

- Aplicar `data-testid="actions-table"`, `data-testid="actions-row-{actionId}"`.
- Aplicar `data-testid="action-concepts-table"`, `data-testid="action-detail"`.
- Aplicar `data-testid="btn-add-concept"`, `data-testid="btn-new-concept"`.
- Al desasociar concepto, mostrar confirmación.
- Validar accesibilidad WCAG 2.1 AA.
