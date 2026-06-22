# A3 Innuva Nóminas — piezas pendientes de la compañera

**Fecha:** 2026-06-22 · **Rama:** `feat/pantalla-a3-erp`

El merge de `origin/main` (commit `c0108de` "motor v2 + A3 Innuva") trajo la pantalla de
A3 Innuva Nóminas, pero el push **estaba incompleto**: faltan archivos que solo existían en
la máquina de la compañera. Estado tras integrar lo recibido por OneDrive (carpeta INNUVA):

## Integrado y compilando ✅
- Pantalla frontend: `features/a3-innuva/` + `core/api/a3-innuva.service.ts`
- DTOs: `A3InnuvaNominasCompanyDto`, `A3InnuvaNominasPayrollDto`
- Cliente HTTP `A3InnuvaClient` (con las mejoras del API real: cabecera `api-version: 2`,
  `filter`/`orderBy`, ruta `/Laboral/api/`)
- Entidad `A3InnuvaOAuthToken` + migración `AddA3InnuvaOAuthToken` + `DbSet`
- **`WoltersKluwerOAuthService`** (flujo OAuth + PKCE) — recibido por OneDrive y añadido en
  `SIG.Infrastructure/Integrations/Http/WoltersKluwerOAuthService.cs`. El registro DI se
  actualizó a 7 args (añadido `IHostEnvironment env`).

## Pendiente de que la compañera suba ❌
1. **`IA3InnuvaNominasService` + `A3InnuvaNominasService`** — servicio que orquesta cliente +
   staging. Solo se referenciaba en `DependencyInjection.cs:113` (registro desactivado con TODO).
2. **`A3InnuvaNominasController`** (ruta `api/a3-innuva-nominas`) con las acciones que llama el
   frontend: `POST sync/companies`, `POST sync/payrolls`, `GET companies`, `GET payrolls`, y los
   equivalentes `test/...`. **No existe ningún controller que sirva esos endpoints.**

Sin (1) y (2) la pantalla se ve pero sus botones dan **404**.

## Al recibirlas
1. Añadir el servicio + controller en su sitio.
2. Reactivar la línea en `DependencyInjection.cs` (registro de `IA3InnuvaNominasService`).
3. `dotnet build` + verificar endpoints `api/a3-innuva-nominas/*`.

## Nota sobre el motor
El `main` de la compañera traía un motor de cálculo simplificado (sin idQuestion Celero ni
flags de excepción). En este merge **se conservó el motor de esta rama** (alineado con el Excel
CierresIntegralesSIG). Verificado: 78/78 tests de cálculo en verde tras el merge.
