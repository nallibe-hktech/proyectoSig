# Cambio visual: proyectos

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-proyectos.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-C02** — CRUD Project (N:M CostCenter, N:M User)
- **RF-G01** — Filtrado por ownership (ProjectManager solo ve proyectos asignados)

## Componente Angular afectado

- `frontend/src/app/features/projects/` (ruta lazy-loaded bajo `/proyectos`).
- Incluye: `ProjectsListComponent` (tabla + filtros), `ProjectFormComponent` (crear/editar), `ProjectDetailComponent`.

## Endpoints relacionados (sin cambio)

- `GET /api/projects` — listado paginado (`?clienteId=&estado=&search=`)
- `GET /api/projects/{id}` — detalle
- `POST /api/projects` — crear (Administrator, Backoffice)
- `PUT /api/projects/{id}` — actualizar
- `DELETE /api/projects/{id}` — eliminar (Administrator)

## Elementos UI identificados en el diseño

- Tabla `mat-table` con columnas: Nombre, Cliente, CECO(s), Estado, Interlocutor, Fecha Alta, Acciones
- Barra de búsqueda con filtro por texto libre
- Filtro por Cliente (selector desplegable) y Estado (radio/chips: Todos, Activo, Pausado, Cerrado)
- Botón "Nuevo Proyecto"
- Formulario de proyecto con:
  - Nombre (texto), Cliente (selector), Estado (selector)
  - CECO(s) multi-select con search
  - Interlocutor: Nombre, Email, Teléfono
  - Usuarios asignados multi-select
  - Fecha Alta (datepicker)
- Vista detalle con pestañas: Información general, Acciones asociadas, Cierres del proyecto

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- La paleta debe coincidir con los prototipos existentes (primary `#1F4E78`).
- La selección multi de CECO debe permitir búsqueda por código o nombre.
- En el detalle, la pestaña "Acciones asociadas" debe mostrar enlace a la pantalla de acciones filtrada.

## Notas para Frontend

- Aplicar `data-testid="projects-table"`, `data-testid="projects-row-{projectId}"`.
- Aplicar `data-testid="project-form"`, `data-testid="project-save"`, `data-testid="project-cancel"`.
- El filtro por ownership se aplica automáticamente en backend según el rol JWT.
- Validar accesibilidad WCAG 2.1 AA.
