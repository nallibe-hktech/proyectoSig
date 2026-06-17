# PENDIENTE DE SUBIDA — handoff para revisión + push

> **Fecha:** 2026-06-16
> **Estado del repo:** `main` local == `origin/main` (sincronizado). Todo lo de abajo está **solo en local, SIN commit ni push**.
> **Propósito de este doc:** (1) que mi compañera revise qué se quiere subir y por qué; (2) si le convence, decírmelo (p. ej. *"lee `docs/PENDIENTE_SUBIDA.md` y haz la subida"*) y yo ejecuto el plan de la sección 4.

---

## 1. Resumen en una línea

Se aplicó una tanda de **fixes de funcionalidad + actualización de documentación + limpieza de código muerto** tras el refactor Project→Service. Está todo en el working tree, **compila** (backend y frontend), y **falta commitearlo y subirlo**. La auditoría completa de la que salió esto está resumida en la sección 5.

---

## 2. QUÉ ESTÁ HECHO (local, pendiente de subir)

### A. Fixes de funcionalidad
- **Frontend — llamadas `/api/` que no recibían token (era ALTA):** los componentes de Celero/Galán/Mediapost llamaban a URLs `'/api/...'` hardcodeadas; el `authInterceptor` solo añade el `Bearer` si la URL empieza por `environment.apiUrl`, así que iban **sin token (401)** y en prod ni apuntaban al backend.
  - Nuevo `frontend/src/app/core/api/celero.service.ts`; ampliados `galan.service.ts` y `mediapost.service.ts`.
  - Componentes migrados a usar esos servicios (con `environment.apiUrl`). Eliminados `.toPromise()` deprecados; corregido un campo inexistente (`filasInsertadas`→`registrosInsertados`).
- **Backend — rol fantasma "Admin SIG":** varios `[Authorize(Roles="...,Admin SIG")]` referenciaban un rol que **nunca se siembra** ("Admin SIG" es en realidad el *nombre* del usuario admin; su rol es `Administrator`). Eliminada la referencia fantasma en 5 `[Authorize]` + 1 `User.IsInRole`. **No** se tocó el seed.
- **Backend — integraciones (degradación elegante):** Mediapost devuelve vacío si la carpeta de origen no existe (en vez de 500); PayHawk omite la llamada si `AccountId` no está configurado (en vez de 400). *(Estos dos ya estaban en el working tree de antes; se suben con el resto.)*
- **Tests — `appsettings.Testing.json`:** estaba gitignored y NO versionado → un checkout limpio no podía correr los tests de integración. Creado y versionado (`git add -f`) apuntando a **`:5432`** (Postgres nativo de dev), no al `:5433` de Docker. De paso, arreglado un test ya roto de antes (`ClosureServiceTests` le faltaba el mock del nuevo constructor de closure alerts).

### B. Documentación actualizada al modelo Cliente→Servicio→Concepto
La doc seguía describiendo el modelo viejo (Project/Action). Actualizados 9 ficheros: `ARQUITECTURA.md` (fuente de verdad: glosario, entidades, diagrama, DTOs + entidad `ClosureAlerta`), `DATA-MODEL.md`, `API-SPEC.md`, `ROLES-PERMISOS.md`, `INTEGRACIONES.md` (conservando los campos externos de Intratime por fidelidad al API ajeno), `DISENO.md`, `PROGRESO_BACKEND.md`, `PROGRESO_FRONTEND.md`, y nota de cabecera en `ESTADO_ACTUAL.md`.

### C. Limpieza
- Borrado el árbol muerto del editor de fórmula (`features/calculations/components|services|models`, 11 ficheros). Conservados el canónico `concepts/formula-editor` y el vivo `calculation-detail`.
- Borrado `features/shared/list-with-search-import.example.ts` (importaba tipos inexistentes del refactor).
- Limpiada terminología "Proyecto/Acción" en `roles-list` y `audit`.
- Borrados `docs/SONAR_ISSUES.md` (obsoleto, reaparecido) y los `resultado-*.md` de raíz.

### Verificación
- `dotnet build backend/SIG.slnx` → **0 errores / 0 warnings**.
- `tsc --noEmit` + `ng build --configuration development` → **exit 0**.
- ⚠️ **No** se corrió la suite de tests (requiere Postgres levantado). Conviene correrla antes del merge.

