# SonarQube Issues — QA Gate Report

**Analysis Date**: June 01, 2026
**Quality Gate**: ERROR — FAILED (18 CRITICAL, 15 MAJOR)
**Project**: SIG-es
**Coverage**: 0.0%

---

## Quality Gate Results

| Metric | Value | Threshold | Status |
|--------|-------|-----------|--------|
| new_coverage | 0.0% | < 0% | ✅ OK |
| new_duplicated_lines_density | 3.79% | > 12% | ✅ OK |
| new_security_hotspots_reviewed | 0.0% | >= 20% | ❌ ERROR |
| new_violations | 129 | > 30 | ❌ ERROR |

---

**SONAR-QUALITY-GATE: FAILED**

Razón: 129 code smells (18 CRITICAL, 15 MAJOR), 16 security hotspots sin revisar, 0% cobertura.

---

## Issue Distribution

| Severity | Count |
|----------|-------|
| BLOCKER | 0 |
| CRITICAL | 18 |
| MAJOR | 15 |
| MINOR | 71 |
| INFO | 25 |

| Type | Count |
|------|-------|
| CODE_SMELL | 129 |
| BUG | 0 |
| VULNERABILITY | 0 |

---

## [BACKEND-BUG] CRITICAL Issues (top 5)

### CRITICAL 1: S2068 — Hardcoded credentials
- **Archivo:** `backend/SIG.API/appsettings.json`, `backend/SIG.API/appsettings.Testing.json`, `backend/SIG.API/appsettings.E2E.json`
- **Descripción:** Passwords hardcodeadas en archivos de configuración
- **Severidad:** CRITICAL
- **Corrección:** Mover a variables de entorno o Secret Manager

### CRITICAL 2: S2696 — Static field modified by instance methods
- **Archivo:** `backend/SIG.Infrastructure/Integrations/Fake/FakeClients.cs`
- **Descripción:** Métodos de instancia modifican campos static
- **Corrección:** Hacer los métodos static o quitar el static de los campos

### CRITICAL 3: S4487 — Unread private fields
- **Archivo:** `backend/SIG.Infrastructure/Integrations/Http/HttpClients.cs`
- **Descripción:** Campos `_http` sin usar en varias clases
- **Corrección:** Eliminar campos no usados

### CRITICAL 4: S3776 — Cognitive Complexity > 15
- **Archivo:** `backend/SIG.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs` (16), `backend/SIG.Infrastructure/Seed/DataSeeder.cs` (82)
- **Descripción:** Métodos con complejidad cognitiva excesiva
- **Corrección:** Refactorizar en métodos más pequeños

### CRITICAL 5: S2245 — Pseudorandom RNG
- **Archivo:** `backend/SIG.Infrastructure/Seed/DataSeeder.cs`, `FakeClients.cs`
- **Descripción:** Uso de `Random` en lugar de criptográficamente seguro
- **Corrección:** Usar `RandomNumberGenerator` para datos sensibles

---

## Summary

| Metric | Value |
|--------|-------|
| Total Issues | 129 |
| BLOCKER | 0 |
| CRITICAL | 18 |
| MAJOR | 15 |
| MINOR | 71 |
| INFO | 25 |
| Bugs | 0 |
| Vulnerabilities | 0 |
| Code Smells | 129 |
| Security Hotspots | 16 (0% reviewed) |
| Quality Gate | ❌ FAILED |
| Authorization | ⛔ BLOCKED |
