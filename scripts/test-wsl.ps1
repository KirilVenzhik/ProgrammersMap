# Runs build + tests in WSL (Ubuntu) when Smart App Control blocks local DLLs (ADR-9).
# Usage:  .\scripts\test-wsl.ps1
$ErrorActionPreference = "Stop"
$repo = Join-Path $PSScriptRoot ".."
wsl.exe -d Ubuntu --cd $repo -e bash scripts/test-wsl.sh
exit $LASTEXITCODE
