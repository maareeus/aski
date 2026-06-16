#!/usr/bin/env bash
#
# Avvia l'intero ambiente di sviluppo Aski (Linux/macOS).
#
#   1. Avvia (o crea) il container PostgreSQL condiviso.
#   2. Attende che Postgres sia pronto.
#   3. Applica le migration del Control Plane.
#   4. Lancia Control Plane e istanza Ticketing in background (log in scripts/logs/).
#
# Flag:
#   --fresh        Ricrea da zero il container Postgres (CANCELLA i dati esistenti).
#   --no-browser   Non aprire automaticamente il browser sulla dashboard.
#   --docker       Abilita il provisioning Docker reale (build immagine + rete aski-net).
#                  Senza questo flag il provisioning è simulato (modalità Logging).
#
# Esempi:
#   ./scripts/start-env.sh
#   ./scripts/start-env.sh --fresh
#   ./scripts/start-env.sh --docker
#
set -euo pipefail

# --- Parsing argomenti ---
FRESH=false
NO_BROWSER=false
DOCKER=false
for arg in "$@"; do
    case "$arg" in
        --fresh)      FRESH=true ;;
        --no-browser) NO_BROWSER=true ;;
        --docker)     DOCKER=true ;;
        *) echo "Argomento sconosciuto: $arg" >&2; exit 1 ;;
    esac
done

# --- Percorsi e costanti ---
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(dirname "$SCRIPT_DIR")"
CONTROL_PLANE="$ROOT/src/Aski.ControlPlane"
TICKETING="$ROOT/src/Aski.Ticketing.Api"
LOG_DIR="$SCRIPT_DIR/logs"
PID_DIR="$SCRIPT_DIR/.pids"
PG_CONTAINER='aski-pg'
PG_PASSWORD='postgres'
CONTROL_PLANE_URL='http://localhost:5080'
TICKETING_URL='http://localhost:5090'

mkdir -p "$LOG_DIR" "$PID_DIR"

# Colori (disattivati se non TTY).
if [ -t 1 ]; then
    C_CYAN=$'\033[36m'; C_GREEN=$'\033[32m'; C_YELLOW=$'\033[33m'; C_GRAY=$'\033[90m'; C_RESET=$'\033[0m'
else
    C_CYAN=''; C_GREEN=''; C_YELLOW=''; C_GRAY=''; C_RESET=''
fi
step() { printf '\n%s==> %s%s\n' "$C_CYAN" "$1" "$C_RESET"; }

# --- 0. Prerequisiti ---
step 'Controllo prerequisiti'
command -v docker >/dev/null 2>&1 || { echo 'Docker non trovato nel PATH.' >&2; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo 'dotnet non trovato nel PATH.' >&2; exit 1; }
# Assicura dotnet-ef nel PATH (tool globale).
export PATH="$PATH:$HOME/.dotnet/tools"
if ! command -v dotnet-ef >/dev/null 2>&1; then
    printf '%sdotnet-ef non trovato: installazione...%s\n' "$C_YELLOW" "$C_RESET"
    dotnet tool install --global dotnet-ef >/dev/null
fi

# --- 1. Container Postgres ---
step 'PostgreSQL'
exists="$(docker ps -a --filter "name=^/${PG_CONTAINER}$" --format '{{.Names}}')"
if [ "$FRESH" = true ] && [ -n "$exists" ]; then
    printf '%sRimozione container esistente (%s)...%s\n' "$C_YELLOW" "$PG_CONTAINER" "$C_RESET"
    docker rm -f "$PG_CONTAINER" >/dev/null
    exists=''
fi
if [ -z "$exists" ]; then
    echo "Creo il container $PG_CONTAINER..."
    docker run -d --name "$PG_CONTAINER" -e "POSTGRES_PASSWORD=$PG_PASSWORD" -p 5432:5432 postgres:16-alpine >/dev/null
else
    running="$(docker ps --filter "name=^/${PG_CONTAINER}$" --format '{{.Names}}')"
    if [ -z "$running" ]; then
        echo "Avvio il container $PG_CONTAINER..."
        docker start "$PG_CONTAINER" >/dev/null
    else
        echo "$PG_CONTAINER già in esecuzione."
    fi
fi

