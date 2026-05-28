# QA-GATE: Issues y Bugs Bloqueantes

**Análisis**: SonarQube Quality Gate
**Fecha**: May 27, 2026
**Estado**: QA-GATE: BLOCKED (2 CRITICAL issues detectados)

---

## BUG-001 — [BACKEND-BUG] Hardcoded Demo Password in DataSeeder

**Severidad**: BLOQUEANTE
**Detectado en**: SonarQube - Code Analysis
**Ubicación**: `backend/SIG.Infrastructure/Seed/DataSeeder.cs:20`

```csharp
private const string DemoPassword = "Demo#2026!";
```

**Síntoma**: Credencial sensible hardcodeada en el código fuente. Esta contraseña podría ser comprometida en repositorios públicos o builds.

**Causa raíz**: La contraseña de demostración se definió como constante compilada en lugar de ser inyectada desde variables de entorno o configuration management.

**Riesgo de seguridad**: 
- CRITICAL: Exposición de credenciales en git history
- Imposible cambiar sin redeploy
- Acceso no autorizado a datos de prueba

**Agente que debe corregirlo**: backend
**Corrección requerida**: 
1. Mover a variable de entorno: `Environment.GetEnvironmentVariable("DEMO_PASSWORD")`
2. O usar `IConfiguration` inyectado: `_config["DemoPassword"]`
3. Documentar en `.env.example` sin valores reales
4. Hacer audit de git history si hay exposición anterior

---

## BUG-002 — [BACKEND-BUG] Hardcoded Test Credentials in IntegrationTestBase

**Severidad**: BLOQUEANTE
**Detectado en**: SonarQube - Code Analysis
**Ubicación**: `backend/SIG.Tests/Integration/IntegrationTestBase.cs:29`

```csharp
protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email = "admin@sig.local", st...
```

**Síntoma**: Las pruebas de integración hardcodean credenciales de usuario para autenticación. Si se ejecutan en CI/CD, estas credenciales pueden ser expuestas.

**Causa raíz**: Las credenciales se pasaron como parámetros con valores por defecto en lugar de ser configuradas dinámicamente.

**Riesgo de seguridad**:
- Potencial exposición en logs de CI/CD
- Hardcoding de usuarios de prueba que pueden no corresponder a configuración real

**Agente que debe corregirlo**: backend (SIG.Tests)
**Corrección requerida**:
1. Parametrizar las credenciales desde `appsettings.Testing.json`
2. Usar un usuario/contraseña de prueba específico inyectado en test fixtures
3. No usar email reales como credenciales de demo

---

## BUG-003 — [BACKEND-BUG] Null Reference Risk in User ID Extraction (Multiple Controllers)

**Severidad**: MAYOR
**Detectado en**: SonarQube - Code Analysis
**Ubicación**: `backend/SIG.API/Controllers/` (múltiples)

Archivos afectados:
- `ActionsController.cs:16`
- `ApprovalsController.cs:16`
- `AuthController.cs:40, :49`
- `ClientsController.cs:17`
- `ClosuresController.cs:16`
- `ConceptsController.cs:17`
- `OtherControllers.cs:17, :39, :79`
- `ProjectsController.cs:16`
- `UsersController.cs:41`

**Síntoma**:
```csharp
var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
```

El uso del operador `!` (null-forgiving) suprime el análisis de null sin verificar realmente. Si `FindFirst()` retorna null o si no hay claim, causará `NullReferenceException` en tiempo de ejecución.

**Causa raíz**: 
- El atributo `[Authorize]` en los controllers asegura que hay un usuario autenticado
- Sin embargo, usar `!` es peligroso: no valida que el claim exista específicamente
- Si el middleware de autenticación cambia, el código puede romperse

**Riesgo operacional**:
- Excepción no controlada si claim está ausente
- No se ejecutaría en desarrollo (con usuario hardcoded) pero fallaría en producción

**Agente que debe corregirlo**: backend
**Corrección requerida**:
```csharp
// Mejor: usar null coalescing + throw o default
var uidClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
    ?? throw new InvalidOperationException("NameIdentifier claim not found");
var uid = int.Parse(uidClaim);

// O mejor aún: crear un servicio ICurrentUserService centralizado
var uid = _currentUserService.UserId; // que ya hace validación interna
```

