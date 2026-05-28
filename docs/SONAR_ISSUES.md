# SonarQube Issues - QA Gate Report

**Analysis Date**: May 27, 2026
**Quality Gate**: FAILED (2 CRITICAL issues)
**Project**: SIG-es

---

## Critical Issues (BLOCKING)

### BUG-001 — [BACKEND-BUG] CRITICAL: Hardcoded Demo Password

- **Archivo**: backend/SIG.Infrastructure/Seed/DataSeeder.cs:20
- **Descripción**: Credencial de demostración hardcodeada como constante de compilación
- **Severidad**: CRITICAL
- **Riesgo**: Exposición de credenciales en repositorio, imposible cambiar sin redeploy
- **Corrección requerida**: Mover a `Environment.GetEnvironmentVariable("DEMO_PASSWORD")` o `IConfiguration`

### BUG-002 — [BACKEND-BUG] CRITICAL: Hardcoded Test Credentials

- **Archivo**: backend/SIG.Tests/Integration/IntegrationTestBase.cs:29
- **Descripción**: Credenciales de prueba hardcodeadas con parámetros default
- **Severidad**: CRITICAL
- **Riesgo**: Exposición en CI/CD logs, uso de credenciales real-like
- **Corrección requerida**: Inyectar desde `appsettings.Testing.json` mediante `IConfiguration`

---

## High Priority Issues

### BUG-003 — [BACKEND-BUG] HIGH: Null Reference Risk in Controllers

- **Archivos**: 
  - backend/SIG.API/Controllers/ActionsController.cs:16
  - backend/SIG.API/Controllers/ApprovalsController.cs:16
  - backend/SIG.API/Controllers/AuthController.cs:40, :49
  - backend/SIG.API/Controllers/ClientsController.cs:17
  - backend/SIG.API/Controllers/ClosuresController.cs:16
  - backend/SIG.API/Controllers/ConceptsController.cs:17
  - backend/SIG.API/Controllers/OtherControllers.cs:17, :39, :79
  - backend/SIG.API/Controllers/ProjectsController.cs:16
  - backend/SIG.API/Controllers/UsersController.cs:41
  
- **Patrón**: `int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)`
- **Descripción**: Uso del operador null-forgiving (!) sin validación real. Si el claim no existe, causa NullReferenceException
- **Severidad**: HIGH
- **Corrección requerida**: Validación explícita con throw o crear servicio ICurrentUserService centralizado

### BUG-004 — [BACKEND-BUG] HIGH: Null Reference in CurrentUserService

- **Archivo**: backend/SIG.Infrastructure/Services/CurrentUserService.cs:16, :21
- **Descripción**: HttpContext puede ser null en contextos no-HTTP (background jobs)
- **Severidad**: HIGH
- **Corrección requerida**: Validación explícita con throw si no hay contexto

---

## Medium Priority Issues

### BUG-005 — [BACKEND-BUG] MEDIUM: Broad Exception Catch

- **Archivo**: backend/SIG.API/Middleware/ExceptionHandlingMiddleware.cs:37
- **Descripción**: Captura genérica de Exception sin discriminar tipos específicos
- **Severidad**: MEDIUM
- **Nota**: Tiene logging, pero es mejor ser específico por tipo (DbException, ValidationException, etc)
- **Corrección requerida**: Refactorizar a catch específicos por excepción esperada

### BUG-006 — [BACKEND-BUG] MEDIUM: Broad Exception in Startup

- **Archivo**: backend/SIG.API/Program.cs:119
- **Descripción**: Captura genérica de Exception en configuración
- **Severidad**: MEDIUM
- **Corrección requerida**: Ser específico sobre qué excepciones se esperan

---

## Quality Gate Result

**SONAR-QUALITY-GATE: FAILED**

Razón: 2 CRITICAL security vulnerabilities detectadas (hardcoded credentials)

El proyecto NO puede avanzar a deployment hasta que se resuelvan BUG-001 y BUG-002.

---

## Test Results

- **Unit Tests**: 204/204 PASSED ✅
- **Integration Tests**: PASSED ✅
- **Compilation**: SUCCESS ✅

---

## Summary

| Metric | Value |
|--------|-------|
| Total Issues | 18 |
| CRITICAL | 2 |
| HIGH | 6 |
| MEDIUM | 2 |
| VULNERABILITIES | 2 |
| BUGS | 6 |
| CODE_SMELLS | 2 |
| Quality Gate | FAILED |
| Authorization | BLOCKED |

