# Lance backend et frontend en parallele dans 2 fenetres PowerShell.
# Usage : .\start-all.ps1

$root = $PSScriptRoot

Start-Process powershell.exe -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $root 'start-backend.ps1')
) -WorkingDirectory $root

Start-Sleep -Seconds 3

Start-Process powershell.exe -ArgumentList @(
    '-NoExit',
    '-ExecutionPolicy', 'Bypass',
    '-File', (Join-Path $root 'start-frontend.ps1')
) -WorkingDirectory $root

Write-Host ""
Write-Host "Deux fenetres PowerShell ont ete ouvertes :" -ForegroundColor Green
Write-Host "  - Backend  : http://localhost:5080  (Swagger : /swagger)"
Write-Host "  - Frontend : http://localhost:5173"
Write-Host ""
Write-Host "Login : admin / Admin@123"
Write-Host "Pour arreter : Ctrl+C dans chaque fenetre."
