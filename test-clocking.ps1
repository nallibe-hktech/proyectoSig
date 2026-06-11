  # intratime-buscar-usuario.ps1

  $email = "notificaciones.sig@ftpsig.es"
  $password = 'Siges2025*'
  $from = (Get-Date).AddDays(-30).ToString("yyyy-MM-dd HH:mm:ss")
  $types = "0,1,2,3"
  $usernameBuscado = "VAZQUEZ"  # Buscar por parte del nombre

  Write-Host "========================================" -ForegroundColor Cyan
  Write-Host "INTRATIME - BUSCAR USUARIO CON FICHAJES" -ForegroundColor Cyan
  Write-Host "========================================" -ForegroundColor Cyan
  Write-Host ""

  # PASO 1: LOGIN
  Write-Host "PASO 1: Login..." -ForegroundColor Yellow
  $login = Invoke-RestMethod -Uri "https://newapi.intratime.es/api/companies/login" -Method Post `
      -Body "user=$email&password=$password" `
      -Headers @{
          "Accept" = "application/vnd.apiintratime.v1+json"
          "Content-Type" = "application/x-www-form-urlencoded; charset:utf8"
      }

  Write-Host "Login OK" -ForegroundColor Green
  Write-Host ""

  # PASO 2: LISTAR USUARIOS
  Write-Host "PASO 2: Usuarios disponibles:" -ForegroundColor Yellow
  $login.users | ForEach-Object {
      Write-Host "  - $($_.USER_NAME) (ID: $($_.USER_ID))" -ForegroundColor Gray
  }
  Write-Host ""

  # PASO 3: BUSCAR USUARIO
  Write-Host "PASO 3: Buscando usuario: $usernameBuscado" -ForegroundColor Yellow
  $userEncontrado = $login.users | Where-Object { $_.USER_NAME -like "*$usernameBuscado*" } | Select-Object -First 1

  if ($null -eq $userEncontrado) {
      Write-Host "Usuario no encontrado" -ForegroundColor Red
      exit
  }

  $token = $userEncontrado.USER_TOKEN
  $userId = $userEncontrado.USER_ID
  $userName = $userEncontrado.USER_NAME

  Write-Host "Usuario encontrado: $userName (ID: $userId)" -ForegroundColor Green
  Write-Host "Token: $($token.Substring(0,30))..." -ForegroundColor Green
  Write-Host ""

  # PASO 4: OBTENER FICHAJES
  Write-Host "PASO 4: Obteniendo fichajes..." -ForegroundColor Yellow
  Write-Host "Rango: desde $from" -ForegroundColor Gray

  $fromEncoded = [System.Web.HttpUtility]::UrlEncode($from)
  $url = "https://newapi.intratime.es/api/user/clockings?from=$fromEncoded&type=$types"

  $clockings = Invoke-RestMethod -Uri $url -Method Get `
      -Headers @{
          "Accept" = "application/vnd.apiintratime.v1+json"
          "Content-Type" = "application/x-www-form-urlencoded; charset:utf8"
          "token" = $token
      }

  Write-Host "Fichajes obtenidos: $($clockings.Count)" -ForegroundColor Green
  Write-Host ""

  # PASO 5: MOSTRAR RESULTADOS
  Write-Host "PASO 5: Primeros registros:" -ForegroundColor Yellow
  $clockings | Select-Object -First 5 | ConvertTo-Json -Depth 5 | Write-Host

  # PASO 6: GUARDAR
  $outputFile = "C:\Projects\workspaces\SIG-es\intratime-vazquez.json"
  $clockings | ConvertTo-Json -Depth 10 | Out-File -FilePath $outputFile -Encoding UTF8

  Write-Host ""
  Write-Host "Archivo guardado: $outputFile" -ForegroundColor Green