---

## BUG-004 — [BACKEND-BUG] Null Reference in CurrentUserService

**Severidad**: MAYOR
**Detectado en**: SonarQube - Code Analysis
**Ubicación**: `backend/SIG.Infrastructure/Services/CurrentUserService.cs:16, :21`

```csharp
var v = _accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

public string? Email => _accessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;
```

**Síntoma**: Aunque usa operador `?.` (safe navigation), el valor podría ser null sin throw. El servicio devuelve null implícitamente sin documentación clara.

**Causa raíz**: La implementación asume que siempre hay un contexto HTTP, pero en algunos escenarios (background jobs, seeding) podría no haberlo.

**Agente que debe corregirlo**: backend (Infrastructure)
**Corrección requerida**:
```csharp
public int UserId => 
    int.TryParse(_accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) 
        ? id 
        : throw new InvalidOperationException("User ID not found in context");
```

---

## BUG-005 — [BACKEND-BUG] Broad Exception Catching Without Proper Logging

**Severidad**: MENOR
**Detectado en**: SonarQube - Code Analysis
**Ubicación**: `backend/SIG.API/Middleware/ExceptionHandlingMiddleware.cs:37`

```csharp
catch (Exception ex)
{
    _logger.LogError(ex, "Error no controlado en pipeline");
    ctx.Response.StatusCode = 500;
    var pd = new ProblemDetails { Status = 500, Title = "Internal server error" };
    pd.Extensions["code"] = "internal_error";
    await ctx.Response.WriteAsJsonAsync(pd);
}
```

**Síntoma**: Captura genérica `Exception`. Mientras que hay logging, es mejor ser específico sobre qué excepciones se esperan (DbException, ValidationException, etc).

**Causa raíz**: Patrón de middleware genérico.

**Nota**: Este código ESTÁ bien diseñado (tiene logging). La recomendación es refactorizar a específico por tipo de excepción para mejor rastreabilidad.

**Agente que debe corregirlo**: backend (opcional - no bloqueante)
**Corrección requerida**:
```csharp
catch (DbUpdateConcurrencyException ex)
{
    _logger.LogWarning(ex, "Concurrency conflict");
    // ... 412 response
}
catch (ValidationException ex)
{
    _logger.LogInformation(ex, "Validation failed");
    // ... 400 response
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unhandled exception in pipeline");
    // ... 500 response
}
```

---

## BUG-006 — [BACKEND-BUG] Broad Exception in Program.cs

**Severidad**: MENOR
**Detectado en**: SonarQube - Code Analysis
**Ubicación**: `backend/SIG.API/Program.cs:119`

```csharp
catch (Exception ex)
{
    // ...
}
```

**Síntoma**: Similar a BUG-005, captura genérica en startup.

**Agente que debe corregirlo**: backend (opcional)

---

## Resumen de Calificación QA

| Categoría | Cantidad | Estado |
|-----------|----------|--------|
| CRITICAL | 2 | BLOQUEANTE |
| HIGH | 6 | REQUIERE CORRECCIÓN |
| MEDIUM | 2 | MEJORA RECOMENDADA |
| **Total** | **10** | **QA-GATE: BLOCKED** |

### Distribución por Agente

- **[BACKEND-BUG]**: 10 issues
  - 2 CRITICAL (hardcoded passwords)
  - 6 HIGH (null reference risks)
  - 2 MEDIUM (exception handling)

---

## Autorización de Producción

**ESTADO**: 🔴 **BLOQUEADO**

**Razón**: 2 CRITICAL security issues detectados:
1. Credencial de demostración hardcodeada en código fuente
2. Credenciales de prueba en fixture de integración

**Acción requerida antes de COMMIT/DEPLOY**:
1. Agente BACKEND debe corregir BUG-001 y BUG-002 (CRITICAL)
2. Agente BACKEND debería corregir BUG-003, BUG-004 (HIGH null references)
3. Reejecutar análisis de SonarQube para verificación

**Status**: ❌ NO AUTORIZADO PARA DEPLOYMENT
