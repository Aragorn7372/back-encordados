# back-encordados 🔧

> API REST para la plataforma de gestión de talleres de encordado. Construida con .NET 10, EF Core, SignalR y múltiples bases de datos.

<div align="center">

[![CI](https://img.shields.io/badge/CI-Trigger_Parent_Build-blue?logo=githubactions)](https://github.com/Aragorn7372/back-encordados/actions/workflows/trigger-parent.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![EF Core](https://img.shields.io/badge/EF_Core-10-512BD4?logo=dotnet)](https://learn.microsoft.com/ef/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)](https://www.postgresql.org/)
[![MongoDB](https://img.shields.io/badge/MongoDB-7-47A248?logo=mongodb)](https://www.mongodb.com/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D?logo=redis)](https://redis.io/)
[![SignalR](https://img.shields.io/badge/SignalR-✓-512BD4?logo=dotnet)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Docker](https://img.shields.io/badge/Docker-✓-2496ED?logo=docker)](https://www.docker.com/)
[![JWT](https://img.shields.io/badge/Auth-JWT_Bearer-000000?logo=jsonwebtokens)](https://jwt.io/)
[![License](https://img.shields.io/badge/License-MIT-yellow)](LICENSE)

</div>

---

## Índice

- [Stack](#stack)
- [Estructura del proyecto](#estructura-del-proyecto)
- [Inicio rápido](#inicio-rápido)
- [Tests](#tests)
- [Scripts](#scripts)
- [Pipeline de middleware](#pipeline-de-middleware)
- [Endpoints principales](#endpoints-principales)
- [Eventos SignalR](#eventos-signalr)
- [Estrategia de bases de datos](#estrategia-de-bases-de-datos)
- [Caché](#caché)
- [Variables de entorno](#variables-de-entorno)
- [Patrones y decisiones de diseño](#patrones-y-decisiones-de-diseño)
- [Colección Bruno](#colección-bruno)
- [Contribución](#contribución)
- [Desarrolladores](#desarrolladores)

---

## Stack

| | |
|-|-|
| **Runtime** | .NET 10 / ASP.NET Core 10 |
| **ORM** | Entity Framework Core 10 |
| **Bases de datos** | PostgreSQL 16 · MongoDB 7 · SQLite (tests) |
| **Caché** | Redis 7 (HybridCache) + MemoryCache |
| **API REST** | ASP.NET Core Controllers |
| **Tiempo real** | SignalR (`/hubs/signal`) |
| **Validación** | FluentValidation 12 |
| **Resultados** | CSharpFunctionalExtensions (`Result<T>`) |
| **Imágenes** | CloudinaryDotNet |
| **Email** | MailKit + background service (`EmailBackgroundService`) |
| **WhatsApp** | Meta API v25.0 |
| **Excel** | ClosedXML |
| **PDF** | QuestPDF |
| **Logs** | Serilog (consola + configuración) |
| **Auth** | JWT Bearer + BCrypt |
| **IDs** | ULID (ordenables por tiempo) |
| **Rate limiting** | AspNetCoreRateLimit |
| **Testing** | nUnit · Testcontainers · Coverlet · ReportGenerator |

---

## Estructura del proyecto

```
BackEncordados/
├── Common/
│   ├── Database/Config/         # DbContexts: Materials, Pedidos, Talleres, Users
│   │   └── Helpers/             # Interceptors (timestamps, versioning), ULID converters
│   ├── Dto/                     # DTOs genéricos (paginación, exportación)
│   ├── Errors/                  # DomainErrors base
│   ├── Service/
│   │   ├── Cache/               # ICacheService → Memory / HybridCache / Redis
│   │   ├── Cloudinary/          # Subida y transformación de imágenes
│   │   ├── Email/               # MailKit + plantillas HTML + background queue
│   │   └── WhatsApp/            # Notificaciones vía WhatsApp Business API
│   ├── SignalR/                 # SignalHub — grupos por torneo
│   └── Utils/                   # TransactionalAttribute, TapAsync, UnitClass
├── Excel/                       # Importación / exportación de datos en .xlsx
├── Export/                      # Backup y restauración completa de la base de datos (ZIP)
├── Infraestructure/             # DI, CORS, autenticación, rate limit, Serilog, DB init…
├── Materials/                   # Materiales y cuerdas (CRUD + filtros paginados)
├── Purchased/                   # Pedidos, líneas de pedido, configuración de encordado
├── Talleres/                    # Torneos, asignación encordador-máquina, supervisores
├── Usuarios/                    # Autenticación JWT, CRUD usuarios, cambio de contraseña
├── Middleware/                  # GlobalExceptionHandler
└── Program.cs                   # Pipeline de middleware

TestEncordados/
├── Integration/                 # Tests con Testcontainers (PostgreSQL efímero)
├── Unit/                        # Tests unitarios con SQLite In-Memory
└── Unit/Fixtures/               # Builders para datos de prueba
```

Cada módulo sigue la misma estructura vertical: `Model → Dto → Mapper → Repository → Service → Controller → Validator → Error`.

---

## Inicio rápido

### Prerrequisitos

- .NET 10 SDK
- PostgreSQL 16 (o cadena de conexión a tu instancia)
- MongoDB 7 con Replica Set (o cadenas de conexión)
- Redis 7 (opcional, se degrada a MemoryCache)

### Configuración

```bash
cp .env.example .env
```

Edita `appsettings.Development.json` o define variables de entorno:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=encordados;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "cambia_esto_por_una_clave_segura_minimo_32_chars",
    "Issuer": "Encordados",
    "Audience": "Encordados",
    "ExpireMinutes": 60
  },
  "Cloudinary": {
    "CloudName": "...",
    "ApiKey": "...",
    "ApiSecret": "..."
  }
}
```

### Ejecutar en local

```bash
cd BackEncordados
dotnet restore
dotnet run
```

La API arranca en `https://localhost:8081` / `http://localhost:8080`.

### Docker

El Dockerfile usa **build multietapa** con dos targets:

| Target | Propósito |
|--------|-----------|
| `base` | Imagen base con runtime .NET 10 |
| `final` | Build y publicación de la API (por defecto) |
| `coverageweb` | Compila, ejecuta tests con cobertura y sirve el reporte HTML |

```bash
# Solo la API (sin tests)
docker build -f Dockerfile -t back-encordados .
docker run -p 8080:8080 --env-file .env back-encordados

# Con tests y cobertura (genera y sirve el reporte)
docker build -f Dockerfile --build-arg RUN_TESTS=true --target coverageweb -t back-encordados .
```

---

## Tests

Los tests usan **xUnit** + **Testcontainers** (PostgreSQL efímero por test) para integración, y **SQLite In-Memory** para tests unitarios más ligeros.

El ensamblado de tests tiene acceso a miembros internos del proyecto principal mediante `InternalsVisibleTo`, lo que permite testear clases sin exponerlas públicamente.

```bash
# Todos los tests
dotnet test TestEncordados/TestEncordados.csproj -c Release

# Solo tests unitarios
dotnet test TestEncordados/TestEncordados.csproj --filter "FullyQualifiedName~Unit"

# Con cobertura (Coverlet → Cobertura XML)
dotnet test TestEncordados/TestEncordados.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# Generar reporte HTML (requiere ReportGenerator)
reportgenerator "-reports:./TestResults/**/coverage.cobertura.xml" \
  "-targetdir:coverage-report" "-reporttypes:Html"
```

El informe HTML se genera automáticamente en CI y se publica en la rama `dev-test` del repositorio padre.

---

## Scripts

| Script | Propósito |
|--------|-----------|
| `scripts/Run-Tests.ps1` | Ejecuta tests con cobertura y abre el reporte HTML (Windows) |
| `scripts/run-tests.sh` | Ejecuta tests unitarios con cobertura y genera reporte HTML (Linux/macOS) |
| `addLib.ps1` | Sincroniza todos los paquetes NuGet del proyecto desde una lista predefinida |

---

## Pipeline de middleware

El pipeline de `Program.cs` se construye en el siguiente orden:

```
Serilog (logging global)
  → AddMvcControllers()
  → FluentValidation (validators desde el ensamblado)
  → AddDatabase (4 DbContexts)
  → AddCorsPolicy
  → AddRateLimitingPolicy
  → AddAuthentication (JWT Bearer)
  → AddAuthorization
  → AddCache (HybridCache o MemoryCache)
  → AddCloudinary
  → AddAppConfig
  → AddWhatsAppHttpClient
  → AddRepositories
  → AddServices
  → AddEmail ( + background service)
  → AddRealtimeSignalR
  → UseGlobalExceptionHandler
  → UseCorsPolicy
  → UseHttpsRedirection
  → UseStaticFiles
  → UseRouting
  → UseAuthentication / UseAuthorization
  → MapSignalRHubs
  → MapControllers
  → MapGet("/health")
  → InitializeDatabaseAsync
```

El middleware de excepciones global (`GlobalExceptionHandler`) captura cualquier error no controlado y devuelve respuestas JSON estructuradas.

---

## Endpoints principales

### Auth — `/api/auth`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/register` | Registro de usuario |
| `POST` | `/login` | Login → devuelve JWT |
| `POST` | `/change-password` | Cambio de contraseña autenticado |

### Usuarios — `/api/users`

| Método | Ruta | Rol mínimo |
|--------|------|-----------|
| `GET` | `/` | Admin |
| `GET` | `/{id}` | Usuario autenticado |
| `PATCH` | `/{id}` | Admin |
| `DELETE` | `/{id}` | Admin |
| `PATCH` | `/{id}/role` | Admin |

### Materiales — `/api/materials`

CRUD completo con filtros paginados por marca, modelo, tipo y torneo.

### Cuerdas — `/api/cuerdas`

CRUD completo con filtros paginados por marca, modelo, formato, tipo de cuerda y torneo.

### Pedidos — `/api/purchased`

Gestión completa del ciclo de vida: `Pendiente → En proceso → Listo → Entregado / Cancelado`.

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/` | Crear pedido (con descuento automático de bonos) |
| `GET` | `/` | Listar pedidos con filtros |
| `GET` | `/{id}` | Detalle del pedido |
| `PATCH` | `/{id}` | Actualizar pedido |
| `DELETE` | `/{id}` | Cancelar pedido |
| `PATCH` | `/{id}/status` | Cambiar estado de pago |
| `POST` | `/{id}/lines` | Añadir línea de pedido |
| `PATCH` | `/lines/{id}` | Actualizar línea |
| `DELETE` | `/lines/{id}` | Cancelar línea |
| `PATCH` | `/{id}/lines/status` | Cambiar estado de todas las líneas |

Estados de línea: `PENDING → IN_PROCESS → COMPLETED → DELIVERED_TO_PLAYER / CANCELED`.

### Torneos — `/api/tournaments`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/` | Crear torneo |
| `GET` | `/` | Listar torneos |
| `GET` | `/{id}` | Detalle del torneo |
| `PATCH` | `/{id}` | Actualizar torneo |
| `DELETE` | `/{id}` | Eliminar torneo |
| `POST` | `/{id}/supervisor` | Asignar supervisor |
| `DELETE` | `/{id}/supervisor` | Desasignar supervisor |
| `POST` | `/{id}/workers` | Asignar encordador a máquina |
| `DELETE` | `/{id}/workers` | Desasignar encordador |

### Excel — `/api/excel`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `POST` | `/import` | Importa datos desde `.xlsx` |
| `GET` | `/export` | Descarga datos en `.xlsx` |
| `GET` | `/export/advanced/{id}` | Informe avanzado de torneo |

### Export — `/api/export`

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/` | Descarga backup completo (ZIP comprimido con SharpCompress) |
| `POST` | `/import` | Restaura backup |
| `GET` | `/manifest` | Metadatos del backup actual |

### Health — `GET /health`

```json
{ "status": "healthy", "timestamp": "2026-05-25T12:00:00Z" }
```

---

## Eventos SignalR

Hub: `/hubs/signal` (autenticado, rol mínimo: Supervisor)

Todos los eventos se reciben mediante `ReceiveTournamentNotification` con el siguiente formato:

```typescript
interface TournamentNotification {
  tournamentId: string;
  pedidoId: string;
  tipo: TipoEvento;
  total: number;
  numberOfLines: number;
  purchased: PurchasedResponseDto;
  timestamp: string;
}
```

| `tipo` | Cuándo se dispara |
|--------|-------------------|
| `PEDIDO_CREADO` | Nuevo pedido creado |
| `PEDIDO_ACTUALIZADO` | Pedido actualizado (datos generales) |
| `PEDIDO_CANCELADO` | Pedido cancelado |
| `ESTATUS_LINEA_PEDIDO_ACTUALIZADA` | Estado de una línea de pedido cambiado |
| `LINEA_PEDIDO_ACTUALIZADA` | Datos de una línea actualizados |

Los mensajes se envían a dos grupos:
- `Tournament_{tournamentId}` — usuarios asociados al torneo (owner, supervisores, workers)
- `Tournament_All_Admin` — todos los administradores del sistema

Al conectarse, el hub agrupa automáticamente al usuario en los torneos donde es owner, supervisor o worker.

---

## Estrategia de bases de datos

El sistema usa **4 bases de datos independientes**, cada una con su propio `DbContext`:

| DbContext | Motor | Propósito | ¿Por qué separado? |
|-----------|-------|-----------|-------------------|
| `UserDbContext` | PostgreSQL | Usuarios, roles, autenticación | Datos críticos, consultas frecuentes, joins relacionales |
| `MaterialsDbContext` | PostgreSQL | Materiales y cuerdas | Datos transaccionales con stock, precios, filtros complejos |
| `PedidosDbContext` | MongoDB (via EF Core) | Pedidos y líneas | Documentos anidados (pedido → líneas), esquema flexible |
| `TalleresDbContext` | MongoDB (via EF Core) | Torneos y asignaciones | Documentos anidados (torneo → workers/supervisores) |

La decisión de usar MongoDB para pedidos y torneos responde a:
- Los pedidos se consultan siempre con sus líneas (documento anidado evita joins)
- Los torneos tienen listas dinámicas de workers/supervisores (arrays en documento)
- Escrituras frecuentes en documentos completos sin necesidad de transacciones distribuidas

---

## Caché

Estrategia de dos niveles implementada en `CacheService`:

```
MemoryCache (L1) ← → Redis (L2, opcional)
     ↓                        ↓
  Rápida, volátil        Distribuida, persistente
```

- `HybridCacheService` — combina L1 + L2; si Redis no está disponible, degrada a `MemoryCacheService` sin cambiar la interfaz
- `CacheService` — implementación directa con `IMemoryCache` para entornos sin Redis
- Las claves de caché están centralizadas en `CacheKeys`
- Los tiempos de expiración varían: 5 min para pedidos, 10 min para usuarios

---

## Variables de entorno

| Variable | Obligatoria | Descripción | Valor por defecto |
|----------|-------------|-------------|-------------------|
| `JWT_KEY` | ✅ | Clave secreta JWT (≥32 chars) | — |
| `JWT_ISSUER` | ❌ | Emisor del token | `Encordados` |
| `JWT_AUDIENCE` | ❌ | Audiencia del token | `Encordados` |
| `DATABASE_URL_USER` | ✅ | Connection string PostgreSQL usuarios | — |
| `DATABASE_URL_MATERIALS` | ✅ | Connection string PostgreSQL materiales | — |
| `MONGODB_URI_PEDIDOS` | ✅ | URI MongoDB pedidos | — |
| `MONGODB_URI_TALLERES` | ✅ | URI MongoDB torneos | — |
| `REDIS_CACHE_URL` | ❌ | URL Redis | `redis://localhost:6379/0` |
| `SMTP_HOST` | ✅ | Servidor SMTP | — |
| `SMTP_PORT` | ❌ | Puerto SMTP | `587` |
| `SMTP_USERNAME` | ✅ | Usuario SMTP | — |
| `SMTP_PASSWORD` | ✅ | Contraseña SMTP | — |
| `SMTP_FROMEMAIL` | ✅ | Email remitente | — |
| `Cloudinary_CloudName` | ✅ | Cloud name Cloudinary | — |
| `Cloudinary_ApiKey` | ✅ | API Key Cloudinary | — |
| `Cloudinary_ApiSecret` | ✅ | API Secret Cloudinary | — |
| `WhatsAppEnabled` | ❌ | Habilitar WhatsApp | `false` |

---

## Patrones y decisiones de diseño

- **Result Pattern** — todos los servicios devuelven `Result<T>` de CSharpFunctionalExtensions. Los errores de dominio no lanzan excepciones; se mapean a respuestas HTTP en los controladores.
- **ULID como PK** — los IDs son ULIDs almacenados como strings de 26 caracteres, ordenables cronológicamente y únicos sin necesidad de secuencias.
- **Versioning automático** — `VersionInterceptor` incrementa una columna de versión en cada `SaveChanges`, útil para concurrencia optimista y ETags.
- **Timestamps automáticos** — `TimestampInterceptor` rellena `CreatedAt` / `UpdatedAt` en todas las entidades que implementan `ITimestamped`.
- **Caché en dos niveles** — `HybridCacheService` combina L1 (memoria) y L2 (Redis). Degradación graceful si Redis no está disponible.
- **Email asíncrono** — `EmailBackgroundService` procesa una cola en memoria para no bloquear las peticiones HTTP.
- **`[Transactional]`** — atributo personalizado que envuelve el método en una transacción EF Core de forma declarativa.
- **Reintentos con concurrencia** — `UpdateUserWithRetryAsync` reintenta hasta 3 veces ante conflictos de concurrencia al actualizar bonos.
- **Soft delete** — todas las entidades usan `IsDeleted` + `HasQueryFilter` para borrado lógico.

---

## Colección Bruno

En `EncordadosBruno/` encontrarás una colección completa de más de **50 peticiones** [Bruno](https://www.usebruno.com/) organizadas por módulo (Auth, User, Materials, Cuerdas, Pedidos, Talleres, Excel, Export, Health), con variables de entorno para `local` y `docker`, y ejemplos de todos los flujos:

```bash
# Importar en Bruno
Archivo → Abrir colección → selecciona la carpeta EncordadosBruno/
```

Incluye entornos separados para `local` y `docker`.

---

## Contribución

1. Haz fork del repositorio
2. Crea una rama desde `dev`: `git checkout -b feature/mi-feature`
3. Realiza los cambios y asegúrate de que los tests pasen:
   ```bash
   dotnet test TestEncordados/TestEncordados.csproj -c Release
   ```
4. Asegúrate de que el código no tenga warnings (`TreatWarningsAsErrors` está activado)
5. Abre un Pull Request contra `dev`

---

## Desarrolladores

<div align="center">
  <table>
    <tr>
      <td align="center" width="50%">
        <img src="https://github.com/Aragorn7372.png" width="120" height="120" style="border-radius:50%" alt="Aragorn7372"/><br />
        <br />
        <b>Aragorn7372</b><br />
        🖥️ <strong>Backend Developer</strong><br />
        <br />
        <sub>Arquitectura de la API, diseño de DbContexts,<br />
        servicios y repositorios, SignalR, pipeline CI/CD,<br />
        estrategia de caché y bases de datos.</sub>
        <br />
        <br />
        <a href="https://github.com/Aragorn7372">@Aragorn7372</a>
      </td>
      <td align="center" width="50%">
        <img src="https://github.com/JorgeMrj.png" width="120" height="120" style="border-radius:50%" alt="JorgeMrj"/><br />
        <br />
        <b>JorgeMrj</b><br />
        🧪 <strong>QA Engineer</strong><br />
        <br />
        <sub>Tests unitarios y de integración,<br />
        validación de reglas de negocio,<br />
        cobertura de código y revisión de PRs.</sub>
        <br />
        <br />
        <a href="https://github.com/JorgeMrj">@JorgeMrj</a>
      </td>
    </tr>
  </table>
</div>
