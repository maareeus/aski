#!/usr/bin/env bash
#
# Ferma l'ambiente di sviluppo Aski (Linux/macOS).
#
# Termina i processi delle due app (sulle porte 5080/5090) e ferma il container
# Postgres. Con --remove-db rimuove anche il container (CANCELLA i dati).
#
# Esempi:
#   ./scripts/stop-env.sh
#   ./scripts/stop-env.sh --remove-db
#
set -uo pipefail

REMOVE_DB=false
for arg in "$@"; do
    case "$arg" in
        --remove-db) REMOVE_DB=true ;;
        *) echo "Argomento sconosciuto: $arg" >&2; exit 1 ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PID_DIR="$SCRIPT_DIR/.pids"
PG_CONTAINER='aski-pg'

if [ -t 1 ]; then
    C_CYAN=$'\033[36m'; C_GREEN=$'\033[32m'; C_YELLOW=$'\033[33m'; C_RESET=$'\033[0m'
else
    C_CYAN=''; C_GREEN=''; C_YELLOW=''; C_RESET=''
fi
step() { printf '\n%s==> %s%s\n' "$C_CYAN" "$1" "$C_RESET"; }

# --- App sulle porte note ---
step 'Arresto applicazioni (porte 5080, 5090)'
for port in 5080 5090; do
    pids="$(lsof -ti tcp:"$port" -sTCP:LISTEN 2>/dev/null || true)"
    for pid in $pids; do
        kill "$pid" 2>/dev/null && echo "  Fermato processo $pid (porta $port)"
    done
done
# Pulisce eventuali PID file lasciati da start-env.sh.
rm -f "$PID_DIR"/*.pid 2>/dev/null || true

# --- Postgres ---
step 'PostgreSQL'
if [ "$REMOVE_DB" = true ]; then
    docker rm -f "$PG_CONTAINER" >/dev/null 2>&1 || true
    printf '%s  Container %s rimosso (dati cancellati).%s\n' "$C_YELLOW" "$PG_CONTAINER" "$C_RESET"
else
    docker stop "$PG_CONTAINER" >/dev/null 2>&1 || true
    echo "  Container $PG_CONTAINER fermato (dati conservati)."
fi

printf '\n%sAmbiente fermato.%s\n' "$C_GREEN" "$C_RESET"
