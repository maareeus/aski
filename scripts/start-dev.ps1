<#
.SYNOPSIS
    Avvia backend API (5095), pannello admin (5200) e customer portal (5300).
.EXAMPLE
    .\scripts\start-dev.ps1
#>
$ErrorActionPreference = 'Stop'
$Root = Split-Path $PSScriptRoot -Parent

function Write-Step($m) { Write-Host "`n==> $m" -ForegroundColor Cyan }

Write-Step 'Avvio Backend API (http://localhost:5095)'
Start-Process powershell -ArgumentList '-NoExit', '-Command',
    "`$Host.UI.RawUI.WindowTitle='Aski API'; `$env:ASPNETCORE_URLS='http://localhost:5095'; `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project '$Root/backend/Aski.Tickets.Api.csproj' --no-launch-profile"

Write-Step 'Avvio Pannello Admin (http://localhost:5200)'
Start-Process powershell -ArgumentList '-NoExit', '-Command',
    "`$Host.UI.RawUI.WindowTitle='Aski Admin'; `$env:ASPNETCORE_URLS='http://localhost:5200'; dotnet run --project '$Root/frontend/Aski.Tickets.Web.csproj' --no-launch-profile"

Write-Step 'Avvio Customer Portal (http://localhost:5300)'
Start-Process powershell -ArgumentList '-NoExit', '-Command',
    "`$Host.UI.RawUI.WindowTitle='Aski Portal'; `$env:ASPNETCORE_URLS='http://localhost:5300'; dotnet run --project '$Root/customer-portal/Aski.Tickets.Portal.csproj' --no-launch-profile"

Write-Step 'Avviati'
Write-Host "Backend  : http://localhost:5095/swagger" -ForegroundColor Green
Write-Host "Admin    : http://localhost:5200  (admin@aski.local / ChangeMe123!)" -ForegroundColor Green
Write-Host "Portal   : http://localhost:5300  (accesso solo clienti)" -ForegroundColor Green
Start-Sleep -Seconds 6
Start-Process "http://localhost:5200"
Start-Process "http://localhost:5300"
