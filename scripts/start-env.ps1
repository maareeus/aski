<#
.SYNOPSIS
    Avvia l'intero ambiente di sviluppo Aski.

.DESCRIPTION
    1. Avvia (o crea) il container PostgreSQL condiviso.
    2. Attende che Postgres sia pronto.
    3. Applica le migration del Control Plane.
    4. Lancia Control Plane e istanza Ticketing in due finestre separate.

.PARAMETER Fresh
    Ricrea da zero il container Postgres (CANCELLA i dati esistenti).

.PARAMETER NoBrowser
    Non aprire automaticamente il browser sulla dashboard.

.PARAMETER Docker
    Abilita il provisioning Docker reale: builda l'immagine ticketing, crea la rete
    'aski-net' e avvia il Control Plane con Provisioning:Mode=Docker. Senza questo flag
    il provisioning è simulato (modalità Logging).

.EXAMPLE
    .\scripts\start-env.ps1
    .\scripts\start-env.ps1 -Fresh
    .\scripts\start-env.ps1 -Docker
#>
param(
    [switch]$Fresh,
    [switch]$NoBrowser,
    [switch]$Docker
)

$ErrorActionPreference = 'Stop'

# --- Percorsi e costanti ---
$Root          = Split-Path $PSScriptRoot -Parent
$ControlPlane  = Join-Path $Root 'src/Aski.ControlPlane'
$Ticketing     = Join-Path $Root 'src/Aski.Ticketing.Api'
$PgContainer   = 'aski-pg'
$PgPassword    = 'postgres'
$ControlPlaneUrl = 'http://localhost:5080'
$TicketingUrl    = 'http://localhost:5090'

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

# --- 0. Prerequisiti ---
Write-Step 'Controllo prerequisiti'
if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { throw 'Docker non trovato nel PATH.' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet non trovato nel PATH.' }
# Assicura dotnet-ef nel PATH (tool globale).
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"
if (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Host 'dotnet-ef non trovato: installazione...' -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef | Out-Null
}

# --- 1. Container Postgres ---
Write-Step 'PostgreSQL'
$exists = (docker ps -a --filter "name=^/$PgContainer$" --format '{{.Names}}')
if ($Fresh -and $exists) {
    Write-Host "Rimozione container esistente ($PgContainer)..." -ForegroundColor Yellow
    docker rm -f $PgContainer | Out-Null
    $exists = $null
}
if (-not $exists) {
    Write-Host "Creo il container $PgContainer..."
    docker run -d --name $PgContainer -e "POSTGRES_PASSWORD=$PgPassword" -p 5432:5432 postgres:16-alpine | Out-Null
} else {
    $running = (docker ps --filter "name=^/$PgContainer$" --format '{{.Names}}')
    if (-not $running) { Write-Host "Avvio il container $PgContainer..."; docker start $PgContainer | Out-Null }
    else { Write-Host "$PgContainer già in esecuzione." }
}

# --- 2. Attesa readiness ---
Write-Step 'Attendo che Postgres sia pronto'
$ready = $false
foreach ($i in 1..30) {
    docker exec $PgContainer pg_isready -U postgres 2>$null | Out-Null
    if ($?) { $ready = $true; break }
    Start-Sleep -Milliseconds 800
}
if (-not $ready) { throw 'Postgres non è diventato pronto in tempo.' }
Write-Host 'Postgres pronto.' -ForegroundColor Green

# --- 3. Migration Control Plane ---
Write-Step 'Applico le migration del Control Plane'
dotnet ef database update --project $ControlPlane
Write-Host 'Migration applicate.' -ForegroundColor Green

# --- 3b. Provisioning Docker (opzionale) ---
$provisioningMode = 'Logging'
if ($Docker) {
    Write-Step 'Provisioning Docker: immagine ticketing + rete'
    docker build -f (Join-Path $Ticketing 'Dockerfile') -t aski-ticketing:latest $Root | Out-Null
    docker network inspect aski-net 2>$null | Out-Null
    if (-not $?) { docker network create aski-net | Out-Null; Write-Host 'Rete aski-net creata.' }
    $provisioningMode = 'Docker'
    Write-Host 'Immagine e rete pronte. Provisioning reale abilitato.' -ForegroundColor Green
    Write-Host "Ricorda: nel Config JSON del server usa dockerHost npipe://./pipe/docker_engine, network aski-net, appImage aski-ticketing:latest." -ForegroundColor DarkGray
}

# --- 4. Avvio applicazioni ---
Write-Step 'Avvio Control Plane e istanza Ticketing'

function Start-App($title, $project, $url, $extraEnv) {
    $cmd = "`$Host.UI.RawUI.WindowTitle='$title'; " +
           "`$env:ASPNETCORE_URLS='$url'; " +
           "`$env:ASPNETCORE_ENVIRONMENT='Development'; " +
           $extraEnv +
           "dotnet run --project '$project' --no-launch-profile"
    Start-Process powershell -ArgumentList '-NoExit', '-Command', $cmd | Out-Null
}

Start-App 'Aski Control Plane' $ControlPlane $ControlPlaneUrl "`$env:Provisioning__Mode='$provisioningMode'; "
Start-App 'Aski Ticketing API'  $Ticketing    $TicketingUrl ''

# --- 5. Riepilogo ---
Write-Step 'Ambiente avviato'
Write-Host "Control Plane : $ControlPlaneUrl/Dashboard" -ForegroundColor Green
Write-Host "Ticketing API : $TicketingUrl  (login: admin@aski.local / ChangeMe123!)" -ForegroundColor Green
Write-Host "Postgres      : localhost:5432 (user/pass: postgres/postgres)" -ForegroundColor Green
Write-Host "Provisioning  : $provisioningMode" -ForegroundColor Green
if ($Docker) { Write-Host "  Dopo l'attivazione di un progetto: 'docker ps' mostra aski-pg-* e aski-app-*." -ForegroundColor DarkGray }
Write-Host ""
Write-Host "Per i webhook Stripe in locale:" -ForegroundColor DarkGray
Write-Host "  stripe listen --forward-to $ControlPlaneUrl/api/stripe/webhook" -ForegroundColor DarkGray
Write-Host "Per fermare tutto: .\scripts\stop-env.ps1" -ForegroundColor DarkGray

if (-not $NoBrowser) {
    Start-Sleep -Seconds 4
    Start-Process "$ControlPlaneUrl/Dashboard"
}
