param(
    [ValidateSet("docker", "local")]
    [string]$Env = ""
)

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BrunoDir = Resolve-Path (Join-Path $ScriptDir "..\bruno-cli")
$ReportFile = Join-Path $BrunoDir "bruno-report.html"

if ($Env -eq "") {
    Write-Host ""
    Write-Host "Seleccioná entorno:"
    Write-Host "  1) docker"
    Write-Host "  2) local"
    $choice = Read-Host "Entorno (1/2)"
    $Env = if ($choice -eq "2") { "local" } else { "docker" }
}

Write-Host ""
Write-Host "========================================="
Write-Host " Ejecutando tests con entorno: $Env"
Write-Host " Reporte: $ReportFile"
Write-Host "========================================="
Write-Host ""

Set-Location $BrunoDir
$output = bru run --env $Env --insecure --reporter-html $ReportFile 2>&1
Write-Host $output

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================="
    Write-Host " Tests pasaron. Abriendo reporte HTML..."
    Write-Host "========================================="
    Start-Process $ReportFile
} else {
    Write-Host ""
    Write-Host "========================================="
    Write-Host " ALGUNOS TESTS FALLARON"
    Write-Host "========================================="
}

Set-Location $ScriptDir
