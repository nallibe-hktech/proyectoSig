# Cambio visual: usuarios

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-usuarios.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-C05** — CRUD User con NIF, multi-rol, asignaciones (clientes/proyectos/acciones)
- **RF-G01** — Filtrado por ownership

## Componente Angular afectado

- `frontend/src/app/features/admin/users/` (ruta lazy-loaded bajo `/admin/usuarios`).
- Incluye: `UserListComponent` (tabla paginada + filtros), `UserFormComponent` (crear/editar con asignaciones multi-select), `UserDetailComponent`.

## Endpoints relacionados (sin cambio)

- `GET /api/users` — listado paginado con filtros (`?rol=&departamento=&estado=&search=`)
- `GET /api/users/{id}` — detalle
- `POST /api/users` — crear (solo Administrator)
- `PUT /api/users/{id}` — actualizar
- `PUT /api/users/{id}/password` — cambiar contraseña
- `DELETE /api/users/{id}` — eliminar
- `PUT /api/users/{id}/asignaciones` — actualizar asignaciones a clientes/proyectos/acciones

## Elementos UI identificados en el diseño

- Tabla `mat-table` con columnas: NIF, Nombre, Apellidos, Email, Rol(es), Departamento(s), Estado, Acciones
- Barra de búsqueda con filtro por texto libre
- Filtros adicionales: selector de rol, selector de departamento
- Botón "Nuevo Usuario" (solo visible para Administrator)
- Formulario de usuario con:
  - Campos texto: NIF, Nombre, Apellidos, Email, Contraseña (solo creación)
  - Selectores multi: Rol(es), Departamento(s)
  - Asignaciones multi-select agrupadas: Clientes, Proyectos, Acciones
  - Switch Activo/Inactivo
- Diálogo de confirmación al eliminar
- Sección de cambio de contraseña en detalle

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- Respetar paleta corporativa SIG: primary `#1F4E78`, success `#70AD47`, danger `#D32F2F`.
- Tabla paginada con `MatPaginator` y `MatSort`.
- Formulario con `mat-card` agrupado por secciones (Datos personales / Roles / Asignaciones).
- Asignaciones multi: usar `mat-select` con search interno o `mat-chip-list` con autocomplete.

## Notas para Frontend

- Aplicar `data-testid="users-table"`, `data-testid="users-row-{userId}"`.
- Aplicar `data-testid="user-form"`, `data-testid="user-save"`, `data-testid="user-cancel"`.
- Validar accesibilidad WCAG 2.1 AA.
- Password field con toggle visibility.
