# Lance le frontend Vite + React sur http://localhost:5173
# Usage : .\start-frontend.ps1

$ErrorActionPreference = 'Stop'

$root = $PSScriptRoot
$webDir = Join-Path $root 'frontend\cashapp-web'
$nodeDir = Join-Path $root '.tools\node-v20.11.1-win-x64'

# Tue tout processus deja sur le port 5173
$conn = Get-NetTCPConnection -LocalPort 5173 -State Listen -ErrorAction SilentlyContinue
if ($conn) {
    Write-Host "Port 5173 deja utilise - arret du processus existant..." -ForegroundColor Yellow
    $conn | Select-Object -ExpandProperty OwningProcess -Unique | ForEach-Object {
        Stop-Process -Id $_ -Force -ErrorAction SilentlyContinue
    }
    Start-Sleep -Seconds 2
}

# Node portable inclus dans le repo (politique Enterprise bloque l'install MSI).
if (-not (Test-Path $nodeDir)) {
    Write-Host "Node portable introuvable a $nodeDir" -ForegroundColor Red
    Write-Host "Telechargement de Node 20.11.1..." -ForegroundColor Cyan
    $tools = Join-Path $root '.tools'
    New-Item -ItemType Directory -Force -Path $tools | Out-Null
    $zip = Join-Path $tools 'node.zip'
    Invoke-WebRequest -Uri 'https://nodejs.org/dist/v20.11.1/node-v20.11.1-win-x64.zip' -OutFile $zip -UseBasicParsing
    Expand-Archive -Path $zip -DestinationPath $tools -Force
    Remove-Item $zip
}

$env:Path = "$nodeDir;$env:Path"

if (-not (Test-Path (Join-Path $webDir '.env'))) {
    Copy-Item (Join-Path $webDir '.env.example') (Join-Path $webDir '.env')
    Write-Host ".env cree depuis .env.example" -ForegroundColor Cyan
}

if (-not (Test-Path (Join-Path $webDir 'node_modules'))) {
    Write-Host "Premiere installation : npm install (peut prendre 1-3 min)..." -ForegroundColor Cyan
    Set-Location $webDir
    npm install --no-audit --no-fund
    if ($LASTEXITCODE -ne 0) { throw "npm install a echoue" }
}

Set-Location $webDir
Write-Host "Frontend en cours de demarrage sur http://localhost:5173 ..." -ForegroundColor Green
Write-Host "Login : admin / Admin@123 (assure-toi que le backend tourne sur :5080)" -ForegroundColor Green
Write-Host "Ctrl+C pour arreter." -ForegroundColor DarkGray
Write-Host ""

npm run dev
