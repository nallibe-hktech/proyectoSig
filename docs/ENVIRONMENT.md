# ENVIRONMENT.md — Verificación del entorno local

Generado por el Arquitecto el 2026-05-25.
Máquina: Windows 11 Enterprise 10.0.26200 — usuario VíctorJavierJiménezR.

## SDKs y tooling

| Tooling | Versión detectada | Comando |
|---|---|---|
| .NET SDK | 10.0.103 y 10.0.104 (se usará 10.0.104) | `dotnet --list-sdks` |
| Node.js | v24.14.0 | `node --version` |
| npm | 11.9.0 | `npm --version` |
| Angular CLI | 21.2.2 | `ng version` |
| PostgreSQL server | 16.12 (Visual C++ build 1944, 64-bit) | `psql --version` + `SELECT version()` |
| pg_isready | localhost:5432 — aceptando conexiones | `pg_isready` |

El stack del proyecto (PostgreSQL + .NET 10 + Angular 21 + Material 21) está completamente cubierto por las versiones instaladas. No hay bloqueantes de tooling.

## Database credentials (CRÍTICO — el Desarrollador debe usar EXACTAMENTE estos valores en appsettings*.json)

PostgreSQL local detectado y autenticado correctamente:

```
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_USER=postgres
POSTGRES_PASSWORD=admin
```

Probe realizado:

```bash
PGPASSWORD=admin psql -h localhost -U postgres -d postgres -c "SELECT 1"
 ?column?
----------
        1
(1 fila)
```

Bases de datos a crear (por el proyecto):

| Entorno | Database name | Connection string |
|---|---|---|
| Development | `sig_plataforma_dev` | `Host=localhost;Port=5432;Database=sig_plataforma_dev;Username=postgres;Password=admin` |
| Testing | `sig_plataforma_test` | `Host=localhost;Port=5432;Database=sig_plataforma_test;Username=postgres;Password=admin` |
| E2E | `sig_plataforma_e2e` | `Host=localhost;Port=5432;Database=sig_plataforma_e2e;Username=postgres;Password=admin` |
| Production | (a definir por IT — placeholder) | (placeholder) |

Aplica el gotcha `[Meta] [Stack: PostgreSQL] Connection string en appsettings con password que no coincide con la instancia local`: la password real de esta máquina es `admin`, NO `postgres`. El Desarrollador debe escribir `Password=admin` en `appsettings.Development.json`, `appsettings.Testing.json` y `appsettings.E2E.json`.

## Notas

- Existen dos SDKs .NET 10 (10.0.103 y 10.0.104). Sin `global.json` el SDK más reciente (10.0.104) será el usado por defecto, lo cual es correcto. No se requiere fijar `global.json`.
- PostgreSQL 16.12 es plenamente compatible con `Npgsql.EntityFrameworkCore.PostgreSQL` 9.0.4.
- Angular CLI 21.2.2 coincide con la versión major del stack documentado (Angular 21). Material 21 instalable vía `ng add @angular/material@21`.
- Node 24 es LTS y compatible con Angular 21.
- No se detectaron procesos `dotnet.exe` huérfanos ni procesos en puerto 4200/5432 que bloqueen.
- El directorio del proyecto (`C:\dev\sig-plataforma`) está vacío salvo `docs/INPUT_APP.md`. No hay código previo que migrar.
