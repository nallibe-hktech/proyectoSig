# QA-GATE VEREDICTO: ❌ BLOQUEADO

## Resumen Ejecutivo

| Dimensión | Resultado | Detalle |
|-----------|-----------|---------|
| Backend Build | ✅ 0 errors | 0 warnings |
| Frontend Build | ✅ 0 errors | Angular 21, 18 módulos |
| Backend Tests | ❌ **5/212 FAIL** | 2.4% failure rate (47s) |
| Frontend Tests | ❌ **1/52 FAIL** | 1.9% failure rate |
| E2E Tests | ❌ **7/15 FAIL** | 47% failure rate (auth, visual, exports) |
| SonarQube | ❌ **ERROR** | 593 issues, 0% coverage, 899min debt |
| Bloqueantes | ❌ **10 abiertos** | 1 CRITICAL, 5 ALTA, 3 MEDIA, 1 pre-existing |

## Test Failures Breakdown

### Backend (5 tests, 3 raíces)
| ID | Root Cause | Archivo | 
|----|-----------|---------|
| B-02 | `Substitute.For<AppDbContext>()` no puede mockear EF Core (non-virtual members) | `DataProcessorServiceTests.cs` |
| B-03/B-04 | BD de tests no disponible (B-01) + DTO properties mismatch | `OtherEndpointsTests.cs` |
| B-05 | Seed de test sin closures en estado Aprobado | `ClosuresControllerTests.cs` |

### Frontend Unit (1 test)
| ID | Root Cause | Archivo |
|----|-----------|---------|
| B-06 | Falta `data-testid="login-card"` en template HTML | `login.component.html` |

### E2E (7 tests, 3 raíces)
| B-07 | Regex `credenciales` muy amplia (2 matches) | `auth.spec.ts:47` |
| B-08 | Login flow en beforeEach no funciona (stuck en /login) | `components-visual-test.spec.ts` |
| B-09 | Download A3 Innuva timeout (30s) | `exports.spec.ts:94` |

### SonarQube (B-10)
- **268 code smells** (vs 129 previamente — empeoró 2x)
- **106 CRITICAL**, **115 MAJOR**, 223 MINOR, 149 INFO
- **Coverage: 0.0%** — no se subió reporte de cobertura
- Posible inclusión de archivos de test en el análisis

## Acciones Requeridas Para QA-GATE: OK

| Prioridad | Acción | Estimate |
|-----------|--------|----------|
| 🔴 P0 | Corregir mock de DbContext en DataProcessorServiceTests (B-02) | ~2h |
| 🔴 P0 | Corregir login flow en beforeEach de componentes-visual-test (B-08) | ~1h |
| 🔴 P0 | Abordar 106 code smells CRITICAL en SonarQube | ~8h |
| 🟡 P1 | Arreglar seed de closures (B-05) y DTO properties (B-04) | ~1h |
| 🟡 P1 | Añadir `data-testid="login-card"` y corregir regex E2E auth (B-06, B-07) | ~0.5h |
| 🟡 P1 | Configurar exclusión de tests en SonarQube + reporte de cobertura (B-10) | ~1h |
| 🟢 P2 | Resolver B-01 (CREATEDB) y B-09 (export timeout) | ~0.5h |

**Veredicto**: NO APTO para despliegue. Se requieren ~14h de trabajo para resolver bloqueantes.
