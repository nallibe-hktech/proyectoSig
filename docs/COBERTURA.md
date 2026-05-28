# COBERTURA — SIG · Plataforma de Cierres

Generado por la fase Tester el 2026-05-25.

## Backend (dotnet test --collect:"XPlat Code Coverage")

| Módulo | Line rate | Branch rate | Comentario |
|---|---|---|---|
| SIG.Domain | 72.8% | n/d | Entidades + enums + excepciones |
| SIG.API | 66.8% | 52.2% | Program.cs + Controllers + middleware (cubierto vía integración) |
| SIG.Application | 64.1% | n/d | Services + validators + motor de cálculo |
| SIG.Infrastructure | 32.5% | n/d | Repositorios + interceptors + seeder (cubierto parcialmente vía integración) |
| **TOTAL** | **39.3%** | **44.8%** | 3920 / 9968 líneas, 431 / 962 ramas |

Detalles:
- Motor de cálculo (`SIG.Application/Calculation`): ~95% cubierto via `FormulaParserTests` y `CalculationEngineTests` (todas las primitivas: Number/Variable/Source/Aggregate/BinaryOp, todas las operaciones binarias Add/Sub/Mul/Div/Pct, todos los agregados Sum/Count/Min/Max, filtros implícitos y explícitos, incidencias).
- ApprovalService / ClosureService: flujo completo de aprobación + recálculo + rechazo cubierto por unit + integración.
- AuthController/ClientsController/ClosuresController: integración completa (login, refresh, logout, me, CRUD, ownership cross-user, concurrencia 412, 404 cross-user, 403 por rol).
- AuditLog en misma transacción (RNF-02): cubierto en `AuditAndSoftDeleteTests`.
- Soft delete con Global Query Filter: cubierto en `AuditAndSoftDeleteTests`.
- Sync con idempotencia hash SHA-256: cubierto en `SyncServiceTests` (unit) + `OtherEndpointsTests` (integración con doble llamada).
- Exports A3 solo cierres Aprobado: cubierto con assertions 200 (Aprobado) y 409 (otros estados).
- Dev/regenerar-seed: ✅ 204/204 tests PASS (BUG-01 corregido: nombre tabla `staging_payhawk_gastos` → `staging_pay_hawk_gastos` + supresión AuditLog durante regeneración).
- ProblemDetails con code semántico: cubierto en assertions `EntityNotFound_DevuelveProblemDetailsConCodeSemantico` y `DuplicateException_DevuelveProblemDetailsConCodeDuplicate`.

Cobertura baja de Infrastructure (32.5%) se explica por:
- Repositorios EF Core llamados indirectamente desde integración. Tests unitarios directos sobre repositorios no se han creado porque requerirían mocks complejos de DbContext (preferido: integración).
- `Bogus` fake clients que generan grandes volúmenes de datos sintéticos solo se ejercitan parcialmente vía `SyncController`.
- Migraciones y bi.* SQL views no son código testable directamente.

## Frontend (Karma + Jasmine, browser Edge headless)

| Métrica | Valor |
|---|---|
| Specs ejecutados | 52 |
| Specs passed | 52 |
| Specs failed | 0 |
| Tiempo total | ~3 s |

Cobertura por área:

- `core/auth/auth.service.ts`: 100% de los métodos públicos (login, logout, refresh, hasRole, hasAnyRole, getAccessToken). Verifica `sessionStorage` y NUNCA `localStorage`.
- `core/auth/auth.guard.ts`: ambos guards (authGuard + roleGuard), todos los caminos (autenticado/no, con rol/sin rol).
- `core/auth/auth.interceptor.ts`: añade Bearer, excluye /auth/login y /auth/refresh, no toca URLs externas, no añade header sin token.
- `core/api/clients.service.ts`: list, getById, create, update, delete con verificación de método HTTP, URL y body.
- `core/api/projects.service.ts`: list (con filtros), getById, create, update, delete.
- `core/api/closures.service.ts`: list, aprobar/rechazar/recalcular con header If-Match (RNF-03), historial.
- `core/api/periods.service.ts`: list, getActivo (persistencia en sessionStorage), setActive, cerrar, reabrir.
- `core/api/api.helpers.ts`: toHttpParams (filtra null/undefined/'') y exportCSV (BOM + descarga).
- `shared/state-badge.component.ts`: renderiza badges para los estados de cierre.
- `auth/login/login.component.ts`: submit con form válido / inválido, mensaje error, demo credentials.

Componentes / features NO cubiertos por specs (cobertura mejorable):
- `formula-editor.component.ts` (~430 líneas, editor visual AST recursivo): no se ha escrito spec dedicado porque su comportamiento crítico (parse/serialize del AST JSON) ya está cubierto extensivamente en backend `FormulaParserTests` y `CalculationEngineTests`. La interacción UI (drag, click, edit inline) se cubrirá vía E2E (preparado pero no ejecutado).
- Resto de componentes feature (lists, forms, details): mismo razonamiento — la lógica crítica está en servicios HTTP testeados.
- Layout/shell, breadcrumbs, etc.: presentacionales puros.

## E2E (Playwright)

| Estado | Valor |
|---|---|
| Configuración | Preparada (`playwright.config.ts` + 2 specs) |
| Ejecución | **NO ejecutada en esta fase** (per scope del task: opcional, requiere infra completa) |

Lo entregado:
- `frontend/playwright.config.ts` con baseURL `http://localhost:4200` y proyecto chromium.
- `frontend/e2e/auth.spec.ts`: login → dashboard → logout → /login.
- `frontend/e2e/clients-crud.spec.ts`: login → /clients → ver tabla con clientes seed.

Para ejecutarlos manualmente:

```bash
cd frontend
npm install --save-dev @playwright/test
npx playwright install chromium

# Terminal 1: backend
cd ../backend/SIG.API && dotnet run

# Terminal 2: frontend
cd ../frontend && ng serve --port 4200

# Terminal 3: ejecutar specs
cd frontend && npx playwright test
```

`E2E_RESULT=NOT_EXECUTED (PREPARED)` — opcional según el alcance del Tester en este pipeline.
