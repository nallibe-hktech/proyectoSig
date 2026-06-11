# ARQUITECTURA — Módulo `registro` (autoservicio de alta de usuario)

> Módulo **propuesto — pendiente de implementación**.
> Detectado a partir de `frontend/public/penpot-design-registro.svg` (no existía en `context_SIG_es.md`).
> El stack se hereda de `docs/ARQUITECTURA.md` (sin cambios).

---

## 0. Origen

- Diseño nuevo: `frontend/public/penpot-design-registro.svg`.
- En el contexto original SIG-ES sólo existe `RF-A01` (login). No hay pantalla ni endpoint de alta autoservicio.
- Los usuarios reales se crean por Administrator vía `POST /api/users` (RF-C05). Esta pantalla introduce una **nueva ruta de alta** que requiere decisión de producto antes de implementar.

---

## 1. Decisión pendiente (DECISION REQUIRED)

Tres alternativas, deben validarse con Product Owner (Eladio / Sergio) antes de codificar:

| Opción | Descripción | Impacto |
|---|---|---|
| **A. Solo Administrator (status quo)** | Descartar la pantalla `registro`. El SVG se trata como mockup interno (admin/usuarios alta). | Cero cambios técnicos. |
| **B. Auto-registro con aprobación** | Anónimo POST → User en estado `PendienteAprobacion` → Administrator confirma activación. | Nueva entidad opcional, nuevo flujo, nuevo endpoint, nuevo email. |
| **C. Auto-registro con invitación** | Administrator envía token por correo → URL `/registro?token=...` → usuario completa contraseña y NIF. | Nuevo flujo más seguro; tokens con expiración (24h). |

> Recomendación del Arquitecto: **Opción C** (invitación). Mantiene control de acceso, no expone alta anónima al público.

Mientras no haya decisión, este documento queda como **propuesta**. NO se generan migraciones ni código.

---

## 2. RF propuestos (solo si se aprueba)

| ID | Descripción | Prioridad |
|---|---|---|
| `RF-A04` | Administrator emite invitación con email y rol → genera `InvitationToken` (SHA-256, 24h TTL) | Media |
| `RF-A05` | Usuario invitado entra a `/registro?token=...` y completa NIF, nombre, apellidos, contraseña | Media |
| `RF-A06` | Sistema valida token, NIF y unicidad de email; crea `User` activo con roles del token | Media |
| `RF-A07` | Token consumido se marca `Usado=true`, inmutable | Media |

---

## 3. Entidad nueva

```csharp
public class InvitationToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = null!; // SHA-256, único
    public string Email { get; set; } = null!;
    public int InvitadoPorUserId { get; set; }     // FK User
    public List<int> RoleIds { get; set; } = new();// JSON int[]
    public DateTime ExpiraEn { get; set; }         // UTC, +24h
    public bool Usado { get; set; }
    public DateTime? UsadoEn { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- Soft delete: NO (inmutable).
- Concurrency: no requerido.
- Índice único en `TokenHash`.

---

## 4. Endpoints propuestos

| Método | Ruta | Auth | DTO Request | DTO Response | Códigos |
|---|---|---|---|---|---|
| POST | `/api/invitations` | Administrator | `InvitationCreateRequest { email, roleIds[] }` | `InvitationDto { id, email, expiraEn }` | 201/400/401/403/409 |
| GET | `/api/invitations/{token}` | Anonymous | — | `InvitationPublicDto { email, expirada, usado }` | 200/404/410 |
| POST | `/api/registro` | Anonymous | `RegistroRequest { token, nif, nombre, apellidos, password }` | `LoginResponse` (auto-login) | 201/400/410/409 |

DTOs detallados se especifican al confirmar la opción.

---

## 5. Trazabilidad

| RF | Entidad | Service | Controller | UI |
|---|---|---|---|---|
| RF-A04 | InvitationToken | `IInvitationService` | `InvitationsController` | admin/usuarios (botón Invitar) |
| RF-A05 / RF-A06 / RF-A07 | InvitationToken, User | `IInvitationService`, `IAuthService` | `RegistroController`, `AuthController` | registro (página pública) |

---

## 6. Acción requerida

1. **Product Owner**: decide opción A/B/C.
2. Si **A** → este archivo se archiva, el SVG se reetiqueta como mockup de admin/usuarios.
3. Si **B** o **C** → se promueven los RF al `docs/ARQUITECTURA.md` principal, se generan migración EF (`AddInvitationTokens`), DTOs, FluentValidation, controller y componente Angular.

Hasta entonces: **bloqueado por decisión funcional** — registrado también en `docs/BLOQUEANTES.md` como NO-bug pendiente de producto.
