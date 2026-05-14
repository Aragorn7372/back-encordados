namespace BackEncordados.Common.Service.Email;

public record OrderItemEmailDto(string ProductName, int Quantity, decimal Price);

public static class EmailTemplates
{
    private const string PrimaryColor = "#2563eb";
    private const string TextColor = "#1f2937";
    private const string LightGray = "#f3f4f6";
    private const string BorderColor = "#e5e7eb";
    private const string SupportPhone = "";

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

    public static string CreateBaseWithButton(string title, string content, string buttonUrl, string buttonText)
    {
        string contentWithButton = content + $@"
        <div style='margin-top: 28px;'>
            <a href='{buttonUrl}' style='display: inline-block; background-color: {PrimaryColor}; color: #ffffff; padding: 14px 28px; border-radius: 8px; text-decoration: none; font-weight: 500; font-size: 15px;'> {buttonText}</a>
        </div>";

        return CreateBase(title, contentWithButton);
    }

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