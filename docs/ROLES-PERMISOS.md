# ROLES-PERMISOS — SIG · Matriz de roles y autorización

> Documento complementario de `docs/ARQUITECTURA.md`. Define los roles del sistema, sus permisos sobre cada módulo y las reglas de autorización.

---

## Roles del sistema

| Rol | Descripción | Código backend |
|---|---|---|
| Administrador | Acceso total. Gestión de maestros, usuarios, configuración, sincronización. | `Administrator` |
| Dirección | Aprobación final de cierres (paso 4 del flujo). Visión global KPIs. | `Direction` |
| FICO | Aprobación financiera (paso 3 del flujo). | `Fico` |
| Backoffice | Validación de datos, gestión de conceptos, periodos (paso 2 del flujo). | `Backoffice` |
| ProjectManager | Gestión de sus proyectos asignados, inicio de cierres (paso 1 del flujo). | `ProjectManager` |
| Auditor | Solo lectura de AuditLog, CalculationLog. Sin acciones de aprobación. | `Auditor` |
| Reader | Solo lectura de información operativa. Sin acciones de escritura ni aprobación. | `Reader` |

---

## Matriz de permisos por módulo

| Módulo | Admin | Direction | Fico | Backoffice | PM | Auditor | Reader |
|---|---|---|---|---|---|---|---|
| **Dashboard** | R | R | R | R | R | R | R |
| **Clientes** | CRUD | R | R | CRUD | R | R | R |
| **Proyectos** | CRUD | R | R | CRUD | R (ownership) | R | R |
| **Acciones** | CRUD | R | R | CRUD | R (ownership) | R | R |
| **Conceptos** | CRUD | R | R | CRUD | R | R | R |
| **Periodos** | CRUD | R | R | CRUD | R | R | R |
| **Cierres** | CRUD | Aprobar (paso 4) | Aprobar (paso 3) | Aprobar (paso 2) | Crear + Aprobar (paso 1) | R | R |
| **Aprobaciones** | CRUD | Aprobar/Rechazar | Aprobar/Rechazar | Aprobar/Rechazar | Aprobar/Rechazar | R | R |
| **Contabilidad (Export)** | R | R | R | — | — | — | — |
| **Usuarios** | CRUD | — | — | — | — | R | — |
| **Roles** | R | — | — | — | — | R | — |
| **Departamentos** | CRUD | — | — | — | — | R | — |
| **CECOs** | CRUD | — | — | CRUD | — | R | — |
| **Sincronización** | Ejecutar | — | — | — | — | — | — |
| **Auditoría** | R | R | — | — | — | R | — |
| **Regenerar Seed** | Dev-only | — | — | — | — | — | — |

> **Leyenda:** CRUD = Crear/Leer/Actualizar/Eliminar | R = Solo lectura | — = Sin acceso

---

## Flujo de aprobación (secuencia por rol)

El cierre (`Closure`) avanza secuencialmente por 5 pasos. Cada paso requiere un rol específico:

```
Paso 1: ProjectManager  →  Inicia/recalcula el cierre
       ↓
Paso 2: Backoffice      →  Valida datos y cálculos. Puede devolver al PM.
       ↓
Paso 3: Fico            →  Aprueba financieramente. Puede devolver a Backoffice.
       ↓
Paso 4: Direction       →  Aprueba finalmente. Puede devolver a Fico.
       ↓
Paso 5: SystemExports   →  Sistema genera ficheros A3 automáticamente.
```

### Reglas de transición

| Acción | Rol | Desde estado | Hacia estado | Condiciones |
|---|---|---|---|---|
| Iniciar cierre | ProjectManager, Backoffice, Admin | — | Borrador | Period abierto, no existe closure duplicado |
| Recalcular | ProjectManager, Backoffice, Admin | Borrador/Rechazado | EnAprobacion (paso 1) | Period abierto |
| Aprobar paso 1 | ProjectManager | paso 1 | paso 2 | Coincide paso actual con su rol |
| Aprobar paso 2 | Backoffice | paso 2 | paso 3 | Coincide paso actual |
| Rechazar paso 2 | Backoffice | paso 2 | paso 1 (vuelta PM) | Motivo obligatorio |
| Aprobar paso 3 | Fico | paso 3 | paso 4 | Coincide paso actual |
| Rechazar paso 3 | Fico | paso 3 | paso 2 (vuelta Backoffice) | Motivo obligatorio |
| Aprobar paso 4 | Direction | paso 4 | paso 5 (SystemExports) | Coincide paso actual |
| Rechazar paso 4 | Direction | paso 4 | paso 3 (vuelta Fico) | Motivo obligatorio |
| Exportar A3 | Admin, Fico, Direction | Aprobado | Exportado | Closure en estado Aprobado |

