namespace BackEncordados.Common.Service.Email;


/// <summary>
/// Representa un mensaje de correo electrónico con todos los datos necesarios para su envío.
/// </summary>
public class EmailMessage
{
    /// <summary>Dirección de correo del destinatario.</summary>
    public string To { get; set; } = string.Empty;

    /// <summary>Asunto del correo electrónico.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Cuerpo del mensaje. Puede contener HTML si <see cref="IsHtml"/> es <c>true</c>.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>Indica si <see cref="Body"/> debe interpretarse como HTML (<c>true</c>) o texto plano (<c>false</c>). Valor por defecto: <c>true</c>.</summary>
    public bool IsHtml { get; set; } = true;
}


/// <summary>
/// Define el contrato para el envío de correos electrónicos, tanto directo como diferido mediante cola en memoria.
/// </summary>
/// <remarks>
/// <para>El sistema soporta dos modos de envío:</para>
/// <list type="table">
///   <listheader>
///     <term>Modo</term>
///     <description>Método</description>
///     <description>Comportamiento</description>
///   </listheader>
///   <item>
///     <term>Directo (síncrono)</term>
///     <description><c>SendEmailAsync</c></description>
///     <description>Envía el email inmediatamente a través del proveedor configurado (SMTP real o <c>MemoryEmailService</c> en desarrollo).</description>
///   </item>
///   <item>
///     <term>Encolado (diferido)</term>
///     <description><c>EnqueueEmailAsync</c></description>
///     <description>Escribe el mensaje en un <c>Channel&lt;EmailMessage&gt;</c> para que <c>EmailBackgroundService</c> lo procese en segundo plano.</description>
///   </item>
/// </list>
///
/// <para><b>Implementaciones disponibles:</b></para>
/// <list type="bullet">
///   <item><description><c>MailKitEmailService</c> — envío real mediante servidor SMTP con MailKit.</description></item>
///   <item><description><c>MemoryEmailService</c> — simulación en memoria para entornos de desarrollo y tests (solo loggea).</description></item>
/// </list>
/// </remarks>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo electrónico de forma inmediata.
    /// </summary>
    /// <param name="message">Mensaje de correo con destinatario, asunto, cuerpo y formato.</param>
    Task SendEmailAsync(EmailMessage message);

    /// <summary>
    /// Encola un correo electrónico para envío diferido en segundo plano.
    /// </summary>
    /// <remarks>
    /// <para>El mensaje se escribe en un <c>Channel&lt;EmailMessage&gt;</c> y es procesado
    /// por <c>EmailBackgroundService</c>. Esto permite que la respuesta HTTP no espere
    /// a que el envío se complete, mejorando la experiencia del usuario.</para>
    /// </remarks>
    /// <param name="message">Mensaje de correo a encolar.</param>
    Task EnqueueEmailAsync(EmailMessage message);
}