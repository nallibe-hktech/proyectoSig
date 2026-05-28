# Script de prueba: Sincronizacion Celero One (PowerShell)
# Uso: .\test-celero-sync.ps1

$BaseUrl = "http://localhost:5180/api"
$Email = "admin@sig.local"
$Password = "Demo#2026!"

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "     PRUEBA: SINCRONIZACION CELERO ONE" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

# PASO 1: Login
Write-Host "PASO 1: Autenticacion como Administrator" -ForegroundColor Yellow
Write-Host "=====================================================" -ForegroundColor Gray
Write-Host ""
Write-Host "POST $BaseUrl/auth/login" -ForegroundColor Gray
Write-Host ""

$LoginBody = @{
    email    = $Email
    password = $Password
} | ConvertTo-Json

try {
    $LoginResponse = Invoke-WebRequest -Uri "$BaseUrl/auth/login" `
        -Method POST `
        -ContentType "application/json" `
        -Body $LoginBody `
        -ErrorAction Stop

    $LoginData = $LoginResponse.Content | ConvertFrom-Json
    Write-Host ($LoginResponse.Content | ConvertFrom-Json | ConvertTo-Json -Depth 3)
    Write-Host ""

    $AccessToken = $LoginData.accessToken

    if (-not $AccessToken) {
        Write-Host "ERROR: No se obtuvo token de acceso" -ForegroundColor Red
        Write-Host "Verifica que:" -ForegroundColor Red
        Write-Host "- La app esta corriendo en http://localhost:5000" -ForegroundColor Red
        Write-Host "- Las credenciales son correctas (admin@sig.local / Demo#2026!)" -ForegroundColor Red
        exit 1
    }

    Write-Host "OK: Token obtenido" -ForegroundColor Green
    Write-Host "Token: $($AccessToken.Substring(0, [Math]::Min(30, $AccessToken.Length)))..." -ForegroundColor Green
    Write-Host ""
}
catch {
    Write-Host "ERROR en Login: $_" -ForegroundColor Red
    exit 1
}

# PASO 2: Sincronizar Celero
Write-Host "PASO 2: Sincronizacion con Celero One" -ForegroundColor Yellow
Write-Host "=====================================================" -ForegroundColor Gray
Write-Host ""
Write-Host "POST $BaseUrl/sync/celero" -ForegroundColor Gray
Write-Host ""

