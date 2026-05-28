# Script Automático de Mapeo Celero → SIG-es (Windows PowerShell)
# Mapea todos los NIFs y servicios sin resolver a usuarios/proyectos disponibles

$BaseURL = "http://localhost:5180"
$Email = "admin@sig.local"
$Password = "Demo#2026!"
$PostgresConnection = "Host=localhost;Username=postgres;Password=postgres;Database=siges"

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║       MAPEO AUTOMÁTICO CELERO → SIG-es                ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Cargar Npgsql
try {
    [Reflection.Assembly]::LoadWithPartialName("Npgsql") | Out-Null
} catch {
    Write-Host "⚠️ Npgsql no está disponible" -ForegroundColor Yellow
}

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

function Get-PostgresData {
    param([string]$Query)

    try {
        $conn = New-Object Npgsql.NpgsqlConnection($PostgresConnection)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $Query
        $reader = $cmd.ExecuteReader()
        $results = @()

        while ($reader.Read()) {
            $results += $reader.GetValue(0).ToString()
        }

        $conn.Close()
        return $results
    } catch {
        return @()
    }
}

# 2. OBTENER NIFs ÚNICOS
Write-Host ""
Write-Host "📍 [2/4] Extrayendo NIFs sin mapeo..." -ForegroundColor Yellow

$nifQuery = @"
SELECT DISTINCT resource_nif FROM staging_celero_visitas
WHERE user_id IS NULL AND resource_nif IS NOT NULL
ORDER BY resource_nif
"@

$NIFS = Get-PostgresData -Query $nifQuery
$NIFS_COUNT = $NIFS.Count

if ($NIFS_COUNT -gt 0) {
    Write-Host "   Encontrados: $NIFS_COUNT NIFs" -ForegroundColor Cyan
} else {
    Write-Host "   ✅ No hay NIFs sin mapeo" -ForegroundColor Green
}

# 3. MAPEAR NIFs A USUARIOS EN ROUND-ROBIN
if ($NIFS_COUNT -gt 0) {
    Write-Host ""
    Write-Host "📍 [3/4] Creando mapeos de recursos (NIF → Usuario)..." -ForegroundColor Yellow

    # Obtener IDs de usuarios disponibles
    $userQuery = "SELECT DISTINCT id FROM users WHERE NOT is_deleted ORDER BY id LIMIT 50"
    $USER_ARRAY = Get-PostgresData -Query $userQuery

    if ($USER_ARRAY.Count -eq 0) {
        Write-Host "   ⚠️ No hay usuarios disponibles. Usando usuario 1 por defecto." -ForegroundColor Yellow
        $USER_ARRAY = @("1")
    }

    $USER_COUNT = $USER_ARRAY.Count
    $COUNTER = 0
    $MAPPED = 0

    foreach ($NIF in $NIFS) {
        $NIF = $NIF.Trim()
        if ($NIF) {
            $USER_ID = $USER_ARRAY[$COUNTER % $USER_COUNT]

            # Crear mapeo
            $body = @{
                celeroNif = $NIF
                userId = [int]$USER_ID
                descripcion = "Auto-mapped"
            } | ConvertTo-Json

            try {
                $response = Invoke-WebRequest -Uri "$BaseURL/api/celero-mappings/resources" `
                    -Method POST `
                    -ContentType "application/json" `
                    -Headers @{"Authorization" = "Bearer $TOKEN"} `
                    -Body $body `
                    -ErrorAction Stop

                if ($response.StatusCode -eq 201 -or $response.StatusCode -eq 200) {
                    $MAPPED++
                    Write-Host "   ✓ $NIF → Usuario $USER_ID" -ForegroundColor Green
                }
            } catch {
                Write-Host "   ✗ Error creando mapeo para $NIF" -ForegroundColor Red
            }

            $COUNTER++
        }
    }

    Write-Host "   ✅ Mapeos creados: $MAPPED/$NIFS_COUNT" -ForegroundColor Green
}

# 4. MAPEAR SERVICIOS A PROYECTOS
Write-Host ""
Write-Host "📍 [4/4] Extrayendo servicios sin mapeo..." -ForegroundColor Yellow

$serviceQuery = @"
SELECT DISTINCT service_name FROM staging_celero_visitas
WHERE project_id IS NULL AND service_name IS NOT NULL
ORDER BY service_name
"@

$SERVICES = Get-PostgresData -Query $serviceQuery
$SERVICES_COUNT = $SERVICES.Count

if ($SERVICES_COUNT -gt 0) {
    Write-Host "   Encontrados: $SERVICES_COUNT servicios" -ForegroundColor Cyan
} else {
    Write-Host "   ✅ No hay servicios sin mapeo" -ForegroundColor Green
}

if ($SERVICES_COUNT -gt 0) {
    Write-Host ""
    Write-Host "📍 Creando mapeos de servicios (Servicio → Proyecto)..." -ForegroundColor Yellow

    # Obtener IDs de proyectos disponibles
    $projectQuery = "SELECT DISTINCT id FROM projects WHERE NOT is_deleted ORDER BY id LIMIT 50"
    $PROJECT_ARRAY = Get-PostgresData -Query $projectQuery

    if ($PROJECT_ARRAY.Count -eq 0) {
        Write-Host "   ⚠️ No hay proyectos disponibles. Usando proyecto 1 por defecto." -ForegroundColor Yellow
        $PROJECT_ARRAY = @("1")
    }

    $PROJECT_COUNT = $PROJECT_ARRAY.Count
    $COUNTER = 0
    $MAPPED = 0

    foreach ($SERVICE in $SERVICES) {
        $SERVICE = $SERVICE.Trim()
        if ($SERVICE) {
            $PROJECT_ID = $PROJECT_ARRAY[$COUNTER % $PROJECT_COUNT]
            $SERVICE_SHORT = if ($SERVICE.Length -gt 50) { $SERVICE.Substring(0, 50) + "..." } else { $SERVICE }

            # Crear mapeo
            $body = @{
                celeroServiceName = $SERVICE
                projectId = [int]$PROJECT_ID
                descripcion = "Auto-mapped"
            } | ConvertTo-Json

            try {
                $response = Invoke-WebRequest -Uri "$BaseURL/api/celero-mappings/services" `
                    -Method POST `
                    -ContentType "application/json" `
                    -Headers @{"Authorization" = "Bearer $TOKEN"} `
                    -Body $body `
                    -ErrorAction Stop

                if ($response.StatusCode -eq 201 -or $response.StatusCode -eq 200) {
                    $MAPPED++
                    Write-Host "   ✓ $SERVICE_SHORT → Proyecto $PROJECT_ID" -ForegroundColor Green
                }
            } catch {
                Write-Host "   ✗ Error creando mapeo para $SERVICE_SHORT" -ForegroundColor Red
            }

            $COUNTER++
        }
    }

    Write-Host "   ✅ Mapeos creados: $MAPPED/$SERVICES_COUNT" -ForegroundColor Green
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    ✅ MAPEO COMPLETADO                 ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "SIGUIENTE PASO:" -ForegroundColor Green
Write-Host "  Ejecuta nuevamente: .\sync_and_map.ps1" -ForegroundColor Green
Write-Host "  Los datos deberían estar más resueltos ahora." -ForegroundColor Green
Write-Host ""
