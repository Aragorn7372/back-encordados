# run-tests-with-html-coverage.ps1
# Ejecuta tests de C# con cobertura y genera reporte HTML
# El reporte se crea en la carpeta desde donde ejecutas el script

param(
    [string]$Project = ""
)

$RootDir = Get-Location
$CoverageDir = Join-Path $RootDir "coverage"

Write-Host "========================================"
Write-Host " Ejecutando tests y generando coverage "
Write-Host "========================================"

# Crear carpeta coverage
if (Test-Path $CoverageDir) {
    Remove-Item $CoverageDir -Recurse -Force
}

New-Item -ItemType Directory -Path $CoverageDir | Out-Null

# Verificar reportgenerator
$reportGeneratorInstalled = dotnet tool list -g | Select-String "dotnet-reportgenerator-globaltool"

if (-not $reportGeneratorInstalled) {
    Write-Host "Instalando ReportGenerator..."
    dotnet tool install -g dotnet-reportgenerator-globaltool
}

# Construir comando
$cmd = "dotnet test"

if ($Project -ne "") {
    $cmd += " `"$Project`""
}

# IMPORTANTE:
# CoverletOutput apunta a la carpeta raíz desde donde ejecutas el script
$cmd += " /p:CollectCoverage=true"
$cmd += " /p:CoverletOutput=`"$CoverageDir/coverage.`""
$cmd += " /p:CoverletOutputFormat=cobertura"

Write-Host ""
Write-Host "Ejecutando tests..."
Write-Host $cmd
Write-Host ""

Invoke-Expression $cmd

if ($LASTEXITCODE -ne 0) {
    Write-Error "Los tests fallaron."
    exit $LASTEXITCODE
}

# Buscar archivo cobertura generado
$coverageFile = Get-ChildItem $CoverageDir -Filter "*.cobertura.xml" -Recurse | Select-Object -First 1

if (-not $coverageFile) {
    Write-Error "No se encontró el archivo de coverage."
    exit 1
}

Write-Host ""
Write-Host "Generando reporte HTML..."

reportgenerator `
    "-reports:$($coverageFile.FullName)" `
    "-targetdir:$CoverageDir/html" `
    "-reporttypes:Html"

$indexFile = Join-Path $CoverageDir "html/index.html"

Write-Host ""
Write-Host "========================================"
Write-Host " Coverage generado correctamente"
Write-Host "========================================"
Write-Host "HTML Report:"
Write-Host $indexFile
Write-Host ""

# Abrir automáticamente el reporte
Start-Process $indexFile