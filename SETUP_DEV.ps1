# AlloyDB Setup Script for SIG-ES

Write-Host ""
Write-Host "=== SIG-ES AlloyDB Setup ===" -ForegroundColor Cyan
Write-Host ""

# Check requirements
Write-Host "1. Checking requirements..." -ForegroundColor Green
$checks = @("dotnet", "gcloud", "docker")
foreach ($cmd in $checks) {
    if (Get-Command $cmd -ErrorAction SilentlyContinue) {
        Write-Host "   OK: $cmd"
    } else {
        Write-Host "   MISSING: $cmd" -ForegroundColor Red
        exit 1
    }
}

# GCP Auth
Write-Host ""
Write-Host "2. GCP Authentication..." -ForegroundColor Green
Write-Host "   Running: gcloud auth application-default login"
gcloud auth application-default login
gcloud config set project sig-prod
Write-Host "   Done"

# Download Proxy
Write-Host ""
Write-Host "3. Downloading AlloyDB Auth Proxy..." -ForegroundColor Green
$proxyDir = "$env:LOCALAPPDATA\AlloyDbAuthProxy"
$proxyPath = "$proxyDir\alloydb-auth-proxy.exe"

if (-not (Test-Path $proxyDir)) {
    New-Item -ItemType Directory -Path $proxyDir -Force | Out-Null
}

if (-not (Test-Path $proxyPath)) {
    $url = "https://dl.google.com/cloudsql/cloud_sql_proxy.windows.amd64.exe"
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $url -OutFile $proxyPath
    Write-Host "   Downloaded to: $proxyPath"
} else {
    Write-Host "   Already exists: $proxyPath"
}

# Configure User Secrets
Write-Host ""
Write-Host "4. Configuring user-secrets..." -ForegroundColor Green
cd "backend\SIG.API"

dotnet user-secrets init --force 2>&1 | Out-Null

Write-Host "   Enter AlloyDB password:"
$dbPassword = Read-Host -AsSecureString
$dbPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPassword))

$connStr = "Host=localhost;Port=5432;Database=siges;Username=sigesbi;Password=$dbPasswordPlain;SslMode=Prefer;Trust Server Certificate=false"
dotnet user-secrets set "ConnectionStrings:Default" $connStr
dotnet user-secrets set "ConnectionStrings:Celero" "Host=Celero-bi.celero-one.com;Port=5432;Database=siges;Username=sigesbi;Password=$dbPasswordPlain;SslMode=Require"
dotnet user-secrets set "JwtSettings:SigningKey" "SIG-ES-JWT-Key-$(Get-Random)-ChangeInProduction"
dotnet user-secrets set "Seed:DemoPassword" "Demo#2026!"

Write-Host "   User-secrets configured"

# Build
Write-Host ""
Write-Host "5. Building backend..." -ForegroundColor Green
dotnet restore
dotnet build

Write-Host ""
Write-Host "=== SETUP COMPLETE ===" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host ""
Write-Host "Terminal 1 - Start the proxy:"
Write-Host "  $proxyPath projects/sig-prod/locations/us-central1/clusters/prod/instances/primary --port 5432"
Write-Host ""
Write-Host "Terminal 2 - Start the backend:"
Write-Host "  cd backend\SIG.API"
Write-Host "  dotnet run"
Write-Host ""
Write-Host "Terminal 3 - Start the frontend:"
Write-Host "  cd frontend"
Write-Host "  npm install"
Write-Host "  npm start"
Write-Host ""
Write-Host "Then open: http://localhost:4200"
Write-Host "Login: admin@sig.local / Demo#2026!"
Write-Host ""
