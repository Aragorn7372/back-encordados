using System.Threading.Channels;

namespace BackEncordados.Common.Service.Email;


/// <summary>
/// Servicio en segundo plano que procesa y envía emails de forma asíncrona
/// desde una cola en memoria (<see cref="Channel{T}"/>).
/// </summary>
/// <remarks>
/// <para>Implementa el patrón <b>productor-consumidor</b> para el envío de correos electrónicos:</para>
/// <list type="bullet">
///   <item><description><b>Productor:</b> Cualquier componente del sistema escribe mensajes en
///   <c>emailChannel.Writer</c> mediante <c>TryWrite</c> o <c>WriteAsync</c>. Ej: registro de usuario,
///   cambio de contraseña, confirmación de pedido.</description></item>
///   <item><description><b>Consumidor:</b> Este servicio lee del <c>emailChannel.Reader</c> en un bucle
///   <c>await foreach</c> y envía cada mensaje usando <see cref="IEmailService"/> dentro de un
///   <see cref="IServiceScope"/> independiente.</description></item>
/// </list>
///
/// <para><b>Ciclo de vida:</b></para>
/// <list type="number">
///   <item><description>Al iniciar, registra <c>"Servicio de email en segundo plano iniciado"</c>.</description></item>
///   <item><description>Itera sobre <c>emailChannel.Reader.ReadAllAsync(stoppingToken)</c> hasta que
///   el token de cancelación se active o el canal se cierre.</description></item>
///   <item><description>Por cada mensaje, crea un <c>IServiceScope</c>, resuelve <see cref="IEmailService"/>
///   y ejecuta <see cref="IEmailService.SendEmailAsync"/>.</description></item>
///   <item><description>Si falla un envío, captura la excepción, registra el error y continúa
///   con el siguiente mensaje (no reintenta).</description></item>
///   <item><description>Al detenerse, registra <c>"Servicio de email en segundo plano detenido"</c>.</description></item>
/// </list>
/// </remarks>
/// <param name="emailChannel">Canal en memoria (<see cref="Channel{EmailMessage}"/>) que funciona como cola de mensajes entrantes.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> para crear scopes DI por cada mensaje procesado.</param>
/// <param name="logger">Logger para registrar el ciclo de vida y errores de envío.</param>
public class EmailBackgroundService(
    Channel<EmailMessage> emailChannel,
    IServiceProvider serviceProvider,
    ILogger<EmailBackgroundService> logger
) : BackgroundService {

    /// <summary>
    /// Bucle principal del servicio que lee y procesa mensajes de la cola de emails.
    /// </summary>
    /// <remarks>
    /// <para><b>Flujo detallado:</b></para>
    /// <list type="number">
    ///   <item><description>Registra el inicio del servicio.</description></item>
    ///   <item><description>Itera sobre <c>ReadAllAsync</c>, que completa cuando el canal se marca como
    ///   completo y todos los mensajes fueron leídos, o cuando <paramref name="stoppingToken"/> se cancela.</description></item>
    ///   <item><description>Por cada <see cref="EmailMessage"/>, crea un <see cref="IServiceScope"/> para
    ///   resolver un <see cref="IEmailService"/> independiente (evita capturar scopes del host).</description></item>
    ///   <item><description>Envía el email. Si falla, loggea el error pero <b>no interrumpe</b> el procesamiento
    ///   de mensajes posteriores.</description></item>
    ///   <item><description>Al finalizar (por cancelación o cierre del canal), registra la detención.</description></item>
    /// </list>
    /// <para>No se implementa reintento: si el envío falla, el mensaje se pierde y se registra en log.
    /// Para mayor resiliencia, considerar un mecanismo de cola con persistencia (ej: RabbitMQ, Azure Service Bus).</para>
    /// </remarks>
    /// <param name="stoppingToken">Token de cancelación que detiene el bucle cuando el host se apaga.</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        
        logger.LogInformation("Servicio de email en segundo plano iniciado");

        await foreach (var emailMessage in emailChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                logger.LogInformation("Procesando email de la cola para: {To}", emailMessage.To);

                await emailService.SendEmailAsync(emailMessage);

                logger.LogInformation("Email procesado exitosamente para: {To}", emailMessage.To);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error procesando email para: {To}", emailMessage.To);
            }
        }

        logger.LogInformation("Servicio de email en segundo plano detenido");
    }
}