---

## Regla de ownership (RF-G01)

La plataforma implementa **filtrado por ownership** para el rol `ProjectManager`:

| Rol | Regla |
|---|---|
| Administrator | Ve TODO. Sin restricción de ownership. |
| Direction | Ve TODO. Sin restricción. |
| Fico | Ve TODO. Sin restricción. |
| Backoffice | Ve TODO. Sin restricción. |
| ProjectManager | Solo ve entidades donde tiene asignación (`ProjectUser`, `ActionUser`). |
| Auditor | Ve TODO (solo lectura). |
| Reader | Ve TODO (solo lectura). |

Implementación en repositorios:
```csharp
Task<Project?> GetByIdAndUsuarioIdAsync(int id, int usuarioId, CancellationToken ct);
Task<PagedResult<Project>> ListPaginatedForUserAsync(int usuarioId, int page, int pageSize, ...);
```

---

## Autorización en endpoints

| Endpoint | Autorización |
|---|---|
| POST `/api/auth/login` | `[AllowAnonymous]` |
| POST `/api/auth/refresh` | `[AllowAnonymous]` |
| GET `/api/health` | `[AllowAnonymous]` |
| GET `/api/auth/me` | `[Authorize]` |
| POST `/api/auth/logout` | `[Authorize]` |
| GET `/api/clients` | `[Authorize(Roles = "Administrator,Direction,Fico,Backoffice,ProjectManager,Auditor,Reader")]` |
| POST `/api/clients` | `[Authorize(Roles = "Administrator")]` |
| PUT/DELETE `/api/clients/{id}` | `[Authorize(Roles = "Administrator")]` |
| GET `/api/projects` | `[Authorize]` |
| POST/PUT `/api/projects` | `[Authorize(Roles = "Administrator,Backoffice")]` |
| DELETE `/api/projects/{id}` | `[Authorize(Roles = "Administrator")]` |
| POST/PUT `/api/actions` | `[Authorize(Roles = "Administrator,Backoffice")]` |
| DELETE `/api/actions/{id}` | `[Authorize(Roles = "Administrator")]` |
| POST/PUT `/api/concepts` | `[Authorize(Roles = "Administrator,Backoffice")]` |
| DELETE `/api/concepts/{id}` | `[Authorize(Roles = "Administrator")]` |
| GET `/api/users` | `[Authorize(Roles = "Administrator,Auditor")]` |
| POST/PUT/DELETE `/api/users` | `[Authorize(Roles = "Administrator")]` |
| POST `/api/periods` | `[Authorize(Roles = "Administrator")]` |
| POST `/api/periods/{id}/cerrar\|reabrir` | `[Authorize(Roles = "Administrator")]` |
| POST `/api/closures` | `[Authorize(Roles = "ProjectManager,Backoffice,Administrator")]` |
| POST `/api/closures/{id}/aprobar` | `[Authorize]` + validación interna de paso actual |
| POST `/api/closures/{id}/rechazar` | `[Authorize(Roles = "Backoffice,Fico,Direction")]` + validación paso |
| POST `/api/sync/{system}` | `[Authorize(Roles = "Administrator")]` |
| GET `/api/exports/a3-*` | `[Authorize(Roles = "Administrator,Fico,Direction")]` |
| GET `/api/audit` | `[Authorize(Roles = "Administrator,Auditor")]` |
| GET `/api/roles` | `[Authorize(Roles = "Administrator,Auditor")]` |
| POST `/api/dev/regenerar-seed` | `[Authorize(Roles = "Administrator")]` + entorno Dev |

---

## Implementación backend

### Claims en JWT

```json
{
  "nameid": "123",           // User.Id
  "email": "user@sig.local",
  "role": ["ProjectManager", "Interlocutor"]
}
```

### RoleGuard en Angular

```typescript
export const roleGuard = (allowedRoles: string[]): CanActivateFn => {
  return () => inject(AuthService).hasAnyRole(allowedRoles);
};
```

Uso en rutas:
```typescript
{
  path: 'admin',
  component: AdminLayoutComponent,
  canActivate: [roleGuard(['Administrator'])]
}
```

### Data-testid en UI

Cada elemento interactivo lleva `data-testid` con convención `<entidad>-<accion>` o `<entidad>-<campo>`:

```html
<button data-testid="client-create">Nuevo Cliente</button>
<input data-testid="client-nombre" formControlName="nombre" />
```

---

> **Nota:** Este documento complementa a `docs/ARQUITECTURA.md`. La verdad única vinculante es ARQUITECTURA.md.