try {
    $SyncResponse = Invoke-WebRequest -Uri "$BaseUrl/sync/celero" `
        -Method POST `
        -Headers @{ Authorization = "Bearer $AccessToken" } `
        -ErrorAction Stop

    $SyncData = $SyncResponse.Content | ConvertFrom-Json
    Write-Host ($SyncResponse.Content | ConvertFrom-Json | ConvertTo-Json -Depth 3)
    Write-Host ""

    $Sistema = $SyncData.sistema
    $Insertadas = $SyncData.filasInsertadas
    $Duplicadas = $SyncData.filasDuplicadasIgnoradas
    $Errores = $SyncData.filasConError

    if ($Insertadas -eq 0 -and $Duplicadas -eq 0 -and $Errores -eq 0) {
        Write-Host "ADVERTENCIA: No se sincronizaron registros" -ForegroundColor Yellow
        Write-Host "Posibles causas:" -ForegroundColor Yellow
        Write-Host "- Celero no tiene visitas con status='done' en los ultimos 6 meses" -ForegroundColor Yellow
        Write-Host "- La conexion a Celero no esta configurada (UseFake sigue en true)" -ForegroundColor Yellow
        Write-Host "- Los nombres de proyectos/acciones no coinciden" -ForegroundColor Yellow
    }
    else {
        Write-Host "OK: Sincronizacion completada" -ForegroundColor Green
    }

    Write-Host ""
    Write-Host "RESULTADOS:" -ForegroundColor Cyan
    Write-Host "   Sistema: $Sistema"
    Write-Host "   Insertadas: $Insertadas"
    Write-Host "   Duplicadas: $Duplicadas"
    Write-Host "   Errores: $Errores"
    Write-Host ""
}
catch {
    Write-Host "ERROR en Sync: $_" -ForegroundColor Red
    exit 1
}

# PASO 3: Consultas de verificacion
Write-Host "PASO 3: Verificacion en Base de Datos" -ForegroundColor Yellow
Write-Host "=====================================================" -ForegroundColor Gray
Write-Host ""
Write-Host "IMPORTANTE: Ejecuta ESTAS QUERIES en pgAdmin/DBeaver" -ForegroundColor Yellow
Write-Host ""

Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host "QUERY 1: Contar total de visitas sincronizadas" -ForegroundColor Magenta
Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "SELECT COUNT(*) as total_visitas FROM staging_celero_visitas;" -ForegroundColor White
Write-Host ""
Write-Host ""

Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host "QUERY 2: Ver ultimas 10 visitas con todos los datos" -ForegroundColor Magenta
Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "SELECT" -ForegroundColor White
Write-Host "  visita_id_externo," -ForegroundColor White
Write-Host "  resource_nif," -ForegroundColor White
Write-Host "  service_name," -ForegroundColor White
Write-Host "  mission_name," -ForegroundColor White
Write-Host "  fecha," -ForegroundColor White
Write-Host "  user_id," -ForegroundColor White
Write-Host "  project_id," -ForegroundColor White
Write-Host "  action_id," -ForegroundColor White
Write-Host "  flag_procesado," -ForegroundColor White
Write-Host "  fecha_ultima_sincronizacion" -ForegroundColor White
Write-Host "FROM staging_celero_visitas" -ForegroundColor White
Write-Host "ORDER BY fecha_ultima_sincronizacion DESC" -ForegroundColor White
Write-Host "LIMIT 10;" -ForegroundColor White
Write-Host ""
Write-Host ""

Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host "QUERY 3: Ver visitas con IDs NO resueltos" -ForegroundColor Magenta
Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "SELECT COUNT(*) as no_resueltos" -ForegroundColor White
Write-Host "FROM staging_celero_visitas" -ForegroundColor White
Write-Host "WHERE user_id IS NULL" -ForegroundColor White
Write-Host "   OR project_id IS NULL" -ForegroundColor White
Write-Host "   OR action_id IS NULL;" -ForegroundColor White
Write-Host ""
Write-Host ""

Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host "QUERY 4: Detalles de lo que NO se resolvio" -ForegroundColor Magenta
Write-Host "=====================================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "SELECT" -ForegroundColor White
Write-Host "  visita_id_externo," -ForegroundColor White
Write-Host "  resource_nif," -ForegroundColor White
Write-Host "  service_name," -ForegroundColor White
Write-Host "  mission_name," -ForegroundColor White
Write-Host "  user_id," -ForegroundColor White
Write-Host "  project_id," -ForegroundColor White
Write-Host "  action_id" -ForegroundColor White
Write-Host "FROM staging_celero_visitas" -ForegroundColor White
Write-Host "WHERE user_id IS NULL" -ForegroundColor White
Write-Host "   OR project_id IS NULL" -ForegroundColor White
Write-Host "   OR action_id IS NULL" -ForegroundColor White
Write-Host "LIMIT 20;" -ForegroundColor White
Write-Host ""
Write-Host ""

Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "                OK: PRUEBA COMPLETADA" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "PROXIMOS PASOS:" -ForegroundColor Green
Write-Host "1. Ejecuta las queries en pgAdmin/DBeaver" -ForegroundColor Green
Write-Host "2. Verifica que haya datos en staging_celero_visitas" -ForegroundColor Green
Write-Host "3. Comprueba que user_id, project_id, action_id tengan valores" -ForegroundColor Green
Write-Host "4. Si alguno esta NULL, investiga los nombres con QUERY 4" -ForegroundColor Green
Write-Host ""