# --- 2. Attesa readiness ---
step 'Attendo che Postgres sia pronto'
ready=false
for _ in $(seq 1 30); do
    if docker exec "$PG_CONTAINER" pg_isready -U postgres >/dev/null 2>&1; then
        ready=true; break
    fi
    sleep 0.8
done
[ "$ready" = true ] || { echo 'Postgres non è diventato pronto in tempo.' >&2; exit 1; }
printf '%sPostgres pronto.%s\n' "$C_GREEN" "$C_RESET"

# --- 3. Migration Control Plane ---
step 'Ripristino pacchetti NuGet'
dotnet restore "$CONTROL_PLANE"
dotnet restore "$TICKETING"

step 'Applico le migration del Control Plane'
dotnet ef database update --project "$CONTROL_PLANE"
printf '%sMigration applicate.%s\n' "$C_GREEN" "$C_RESET"

# --- 3b. Provisioning Docker (opzionale) ---
PROVISIONING_MODE='Logging'
if [ "$DOCKER" = true ]; then
    step 'Provisioning Docker: immagine ticketing + rete'
    docker build -f "$TICKETING/Dockerfile" -t aski-ticketing:latest "$ROOT" >/dev/null
    if ! docker network inspect aski-net >/dev/null 2>&1; then
        docker network create aski-net >/dev/null
        echo 'Rete aski-net creata.'
    fi
    PROVISIONING_MODE='Docker'
    printf '%sImmagine e rete pronte. Provisioning reale abilitato.%s\n' "$C_GREEN" "$C_RESET"
    printf '%sRicorda: nel Config JSON del server usa dockerHost unix:///var/run/docker.sock, network aski-net, appImage aski-ticketing:latest.%s\n' "$C_GRAY" "$C_RESET"
fi

# --- 4. Avvio applicazioni ---
step 'Avvio Control Plane e istanza Ticketing'

start_app() {
    local title="$1" project="$2" url="$3" provisioning="$4"
    local slug log
    slug="$(echo "$title" | tr '[:upper:] ' '[:lower:]-')"
    log="$LOG_DIR/$slug.log"
    (
        export ASPNETCORE_URLS="$url"
        export ASPNETCORE_ENVIRONMENT='Development'
        [ -n "$provisioning" ] && export Provisioning__Mode="$provisioning"
        exec dotnet run --project "$project" --no-launch-profile
    ) >"$log" 2>&1 &
    echo $! > "$PID_DIR/$slug.pid"
    printf '  %s -> %s (log: %s)\n' "$title" "$url" "$log"
}

start_app 'Aski Control Plane' "$CONTROL_PLANE" "$CONTROL_PLANE_URL" "$PROVISIONING_MODE"
start_app 'Aski Ticketing API' "$TICKETING"    "$TICKETING_URL"     ''

# --- 5. Riepilogo ---
step 'Ambiente avviato'
printf '%sControl Plane : %s/Dashboard%s\n' "$C_GREEN" "$CONTROL_PLANE_URL" "$C_RESET"
printf '%sTicketing API : %s  (login: admin@aski.local / ChangeMe123!)%s\n' "$C_GREEN" "$TICKETING_URL" "$C_RESET"
printf '%sPostgres      : localhost:5432 (user/pass: postgres/postgres)%s\n' "$C_GREEN" "$C_RESET"
printf '%sProvisioning  : %s%s\n' "$C_GREEN" "$PROVISIONING_MODE" "$C_RESET"
if [ "$DOCKER" = true ]; then
    printf "%s  Dopo l'attivazione di un progetto: 'docker ps' mostra aski-pg-* e aski-app-*.%s\n" "$C_GRAY" "$C_RESET"
fi
echo
printf '%sPer i webhook Stripe in locale:%s\n' "$C_GRAY" "$C_RESET"
printf '%s  stripe listen --forward-to %s/api/stripe/webhook%s\n' "$C_GRAY" "$CONTROL_PLANE_URL" "$C_RESET"
printf '%sPer fermare tutto: ./scripts/stop-env.sh%s\n' "$C_GRAY" "$C_RESET"

if [ "$NO_BROWSER" != true ]; then
    sleep 4
    if command -v open >/dev/null 2>&1; then
        open "$CONTROL_PLANE_URL/Dashboard"            # macOS
    elif command -v xdg-open >/dev/null 2>&1; then
        xdg-open "$CONTROL_PLANE_URL/Dashboard" >/dev/null 2>&1 &  # Linux
    fi
fi
