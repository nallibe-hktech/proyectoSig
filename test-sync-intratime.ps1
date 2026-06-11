# test-sync-intratime.ps1
# Sincroniza fichajes de Intratime en SIG-ES

Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "SINCRONIZAR INTRATIME EN SIG-ES" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Login en SIG-ES
Write-Host "PASO 1: Login en SIG-ES..." -ForegroundColor Yellow
$loginUrl = "http://localhost:5180/api/auth/login"
$loginBody = @{
    email = "admin@sig.local"
    password = "Demo#2026!"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest -Uri $loginUrl `
        -Method POST `
        -Headers @{ "Content-Type" = "application/json" } `
        -Body $loginBody

    $jwtToken = ($loginResponse.Content | ConvertFrom-Json).accessToken
    Write-Host "✅ JWT obtenido" -ForegroundColor Green
    Write-Host "Token: $($jwtToken.Substring(0, 40))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit
}

Write-Host ""

# PASO 2: Sincronizar Intratime
Write-Host "PASO 2: Sincronizando Intratime..." -ForegroundColor Yellow
$syncUrl = "http://localhost:5180/api/sync/intratime"
$syncHeaders = @{
    "Content-Type" = "application/json"
    "Authorization" = "Bearer $jwtToken"
}

try {
    $syncResponse = Invoke-WebRequest -Uri $syncUrl `
        -Method POST `
        -Headers $syncHeaders

    $result = $syncResponse.Content | ConvertFrom-Json

    Write-Host "✅ Sincronización completada" -ForegroundColor Green
    Write-Host ""
    Write-Host "Resultados:" -ForegroundColor Cyan
    Write-Host "  Insertados: $($result.insertados)" -ForegroundColor Green
    Write-Host "  Duplicados: $($result.duplicados)" -ForegroundColor Yellow
    Write-Host "  Errores: $($result.errores)" -ForegroundColor $(if ($result.errores -gt 0) { "Red" } else { "Green" })
    Write-Host ""
    Write-Host "Respuesta completa:" -ForegroundColor Gray
    $result | ConvertTo-Json -Depth 5 | Write-Host

} catch {
    Write-Host "❌ Error en sincronización: $_" -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Listo! Los fichajes se sincronizaron correctamente." -ForegroundColor Green
