# Autopilot: runs /next in a loop in headless mode.
# Usage:  .\scripts\autopilot.ps1 -MaxTasks 5
# Stops when: MaxTasks reached, the phase ends (STATUS says review needed), or a run fails.
param([int]$MaxTasks = 5)

$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "..")

for ($i = 1; $i -le $MaxTasks; $i++) {
    Write-Host "=== Task $i of $MaxTasks ===" -ForegroundColor Cyan
    claude -p "/next" --model opus --permission-mode acceptEdits
    if ($LASTEXITCODE -ne 0) { Write-Host "Run failed, stopping." -ForegroundColor Red; break }

    $status = Get-Content "docs/STATUS.md" -Raw -Encoding UTF8
    if ($status -match "REVIEW-NEEDED") { Write-Host "Phase finished. Review needed." -ForegroundColor Yellow; break }
}
git log --oneline -n $MaxTasks
