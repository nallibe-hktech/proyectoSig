# Script Automático de Mapeo Celero → SIG-es (Windows PowerShell)
# MEJORADO: Crea mapeos automáticamente en lugar de solo mostrar instrucciones
# Mapea NIFs, Servicios y Misiones a usuarios/proyectos/acciones disponibles

$BaseURL = "http://localhost:5180"
$Email = "admin@sig.local"
$Password = "Demo#2026!"

Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║    MAPEO AUTOMÁTICO MEJORADO CELERO → SIG-es          ║" -ForegroundColor Cyan
Write-Host "║    (Crea mapeos automáticamente)                       ║" -ForegroundColor Cyan
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
            $response = Invoke-WebRequest -Uri $Uri -Method $Method -Headers $headers `
                -ContentType "application/json" -Body (ConvertTo-Json $Body) -ErrorAction Stop
        } else {
            $response = Invoke-WebRequest -Uri $Uri -Method $Method -Headers $headers -ErrorAction Stop
        }
        return $response
    } catch {
        Write-Host "   ⚠️ Error: $_" -ForegroundColor Yellow
        return $null
    }
}

# 2. OBTENER DATOS DE REFERENCIA
Write-Host ""
Write-Host "📍 [2/5] Obteniendo usuarios, proyectos y acciones..." -ForegroundColor Yellow

$usuarios = $null
$proyectos = $null
$acciones = $null

try {
    $usersResponse = Invoke-ApiRequest -Uri "$BaseURL/api/users" -Method GET
    if ($usersResponse) {
        $usersJson = $usersResponse.Content | ConvertFrom-Json
        if ($usersJson -is [array]) {
            $usuarios = $usersJson
        } elseif ($usersJson.items) {
            $usuarios = $usersJson.items
        } else {
            $usuarios = @($usersJson)
        }
        Write-Host "   ✅ Usuarios: $($usuarios.Count)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ No se pudo obtener usuarios" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Error obteniendo usuarios: $_" -ForegroundColor Red
}

try {
    $projectsResponse = Invoke-ApiRequest -Uri "$BaseURL/api/projects" -Method GET
    if ($projectsResponse) {
        $projectsJson = $projectsResponse.Content | ConvertFrom-Json
        if ($projectsJson -is [array]) {
            $proyectos = $projectsJson
        } elseif ($projectsJson.items) {
            $proyectos = $projectsJson.items
        } else {
            $proyectos = @($projectsJson)
        }
        Write-Host "   ✅ Proyectos: $($proyectos.Count)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ No se pudo obtener proyectos" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Error obteniendo proyectos: $_" -ForegroundColor Red
}

try {
    $actionsResponse = Invoke-ApiRequest -Uri "$BaseURL/api/actions" -Method GET
    if ($actionsResponse) {
        $actionsJson = $actionsResponse.Content | ConvertFrom-Json
        if ($actionsJson -is [array]) {
            $acciones = $actionsJson
        } elseif ($actionsJson.items) {
            $acciones = $actionsJson.items
        } else {
            $acciones = @($actionsJson)
        }
        Write-Host "   ✅ Acciones: $($acciones.Count)" -ForegroundColor Green
    } else {
        Write-Host "   ❌ No se pudo obtener acciones" -ForegroundColor Red
    }
} catch {
    Write-Host "   ❌ Error obteniendo acciones: $_" -ForegroundColor Red
}

# Validar que se obtuvieron los datos
if (-not $usuarios -or -not $proyectos) {
    Write-Host ""
    Write-Host "❌ No hay suficientes datos de referencia (usuarios/proyectos/acciones)" -ForegroundColor Red
    Write-Host "   Ejecuta manualmente en http://localhost:4200/celero-mapeos" -ForegroundColor Yellow
    exit 1
}

# 3. OBTENER VALORES PENDIENTES DE MAPEAR
Write-Host ""
Write-Host "📍 [3/5] Obteniendo valores pendientes..." -ForegroundColor Yellow

try {
    $pendientesResponse = Invoke-ApiRequest -Uri "$BaseURL/api/celero-mappings/pendientes" -Method GET
    if ($pendientesResponse) {
        $pendientes = $pendientesResponse.Content | ConvertFrom-Json
        Write-Host "   ✅ Valores pendientes obtenidos" -ForegroundColor Green
    } else {
        Write-Host "   ❌ No se pudo obtener valores pendientes" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "   ❌ Error: $_" -ForegroundColor Red
    exit 1
}

# 4. CREAR MAPEOS AUTOMÁTICOS
Write-Host ""
Write-Host "📍 [4/5] Creando mapeos automáticos..." -ForegroundColor Yellow

$mapeosCreadosRecursos = 0
$mapeosCreadosServicios = 0
$mapeosCreadosMisiones = 0
$errores = 0

# Mapear Recursos (NIFs → Usuarios)
if ($pendientes.recursos -and $pendientes.recursos.Count -gt 0) {
    Write-Host ""
    Write-Host "   📦 Mapeando recursos (NIFs)..." -ForegroundColor Cyan
    $userIndex = 0

    foreach ($recurso in $pendientes.recursos) {
        if (-not $recurso.estaMapado) {
            $usuarioAsignado = $usuarios[$userIndex % $usuarios.Count]

            $mappingBody = @{
                celeroNif = $recurso.valor
                userId = $usuarioAsignado.id
                descripcion = "Auto-mapped by script"
            }

            $mapResponse = Invoke-ApiRequest -Uri "$BaseURL/api/celero-mappings/resources" `
                -Method POST -Body $mappingBody

            if ($mapResponse) {
                $mapeosCreadosRecursos++
                Write-Host "      ✅ $($recurso.valor) → Usuario $($usuarioAsignado.id)" -ForegroundColor Green
                $userIndex++
            } else {
                $errores++
                Write-Host "      ❌ Error mapeando $($recurso.valor)" -ForegroundColor Red
            }
        }
    }
    Write-Host "   Subtotal: $mapeosCreadosRecursos NIFs mapeados"
}

