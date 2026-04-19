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
Write-Host "Usando proyecto: $($csproj.Name)" -ForegroundColor Cyan

# Lista de paquetes con versiones y propiedades especiales si las tienen
$packages = @(
    @{ Name="AspNetCoreRateLimit"; Version="5.0.0" },
    @{ Name="BCrypt.Net-Next"; Version="4.0.3" },
    @{ Name="coverlet.msbuild"; Version="6.0.4";  },
    @{ Name="CSharpFunctionalExtensions"; Version="3.6.0" },
    @{ Name="GreenDonut"; Version="15.1.12" },
    @{ Name="Microsoft.AspNetCore.Identity.EntityFrameworkCore"},
    @{ Name="Stripe.net"; Version="43.14.0" },
    @{ Name="HotChocolate.AspNetCore"; Version="15.1.12" },
    @{ Name="HotChocolate.AspNetCore.Authorization"; Version="15.1.12" },
    @{ Name="HotChocolate.Data.EntityFramework"; Version="15.1.12" },
    @{ Name="HotChocolate.Subscriptions.InMemory"; Version="15.1.12" },
    @{ Name="MailKit"; Version="4.14.1" },
    @{ Name="Microsoft.AspNetCore.Authentication.JwtBearer"; Version="10.0.2" },
    @{ Name="Microsoft.EntityFrameworkCore"; Version="10.0.2" },
    @{ Name="Microsoft.EntityFrameworkCore.InMemory"; Version="10.0.2" },
    @{ Name="Microsoft.EntityFrameworkCore.Relational"; Version="10.0.2" },
    @{ Name="Microsoft.Extensions.Caching.StackExchangeRedis"; Version="10.0.2" },
    @{ Name="Microsoft.IdentityModel.Tokens"; Version="8.15.0" },
    @{ Name="MimeKit"; Version="4.15.1" },
    @{ Name="Npgsql.EntityFrameworkCore.PostgreSQL"; Version="10.0.0" },
    @{ Name="Serilog"; Version="4.3.1-dev-02395" },
    @{ Name="Serilog.AspNetCore"; Version="10.0.0" },
    @{ Name="Serilog.Settings.Configuration"; Version="10.0.0" },
    @{ Name="Serilog.Sinks.Console"; Version="6.1.1" },
    @{ Name="System.IdentityModel.Tokens.Jwt"; Version="8.15.0" },
    @{ Name="QuestPDF"; Version="2025.4.0" }
)

# Instala los paquetes
foreach ($pkg in $packages) {
    Write-Host "Instalando paquete: $($pkg.Name)..." -ForegroundColor Cyan

    # Construye el comando
    $cmd = "dotnet add `"$($csproj.Name)`" package $($pkg.Name) --version $($pkg.Version)"

    # Agrega IncludeAssets y PrivateAssets si existen
    if ($pkg.ContainsKey("IncludeAssets")) {
        $cmd += " --include-assets $($pkg.IncludeAssets)"
    }
    if ($pkg.ContainsKey("PrivateAssets")) {
        $cmd += " --private-assets $($pkg.PrivateAssets)"
    }

    # Ejecuta el comando
    Invoke-Expression $cmd

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Ocurrió un error instalando $($pkg.Name)"
    } else {
        Write-Host "$($pkg.Name) instalado correctamente." -ForegroundColor Green
    }
}

Write-Host "Todos los paquetes han sido procesados." -ForegroundColor Yellow