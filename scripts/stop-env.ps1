<#
.SYNOPSIS
    Ferma l'ambiente di sviluppo Aski.

.DESCRIPTION
    Termina i processi delle due app (sulle porte 5080/5090) e ferma il container
    Postgres. Con -RemoveDb rimuove anche il container (CANCELLA i dati).

.EXAMPLE
    .\scripts\stop-env.ps1
    .\scripts\stop-env.ps1 -RemoveDb
#>
param(
    [switch]$RemoveDb
)

$ErrorActionPreference = 'SilentlyContinue'
$PgContainer = 'aski-pg'

function Write-Step($msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

# --- App sulle porte note ---
Write-Step 'Arresto applicazioni (porte 5080, 5090)'
foreach ($port in 5080, 5090) {
    $conns = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
    foreach ($procId in ($conns.OwningProcess | Select-Object -Unique)) {
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
        Write-Host "  Fermato processo $procId (porta $port)"
    }
}

# --- Postgres ---
Write-Step 'PostgreSQL'
if ($RemoveDb) {
    docker rm -f $PgContainer | Out-Null
    Write-Host "  Container $PgContainer rimosso (dati cancellati)." -ForegroundColor Yellow
} else {
    docker stop $PgContainer | Out-Null
    Write-Host "  Container $PgContainer fermato (dati conservati)."
}

Write-Host "`nAmbiente fermato." -ForegroundColor Green
