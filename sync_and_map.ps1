# Script de Sincronización y Mapeo Automático para Celero (Windows PowerShell)
# Uso: .\sync_and_map.ps1

$BaseURL = "http://localhost:5180"
$Email = "admin@sig.local"
$Password = "Demo#2026!"
$PostgresConnection = "Host=localhost;Username=postgres;Password=postgres;Database=siges"

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║  SINCRONIZACIÓN AUTOMÁTICA CELERO + MAPEO             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

# Cargar Npgsql
try {
    Add-Type -Path "C:\Program Files\dotnet\packs\NETStandard.Library\*\ref\netstandard.dll" -ErrorAction SilentlyContinue
    [Reflection.Assembly]::LoadWithPartialName("Npgsql") | Out-Null
} catch {
    Write-Host "⚠️ Npgsql no está disponible, intentando sin queries a BD" -ForegroundColor Yellow
}

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

# 3-5. OBTENER ESTADÍSTICAS (usando endpoint o BD)
Write-Host ""
Write-Host "📍 [3/5] Extrayendo NIFs únicos sin resolver..." -ForegroundColor Yellow

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
        return $null
    }
}

$nifQuery = @"
SELECT DISTINCT resource_nif FROM staging_celero_visitas
WHERE user_id IS NULL AND resource_nif IS NOT NULL
ORDER BY resource_nif LIMIT 20
"@

$nifResult = Get-PostgresData -Query $nifQuery
if ($nifResult) {
    Write-Host "   Encontrados:" -ForegroundColor Cyan
    $nifResult | ForEach-Object { Write-Host "     • $_" }
} else {
    Write-Host "   ℹ️ No hay NIFs sin resolver (o BD no disponible)" -ForegroundColor Cyan
}

# 4. SERVICIOS ÚNICOS
Write-Host ""
Write-Host "📍 [4/5] Extrayendo servicios únicos sin resolver..." -ForegroundColor Yellow

$serviceQuery = @"
SELECT DISTINCT service_name FROM staging_celero_visitas
WHERE project_id IS NULL AND service_name IS NOT NULL
ORDER BY service_name LIMIT 20
"@

$serviceResult = Get-PostgresData -Query $serviceQuery
if ($serviceResult) {
    Write-Host "   Encontrados:" -ForegroundColor Cyan
    $serviceResult | ForEach-Object { Write-Host "     • $_" }
} else {
    Write-Host "   ℹ️ No hay servicios sin resolver (o BD no disponible)" -ForegroundColor Cyan
}

# 5. VERIFICAR ESTADO
Write-Host ""
Write-Host "📍 [5/5] Verificando estado de resolución..." -ForegroundColor Yellow

$statsQuery = @"
SELECT
  COUNT(*) as total,
  COUNT(user_id) as con_usuario,
  COUNT(project_id) as con_proyecto,
  COUNT(action_id) as con_accion,
  ROUND(COUNT(user_id) * 100.0 / COUNT(*), 1) as porcentaje
FROM staging_celero_visitas
"@

try {
    $conn = New-Object Npgsql.NpgsqlConnection($PostgresConnection)
    $conn.Open()
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $statsQuery
    $reader = $cmd.ExecuteReader()

    if ($reader.Read()) {
        $total = $reader.GetValue(0)
        $conUsuario = $reader.GetValue(1)
        $conProyecto = $reader.GetValue(2)
        $conAccion = $reader.GetValue(3)
        $porcentaje = $reader.GetValue(4)

        Write-Host "   Total visitas: $total" -ForegroundColor Cyan
        Write-Host "   Con usuario: $conUsuario" -ForegroundColor Cyan
        Write-Host "   Con proyecto: $conProyecto" -ForegroundColor Cyan
        Write-Host "   Con acción: $conAccion" -ForegroundColor Cyan
        Write-Host "   Porcentaje resuelto: $porcentaje%" -ForegroundColor Cyan
    }

    $conn.Close()
} catch {
    Write-Host "   ⚠️ No se pudo obtener estadísticas (BD no disponible)" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                    ✅ COMPLETADO                       ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "PRÓXIMO PASO:" -ForegroundColor Green
Write-Host "  Si el porcentaje de resolución es bajo (<50%):" -ForegroundColor Green
Write-Host "  1. Ejecuta: .\auto_map_nifs.ps1" -ForegroundColor Green
Write-Host "  2. Luego: .\sync_and_map.ps1" -ForegroundColor Green
Write-Host ""
