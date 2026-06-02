# SUPOSICIONES_CRITICAS — SIG · Decisiones autónomas del Arquitecto

> Documento complementario de `docs/ARQUITECTURA.md` (§11). Enumera cada suposición crítica del diseño, su justificación y el impacto si resulta incorrecta.

---

## Infraestructura y puertos

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-A01 | Backend escucha en `http://localhost:5180` y `https://localhost:5181` | Puertos fijados en `launchSettings.json` para evitar colisiones y predecir URLs en frontend/tests | Frontend no conecta; E2E fallan; hay que reconfigurar CORS y proxies |
| SUP-A02 | Frontend sirve en `http://localhost:4200` | Puerto estándar Angular CLI | Proxy inverso o CORS mal configurado |
| SUP-A03 | Base de datos local en `localhost:5432` | PostgreSQL puerto estándar | Conexión falla; environment probe lo detecta |

## Base de datos y credenciales

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-B01 | PostgreSQL local con usuario `postgres` y password `admin` | Detectado en environment probe del 2026-05-25 | Seed y migraciones fallan; hay que actualizar `appsettings.*.json` |
| SUP-B02 | Las tres bases de datos (dev/test/e2e) comparten misma password | Conveniencia desarrollo | Solo afecta a cadenas de conexión |
| SUP-B03 | Npgsql 9.0.4 es compatible con PostgreSQL 16.12 | Documentado en NUGET_VERSIONS.md | Migrations o queries fallan; actualizar driver |

## Autenticación y seguridad

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-C01 | JWT access token expira en 30 min; refresh token en 7 días | Valores estándar industria; refresh token con hash SHA-256 | Sesiones demasiado cortas/largas; ajustar en configuración |
| SUP-C02 | UserId del JWT se lee de `ClaimTypes.NameIdentifier` y nunca del body | RNF-05: API stateless. Regla del sistema multiagente | Violación de seguridad si algún endpoint lee userId del body |
| SUP-C03 | Entra ID queda fuera de MVP; JWT propio con BCrypt suficiente | Vinculante por contexto INPUT_APP | Migración a SSO requiere nuevo middleware y flujo de login |

## Motor de cálculo

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-D01 | AST JSON de fórmula con tipos `Number`, `Variable`, `Source`, `Aggregate`, `BinaryOp` | Diseño suficiente para cubrir taxonomía de tipos de concepto del cliente | Fórmulas complejas no representables; extender AST |
| SUP-D02 | Datasets de staging se cargan en memoria una vez por Closure | Rendimiento: O(L×D) para 1000 líneas < 5s estimado | Si dataset > 10K filas, timeout; pasar a evaluación lazy |
| SUP-D03 | Variables de Celero se resuelven contra `MapeoValoresJson` | Patrón questionId → array {respuesta, valor} | Esquema de respuestas distinto al esperado; adaptar parser |

## Exportaciones A3

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-E01 | A3 Innuva acepta XML con nodos `Nomina → Empleado → Conceptos → Totales` | Estructura razonable sin confirmación contractual | Formato real distinto; reescribir export service |
| SUP-E02 | A3 ERP acepta XML con nodos `Factura → Cliente → Lineas → Totales` | Misma nota que E01 | Formato real distinto; reescribir export service |
| SUP-E03 | Exportación solo desde estado `Aprobado` | Lógica de negocio: no se exportan cierres no aprobados | Si necesitan exportar borradores, relajar validación |
| SUP-E04 | Ficheros se descargan vía HTTP (no SFTP/email) | MVP sin pipeline de entrega automatizada | Necesario canal adicional en Fase 2 |

