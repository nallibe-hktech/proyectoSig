# debug-intratime-json.ps1
# Obtiene un evento de Intratime y guarda el JSON raw

$email = "notificaciones.sig@ftpsig.es"
$password = 'Siges2025*'

Write-Host "Login en Intratime..." -ForegroundColor Cyan
$login = Invoke-RestMethod -Uri "https://newapi.intratime.es/api/companies/login" -Method Post `
    -Body "user=$email&password=$password" `
    -Headers @{
        "Accept" = "application/vnd.apiintratime.v1+json"
        "Content-Type" = "application/x-www-form-urlencoded"
    }

$token = $login.users[0].USER_TOKEN
Write-Host "Token obtenido: $($token.Substring(0,30))..." -ForegroundColor Green

Write-Host "Obteniendo clockings..." -ForegroundColor Cyan
$from = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss")
$fromEncoded = [System.Web.HttpUtility]::UrlEncode($from)
$url = "https://newapi.intratime.es/api/user/clockings?from=$fromEncoded&type=0,1,2,3"

$response = Invoke-RestMethod -Uri $url -Method Get `
    -Headers @{
        "Accept" = "application/vnd.apiintratime.v1+json"
        "Content-Type" = "application/x-www-form-urlencoded; charset:utf8"
        "token" = $token
    }

# Guardar JSON completo
$outputFile = "C:\Projects\workspaces\SIG-es\intratime-raw.json"
$response | ConvertTo-Json -Depth 10 | Out-File -FilePath $outputFile -Encoding UTF8
Write-Host "Archivo guardado: $outputFile" -ForegroundColor Green

# Mostrar estructura del primer evento
Write-Host "Total eventos: $($response.Count)" -ForegroundColor Green

if ($response.Count -gt 0) {
    Write-Host "Primer evento:" -ForegroundColor Yellow
    $response[0] | ConvertTo-Json -Depth 10 | Write-Host
}
