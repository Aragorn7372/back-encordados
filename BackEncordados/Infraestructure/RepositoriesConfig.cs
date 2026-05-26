using BackEncordados.Excel.Repository;
using BackEncordados.Export.Repository;
using BackEncordados.Materials.Repository.Materials;
using BackEncordados.Materials.Repository.Strings;
using BackEncordados.Purchased.Repository;
using BackEncordados.Talleres.Repository;
using BackEncordados.Usuarios.Repository;
using Serilog;

namespace BackEncordados.Infraestructure;

/// <summary>
/// Configuración y registro de todos los repositorios de la aplicación
/// en el contenedor de inyección de dependencias.
/// </summary>
/// <remarks>
/// <para>Proporciona un método de extensión sobre <c>IServiceCollection</c> que
/// registra los repositorios como servicios <c>Scoped</c>.</para>
/// <para><b>Repositorios registrados:</b></para>
/// <list type="table">
///   <listheader>
///     <term>Interfaz</term>
///     <description>Implementación</description>
///     <description>Módulo</description>
///   </listheader>
///   <item>
///     <term><c>IUserRepository</c></term>
///     <description>UserRepository</description>
///     <description>Usuarios</description>
///   </item>
///   <item>
///     <term><c>IPuchasedRepository</c></term>
///     <description>PurchasedReposirtory</description>
///     <description>Pedidos</description>
///   </item>
///   <item>
///     <term><c>ITournamentRepository</c></term>
///     <description>TournamentRepository</description>
///     <description>Talleres</description>
///   </item>
///   <item>
///     <term><c>IMaterialsRepository</c></term>
///     <description>MaterialsRepository</description>
///     <description>Materials</description>
///   </item>
///   <item>
///     <term><c>ICuerdasRepository</c></term>
///     <description>CuerdasRepository</description>
///     <description>Materials</description>
///   </item>
///   <item>
///     <term><c>IExportRepository</c></term>
///     <description>ExportRepository</description>
///     <description>Export</description>
///   </item>
///   <item>
///     <term><c>IExcelRepository</c></term>
///     <description>ExcelRepository</description>
///     <description>Excel</description>
///   </item>
/// </list>
/// <para>Usar <c>services.AddRepositories()</c> en <c>Program.cs</c>.</para>
/// </remarks>
public static class RepositoriesConfig
{
    /// <summary>
    /// Registra todos los repositorios de la aplicación como servicios <c>Scoped</c>
    /// en el contenedor de DI.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo:</b></para>
    /// <list type="number">
    ///   <item><description>Registra <c>IUserRepository → UserRepository</c> (Usuarios).</description></item>
    ///   <item><description>Registra <c>IPuchasedRepository → PurchasedReposirtory</c> (Pedidos).</description></item>
    ///   <item><description>Registra <c>ITournamentRepository → TournamentRepository</c> (Talleres).</description></item>
    ///   <item><description>Registra <c>IMaterialsRepository → MaterialsRepository</c> (Materials).</description></item>
    ///   <item><description>Registra <c>ICuerdasRepository → CuerdasRepository</c> (Cuerdas).</description></item>
    ///   <item><description>Registra <c>IExportRepository → ExportRepository</c> (Exportación BD).</description></item>
    ///   <item><description>Registra <c>IExcelRepository → ExcelRepository</c> (Exportación Excel).</description></item>
    /// </list>
    /// <para>Todos se registran como <c>Scoped</c> (una instancia por request HTTP).</para>
    /// </remarks>
    /// <param name="services">Colección de servicios de DI.</param>
    /// <returns>La misma colección de servicios para encadenamiento fluido.</returns>
    public static IServiceCollection AddRepositories(
        this IServiceCollection services)
    {
        Log.Information(" Registrando repositorios...");

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IPuchasedRepository, PurchasedReposirtory>();
        services.AddScoped<ITournamentRepository, TournamentRepository>();
        services.AddScoped<IMaterialsRepository, MaterialsRepository>();
        services.AddScoped<ICuerdasRepository, CuerdasRepository>();
services.AddScoped<IExportRepository, ExportRepository>();
        services.AddScoped<IExcelRepository, ExcelRepository>();

        return services;
    }
}