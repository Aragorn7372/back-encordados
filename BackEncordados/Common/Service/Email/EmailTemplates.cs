namespace BackEncordados.Common.Service.Email;

/// <summary>Representa un artículo dentro de un pedido para su inclusión en emails de resumen.</summary>
/// <param name="ProductName">Nombre del producto o servicio.</param>
/// <param name="Quantity">Cantidad adquirida.</param>
/// <param name="Price">Precio unitario del artículo.</param>
public record OrderItemEmailDto(string ProductName, int Quantity, decimal Price);

/// <summary>
/// Genera plantillas HTML de correos electrónicos con diseño corporativo consistente.
/// </summary>
/// <remarks>
/// <para>Todas las plantillas utilizan un diseño base responsive de 600px con:</para>
/// <list type="bullet">
///   <item><description><b>Header:</b> Logo "Encordados" con separador inferior.</description></item>
///   <item><description><b>Cuerpo:</b> Título + contenido específico de cada template.</description></item>
///   <item><description><b>Footer:</b> Copyright + teléfono de soporte.</description></item>
/// </list>
/// <para>Los templates se construyen con dos capas: <see cref="CreateBase"/> (estructura completa)
/// y <see cref="CreateBaseWithButton"/> (agrega botón CTA). Los templates específicos generan
/// solo el contenido interno y delegan el armado HTML a estos métodos base.</para>
/// <para><b>Colores corporativos:</b></para>
/// <list type="table">
///   <listheader><term>Constante</term><description>Color</description><description>Uso</description></listheader>
///   <item><term><c>PrimaryColor</c></term><description><c>#2563eb</c> (azul)</description><description>Botones, enlaces</description></item>
///   <item><term><c>TextColor</c></term><description><c>#1f2937</c> (gris oscuro)</description><description>Textos principales</description></item>
///   <item><term><c>LightGray</c></term><description><c>#f3f4f6</c> (gris claro)</description><description>Fondos de tabla, footer</description></item>
///   <item><term><c>BorderColor</c></term><description><c>#e5e7eb</c> (gris borde)</description><description>Separadores</description></item>
/// </list>
/// </remarks>
public static class EmailTemplates
{
    /// <summary>Color primario de la marca (azul). Usado en botones y enlaces.</summary>
    private const string PrimaryColor = "#2563eb";

    /// <summary>Color de texto principal (gris oscuro).</summary>
    private const string TextColor = "#1f2937";

    /// <summary>Color de fondo secundario (gris claro). Usado en tablas, footer y boxes.</summary>
    private const string LightGray = "#f3f4f6";

    /// <summary>Color de bordes y separadores (gris).</summary>
    private const string BorderColor = "#e5e7eb";

    /// <summary>Teléfono de soporte al cliente. Debe configurarse con el número real antes de producción.</summary>
    private const string SupportPhone = "";

