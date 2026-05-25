namespace BackEncordados.Common.Service.Email;


/// <summary>
/// Implementación de <see cref="IEmailService"/> que registra en log los correos electrónicos
/// sin enviarlos realmente. Utilizada en entornos de desarrollo y pruebas automatizadas.
/// </summary>
/// <remarks>
/// <para>Simula el envío de correos electrónicos escribiendo los detalles del mensaje
/// en los logs de la aplicación. No realiza ninguna conexión SMTP real.</para>
///
/// <para><b>Formato de log:</b></para>
/// <list type="bullet">
///   <item><description><c>=== EMAIL SENT/ENQUEUED ===</c></description></item>
///   <item><description><c>Para: {destinatario}</c></description></item>
///   <item><description><c>Asunto: {subject}</c></description></item>
///   <item><description><c>Tipo: HTML | Texto plano</c></description></item>
///   <item><description><c>Cuerpo: {body}</c> (solo en nivel Debug)</description></item>
///   <item><description><c>======================</c></description></item>
/// </list>
///
/// <para>Tanto <see cref="SendEmailAsync"/> como <see cref="EnqueueEmailAsync"/>
/// son operaciones síncronas que retornan <see cref="Task.CompletedTask"/> inmediatamente.</para>
/// </remarks>
/// <param name="logger">Logger para registrar los detalles del mensaje simulado.</param>
public class MemoryEmailService : IEmailService
{
    private readonly ILogger<MemoryEmailService> _logger;

    public MemoryEmailService(ILogger<MemoryEmailService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Simula el encolado de un correo electrónico registrando sus detalles en log.
    /// </summary>
    /// <param name="message">Mensaje de correo a simular.</param>
    public Task EnqueueEmailAsync(EmailMessage message)
    {
        LogEmail(message, "ENQUEUED");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Simula el envío inmediato de un correo electrónico registrando sus detalles en log.
    /// </summary>
    /// <param name="message">Mensaje de correo a simular.</param>
    public Task SendEmailAsync(EmailMessage message)
    {
        LogEmail(message, "SENT");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Registra los detalles completos de un mensaje de correo en el log.
    /// </summary>
    /// <remarks>
    /// <para>El cuerpo del mensaje se registra en nivel <c>Debug</c> para evitar
    /// exponer contenido sensible en logs de producción por accidente.</para>
    /// </remarks>
    /// <param name="message">Mensaje de correo a loggear.</param>
    /// <param name="status">Estado del mensaje: <c>"SENT"</c> o <c>"ENQUEUED"</c>.</param>
    private void LogEmail(EmailMessage message, string status)
    {
        _logger.LogInformation("=== EMAIL {Status} ===", status);
        _logger.LogInformation("Para: {To}", message.To);
        _logger.LogInformation("Asunto: {Subject}", message.Subject);
        _logger.LogInformation("Tipo: {Type}", message.IsHtml ? "HTML" : "Texto plano");
        _logger.LogDebug("Cuerpo: {Body}", message.Body);
        _logger.LogInformation("======================");
    }
}