## Concurrencia y auditoría

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-F01 | `xmin` PostgreSQL funciona como rowVersion para concurrency token en Closure y ClosureLine | Patrón Npgsql estándar; `IsRowVersion()` + `HasColumnType("xid")` | Si xmin no es fiable, implementar concurrency con columna `RowVersion` propia (int o GUID) |
| SUP-F02 | Optimistic concurrency suficiente; no hay locking pesimista | Cierres aprobados por un usuario a la vez en flujo secuencial | Dos usuarios aprobando simultáneo → uno recibe 412 PreconditionFailed |
| SUP-G01 | AuditLog se inserta en misma transacción que la operación vía `SaveChangesInterceptor` | RNF-02: garantía transaccional | Si interceptor falla, operación no se completa; implementar outbox pattern |
| SUP-G02 | AuditLog tabla bigserial (long) suficiente para el volumen esperado | ~50 operaciones/día × 365 días × 5 años ≈ 91K filas | Si supera 2B filas, migrar a tabla particionada |

## Ownership y permisos

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-H01 | Administrator/Direction/Fico/Backoffice/Auditor/Reader ven TODO; ProjectManager solo lo asignado | Regla de negocio establecida por stakeholders | Si algún rol necesita ownership parcial, ajustar repositorios |
| SUP-H02 | ProjectManager se asigna a proyectos vía `ProjectUser` | N:M Project ↔ User | Si asignación es por cliente o acción, cambiar lógica |
| SUP-H03 | `GetByIdAndUsuarioIdAsync` es el patrón obligatorio para filtrado de ownership | Convención del sistema multiagente | Repositorios sin este filtro exponen datos a usuarios no autorizados |

## Desarrollo y testing

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-I01 | Endpoint `/api/dev/regenerar-seed` protegido por doble guard: `IHostEnvironment.IsDevelopment()` + `Features:AllowSeedRegeneration=true` | Evita ejecución accidental en producción | Si un guard falla, seed peligroso en producción |
| SUP-I02 | Integraciones fake activadas con `Integrations:UseFake=true` en Development | MVP sin conexión real a sistemas externos | Si UseFake=false sin APIs configuradas, SyncService devuelve 502 |

## Infraestructura técnica

| ID | Suposición | Justificación | Impacto si incorrecta |
|---|---|---|---|
| SUP-K01 | Flujo de aprobación secuencial hardcoded en C# (5 pasos fijos) | Suficiente para MVP; Fase 2 puede requerir motor de reglas | Si el orden cambia por cliente, modificar ApprovalStep y transiciones |
| SUP-L01 | Angular Material 21 paleta `mat.$azure-palette` como base con overrides CSS `--mat-sys-*` para SIG navy (#1F4E78) | Material 3 Theming; custom paleta no disponible sin build tokens | Tema visual incorrecto; Designer debe generar paleta personalizada |
| SUP-M01 | Soft delete solo en entidades maestro (User, Client, Project, Action, Concept, Variable, Department, CostCenter) | Las transaccionales (Closure, ClosureLine, Approval, etc.) son inmutables o no se borran lógicamente | Si se requiere borrado lógico de transaccionales, añadir ISoftDeletable |
| SUP-M02 | `JsonStringEnumConverter` global para todos los enums | Convención del sistema | Frontend debe usar strings alineados con nombres C# |
| SUP-M03 | Base de datos en español de UI (nombres de tablas en español, columnas en inglés) | Convención establecida en INPUT_APP | Confusión si se mezclan idiomas en queries SQL |
| SUP-M04 | NIF español con letra de control verificable (regex) | Requisito fiscal | Si cliente acepta NIFs extranjeros, relajar validación |
| SUP-M05 | Fechas de negocio como `DateOnly`; timestamps como `DateTime.UtcNow` con `Kind=Utc` | Gotcha Npgsql: rechaza DateTime con Kind distinto de Utc | Bug en producción si se persiste DateTime con Kind=Local o Unspecified |
| SUP-M06 | Pool de conexiones PostgreSQL con `MaxPoolSize=100` | Valor por defecto Npgsql; suficiente para 50 usuarios concurrentes | Agotamiento de conexiones; aumentar pool o implementar retry |

---

## Histórico de revisiones

| Fecha | Versión | Cambio |
|---|---|---|
| 2026-05-25 | 1.0 | Creación inicial por Arquitecto |
