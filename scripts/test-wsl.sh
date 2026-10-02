#!/usr/bin/env bash
# Builds and tests the solution inside WSL (see ADR-9).
# Build output goes to the Linux file system, so Windows bin/obj stay untouched.
set -euo pipefail

export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.dotnet}"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

if ! ldconfig -p | grep -qi libicu; then
  export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
  echo "libicu not found: running in invariant globalization mode"
fi

cd "$(dirname "$0")/.."
artifacts="$HOME/.cache/progchecklist/artifacts"

dotnet build --artifacts-path "$artifacts"
dotnet test --no-build --artifacts-path "$artifacts"
