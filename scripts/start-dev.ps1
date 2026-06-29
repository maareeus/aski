<#
.SYNOPSIS
    Avvia backend API (5095) e frontend Blazor (5200) per lo sviluppo.
.EXAMPLE
    .\scripts\start-dev.ps1
#>
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

function Write-Step($m) { Write-Host "`n==> $m" -ForegroundColor Cyan }

Write-Step 'Avvio Backend API (http://localhost:5095)'
Start-Process powershell -ArgumentList '-NoExit', '-Command',
    "`$Host.UI.RawUI.WindowTitle='Aski API'; `$env:ASPNETCORE_URLS='http://localhost:5095'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project '$Root/backend/Aski.Tickets.Api.csproj' --no-launch-profile"

Write-Step 'Avvio Frontend Blazor (http://localhost:5200)'
Start-Process powershell -ArgumentList '-NoExit', '-Command',
    "`$Host.UI.RawUI.WindowTitle='Aski Web'; `$env:ASPNETCORE_URLS='http://localhost:5200'; dotnet run --project '$Root/frontend/Aski.Tickets.Web.csproj' --no-launch-profile"

Write-Step 'Avviati'
Write-Host "Backend  : http://localhost:5095/swagger" -ForegroundColor Green
Write-Host "Frontend : http://localhost:5200  (login: admin@aski.local / ChangeMe123!)" -ForegroundColor Green
Start-Sleep -Seconds 6
Start-Process "http://localhost:5200"
