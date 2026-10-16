#!/usr/bin/env bash
# Runs the web app inside WSL (see ADR-9); WSL 2 forwards localhost to Windows.
# The database connection string comes from ConnectionStrings__Default or, if unset, from .env (ADR-12).
set -euo pipefail

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
export ASPNETCORE_ENVIRONMENT=Development

if ! ldconfig -p | grep -qi libicu; then
  export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
fi

cd "$(dirname "$0")/.."

if [ -z "${ConnectionStrings__Default:-}" ]; then
  if [ -f ./.env ]; then
    set -a
    # strip CR so a Windows-edited .env works
    . <(tr -d '\r' < ./.env)
    set +a
    : "${POSTGRES_DB:?POSTGRES_DB is not set in .env}"
    : "${POSTGRES_USER:?POSTGRES_USER is not set in .env}"
    : "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is not set in .env}"
    export ConnectionStrings__Default="Host=localhost;Port=${POSTGRES_PORT:-5432};Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
  else
    echo "No database configured. Copy .env.example to .env, set POSTGRES_PASSWORD," >&2
    echo "run 'docker compose up -d', then start this script again." >&2
    echo "(Or set the ConnectionStrings__Default environment variable.)" >&2
    exit 1
  fi
fi

artifacts="$HOME/.cache/progchecklist/artifacts"
url="${1:-http://localhost:5043}"

dotnet build src/ProgChecklist.Web --artifacts-path "$artifacts"
cd src/ProgChecklist.Web
echo "Open $url (Ctrl+C to stop)"
exec dotnet "$artifacts/bin/ProgChecklist.Web/debug/ProgChecklist.Web.dll" --urls "$url"
