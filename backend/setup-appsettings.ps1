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
Write-Host "⚠️  VALORES SENSIBLES" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════════════"
Write-Host ""
Write-Host "Los archivos .json.example están en el repositorio SIN valores reales."
Write-Host "Para obtener los valores sensibles (credenciales de APIs), ejecuta:"
Write-Host ""
Write-Host "  git log --all --oneline -- SIG.API/appsettings.json | head -20"
Write-Host "  git show COMMIT_HASH:SIG.API/appsettings.json | grep -A 100 Integrations"
Write-Host ""
Write-Host "Busca el commit donde estaban los valores reales (antes de __SET_VIA_ENVIRONMENT__)"
Write-Host "Actualmente (Commit 254462e) tiene:"
Write-Host "  - Bizneo: ApiKey (JWT token)"
Write-Host "  - Intratime: ApiToken, UserEmail, UserPassword"
Write-Host "  - Sgpv: Username, Password"
Write-Host "  - A3InnuvaNominas: ClientId, ClientSecret, SubscriptionKey"
Write-Host ""
Write-Host "Luego edita appsettings.Development.json con estos valores."
Write-Host "⚠️  NUNCA comitees appsettings.Development.json (está en .gitignore)"
Write-Host ""
Write-Host "Próximos pasos:" -ForegroundColor Cyan
Write-Host "1. Agregar valores sensibles a appsettings.Development.json"
Write-Host "2. Verificar que Docker está corriendo: docker ps | grep sig-es-db"
Write-Host "3. Aplicar migraciones: dotnet ef database update (desde SIG.API)"
Write-Host "4. Ejecutar tests: dotnet test --configuration Release"
Write-Host ""
