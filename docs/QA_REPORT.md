# QA Report — SIG-es

## Veredicto Final

**QA-GATE: BLOCKED**

Proyecto no apto para producción. SonarQube FAILED, 4 integration tests FAIL, E2E no ejecutables.

---

## Tests ejecutados

### Backend Build (dotnet build)
| Estado | Detalle |
|--------|---------|
| ✅ PASS | 0 errors, 0 warnings (except NU1903) |

### Backend Unit Tests (dotnet test)
| Estado | Detalle |
|--------|---------|
| ✅ PASS | 121/121 tests pasan |

### Backend Integration Tests (dotnet test)
| Estado | Detalle |
|--------|---------|
| ⚠️ Casi | 80/84 PASS · 4 FAIL |

**Test failures:**
1. `PostSyncCelero_DosVecesSeguidas_DevuelveDuplicadosEnSegundoIntentoConHashSHA256` → **Causa:** B-03 (schema mismatch `estado_mapeo`)
2. `GetClosures_FiltradoPorEstadoAprobado_FuncionaParaAdmin` → **Causa:** B-06 (sin datos de prueba con estado Aprobado)

### Frontend Unit Tests (Karma + ChromeHeadless)
| Estado | Detalle |
|--------|---------|
| ✅ PASS | 52/52 tests pasan |

### SonarQube Static Analysis
| Estado | Detalle |
|--------|---------|
| ❌ FAILED | Quality Gate ERROR · 129 issues · 16 hotspots |

| Metric | Value | Threshold | Status |
|--------|-------|-----------|--------|
| new_coverage | 0.0% | < 0% | ✅ OK |
| new_duplicated_lines_density | 3.79% | > 12% | ✅ OK |
| new_security_hotspots_reviewed | 0.0% | >= 20% | ❌ ERROR |
| new_violations | 129 | > 30 | ❌ ERROR |

**Hotspots:**
- CRITICAL: S2068 passwords hardcodeadas, S2696 static field set, S4487 private field sin usar, S3776 complejidad cognitiva

### E2E Tests (Playwright)
| Estado | Detalle |
|--------|---------|
| ⏭️ SKIPPED | B-03 bloquea flujo de sincronización |

---

## Hallazgos

| Bug | Severidad | Estado |
|-----|-----------|--------|
| B-01 | Conexión BD | RESUELTO |
| B-02 | Login retorna 400 | RESUELTO |
| B-03 | Schema mismatch `estado_mapeo` | ❌ ACTIVO |
| B-04 | SonarQube Quality Gate ERROR | ❌ ACTIVO |
| B-05 | E2E no ejecutados (B-03 bloquea) | ⏸️ BLOQUEADO |
| B-06 | Filtro Aprobado sin datos seed | 🟢 BAJO |

---

## Recomendaciones

1. **backend** — Corregir B-03: ejecutar `dotnet ef database update` en `sig_plataforma_test`
2. **backend** — Corregir B-06: agregar closure Aprobado al seed de tests
3. **backend** — Corregir B-04: resolver CRITICAL smells (passwords, static fields, fields sin usar, complejidad)
4. **backend** — Generar reporte de cobertura (`XPlat Code Coverage`) y re-ejecutar SonarScanner
5. **infra** — Resolver B-03 primero, luego ejecutar E2E Playwright
6. Reiniciar QA-GATE tras correcciones
