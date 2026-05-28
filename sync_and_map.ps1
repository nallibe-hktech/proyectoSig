# Script de Sincronización y Mapeo Automático para Celero (Windows PowerShell)
# Uso: .\sync_and_map.ps1

$BaseURL = "http://localhost:5180"
$Email = "admin@sig.local"
$Password = "Demo#2026!"

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  SINCRONIZACIÓN AUTOMÁTICA CELERO + MAPEO             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# 1. LOGIN
Write-Host "📍 [1/5] Realizando login..." -ForegroundColor Yellow

try {
    $loginResponse = Invoke-WebRequest -Uri "$BaseURL/api/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body (ConvertTo-Json @{email=$Email; password=$Password}) `
        -ErrorAction Stop

    $loginJson = $loginResponse.Content | ConvertFrom-Json
    $TOKEN = $loginJson.accessToken

    if ([string]::IsNullOrEmpty($TOKEN)) {
        Write-Host "❌ Error: No se pudo obtener token" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Token obtenido: $($TOKEN.Substring(0, 30))..." -ForegroundColor Green
} catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

# 2. SINCRONIZAR
Write-Host ""
Write-Host "📍 [2/5] Sincronizando datos de Celero..." -ForegroundColor Yellow

try {
    $syncResponse = Invoke-WebRequest -Uri "$BaseURL/api/sync/celero" `
        -Method POST `
        -ContentType "application/json" `
        -Headers @{"Authorization" = "Bearer $TOKEN"} `
        -ErrorAction Stop

    $syncJson = $syncResponse.Content | ConvertFrom-Json

    $INSERTADAS = $syncJson.filasInsertadas
    $DUPLICADAS = $syncJson.filasDuplicadasIgnoradas
    $ERRORES = $syncJson.filasError

    Write-Host "   Filas insertadas: $INSERTADAS" -ForegroundColor Cyan
    Write-Host "   Filas duplicadas ignoradas: $DUPLICADAS" -ForegroundColor Cyan
    Write-Host "   Errores: $ERRORES" -ForegroundColor Cyan

    if ($ERRORES -ne 0) {
        Write-Host "❌ Errores en sincronización" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ Sincronización completada" -ForegroundColor Green
} catch {
    Write-Host "❌ Error en sincronización: $_" -ForegroundColor Red
    exit 1
}

# 3-5. OBTENER ESTADÍSTICAS VÍA ENDPOINT
Write-Host ""
Write-Host "📍 [3/5] Extrayendo estadísticas de resolución..." -ForegroundColor Yellow

try {
    $statsResponse = Invoke-WebRequest -Uri "$BaseURL/api/sync/celero/stats" `
        -Method GET `
        -Headers @{"Authorization" = "Bearer $TOKEN"} `
        -ErrorAction Stop

    $statsJson = $statsResponse.Content | ConvertFrom-Json

    Write-Host "   Total visitas: $($statsJson.totalVisitas)" -ForegroundColor Cyan
    Write-Host "   Con usuario: $($statsJson.conUsuario)" -ForegroundColor Cyan
    Write-Host "   Con proyecto: $($statsJson.conProyecto)" -ForegroundColor Cyan
    Write-Host "   Con acción: $($statsJson.conAccion)" -ForegroundColor Cyan
    Write-Host "   Porcentaje resuelto: $($statsJson.porcentajeResuelto)%" -ForegroundColor Cyan

    # Información de NIFs y servicios sin mapear
    $RESOLUCION = [int]$statsJson.porcentajeResuelto

} catch {
    Write-Host "   ⚠️ No se pudo obtener estadísticas: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "📍 [4/5] Obteniendo resumen de datos sin resolver..." -ForegroundColor Yellow
Write-Host "   (Consult pgAdmin for details)" -ForegroundColor Gray

Write-Host ""
Write-Host "📍 [5/5] Resumen de sincronización" -ForegroundColor Yellow

if ($RESOLUCION -lt 50) {
    Write-Host "   ⚠️ Porcentaje de resolución bajo ($RESOLUCION%)" -ForegroundColor Yellow
    Write-Host "   Se recomienda ejecutar: .\auto_map_nifs.ps1" -ForegroundColor Yellow
} elseif ($RESOLUCION -lt 90) {
    Write-Host "   ℹ️ Porcentaje de resolución: $RESOLUCION%" -ForegroundColor Cyan
    Write-Host "   Considera ejecutar: .\auto_map_nifs.ps1 para mejorar" -ForegroundColor Cyan
} else {
    Write-Host "   ✅ Porcentaje de resolución excelente: $RESOLUCION%" -ForegroundColor Green
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    ✅ COMPLETADO                       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "PRÓXIMO PASO:" -ForegroundColor Green
if ($RESOLUCION -lt 50) {
    Write-Host "  1. Ejecuta: .\auto_map_nifs.ps1" -ForegroundColor Green
    Write-Host "  2. Luego: .\sync_and_map.ps1" -ForegroundColor Green
} else {
    Write-Host "  ✅ Los datos están suficientemente resueltos" -ForegroundColor Green
}
Write-Host ""
