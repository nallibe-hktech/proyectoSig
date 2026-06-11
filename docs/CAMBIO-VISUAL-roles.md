# Cambio visual: roles

Tipo: Actualización de diseño UI (sin nuevos RFs)
Archivo: `frontend/public/penpot-design-roles.svg`
Acción requerida: Designer y Frontend deben leer el SVG y actualizar el componente Angular correspondiente para que coincida con el nuevo diseño.

## RFs afectados (existentes, sin cambios estructurales)

- **RF-C06** — CRUD Role/Department/CostCenter (admin)
- **RF-G01** — Filtrado por ownership (Administrator ve todo; Auditor solo lectura)

## Componente Angular afectado

- `frontend/src/app/features/admin/roles/` (ruta lazy-loaded bajo `/admin/roles`).
- Service consumido: `RoleService` → `GET /api/roles` (endpoint #32 en `docs/ARQUITECTURA.md` §7).

## Endpoints relacionados (sin cambio)

- `GET /api/roles` → `RoleDto[]` (Administrator, Auditor).
- Read-only en el MVP: los Roles son catálogo seed inmutable (`Administrator`, `Direction`, `Fico`, `Backoffice`, `ProjectManager`, `Auditor`, `Reader`). No se exponen POST/PUT/DELETE de Role en esta fase.

## Entidades/endpoints

Sin cambios.

## Notas para Designer

- Respetar paleta `--mat-sys-primary: #1F4E78` (SIG navy), `--mat-sys-secondary: #2E5C8A` (light), `--mat-sys-tertiary: #163A52` (dark).
- Tabla MatTable con columnas: nombre del rol, descripción, número de usuarios asignados (calculado en backend si se decide ampliar el DTO).
- Vista solo lectura: sin botones de "Nuevo", "Editar", "Eliminar". Botón único "Ver usuarios" filtra `/admin/usuarios?roleId=...`.

## Notas para Frontend

- Aplicar `data-testid="roles-table"`, `data-testid="roles-row-{roleId}"` para los E2E.
- No introducir endpoints nuevos. No modificar `RoleService`.
- Validar accesibilidad WCAG 2.1 AA: contraste de texto sobre fondo navy ≥ 4.5:1.
