# ==============================================================================
# 1. Etapa de compilación y tests (build)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV LANG=es_ES.UTF-8
ENV LC_ALL=es_ES.UTF-8
# Restaurar dependencias
COPY ["BackEncordados/BackEncordados.csproj", "BackEncordados/"]
COPY ["TestEncordados/TestEncordados.csproj", "TestEncordados/"]
RUN dotnet restore "BackEncordados/BackEncordados.csproj"
RUN dotnet restore "TestEncordados/TestEncordados.csproj"

COPY . .

# Configuración Testcontainers
ARG DOCKER_HOST_ARG=tcp://host.docker.internal:2375
ENV DOCKER_HOST=$DOCKER_HOST_ARG

# Argumento condicional para tests
ARG RUN_TESTS=false

# Compilar
RUN dotnet build "BackEncordados/BackEncordados.csproj" -c Release -o /app/build
RUN dotnet build "TestEncordados/TestEncordados.csproj" -c Release

# Lógica condicional: Tests y Reporte HTML
RUN if [ "$RUN_TESTS" = "true" ] ; then \
         mkdir -p /app/coverage && \
         \
         echo "Instalando ReportGenerator..." && \
         dotnet tool install -g dotnet-reportgenerator-globaltool && \
         export PATH="$PATH:/root/.dotnet/tools" && \
         \
         echo "Ejecutando tests..." && \
         dotnet test "TestEncordados/TestEncordados.csproj" -c Release --no-build \
             /p:CollectCoverage=true \
             /p:CoverletOutput="/app/coverage/coverage" \
             /p:CoverletOutputFormat="cobertura" && \
         \
         echo "Generando reporte HTML..." && \
         reportgenerator \
             -reports:"/app/coverage/coverage.cobertura.xml" \
             -targetdir:"/app/coverage/html" \
             -reporttypes:Html ; \
     else \
         echo "Saltando tests (RUN_TESTS=false). Creando HTML por defecto..." && \
         mkdir -p /app/coverage/html && \
         echo "<h1>Cobertura no calculada</h1><p>Se compilo con RUN_TESTS=false</p>" > /app/coverage/html/index.html ; \
     fi

# Publicar API
RUN dotnet publish "BackEncordados/BackEncordados.csproj" -c Release -o /app/publish /p:UseAppHost=false


# ==============================================================================
# 2. nginx etapa coverageweb (Para ver el informe HTML en el navegador)
# ==============================================================================
FROM nginx:alpine AS coverageweb
WORKDIR /usr/share/nginx/html
RUN rm -rf *
# Copiamos la carpeta HTML generada en el paso anterior
COPY --from=build /app/coverage/html .


# ==============================================================================
# 3. Etapa de ejecución (run - API .NET)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS="http://*:${PORT:-8080}"

ENTRYPOINT ["dotnet", "BackEncordados.dll"]