$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot

$TestDir = Join-Path $ProjectRoot "TestEncordados"
$OutputDir = Join-Path $ProjectRoot "coverage-report"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$Filter = "FullyQualifiedName~Unit"

Write-Host "Running unit tests with coverage..." -ForegroundColor Cyan

dotnet test "$TestDir" `
    --filter $Filter `
    --collect:"XPlat Code Coverage" `
    -- RunConfiguration.DisableAutoOutputBuffers=true `
    2>&1

if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host "Finding coverage files..." -ForegroundColor Cyan
$latestCoverage = Get-ChildItem -Path "$TestDir\TestResults" -Filter "coverage.cobertura.xml" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if ($null -eq $latestCoverage) {
    Write-Host "No coverage file found!" -ForegroundColor Red
    exit 1
}

Write-Host "Generating HTML report..." -ForegroundColor Cyan
reportgenerator "-reports:$($latestCoverage.FullName)" "-targetdir:$OutputDir" "-reporttypes:Html;Badges" 2>&1

Write-Host "Coverage report generated at: $OutputDir\index.html" -ForegroundColor Green