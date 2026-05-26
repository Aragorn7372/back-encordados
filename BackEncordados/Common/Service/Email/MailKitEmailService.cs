using System.Threading.Channels;
using MailKit.Net.Smtp;
using MimeKit;

namespace BackEncordados.Common.Service.Email;


/// <summary>
/// Implementación de <see cref="IEmailService"/> que envía correos electrónicos reales
/// mediante SMTP utilizando la librería MailKit.
/// </summary>
/// <remarks>
/// <para>Lee la configuración SMTP de las siguientes claves de <see cref="IConfiguration"/>:</para>
/// <list type="table">
///   <listheader>
///     <term>Clave</term>
///     <description>Descripción</description>
///     <description>Requerido</description>
///   </listheader>
///   <item>
///     <term><c>Smtp:Host</c></term>
///     <description>Servidor SMTP (ej: <c>smtp.gmail.com</c>)</description>
///     <description>Sí</description>
///   </item>
///   <item>
///     <term><c>Smtp:Port</c></term>
///     <description>Puerto SMTP (default: <c>587</c>)</description>
///     <description>No (default 587)</description>
///   </item>
///   <item>
///     <term><c>Smtp:Username</c></term>
///     <description>Usuario de autenticación SMTP</description>
///     <description>Sí</description>
///   </item>
///   <item>
///     <term><c>Smtp:Password</c></term>
///     <description>Contraseña o App Password SMTP</description>
///     <description>No (conexión sin autenticar si se omite)</description>
///   </item>
///   <item>
///     <term><c>Smtp:FromEmail</c></term>
///     <description>Dirección remitente (default: <c>Smtp:Username</c>)</description>
///     <description>No</description>
///   </item>
///   <item>
///     <term><c>Smtp:FromName</c></term>
///     <description>Nombre del remitente (default: <c>"TiendaApi"</c>)</description>
///     <description>No</description>
///   </item>
/// </list>
///
/// <para>Si <c>Smtp:Host</c> o <c>Smtp:Username</c> no están configurados, el envío se omite
/// con un warning en log — permite que la aplicación funcione sin SMTP en entornos de desarrollo.</para>
///
/// <para><b>Comportamiento de conexión:</b></para>
/// <list type="bullet">
///   <item><description>Conexión con <c>StartTlsWhenAvailable</c> (cifrado oportunista).</description></item>
///   <item><description>Autenticación solo si <c>Smtp:Password</c> está presente.</description></item>
///   <item><description>Desconexión explícita al finalizar (<c>DisconnectAsync(true)</c>).</description></item>
/// </list>
/// </remarks>
/// <param name="configuration">Configuración de la aplicación (sección <c>Smtp</c>).</param>
/// <param name="logger">Logger para registrar envíos y errores.</param>
/// <param name="emailChannel">Canal en memoria para encolar mensajes (<see cref="EnqueueEmailAsync"/>).</param>
public class MailKitEmailService(
    IConfiguration configuration,
    ILogger<MailKitEmailService> logger,
    Channel<EmailMessage> emailChannel
) : IEmailService {


    /// <summary>
    /// Envía un correo electrónico real mediante SMTP usando MailKit.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Lee configuración SMTP de <see cref="IConfiguration"/>.</description></item>
    ///   <item><description>Si <c>Host</c> o <c>Username</c> faltan → log warning y retorna sin error (graceful degradation).</description></item>
    ///   <item><description>Construye un <c>MimeMessage</c> con <c>MailboxAddress</c> para el remitente y destinatario.</description></item>
    ///   <item><description>Configura el cuerpo: HTML si <c>message.IsHtml</c> es <c>true</c>, texto plano en caso contrario.</description></item>
    ///   <item><description>Abre conexión SMTP con <c>StartTlsWhenAvailable</c>, autentica si hay password, envía y desconecta.</description></item>
    ///   <item><description>Si falla cualquier paso, loggea el error y relanza la excepción (no se traga).</description></item>
    /// </list>
    /// </remarks>
    /// <param name="message">Mensaje de correo con destinatario, asunto, cuerpo y formato.</param>
    public async Task SendEmailAsync(EmailMessage message)
    {
        try
        {
            var smtpHost = configuration["Smtp:Host"];
            var smtpPort = int.Parse(configuration["Smtp:Port"] ?? "587");
            var smtpUser = configuration["Smtp:Username"];
            var smtpPassword = configuration["Smtp:Password"];
            var fromEmail = configuration["Smtp:FromEmail"] ?? smtpUser;
            var fromName = configuration["Smtp:FromName"] ?? "TiendaApi";

            if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
            {
                logger.LogWarning("SMTP no configurado, omitiendo envío de email");
                return;
            }

            var mimeMessage = new MimeMessage();
            if (fromEmail != null) mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));
            mimeMessage.To.Add(MailboxAddress.Parse(message.To));
            mimeMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder();
            if (message.IsHtml)
            {
                bodyBuilder.HtmlBody = message.Body;
            }
            else
            {
                bodyBuilder.TextBody = message.Body;
            }
            mimeMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTlsWhenAvailable);
            if (smtpPassword != null) await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);

            logger.LogInformation("Email enviado exitosamente a: {To}", message.To);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al enviar email a: {To}", message.To);
            throw;
        }
    }
    
    /// <summary>
    /// Encola un correo electrónico para envío diferido en segundo plano.
    /// </summary>
    /// <remarks>
    /// <para>Escribe el mensaje en <c>emailChannel.Writer</c> para que
    /// <c>EmailBackgroundService</c> lo procese asincrónicamente.</para>
    /// <para>Si el canal está completo o cerrado, captura la excepción y la registra
    /// sin propagarla al llamante.</para>
    /// </remarks>
    /// <param name="message">Mensaje de correo a encolar.</param>
    public async Task EnqueueEmailAsync(EmailMessage message)
    {
        try
        {
            await emailChannel.Writer.WriteAsync(message);
            logger.LogInformation("Email encolado para procesamiento en segundo plano a: {To}", message.To);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al encolar email para: {To}", message.To);
        }
    }
}