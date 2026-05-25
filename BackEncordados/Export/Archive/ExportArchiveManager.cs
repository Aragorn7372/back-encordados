using System.IO.Compression;
using System.Text.Json;
using BackEncordados.Common.Dto;
using BackEncordados.Export.Dto;
using BackEncordados.Materials.Model;
using BackEncordados.Talleres.Model;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using IOPath = System.IO.Path;
using IOFile = System.IO.File;
using IODirectory = System.IO.Directory;

namespace BackEncordados.Export.Archive;

/// <summary>
/// Implementación de <see cref="IExportArchiveManager"/> que gestiona la creación
/// y extracción de archivos ZIP con datos completos de la base de datos.
/// </summary>
/// <remarks>
/// <para>Genera archivos ZIP conteniendo archivos JSON independientes por cada
/// módulo de datos (usuarios, torneos, materiales, cuerdas, pedidos) más un
/// manifest con metadatos de exportación.</para>
/// <para><b>Configuraciones de serialización:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Propiedad</term>
///     <term>Valor</term>
///     <description>Uso</description>
///   </listheader>
///   <item>
///     <term><c>_jsonOptions</c></term>
///     <term>WriteIndented + CamelCase</term>
///     <description>Manifest (System.Text.Json).</description>
///   </item>
///   <item>
///     <term><c>NewtonsoftSettings</c></term>
///     <term>ReferenceLoopHandling.Ignore</term>
///     <description>Datos de entidades con relaciones de navegación (Newtonsoft.Json).</description>
///   </item>
/// </list>
/// <para><b>Aliases de System.IO:</b></para>
/// <list type="bullet">
///   <item><description><c>IOPath</c> = <c>System.IO.Path</c> (evita conflicto con otras clases Path).</description></item>
///   <item><description><c>IOFile</c> = <c>System.IO.File</c> (evita conflicto con otras clases File).</description></item>
///   <item><description><c>IODirectory</c> = <c>System.IO.Directory</c> (evita conflicto con otras clases Directory).</description></item>
/// </list>
/// <para>Todos los métodos crean directorios temporales en <c>%TEMP%</c> con nombres
/// únicos (GUID) y los eliminan en bloques <c>finally</c> para garantizar limpieza
/// incluso si ocurre una excepción.</para>
/// </remarks>
/// <param name="logger">Logger para seguimiento de operaciones de exportación/importación.</param>
public class ExportArchiveManager(ILogger<ExportArchiveManager> logger) : IExportArchiveManager
{
    /// <summary>
    /// Configuración de <c>System.Text.Json</c> para serialización del manifest.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item><description><c>WriteIndented = true</c>: formato legible con indentación.</description></item>
    ///   <item><description><c>PropertyNamingPolicy = JsonNamingPolicy.CamelCase</c>: nombres de propiedad en camelCase.</description></item>
    /// </list>
    /// </remarks>
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Configuración de <c>Newtonsoft.Json</c> para serialización de datos de entidades.
    /// </summary>
    /// <remarks>
    /// <para>Se usa <c>ReferenceLoopHandling.Ignore</c> para evitar errores de
    /// referencias circulares en las relaciones de navegación de Entity Framework
    /// (ej: un Pedido referencia un Usuario que referencia una lista de Pedidos).</para>
    /// </remarks>
    private static readonly JsonSerializerSettings NewtonsoftSettings = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
    };

    /// <summary>
    /// Crea un archivo ZIP con todos los datos de la base de datos en formato JSON.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Genera un nombre de directorio temporal único en <c>%TEMP%</c>
    ///   con formato <c>archive_{Guid}</c> y lo crea.</description></item>
    ///   <item><description>Construye un <see cref="ExportManifestDto"/> con
    ///   <c>ExportedAt = DateTime.UtcNow</c> y una lista vacía de entidades.</description></item>
    ///   <item><description>Serializa <c>data.Users</c> con Newtonsoft.Json a <c>users.json</c>
    ///   y agrega entrada al manifest (nombre: "users", cantidad de registros, archivo: "users.json").</description></item>
    ///   <item><description>Serializa <c>data.Tournaments</c> a <c>tournaments.json</c>
    ///   y registra en el manifest.</description></item>
    ///   <item><description>Serializa <c>data.Materials</c> a <c>materials.json</c>
    ///   y registra en el manifest.</description></item>
    ///   <item><description>Serializa <c>data.Cuerdas</c> a <c>cuerdas.json</c>
    ///   y registra en el manifest.</description></item>
    ///   <item><description>Serializa <c>data.Pedidos</c> a <c>orders.json</c>
    ///   y registra en el manifest.</description></item>
    ///   <item><description>Serializa el manifest completo con System.Text.Json
    ///   (formato camelCase indentado) a <c>manifest.json</c>.</description></item>
    ///   <item><description>Comprime todo el directorio temporal a un archivo ZIP
    ///   con nombre <c>export_{yyyyMMdd_HHmmss}.zip</c> mediante
    ///   <c>ZipFile.CreateFromDirectory</c>. Si el archivo ya existe, lo elimina primero.</description></item>
    ///   <item><description>Lee el ZIP completo como arreglo de bytes y lo retorna.</description></item>
    /// </list>
    /// <para><b>Limpieza:</b> El directorio temporal se elimina recursivamente
    /// en el bloque <c>finally</c>, incluso si ocurre una excepción.</para>
    /// <para><b>Nota:</b> Se usa Newtonsoft.Json para los datos de entidades porque
    /// <c>ReferenceLoopHandling.Ignore</c> es necesario para las relaciones de
    /// navegación de EF Core. System.Text.Json no maneja referencias circulares
    /// sin configuraciones adicionales (<c>ReferenceHandler.Preserve</c>).</para>
    /// </remarks>
    /// <param name="data">DTO con todas las listas de entidades a exportar.</param>
    /// <returns>Arreglo de bytes con el archivo ZIP generado.</returns>
    public async Task<byte[]> CreateZipAsync(ExportDataDto data)
    {
        logger.LogInformation("Creating ZIP archive");

        var tempDir = IOPath.Combine(IOPath.GetTempPath(), $"archive_{Guid.NewGuid()}");
        IODirectory.CreateDirectory(tempDir);

        try
        {
            var manifest = new ExportManifestDto
            {
                ExportedAt = DateTime.UtcNow,
                Entities = new List<ExportEntityInfo>()
            };

            var usersJson = JsonConvert.SerializeObject(data.Users, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "users.json"), usersJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "users", RecordCount = data.Users.Count, FileName = "users.json" });
            logger.LogInformation("Added {Count} users to archive", data.Users.Count);

            var tournamentsJson = JsonConvert.SerializeObject(data.Tournaments, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "tournaments.json"), tournamentsJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "tournaments", RecordCount = data.Tournaments.Count, FileName = "tournaments.json" });
            logger.LogInformation("Added {Count} tournaments to archive", data.Tournaments.Count);

            var materialsJson = JsonConvert.SerializeObject(data.Materials, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "materials.json"), materialsJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "materials", RecordCount = data.Materials.Count, FileName = "materials.json" });
            logger.LogInformation("Added {Count} materials to archive", data.Materials.Count);

            var cuerdasJson = JsonConvert.SerializeObject(data.Cuerdas, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "cuerdas.json"), cuerdasJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "cuerdas", RecordCount = data.Cuerdas.Count, FileName = "cuerdas.json" });
            logger.LogInformation("Added {Count} cuerdas to archive", data.Cuerdas.Count);

            var pedidosJson = JsonConvert.SerializeObject(data.Pedidos, Formatting.Indented, NewtonsoftSettings);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "orders.json"), pedidosJson);
            manifest.Entities.Add(new ExportEntityInfo { Name = "orders", RecordCount = data.Pedidos.Count, FileName = "orders.json" });
            logger.LogInformation("Added {Count} pedidos to archive", data.Pedidos.Count);

            var manifestJson = JsonSerializer.Serialize(manifest, _jsonOptions);
            await IOFile.WriteAllTextAsync(IOPath.Combine(tempDir, "manifest.json"), manifestJson);

            var zipPath = IOPath.Combine(IOPath.GetTempPath(), $"export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");
            if (IOFile.Exists(zipPath))
                IOFile.Delete(zipPath);

            ZipFile.CreateFromDirectory(tempDir, zipPath);
            logger.LogInformation("ZIP created: {ZipPath}", zipPath);

            return await IOFile.ReadAllBytesAsync(zipPath);
        }
        finally
        {
            if (IODirectory.Exists(tempDir))
                IODirectory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Extrae los datos de un archivo ZIP y los deserializa en un <see cref="ExportDataDto"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Genera un nombre de directorio temporal único en <c>%TEMP%</c>
    ///   con formato <c>extract_{Guid}</c> y lo crea.</description></item>
    ///   <item><description>Guarda el contenido del <paramref name="zipStream"/> en un archivo
    ///   <c>data.zip</c> dentro del directorio temporal.</description></item>
    ///   <item><description>Extrae el contenido del ZIP con <c>ZipFile.ExtractToDirectory</c>
    ///   usando <c>overwriteFiles: true</c> para sobrescribir archivos existentes.</description></item>
    ///   <item><description>Lee y deserializa cada archivo JSON si existe:</description></item>
    /// </list>
    /// <para><b>Archivos extraídos:</b></para>
    /// <list type="table">
    ///   <listheader>
    ///     <term>Archivo</term>
    ///     <description>Deserializa a</description>
    ///     <description>Propiedad en ExportDataDto</description>
    ///   </listheader>
    ///   <item>
    ///     <term><c>tournaments.json</c></term>
    ///     <description><c>List&lt;Tournaments&gt;</c></description>
    ///     <description><c>data.Tournaments</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>users.json</c></term>
    ///     <description><c>List&lt;User&gt;</c></description>
    ///     <description><c>data.Users</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>materials.json</c></term>
    ///     <description><c>List&lt;Material&gt;</c></description>
    ///     <description><c>data.Materials</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>cuerdas.json</c></term>
    ///     <description><c>List&lt;Cuerdas&gt;</c></description>
    ///     <description><c>data.Cuerdas</c></description>
    ///   </item>
    ///   <item>
    ///     <term><c>orders.json</c></term>
    ///     <description><c>List&lt;Pedidos&gt;</c></description>
    ///     <description><c>data.Pedidos</c></description>
    ///   </item>
    /// </list>
    /// <para>Si algún archivo no existe en el ZIP, la lista correspondiente se inicializa
    /// como vacía (<c>new List&lt;T&gt;()</c>) gracias al operador <c>??</c>.</para>
    /// <para><b>Limpieza:</b> El directorio temporal se elimina recursivamente
    /// en el bloque <c>finally</c>.</para>
    /// </remarks>
    /// <param name="zipStream">Stream del archivo ZIP a extraer.</param>
    /// <returns>DTO con todas las entidades deserializadas desde el ZIP.</returns>
    public async Task<ExportDataDto> ExtractZipAsync(Stream zipStream)
    {
        logger.LogInformation("Extracting ZIP archive");

        var tempDir = IOPath.Combine(IOPath.GetTempPath(), $"extract_{Guid.NewGuid()}");
        IODirectory.CreateDirectory(tempDir);

        try
        {
            var zipPath = IOPath.Combine(tempDir, "data.zip");
            await using (var fileStream = new FileStream(zipPath, FileMode.Create))
            {
                await zipStream.CopyToAsync(fileStream);
            }

            ZipFile.ExtractToDirectory(zipPath, tempDir, true);

            var data = new ExportDataDto();

            var tournamentsPath = IOPath.Combine(tempDir, "tournaments.json");
            if (IOFile.Exists(tournamentsPath))
            {
                var json = await IOFile.ReadAllTextAsync(tournamentsPath);
                data.Tournaments = JsonConvert.DeserializeObject<List<Tournaments>>(json) ?? new List<Tournaments>();
                logger.LogInformation("Extracted {Count} tournaments", data.Tournaments.Count);
            }

            var usersPath = IOPath.Combine(tempDir, "users.json");
            if (IOFile.Exists(usersPath))
            {
                var json = await IOFile.ReadAllTextAsync(usersPath);
                data.Users = JsonConvert.DeserializeObject<List<Usuarios.Model.User>>(json) ?? new List<Usuarios.Model.User>();
                logger.LogInformation("Extracted {Count} users", data.Users.Count);
            }

            var materialsPath = IOPath.Combine(tempDir, "materials.json");
            if (IOFile.Exists(materialsPath))
            {
                var json = await IOFile.ReadAllTextAsync(materialsPath);
                data.Materials = JsonConvert.DeserializeObject<List<Material>>(json) ?? new List<Material>();
                logger.LogInformation("Extracted {Count} materials", data.Materials.Count);
            }

            var cuerdasPath = IOPath.Combine(tempDir, "cuerdas.json");
            if (IOFile.Exists(cuerdasPath))
            {
                var json = await IOFile.ReadAllTextAsync(cuerdasPath);
                data.Cuerdas = JsonConvert.DeserializeObject<List<Materials.Model.Cuerdas>>(json) ?? new List<Materials.Model.Cuerdas>();
                logger.LogInformation("Extracted {Count} cuerdas", data.Cuerdas.Count);
            }

            var ordersPath = IOPath.Combine(tempDir, "orders.json");
            if (IOFile.Exists(ordersPath))
            {
                var json = await IOFile.ReadAllTextAsync(ordersPath);
                data.Pedidos = JsonConvert.DeserializeObject<List<Purchased.Model.Pedidos>>(json) ?? new List<Purchased.Model.Pedidos>();
                logger.LogInformation("Extracted {Count} pedidos", data.Pedidos.Count);
            }

            return data;
        }
        finally
        {
            if (IODirectory.Exists(tempDir))
                IODirectory.Delete(tempDir, true);
        }
    }

    /// <summary>
    /// Obtiene el manifest de un archivo ZIP sin extraer los datos completos.
    /// </summary>
    /// <remarks>
    /// <para>Método ligero para inspeccionar el contenido de un ZIP de exportación
    /// sin necesidad de deserializar todas las entidades.</para>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Escribe el arreglo de bytes <paramref name="zipData"/> a un archivo
    ///   temporal en <c>%TEMP%</c> con nombre <c>manifest_{Guid}.zip</c>.</description></item>
    ///   <item><summary>Abre el archivo ZIP temporal como <c>ZipArchive</c> en modo lectura.</description></item>
    ///   <item><description>Busca la entrada <c>manifest.json</c> dentro del ZIP.</description></item>
    ///   <item><description>Si la entrada no existe → lanza <c>InvalidOperationException</c>
    ///   con mensaje "Manifest not found in zip".</description></item>
    ///   <item><description>Abre un stream de lectura sobre la entrada, lee todo el JSON
    ///   y lo deserializa a <see cref="ExportManifestDto"/> usando System.Text.Json.</description></item>
    ///   <item><description>Si la deserialización retorna null → lanza <c>InvalidOperationException</c>
    ///   con mensaje "Failed to deserialize manifest".</description></item>
    ///   <item><description>Retorna el manifest deserializado.</description></item>
    /// </list>
    /// <para><b>Limpieza:</b> El archivo temporal se elimina en el bloque <c>finally</c>.</para>
    /// </remarks>
    /// <param name="zipData">Arreglo de bytes del archivo ZIP a inspeccionar.</param>
    /// <returns>Manifest con metadatos de la exportación (fecha, lista de entidades).</returns>
    /// <exception cref="InvalidOperationException">El ZIP no contiene <c>manifest.json</c>
    /// o el contenido no pudo deserializarse correctamente.</exception>
    public async Task<ExportManifestDto> GetManifestAsync(byte[] zipData)
    {
        var tempFile = IOPath.Combine(IOPath.GetTempPath(), $"manifest_{Guid.NewGuid()}.zip");
        await IOFile.WriteAllBytesAsync(tempFile, zipData);

        try
        {
            using var archive = ZipFile.OpenRead(tempFile);
            var manifestEntry = archive.GetEntry("manifest.json");
            if (manifestEntry == null)
                throw new InvalidOperationException("Manifest not found in zip");

            await using var stream = manifestEntry.Open();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<ExportManifestDto>(json, _jsonOptions) 
                ?? throw new InvalidOperationException("Failed to deserialize manifest");
        }
        finally
        {
            IOFile.Delete(tempFile);
        }
    }
}