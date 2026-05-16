# ============================================
# Stage 1: Build
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:latest AS build
WORKDIR /src

COPY ["BackEncordados/BackEncordados.csproj", "BackEncordados/"]
COPY ["TestEncordados/TestEncordados.csproj", "TestEncordados/"]

RUN dotnet restore "BackEncordados/BackEncordados.csproj"
RUN dotnet restore "TestEncordados/TestEncordados.csproj"

COPY . .

WORKDIR "/src/BackEncordados"
RUN dotnet build "BackEncordados.csproj" -c Release -o /app/build
RUN dotnet publish "BackEncordados.csproj" -c Release -o /app/publish

# ============================================
# Stage 2: Tests
# ============================================
FROM build AS test
WORKDIR "/src/TestEncordados"
RUN dotnet test "TestEncordados.csproj" --no-build -v normal

# ============================================
# Stage 3: Runtime
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:latest AS final
WORKDIR /app

EXPOSE 80
EXPOSE 5001

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BackEncordados.dll"]