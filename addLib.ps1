param (
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath
)

# Verifica que la carpeta existe
if (-Not (Test-Path $ProjectPath)) {
    Write-Error "La ruta '$ProjectPath' no existe."
    exit 1
}

# Detecta el archivo .csproj
$csproj = Get-ChildItem -Path $ProjectPath -Filter *.csproj | Select-Object -First 1
if (-not $csproj) {
    Write-Error "No se encontró un archivo .csproj en la carpeta."
    exit 1
}

# Cambia al directorio del proyecto
Set-Location $ProjectPath
Write-Host "Sincronizando paquetes para: $($csproj.Name) (Target: net10.0)" -ForegroundColor Cyan

$packages = @(
    @{ Name="AspNetCoreRateLimit"; Version="5.0.0" },
    @{ Name="BCrypt.Net-Next"; Version="4.0.3" },
    @{
        Name="coverlet.msbuild";
        Version="6.0.4";
        IncludeAssets="runtime; build; native; contentfiles; analyzers; buildtransitive";
        PrivateAssets="all"
    },
    @{ Name="CSharpFunctionalExtensions"; Version="3.6.0" },
    @{ Name="FluentValidation"; Version="12.1.1" },
    @{ Name="FluentValidation.AspNetCore"; Version="11.3.1" },
    @{ Name="FluentValidation.DependencyInjectionExtensions"; Version="12.1.1" },
    @{ Name="GreenDonut"; Version="15.1.14" },
    @{ Name="HotChocolate.AspNetCore"; Version="15.1.14" },
    @{ Name="HotChocolate.AspNetCore.Authorization"; Version="15.1.14" },
    @{ Name="HotChocolate.Data.EntityFramework"; Version="15.1.14" },
    @{ Name="HotChocolate.Subscriptions.InMemory"; Version="15.1.14" },
    @{ Name="MailKit"; Version="4.16.0" },
    @{ Name="MimeKit"; Version="4.16.0" },
    @{ Name="Microsoft.AspNetCore.Authentication.JwtBearer"; Version="10.0.2" },
    @{ Name="Microsoft.AspNetCore.OpenApi"; Version="10.0.2" },
    @{ Name="Microsoft.EntityFrameworkCore"; Version="10.0.2" },
    @{ Name="Microsoft.EntityFrameworkCore.InMemory"; Version="10.0.2" },
    @{ Name="Microsoft.EntityFrameworkCore.Relational"; Version="10.0.2" },
    @{ Name="Microsoft.Extensions.Caching.StackExchangeRedis"; Version="10.0.2" },
    @{ Name="Microsoft.IdentityModel.Tokens"; Version="8.15.0" },
    @{ Name="System.IdentityModel.Tokens.Jwt"; Version="8.15.0" },
    @{ Name="Npgsql.EntityFrameworkCore.PostgreSQL"; Version="10.0.0" },
    @{ Name="QuestPDF"; Version="2025.4.0" },
    @{ Name="Serilog"; Version="4.3.1-dev-02395" },
    @{ Name="Serilog.AspNetCore"; Version="10.0.0" },
    @{ Name="Serilog.Settings.Configuration"; Version="10.0.0" },
    @{ Name="Serilog.Sinks.Console"; Version="6.1.1" },
    @{ Name="Stripe.net"; Version="43.14.0" }
)

# Instala los paquetes
foreach ($pkg in $packages) {
    Write-Host "📦 Instalando $($pkg.Name) (@$($pkg.Version))..." -ForegroundColor Cyan

    # Construye el comando base
    $cmd = "dotnet add `"$($csproj.Name)`" package $($pkg.Name) --version $($pkg.Version)"

    # Agrega metadatos de assets si existen (caso coverlet)
    if ($pkg.ContainsKey("IncludeAssets")) {
        $cmd += " --include-assets `"$($pkg.IncludeAssets)`""
    }
    if ($pkg.ContainsKey("PrivateAssets")) {
        $cmd += " --private-assets `"$($pkg.PrivateAssets)`""
    }

    # Ejecuta el comando
    Invoke-Expression $cmd

    if ($LASTEXITCODE -ne 0) {
        Write-Warning " Error al procesar $($pkg.Name)"
    }
}

Write-Host "`nSincronización completada. Todos los paquetes coinciden con el .csproj." -ForegroundColor Yellow