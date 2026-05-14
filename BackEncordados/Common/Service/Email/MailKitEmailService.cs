using System.Threading.Channels;
using MailKit.Net.Smtp;
using MimeKit;

namespace BackEncordados.Common.Service.Email;


public class MailKitEmailService(
    IConfiguration configuration,
    ILogger<MailKitEmailService> logger,
    Channel<EmailMessage> emailChannel
) : IEmailService {


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
            await client.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
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