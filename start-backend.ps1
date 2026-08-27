# Lance le backend ASP.NET Core sur http://localhost:5080
# Usage : .\start-backend.ps1

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$apiDir = Join-Path $root 'backend\src\CashApp.Api'
$dll = Join-Path $apiDir 'bin\Debug\net8.0\CashApp.Api.dll'

# Tue tout processus deja sur le port 5080
$conn = Get-NetTCPConnection -LocalPort 5080 -State Listen -ErrorAction SilentlyContinue
if ($conn) {
    Write-Host "Port 5080 deja utilise - arret du processus existant..." -ForegroundColor Yellow
    $conn | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object {
        Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

if (-not (Test-Path $dll)) {
    Write-Host "Build initiale (la DLL n'existe pas encore)..." -ForegroundColor Cyan
    dotnet build (Join-Path $root 'backend\CashApp.sln') -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "Build a echoue" }
}

# Contournements Enterprise : ASP.NET Core 8 runtime absent -> roll-forward vers 10 ;
# `dotnet exec` evite l'.exe bloque par la politique d'execution.
$env:DOTNET_ROLL_FORWARD   = 'Major'
$env:ASPNETCORE_URLS       = 'http://localhost:5080'
$env:ASPNETCORE_ENVIRONMENT = 'Development'

Set-Location $apiDir
Write-Host "Backend en cours de demarrage sur http://localhost:5080 ..." -ForegroundColor Green
Write-Host "Swagger : http://localhost:5080/swagger" -ForegroundColor Green
Write-Host "Login   : admin / Admin@123" -ForegroundColor Green
Write-Host "Ctrl+C pour arreter." -ForegroundColor DarkGray
Write-Host ""

dotnet exec $dll