    /// <summary>
    /// Construye la estructura HTML completa de un correo electrónico.
    /// </summary>
    /// <remarks>
    /// <para>Incluye header con el nombre de la marca, el título del email, el contenido
    /// específico y un footer con copyright y teléfono de soporte.</para>
    /// <para>Diseño responsive con max-width 600px, bordes redondeados (12px) y sombra suave.</para>
    /// </remarks>
    /// <param name="title">Título del email (se muestra en el <c>&lt;title&gt;</c> y como encabezado <c>&lt;h2&gt;</c>).</param>
    /// <param name="content">HTML del contenido específico del template (insertado en el cuerpo).</param>
    /// <returns>String HTML completo del email.</returns>
    public static string CreateBase(string title, string content)
    {
        return $@"<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{title}</title>
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet'>
</head>
<body style='font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; background-color: #f9fafb; margin: 0; padding: 40px 20px;'>
    <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.1);'>
        <div style='padding: 40px 32px; border-bottom: 1px solid {BorderColor};'>
            <h1 style='margin: 0; color: {TextColor}; font-size: 24px; font-weight: 600;'>Encordados</h1>
        </div>
        <div style='padding: 32px;'>
            <h2 style='color: {TextColor}; margin: 0 0 24px 0; font-size: 20px; font-weight: 600;'>{title}</h2>
            <div style='color: #4b5563; line-height: 1.6; font-size: 15px;'>
                {content}
            </div>
        </div>
        <div style='padding: 24px 32px; background-color: {LightGray}; border-top: 1px solid {BorderColor};'>
            <p style='margin: 0; color: #6b7280; font-size: 13px;'>© 2026 Encordados. Todos los derechos reservados.</p>
            <p style='margin: 8px 0 0 0; color: #6b7280; font-size: 13px;'>
                ¿Necesitas ayuda? Llámanos al <a href='tel:{SupportPhone}' style='color: {PrimaryColor}; text-decoration: none;'>{SupportPhone}</a>
            </p>
        </div>
    </div>
</body>
</html>";
    }

    /// <summary>
    /// Construye la estructura HTML completa de un email con un botón de llamada a la acción.
    /// </summary>
    /// <remarks>
    /// <para>Agrega un botón estilizado (<c>background-color: PrimaryColor</c>) debajo del contenido
    /// y delega el armado HTML a <see cref="CreateBase"/>.</para>
    /// </remarks>
    /// <param name="title">Título del email.</param>
    /// <param name="content">HTML del contenido previo al botón.</param>
    /// <param name="buttonUrl">URL de destino del botón.</param>
    /// <param name="buttonText">Texto del botón.</param>
    /// <returns>String HTML completo del email con botón CTA.</returns>
    public static string CreateBaseWithButton(string title, string content, string buttonUrl, string buttonText)
    {
        string contentWithButton = content + $@"
        <div style='margin-top: 28px;'>
            <a href='{buttonUrl}' style='display: inline-block; background-color: {PrimaryColor}; color: #ffffff; padding: 14px 28px; border-radius: 8px; text-decoration: none; font-weight: 500; font-size: 15px;'> {buttonText}</a>
        </div>";

        return CreateBase(title, contentWithButton);
    }

    /// <summary>Plantilla de bienvenida: notifica que la cuenta fue creada y muestra usuario/email.</summary>
    /// <param name="userName">Nombre de usuario registrado.</param>
    /// <param name="email">Correo electrónico registrado.</param>
    /// <returns>HTML del email de bienvenida.</returns>
    public static string AccountCreated(string userName, string email)
    {
        string content = $@"
        <p style='margin: 0 0 20px 0;'>Hola <strong>{userName}</strong>,</p>
        <p style='margin: 0 0 20px 0;'>Tu cuenta ha sido creada exitosamente. Ya puedes iniciar sesión en nuestra plataforma.</p>
        <div style='background-color: {LightGray}; border-radius: 8px; padding: 20px; margin: 24px 0;'>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>Usuario:</td>
                    <td style='padding: 8px 0; color: {TextColor}; font-size: 14px; font-weight: 500; text-align: right;'>{userName}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>Email:</td>
                    <td style='padding: 8px 0; color: {TextColor}; font-size: 14px; font-weight: 500; text-align: right;'>{email}</td>
                </tr>
            </table>
        </div>
        <p style='margin: 0; color: #6b7280; font-size: 14px;'>Si no solicitaste esta cuenta, por favor contacta con nuestro soporte.</p>
        <div style='background-color: #fef3c7; border-radius: 8px; padding: 16px; border-left: 4px solid #f59e0b;'>
            <p style='margin: 0; font-size: 14px; color: #92400e;'>📞 Para más información, llámanos al <strong>{SupportPhone}</strong></p>
         </div>";

        return CreateBase("Cuenta creada", content);
    }

    /// <summary>Plantilla de restablecimiento de contraseña con botón de enlace y tiempo de expiración.</summary>
    /// <param name="resetUrl">URL de restablecimiento (con token incluido).</param>
    /// <param name="expiryHours">Horas hasta que expire el enlace (default: 1).</param>
    /// <returns>HTML del email de restablecimiento con botón CTA.</returns>
    public static string PasswordReset(string resetUrl, int expiryHours = 1)
    {
        string content = $@"
        <p style='margin: 0 0 20px 0;'>Parece que solicitaste un cambio de contraseña para tu cuenta.</p>
        <p style='margin: 0 0 24px 0;'>Haz clic en el botón de abajo para establecer una nueva contraseña. Este enlace expirará en <strong>{expiryHours} hora{(expiryHours != 1 ? "s" : "")}</strong>.</p>
        <div style='background-color: {LightGray}; border-radius: 8px; padding: 16px; margin: 24px 0;'>
            <p style='margin: 0; color: #6b7280; font-size: 13px;'>Si no solicitaste este cambio, puedes ignorar este email. Tu contraseña no cambiará hasta que crees una nueva.</p>
        </div>";

        return CreateBaseWithButton("Restablecer contraseña", content, resetUrl, "Restablecer contraseña");
    }

    /// <summary>Plantilla de cancelación de pedido con número de pedido destacado y box de advertencia.</summary>
    /// <param name="orderId">Identificador del pedido cancelado.</param>
    /// <returns>HTML del email de cancelación.</returns>
    public static string OrderCancelled(string orderId)
    {
        string content = $@"
        <p style='margin: 0 0 20px 0;'>Lamentamos informarte que tu pedido ha sido cancelado.</p>
        <div style='background-color: {LightGray}; border-radius: 8px; padding: 20px; margin: 24px 0;'>
            <p style='margin: 0 0 12px 0; font-size: 14px; color: #6b7280;'>Número de pedido:</p>
            <p style='margin: 0; font-size: 20px; font-weight: 600; color: {TextColor};'>#{orderId}</p>
        </div>
        <p style='margin: 0 0 20px 0;'>Si crees que esto es un error o tienes alguna consulta, no dudes en contactarnos.</p>
        <div style='background-color: #fef3c7; border-radius: 8px; padding: 16px; border-left: 4px solid #f59e0b;'>
            <p style='margin: 0; font-size: 14px; color: #92400e;'>📞 Para más información, llámanos al <strong>{SupportPhone}</strong></p>
        </div>";

        return CreateBase("Pedido cancelado", content);
    }

    

    /// <summary>Plantilla de confirmación de pago con importe destacado en verde, número de pedido y nota de seguimiento.</summary>
    /// <param name="orderId">Identificador del pedido pagado.</param>
    /// <param name="amount">Importe pagado en euros.</param>
    /// <returns>HTML del email de confirmación de pago.</returns>
    public static string PaymentConfirmed(string orderId, double amount)
    {
        string content = $@"
        <p style='margin: 0 0 20px 0;'>Hemos recibido tu pago correctamente.</p>
        <div style='background-color: #ecfdf5; border-radius: 8px; padding: 24px; margin: 24px 0; text-align: center;'>
            <p style='margin: 0 0 8px 0; font-size: 14px; color: #059669;'>Importe pagado</p>
            <p style='margin: 0; font-size: 32px; font-weight: 700; color: #059669;'>{amount:N2} €</p>
        </div>
        <div style='background-color: {LightGray}; border-radius: 8px; padding: 20px; margin: 24px 0;'>
            <p style='margin: 0 0 4px 0; font-size: 14px; color: #6b7280;'>Número de pedido</p>
            <p style='margin: 0; font-size: 18px; font-weight: 600; color: {TextColor};'>#{orderId}</p>
        </div>
        <p style='margin: 0; color: #6b7280; font-size: 14px;'>Recibirás otro email cuando tu pedido esté listo para ser recogido.</p>";

        return CreateBase("Pago confirmado", content);
    }

    

  

    /// <summary>Plantilla de notificación de línea de encordado completada, lista para recoger.</summary>
    /// <param name="lineaId">Identificador de la línea completada.</param>
    /// <param name="pedidoId">Identificador del pedido al que pertenece.</param>
    /// <param name="model">Modelo de la raqueta encordada.</param>
    /// <returns>HTML del email de línea completada.</returns>
    public static string LineaCompleted(string lineaId, string pedidoId,string model)
    {
        string content = $@"
        <p style='margin: 0 0 20px 0;'>¡Buenas noticias! Tu raqueta: {model} ha sido completada.</p>
        <div style='background-color: #ecfdf5; border-radius: 8px; padding: 20px; margin: 24px 0;'>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>Número de pedido:</td>
                    <td style='padding: 8px 0; color: {TextColor}; font-size: 14px; font-weight: 500; text-align: right;'>#{pedidoId}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>ID de línea:</td>
                    <td style='padding: 8px 0; color: {TextColor}; font-size: 14px; font-weight: 500; text-align: right;'>#{lineaId}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>Estado:</td>
                    <td style='padding: 8px 0; color: #059669; font-size: 14px; font-weight: 600; text-align: right;'>✓ Completada</td>
                </tr>
            </table>
        </div>
        <p style='margin: 0; color: #6b7280; font-size: 14px;'>Tu línea está lista para ser recogida. Si tienes alguna pregunta, no dudes en contactarnos.</p>";

        return CreateBase("Línea completada", content);
    }

    /// <summary>Plantilla de notificación de línea de encordado entregada al cliente.</summary>
    /// <param name="lineaId">Identificador de la línea entregada.</param>
    /// <param name="pedidoId">Identificador del pedido al que pertenece.</param>
    /// <param name="model">Modelo de la raqueta entregada.</param>
    /// <returns>HTML del email de línea entregada.</returns>
    public static string LineaDelivered(string lineaId, string pedidoId, string model)
    {
        string content = $@"
        <p style='margin: 0 0 20px 0;'>¡Tu raqueta: {model} ha sido entregada!</p>
        <div style='background-color: #ecfdf5; border-radius: 8px; padding: 20px; margin: 24px 0;'>
            <table style='width: 100%; border-collapse: collapse;'>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>Número de pedido:</td>
                    <td style='padding: 8px 0; color: {TextColor}; font-size: 14px; font-weight: 500; text-align: right;'>#{pedidoId}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>ID de línea:</td>
                    <td style='padding: 8px 0; color: {TextColor}; font-size: 14px; font-weight: 500; text-align: right;'>#{lineaId}</td>
                </tr>
                <tr>
                    <td style='padding: 8px 0; color: #6b7280; font-size: 14px;'>Estado:</td>
                    <td style='padding: 8px 0; color: #059669; font-size: 14px; font-weight: 600; text-align: right;'>✓ Entregada</td>
                </tr>
            </table>
        </div>
        <p style='margin: 0; color: #6b7280; font-size: 14px;'>Gracias por usar nuestros servicios. Si tienes alguna pregunta, no dudes en contactarnos.</p>";

        return CreateBase("Línea entregada", content);
    }
}