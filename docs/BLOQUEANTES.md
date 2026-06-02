# BLOQUEANTES — SIG · Plataforma Operativa Integral

> **4 tests de integración fallidos — QA-GATE: BLOCKED**

---

## Bloqueantes Activos

### [BACKEND-BUG-001] Falta migración EF Core para columnas en `staging_celero_visitas`

**Severidad:** ALTA  
**Tags:** `[BACKEND-BUG]`

**Descripción:** La entidad `StagingCeleroVisita` (`SIG.Domain/Entities/Staging/StagingEntities.cs:22-25`) tiene 4 propiedades que no existen como columnas en la tabla real de PostgreSQL:
- `notas` (string?)
- `mapeado_por` (int?)
- `fecha_mapeo` (DateTime?)
- `estado_mapeo` (string?)

El modelo de EF (`20260529084625_AddConceptProjectAndColumnaA3.Designer.cs`) incluye estas columnas en el snapshot, pero NINGUNA migración generó los `AddColumn` correspondientes. Las 7 migraciones existentes nunca ejecutan un `ALTER TABLE staging_celero_visitas ADD COLUMN` para estas 4 columnas.

**Impacto:** Causa `DbUpdateException` (PostgreSQL column not found) en cualquier operación INSERT/UPDATE sobre `staging_celero_visitas`, lo que cascada en 4 tests fallidos.

**Tests afectados:**
1. `PostRegenerarSeed_ComoAdminEnTesting_Devuelve200` — seed regenera datos pero falla al insertar staging (HTTP 500 vs 200)
2. `PostSyncCelero_ComoAdmin_Devuelve200ConResumen` — sync inserta staging rows pero falla (HTTP 500 vs 200)
3. `PostSyncCelero_DosVecesSeguidas_DevuelveDuplicadosEnSegundoIntentoConHashSHA256` — ambos syncs fallan
4. `GetClosures_FiltradoPorEstadoAprobado_FuncionaParaAdmin` — **cascading**: seed falla ANTES de insertar closures (que se guardan después del staging), por lo tanto nunca hay closures en BD para filtrar

**Solución:** Generar migración EF Core:
```bash
dotnet ef migrations add AddMissingStagingColumns
```
que ejecute:
```sql
ALTER TABLE staging_celero_visitas 
  ADD COLUMN notas text,
  ADD COLUMN mapeado_por integer,
  ADD COLUMN fecha_mapeo timestamp with time zone,
  ADD COLUMN estado_mapeo text;
```

**Evidence:**
- Entidad: `SIG.Domain/Entities/Staging/StagingEntities.cs:22-25`
- INSERT fallido (log): columna `estado_mapeo` referenciada en INSERT pero no existe en tabla
- Seed falla en: `DataSeeder.cs:403-407` (antes de llegar a closures en línea 453-454)
- Migraciones revisadas: 7 archivos, ninguno tiene AddColumn para estas 4 columnas

---

## Historial

| Fecha | Descripción | Estado |
|-------|-------------|--------|
| 2026-06-01 | QA Gate: 4 tests integración fallidos por columnas faltantes en `staging_celero_visitas` | Activo |
| — | No se detectaron bloqueantes. Environment probe OK (PostgreSQL local con password `admin`). | Resuelto |