# Mapear Servicios (ServiceName → Proyectos)
if ($pendientes.servicios -and $pendientes.servicios.Count -gt 0) {
    Write-Host ""
    Write-Host "   📦 Mapeando servicios..." -ForegroundColor Cyan
    $projectIndex = 0

    foreach ($servicio in $pendientes.servicios) {
        if (-not $servicio.estaMapado) {
            $proyectoAsignado = $proyectos[$projectIndex % $proyectos.Count]

            $mappingBody = @{
                celeroServiceName = $servicio.valor
                projectId = $proyectoAsignado.id
                descripcion = "Auto-mapped by script"
            }

            $mapResponse = Invoke-ApiRequest -Uri "$BaseURL/api/celero-mappings/services" `
                -Method POST -Body $mappingBody

            if ($mapResponse) {
                $mapeosCreadosServicios++
                Write-Host "      ✅ $($servicio.valor) → Proyecto $($proyectoAsignado.id)" -ForegroundColor Green
                $projectIndex++
            } else {
                $errores++
                Write-Host "      ❌ Error mapeando $($servicio.valor)" -ForegroundColor Red
            }
        }
    }
    Write-Host "   Subtotal: $mapeosCreadosServicios servicios mapeados"
}

# Mapear Misiones (MissionName → Acciones)
if ($pendientes.misiones -and $pendientes.misiones.Count -gt 0 -and $acciones) {
    Write-Host ""
    Write-Host "   📦 Mapeando misiones..." -ForegroundColor Cyan
    $actionIndex = 0

    foreach ($mision in $pendientes.misiones) {
        if (-not $mision.estaMapado) {
            $accionAsignada = $acciones[$actionIndex % $acciones.Count]

            $mappingBody = @{
                celeroMissionName = $mision.valor
                actionId = $accionAsignada.id
                descripcion = "Auto-mapped by script"
            }

            $mapResponse = Invoke-ApiRequest -Uri "$BaseURL/api/celero-mappings/missions" `
                -Method POST -Body $mappingBody

            if ($mapResponse) {
                $mapeosCreadosMisiones++
                Write-Host "      ✅ $($mision.valor) → Acción $($accionAsignada.id)" -ForegroundColor Green
                $actionIndex++
            } else {
                $errores++
                Write-Host "      ❌ Error mapeando $($mision.valor)" -ForegroundColor Red
            }
        }
    }
    Write-Host "   Subtotal: $mapeosCreadosMisiones misiones mapeadas"
}

# 5. REPROCESAR VISITAS
Write-Host ""
Write-Host "📍 [5/5] Reprocesando visitas sin mapear..." -ForegroundColor Yellow

try {
    $reprocessResponse = Invoke-ApiRequest -Uri "$BaseURL/api/celero-mappings/reprocesar" `
        -Method POST

    if ($reprocessResponse) {
        $reprocessData = $reprocessResponse.Content | ConvertFrom-Json
        Write-Host "   ✅ Reprocesamiento completado" -ForegroundColor Green
        Write-Host "      Procesados: $($reprocessData.procesados)" -ForegroundColor Cyan
        Write-Host "      Resueltos: $($reprocessData.resueltos)" -ForegroundColor Cyan
        Write-Host "      Sin resolver: $($reprocessData.sinResolver)" -ForegroundColor Cyan
    } else {
        Write-Host "   ⚠️ No se pudo reprocesar" -ForegroundColor Yellow
    }
} catch {
    Write-Host "   ⚠️ Error en reprocesamiento: $_" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║                  ✅ MAPEO COMPLETADO                    ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""

Write-Host "RESUMEN:" -ForegroundColor Green
Write-Host "  NIFs mapeados: $mapeosCreadosRecursos" -ForegroundColor Green
Write-Host "  Servicios mapeados: $mapeosCreadosServicios" -ForegroundColor Green
Write-Host "  Misiones mapeadas: $mapeosCreadosMisiones" -ForegroundColor Green
Write-Host "  Errores: $errores" -ForegroundColor $(if ($errores -gt 0) { "Red" } else { "Green" })
Write-Host ""

Write-Host "PRÓXIMO PASO:" -ForegroundColor Green
Write-Host "  1. Ejecuta: .\sync_and_map.ps1" -ForegroundColor Green
Write-Host "  2. Verifica que el porcentaje de resolución aumentó" -ForegroundColor Green
Write-Host "  3. (Opcional) Abre http://localhost:4200/celero-mapeos para ajustar mapeos manualmente" -ForegroundColor Green
Write-Host ""
