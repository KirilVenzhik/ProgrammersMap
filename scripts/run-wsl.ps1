# Runs the web app in WSL (Ubuntu) when Smart App Control blocks local DLLs (ADR-9).
# Needs the local database: copy .env.example to .env, then `docker compose up -d` (ADR-12).
# Usage:  .\scripts\run-wsl.ps1   then open http://localhost:5043
$ErrorActionPreference = "Stop"
$repo = Join-Path $PSScriptRoot ".."
wsl.exe -d Ubuntu --cd $repo -e bash scripts/run-wsl.sh @args
exit $LASTEXITCODE
