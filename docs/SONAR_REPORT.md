# SonarQube Code Analysis Report - SIG-es Project

**Analysis Date**: May 27, 2026
**Total Issues Found**: 18
**Files Analyzed**: 121 C# files

## Summary

- **CRITICAL**: 2 (security vulnerabilities)
- **HIGH**: 14 (bugs)
- **MEDIUM**: 2 (code quality)

## Issues by Category

- **VULNERABILITY**: 2
- **BUG**: 14
- **CODE_SMELL**: 2

---

## Detailed Findings

### CRITICAL Issues (2)

**HARDCODED_SECRET**
- File: `backend/SIG.Infrastructure/Seed/DataSeeder.cs:20`
- Issue: Hardcoded secret/credential in code
- Code: `private const string DemoPassword = "Demo#2026!";`

**HARDCODED_SECRET**
- File: `backend/SIG.Tests/Integration/IntegrationTestBase.cs:29`
- Issue: Hardcoded secret/credential in code
- Code: `protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email = "admin@sig.local", st`

### HIGH Issues (14)

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/ActionsController.cs:16`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/ApprovalsController.cs:16`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/AuthController.cs:40`
- Issue: Accessing .Value without null check
- Code: `var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/AuthController.cs:49`
- Issue: Accessing .Value without null check
- Code: `var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/ClientsController.cs:17`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/ClosuresController.cs:16`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/ConceptsController.cs:17`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/OtherControllers.cs:17`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/OtherControllers.cs:39`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/OtherControllers.cs:79`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/ProjectsController.cs:16`
- Issue: Accessing .Value without null check
- Code: `private int UserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.API/Controllers/UsersController.cs:41`
- Issue: Accessing .Value without null check
- Code: `var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.Infrastructure/Services/CurrentUserService.cs:16`
- Issue: Accessing .Value without null check
- Code: `var v = _accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;`

**POTENTIAL_NULL_REFERENCE**
- File: `backend/SIG.Infrastructure/Services/CurrentUserService.cs:21`
- Issue: Accessing .Value without null check
- Code: `public string? Email => _accessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value;`

### MEDIUM Issues (2)

**BROAD_EXCEPTION_CATCH**
- File: `backend/SIG.API/Middleware/ExceptionHandlingMiddleware.cs:37`
- Issue: Catching generic Exception instead of specific types
- Code: `catch (Exception ex)`

**BROAD_EXCEPTION_CATCH**
- File: `backend/SIG.API/Program.cs:119`
- Issue: Catching generic Exception instead of specific types
- Code: `catch (Exception ex)`


---

## Quality Gate

**SONAR-QUALITY-GATE: BLOCKED**

2 CRITICAL issue(s) must be fixed before deployment.
