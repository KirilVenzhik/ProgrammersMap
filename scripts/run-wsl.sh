#!/usr/bin/env bash
# Runs the web app inside WSL (see ADR-9); WSL 2 forwards localhost to Windows.
set -euo pipefail

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1
export ASPNETCORE_ENVIRONMENT=Development

if ! ldconfig -p | grep -qi libicu; then
  export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
fi

cd "$(dirname "$0")/.."
artifacts="$HOME/.cache/progchecklist/artifacts"
url="${1:-http://localhost:5043}"

dotnet build src/ProgChecklist.Web --artifacts-path "$artifacts"
cd src/ProgChecklist.Web
echo "Open $url (Ctrl+C to stop)"
exec dotnet "$artifacts/bin/ProgChecklist.Web/debug/ProgChecklist.Web.dll" --urls "$url"