---

## 3. QUÉ FALTA EN EL REPO Y ES NECESARIO

Lo necesario e inmediato es **subir todo lo de la sección 2** (commit + push + PR para revisión). Esos cambios corrigen bugs reales (token, rol) y dejan la documentación coherente con el código. Hasta que no se suba, el repo de GitHub sigue con la doc desfasada y los bugs.

**Excluido a propósito de esta subida:**
- 🔴 **#1 Seguridad SGVP** — *IGNORADO por ahora a petición del equipo.* (Credenciales reales en `SGVP/` trackeadas en git/historial → rotación + purga. Pendiente, va por separado.)
- 🟡 **Guard de carpeta en Galán** — el guard `Directory.Exists` se aplicó a Mediapost pero **falta replicarlo en `GalanCsvClient`** (3 métodos); aún puede dar 500 si falta la carpeta. *(Backlog, ver sección 5.)*

---

## 4. PLAN DE SUBIDA (lo que ejecutaré cuando me lo aprueben)

> Regla del proyecto: no subir directo a `main`. Se crea rama + PR para que mi compañera revise el código.

1. Crear rama desde `main`, p. ej. `chore/post-refactor-fixes-y-docs`.
2. Añadir el nuevo servicio sin trackear: `git add frontend/src/app/core/api/celero.service.ts` (el resto ya está modificado/staged; `backend/ExploreMediapost/` queda fuera por `.gitignore`).
3. Commits agrupados (mensajes terminados con la línea Co-Authored-By):
   - `fix(integrations): degradar a vacío Mediapost/PayHawk sin carpeta/AccountId`
   - `fix(api): unificar rol Administrator (eliminar "Admin SIG" fantasma)`
   - `test: versionar appsettings.Testing.json (:5432) + fix mock ClosureServiceTests`
   - `fix(frontend): centralizar llamadas /api en servicios con environment.apiUrl`
   - `refactor(frontend): eliminar editor de fórmula muerto, ejemplo roto y terminología Servicio`
   - `docs: actualizar documentación al modelo Cliente→Servicio→Concepto + closure alerts`
4. `git push -u origin <rama>` y abrir PR describiendo lo de la sección 2.
5. (Recomendado) correr la suite de tests con Postgres antes de mergear.

**Confirmaciones que necesito antes de ejecutar:**
- ¿Subir como rama+PR (recomendado) o algo distinto?
- ¿Nombre de rama OK o prefieres otro?
- ¿Incluyo el guard de Galán en esta misma subida o lo dejo para el backlog?

---

## 5. BACKLOG — hallazgos de la auditoría NO abordados aún

Por si se quieren planificar (orden aprox. por severidad):
- 🔴 Seguridad SGVP (rotar credenciales + purgar repo/historial + cerrar huecos `.gitignore`: `/SGVP/`, `**/appsettings-*.json`).
- 🟡 Guard `Directory.Exists` en `GalanCsvClient` (degradación elegante, falta).
- 🟡 Fallbacks de path hardcodeados `C:\dev\SIG-es\...` en `GalanClients.cs:26` y `MediapostClients.cs:23` → deberían ser `throw`/ruta neutra.
- 🟡 `GalanCsvClient`/`MediapostExcelClient` viven en namespace `Integrations.Fake` siendo clientes **reales** → mover a `Integrations/Files/`.
- 🟡 PayHawk: filtro de fechas comentado (`GetGastosAsync` ignora `desde/hasta`) + logs DEBUG con PII (email/NIF).
- 🟡 `SgpvClient.Timeout` mutado por-llamada sobre cliente de `IHttpClientFactory` → moverlo al registro.
- 🟡 Backend: sobrecargas `GetByIdAsync(int id)` sin `usuarioId` usadas para saltar ownership; 4 lecturas sin `AsNoTracking` (`GalanService.cs:174/178/182`, `AdminControllers.cs:259/280/301/326`).
- ⚪ CI/CD: acciones EOL (`upload-artifact@v3`, `setup-dotnet@v3`), jobs security/sonar placeholder, deploy "staging" usa namespace `production`, sin CI de frontend; añadir secret-scanning.
- ⚪ Higiene: `context_SIG_es.md` (histórico) y `.atl/` versionados; pipes + `CommonModule` redundantes en 12 componentes; casing `IntratimedashboardComponent`.
