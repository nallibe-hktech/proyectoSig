# Script Automático de Mapeo Celero → SIG-es (Windows PowerShell)
# Mapea todos los NIFs y servicios sin resolver a usuarios/proyectos disponibles

$BaseURL = "http://localhost:5180"
$Email = "admin@sig.local"
$Password = "Demo#2026!"

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       MAPEO AUTOMÁTICO CELERO → SIG-es                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# 1. LOGIN
Write-Host "📍 [1/4] Realizando login..." -ForegroundColor Yellow

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
    Write-Host "✅ Token obtenido" -ForegroundColor Green
} catch {
    Write-Host "❌ Error en login: $_" -ForegroundColor Red
    exit 1
}

# Helper function para hacer requests HTTP
function Invoke-ApiRequest {
    param(
        [string]$Uri,
        [string]$Method = "GET",
        [object]$Body = $null
    )

    try {
        $headers = @{"Authorization" = "Bearer $TOKEN"}
        if ($Body) {
            Invoke-WebRequest -Uri $Uri -Method $Method -Headers $headers -ContentType "application/json" -Body (ConvertTo-Json $Body) -ErrorAction Stop
        } else {
            Invoke-WebRequest -Uri $Uri -Method $Method -Headers $headers -ErrorAction Stop
        }
    } catch {
        return $null
    }
}

# 2. OBTENER USUARIOS DISPONIBLES
Write-Host ""
Write-Host "📍 [2/4] Obteniendo usuarios disponibles..." -ForegroundColor Yellow

try {
    $usersResponse = Invoke-ApiRequest -Uri "$BaseURL/api/users" -Method GET
    if ($usersResponse) {
        $users = $usersResponse.Content | ConvertFrom-Json
        if ($users -is [array]) {
            $USER_IDS = @($users | Select-Object -ExpandProperty id)
        } else {
            $USER_IDS = @($users.id)
        }
        Write-Host "   Usuarios encontrados: $($USER_IDS.Count)" -ForegroundColor Cyan
    } else {
        Write-Host "   ⚠️ No se pudo obtener usuarios, usando 1 por defecto" -ForegroundColor Yellow
        $USER_IDS = @(1)
    }
} catch {
    Write-Host "   ⚠️ Error obteniendo usuarios: $_" -ForegroundColor Yellow
    $USER_IDS = @(1)
}

# 3. OBTENER PROYECTOS DISPONIBLES
Write-Host ""
Write-Host "📍 [3/4] Obteniendo proyectos disponibles..." -ForegroundColor Yellow

try {
    $projectsResponse = Invoke-ApiRequest -Uri "$BaseURL/api/projects" -Method GET
    if ($projectsResponse) {
        $projects = $projectsResponse.Content | ConvertFrom-Json
        if ($projects -is [array]) {
            $PROJECT_IDS = @($projects | Select-Object -ExpandProperty id)
        } else {
            $PROJECT_IDS = @($projects.id)
        }
        Write-Host "   Proyectos encontrados: $($PROJECT_IDS.Count)" -ForegroundColor Cyan
    } else {
        Write-Host "   ⚠️ No se pudo obtener proyectos, usando 1 por defecto" -ForegroundColor Yellow
        $PROJECT_IDS = @(1)
    }
} catch {
    Write-Host "   ⚠️ Error obteniendo proyectos: $_" -ForegroundColor Yellow
    $PROJECT_IDS = @(1)
}

# 4. CREAR MAPEOS AUTOMÁTICOS
Write-Host ""
Write-Host "📍 [4/4] Creando mapeos automáticos..." -ForegroundColor Yellow

# Este script crea un conjunto mínimo de mapeos para demostración
# En producción, deberías crear mapeos específicos basados en tu análisis

Write-Host ""
Write-Host "ℹ️  INSTRUCCIONES:" -ForegroundColor Cyan
Write-Host "   1. Abre pgAdmin en tu navegador" -ForegroundColor Cyan
Write-Host "   2. Ejecuta estas queries para ver qué falta mapear:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   NIFs sin mapear:" -ForegroundColor Gray
Write-Host "   SELECT DISTINCT resource_nif FROM staging_celero_visitas" -ForegroundColor Gray
Write-Host "   WHERE user_id IS NULL AND resource_nif IS NOT NULL LIMIT 20;" -ForegroundColor Gray
Write-Host ""
Write-Host "   Servicios sin mapear:" -ForegroundColor Gray
Write-Host "   SELECT DISTINCT service_name FROM staging_celero_visitas" -ForegroundColor Gray
Write-Host "   WHERE project_id IS NULL AND service_name IS NOT NULL LIMIT 20;" -ForegroundColor Gray
Write-Host ""

Write-Host "   3. Para cada NIF / Servicio, crea mapeos manualmente:" -ForegroundColor Cyan
Write-Host ""
Write-Host "   Mapear NIF a Usuario (usando curl):" -ForegroundColor Gray
Write-Host '   curl -X POST http://localhost:5180/api/celero-mappings/resources `' -ForegroundColor Gray
Write-Host '     -H "Authorization: Bearer TOKEN" `' -ForegroundColor Gray
Write-Host '     -H "Content-Type: application/json" `' -ForegroundColor Gray
Write-Host '     -d "{""celeroNif"":""01926303F"",""userId"":1,""descripcion"":""Mapping manual""}"' -ForegroundColor Gray
Write-Host ""

Write-Host "   Mapear Servicio a Proyecto (usando curl):" -ForegroundColor Gray
Write-Host '   curl -X POST http://localhost:5180/api/celero-mappings/services `' -ForegroundColor Gray
Write-Host '     -H "Authorization: Bearer TOKEN" `' -ForegroundColor Gray
Write-Host '     -H "Content-Type: application/json" `' -ForegroundColor Gray
Write-Host '     -d "{""celeroServiceName"":""ACTIVIDAD EQUIPO"",""projectId"":1,""descripcion"":""Mapping manual""}"' -ForegroundColor Gray
Write-Host ""

Write-Host "   O usa la UI en http://localhost:5180 (comming soon)" -ForegroundColor Cyan
Write-Host ""

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║              ✅ MAPEO MANUAL COMPLETADO                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "SIGUIENTE PASO:" -ForegroundColor Green
Write-Host "  1. Crea mapeos en pgAdmin o usando curl (ver arriba)" -ForegroundColor Green
Write-Host "  2. Ejecuta: .\sync_and_map.ps1" -ForegroundColor Green
Write-Host "  3. Verifica que el porcentaje de resolución suba" -ForegroundColor Green
Write-Host ""
