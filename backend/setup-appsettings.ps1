# ============================================================
# setup-appsettings.ps1
# ============================================================
# Copia appsettings.*.example a appsettings.* si no existen
# Ejecutar desde: C:\Projects\workspaces\SIG-es\backend\
# Uso: .\setup-appsettings.ps1
# ============================================================

$apiPath = "SIG.API"
$files = @(
    "appsettings.Development.json",
    "appsettings.Testing.json"
)

Write-Host "🔧 Configurando appsettings locales..." -ForegroundColor Cyan

foreach ($file in $files) {
    $target = Join-Path $apiPath $file
    $example = "$target.example"

    if (-not (Test-Path $example)) {
        Write-Host "⚠️  $example no encontrado. Saltando $file" -ForegroundColor Yellow
        continue
    }

    if (Test-Path $target) {
        Write-Host "✅ $file ya existe. No sobrescribiendo." -ForegroundColor Green
    } else {
        Copy-Item $example $target
        Write-Host "✅ Creado $file desde $example" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "✅ Setup completado." -ForegroundColor Green
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Cyan
Write-Host "1. Verificar que Docker está corriendo: docker ps | grep sig-es-db"
Write-Host "2. Aplicar migraciones: dotnet ef database update (desde SIG.API)"
Write-Host "3. Ejecutar tests: dotnet test --configuration Release"
Write-Host ""
